using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrgManagement.Application.Common.Exceptions;
using OrgManagement.Application.Common.Interfaces;
using OrgManagement.Application.Common.Models;
using OrgManagement.Domain.Entities;
using OrgManagement.Domain.Enums;
using OrgManagement.Application.Common.Interfaces;

namespace OrgManagement.Application.Features.Organizations.Commands;

public record UpdateOrganizationCommand(
    Guid Id,
    string Name,
    string? Description,
    string? Code) : IRequest<Result>;

public class UpdateOrganizationCommandHandler : IRequestHandler<UpdateOrganizationCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public UpdateOrganizationCommandHandler(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<Result> Handle(UpdateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        if (organization == null)
        {
            throw new NotFoundException(nameof(Organization), request.Id);
        }

        // Check for duplicate code
        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var codeExists = await _context.Organizations
                .AnyAsync(o => o.Code == request.Code && o.Id != request.Id, cancellationToken);
            if (codeExists)
            {
                return Result.Failure("An organization with this code already exists.");
            }
        }

        var oldValues = new { organization.Name, organization.Description, organization.Code };

        organization.Update(request.Name, request.Description, request.Code);

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            nameof(Organization),
            organization.Id,
            AuditAction.Update,
            oldValues: oldValues,
            newValues: new { request.Name, request.Description, request.Code },
            cancellationToken: cancellationToken);

        return Result.Success();
    }
}

public class UpdateOrganizationCommandValidator : AbstractValidator<UpdateOrganizationCommand>
{
    public UpdateOrganizationCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Organization ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");

        RuleFor(x => x.Code)
            .MaximumLength(50).WithMessage("Code must not exceed 50 characters.")
            .Matches("^[a-zA-Z0-9-_]*$").WithMessage("Code can only contain letters, numbers, hyphens, and underscores.")
            .When(x => !string.IsNullOrWhiteSpace(x.Code));
    }
}
