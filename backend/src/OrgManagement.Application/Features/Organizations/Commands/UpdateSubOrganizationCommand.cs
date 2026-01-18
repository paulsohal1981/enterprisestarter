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

public record UpdateSubOrganizationCommand(
    Guid Id,
    string Name,
    string? Description,
    string? Code) : IRequest<Result>;

public class UpdateSubOrganizationCommandHandler : IRequestHandler<UpdateSubOrganizationCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public UpdateSubOrganizationCommandHandler(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<Result> Handle(UpdateSubOrganizationCommand request, CancellationToken cancellationToken)
    {
        var subOrg = await _context.SubOrganizations
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (subOrg == null)
        {
            throw new NotFoundException(nameof(SubOrganization), request.Id);
        }

        var oldValues = new { subOrg.Name, subOrg.Description, subOrg.Code };

        subOrg.Update(request.Name, request.Description, request.Code);

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            nameof(SubOrganization),
            subOrg.Id,
            AuditAction.Update,
            oldValues: oldValues,
            newValues: new { request.Name, request.Description, request.Code },
            cancellationToken: cancellationToken);

        return Result.Success();
    }
}

public class UpdateSubOrganizationCommandValidator : AbstractValidator<UpdateSubOrganizationCommand>
{
    public UpdateSubOrganizationCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Sub-organization ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");

        RuleFor(x => x.Code)
            .MaximumLength(50).WithMessage("Code must not exceed 50 characters.");
    }
}
