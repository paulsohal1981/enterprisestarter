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

public record CreateSubOrganizationCommand(
    string Name,
    string? Description,
    string? Code,
    Guid OrganizationId,
    Guid? ParentSubOrganizationId) : IRequest<Result<Guid>>;

public class CreateSubOrganizationCommandHandler : IRequestHandler<CreateSubOrganizationCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public CreateSubOrganizationCommandHandler(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<Result<Guid>> Handle(CreateSubOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == request.OrganizationId, cancellationToken);

        if (organization == null)
        {
            throw new NotFoundException(nameof(Organization), request.OrganizationId);
        }

        SubOrganization? parent = null;
        if (request.ParentSubOrganizationId.HasValue)
        {
            parent = await _context.SubOrganizations
                .FirstOrDefaultAsync(s => s.Id == request.ParentSubOrganizationId.Value, cancellationToken);

            if (parent == null)
            {
                throw new NotFoundException(nameof(SubOrganization), request.ParentSubOrganizationId.Value);
            }

            if (parent.OrganizationId != request.OrganizationId)
            {
                return Result.Failure<Guid>("Parent sub-organization belongs to a different organization.");
            }

            if (parent.Level >= SubOrganization.MaxLevel)
            {
                return Result.Failure<Guid>($"Cannot create sub-organization beyond level {SubOrganization.MaxLevel}.");
            }
        }

        var subOrg = SubOrganization.Create(
            request.Name,
            request.OrganizationId,
            parent,
            request.Description,
            request.Code);

        await _context.SubOrganizations.AddAsync(subOrg, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            nameof(SubOrganization),
            subOrg.Id,
            AuditAction.Create,
            newValues: new { request.Name, request.Description, request.Code, Level = subOrg.Level },
            cancellationToken: cancellationToken);

        return Result.Success(subOrg.Id);
    }
}

public class CreateSubOrganizationCommandValidator : AbstractValidator<CreateSubOrganizationCommand>
{
    public CreateSubOrganizationCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");

        RuleFor(x => x.Code)
            .MaximumLength(50).WithMessage("Code must not exceed 50 characters.");

        RuleFor(x => x.OrganizationId)
            .NotEmpty().WithMessage("Organization ID is required.");
    }
}
