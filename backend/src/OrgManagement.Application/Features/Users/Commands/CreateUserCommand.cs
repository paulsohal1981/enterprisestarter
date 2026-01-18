using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrgManagement.Application.Common.Exceptions;
using OrgManagement.Application.Common.Interfaces;
using OrgManagement.Application.Common.Models;
using OrgManagement.Domain.Entities;
using OrgManagement.Domain.Enums;
using OrgManagement.Application.Common.Interfaces;

namespace OrgManagement.Application.Features.Users.Commands;

public record CreateUserCommand(
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    Guid OrganizationId,
    Guid? SubOrganizationId,
    IEnumerable<Guid> RoleIds) : IRequest<Result<Guid>>;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly IEmailService _emailService;
    private readonly IAuditService _auditService;

    public CreateUserCommandHandler(
        IApplicationDbContext context,
        IPasswordService passwordService,
        IEmailService emailService,
        IAuditService auditService)
    {
        _context = context;
        _passwordService = passwordService;
        _emailService = emailService;
        _auditService = auditService;
    }

    public async Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Check for duplicate email within organization
        var emailExists = await _context.Users
            .AnyAsync(u => u.Email == request.Email.ToLowerInvariant() && u.OrganizationId == request.OrganizationId, cancellationToken);

        if (emailExists)
        {
            return Result.Failure<Guid>("A user with this email already exists in this organization.");
        }

        // Validate organization exists
        var organizationExists = await _context.Organizations.AnyAsync(o => o.Id == request.OrganizationId, cancellationToken);
        if (!organizationExists)
        {
            throw new NotFoundException(nameof(Organization), request.OrganizationId);
        }

        // Validate sub-organization if provided
        if (request.SubOrganizationId.HasValue)
        {
            var subOrg = await _context.SubOrganizations
                .FirstOrDefaultAsync(s => s.Id == request.SubOrganizationId.Value, cancellationToken);

            if (subOrg == null)
            {
                throw new NotFoundException(nameof(SubOrganization), request.SubOrganizationId.Value);
            }

            if (subOrg.OrganizationId != request.OrganizationId)
            {
                return Result.Failure<Guid>("Sub-organization does not belong to the specified organization.");
            }
        }

        // Generate temporary password
        var temporaryPassword = _passwordService.GenerateRandomPassword();
        var passwordHash = _passwordService.HashPassword(temporaryPassword);

        var user = User.Create(
            request.Email,
            passwordHash,
            request.FirstName,
            request.LastName,
            request.OrganizationId,
            request.SubOrganizationId,
            request.PhoneNumber,
            mustChangePassword: true);

        // Assign roles
        foreach (var roleId in request.RoleIds.Distinct())
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);
            if (role != null)
            {
                user.AddRole(role);
            }
        }

        await _context.Users.AddAsync(user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Send welcome email with temporary password
        await _emailService.SendWelcomeEmailAsync(user.Email, temporaryPassword, cancellationToken);

        await _auditService.LogAsync(
            nameof(User),
            user.Id,
            AuditAction.Create,
            newValues: new { request.Email, request.FirstName, request.LastName, RoleCount = request.RoleIds.Count() },
            cancellationToken: cancellationToken);

        return Result.Success(user.Id);
    }
}

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email is required.")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20).WithMessage("Phone number must not exceed 20 characters.");

        RuleFor(x => x.OrganizationId)
            .NotEmpty().WithMessage("Organization ID is required.");

        RuleFor(x => x.RoleIds)
            .NotEmpty().WithMessage("At least one role must be assigned.");
    }
}
