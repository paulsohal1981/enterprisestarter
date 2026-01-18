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

public record DeleteOrganizationCommand(Guid Id) : IRequest<Result>;

public class DeleteOrganizationCommandHandler : IRequestHandler<DeleteOrganizationCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public DeleteOrganizationCommandHandler(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<Result> Handle(DeleteOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        if (organization == null)
        {
            throw new NotFoundException(nameof(Organization), request.Id);
        }

        // Soft delete will be handled by SaveChangesAsync interceptor
        _context.Organizations.Remove(organization);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            nameof(Organization),
            organization.Id,
            AuditAction.Delete,
            oldValues: new { organization.Name, organization.Code },
            cancellationToken: cancellationToken);

        return Result.Success();
    }
}

public class DeleteOrganizationCommandValidator : AbstractValidator<DeleteOrganizationCommand>
{
    public DeleteOrganizationCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Organization ID is required.");
    }
}
