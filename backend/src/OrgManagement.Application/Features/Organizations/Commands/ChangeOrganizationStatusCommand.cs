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

public record ChangeOrganizationStatusCommand(Guid Id, OrganizationStatus Status) : IRequest<Result>;

public class ChangeOrganizationStatusCommandHandler : IRequestHandler<ChangeOrganizationStatusCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public ChangeOrganizationStatusCommandHandler(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<Result> Handle(ChangeOrganizationStatusCommand request, CancellationToken cancellationToken)
    {
        var organization = await _context.Organizations
            .Include(o => o.SubOrganizations)
                .ThenInclude(s => s.Users)
            .Include(o => o.Users)
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        if (organization == null)
        {
            throw new NotFoundException(nameof(Organization), request.Id);
        }

        var oldStatus = organization.Status;

        switch (request.Status)
        {
            case OrganizationStatus.Active:
                organization.Activate();
                break;
            case OrganizationStatus.Inactive:
                organization.Deactivate();
                break;
            case OrganizationStatus.Suspended:
                organization.Suspend();
                break;
        }

        await _context.SaveChangesAsync(cancellationToken);

        var action = request.Status == OrganizationStatus.Active
            ? AuditAction.Activate
            : AuditAction.Deactivate;

        await _auditService.LogAsync(
            nameof(Organization),
            organization.Id,
            action,
            oldValues: new { Status = oldStatus },
            newValues: new { Status = request.Status },
            cancellationToken: cancellationToken);

        return Result.Success();
    }
}

public class ChangeOrganizationStatusCommandValidator : AbstractValidator<ChangeOrganizationStatusCommand>
{
    public ChangeOrganizationStatusCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Organization ID is required.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid status value.");
    }
}
