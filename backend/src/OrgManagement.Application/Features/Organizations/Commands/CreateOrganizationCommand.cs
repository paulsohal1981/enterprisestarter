using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrgManagement.Application.Common.Interfaces;
using OrgManagement.Application.Common.Models;
using OrgManagement.Domain.Entities;
using OrgManagement.Domain.Enums;
using OrgManagement.Application.Common.Interfaces;

namespace OrgManagement.Application.Features.Organizations.Commands;

public record CreateOrganizationCommand(
    string Name,
    string? Description,
    string? Code) : IRequest<Result<Guid>>;

public class CreateOrganizationCommandHandler : IRequestHandler<CreateOrganizationCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public CreateOrganizationCommandHandler(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<Result<Guid>> Handle(CreateOrganizationCommand request, CancellationToken cancellationToken)
    {
        // Check for duplicate code
        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var exists = await _context.Organizations
                .AnyAsync(o => o.Code == request.Code, cancellationToken);
            if (exists)
            {
                return Result.Failure<Guid>("An organization with this code already exists.");
            }
        }

        var organization = Organization.Create(request.Name, request.Description, request.Code);

        await _context.Organizations.AddAsync(organization, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            nameof(Organization),
            organization.Id,
            AuditAction.Create,
            newValues: new { request.Name, request.Description, request.Code },
            cancellationToken: cancellationToken);

        return Result.Success(organization.Id);
    }
}

public class CreateOrganizationCommandValidator : AbstractValidator<CreateOrganizationCommand>
{
    public CreateOrganizationCommandValidator()
    {
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
