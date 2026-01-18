using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrgManagement.Application.Common.Exceptions;
using OrgManagement.Application.Common.Interfaces;
using OrgManagement.Application.Common.Models;
using OrgManagement.Domain.Entities;
using OrgManagement.Domain.Enums;
using OrgManagement.Application.Common.Interfaces;

namespace OrgManagement.Application.Features.Roles.Commands;

public record UpdateRoleCommand(
    Guid Id,
    string Name,
    string? Description,
    IEnumerable<Guid> PermissionIds) : IRequest<Result>;

public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public UpdateRoleCommandHandler(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<Result> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _context.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (role == null)
        {
            throw new NotFoundException(nameof(Role), request.Id);
        }

        if (role.IsSystemRole)
        {
            return Result.Failure("System roles cannot be modified.");
        }

        // Check for duplicate name
        var nameExists = await _context.Roles
            .AnyAsync(r => r.Name.ToLower() == request.Name.ToLower() && r.Id != request.Id, cancellationToken);

        if (nameExists)
        {
            return Result.Failure("A role with this name already exists.");
        }

        var oldValues = new { role.Name, role.Description, Permissions = role.RolePermissions.Select(rp => rp.PermissionId) };

        role.Update(request.Name, request.Description);

        // Update permissions
        role.ClearPermissions();
        foreach (var permissionId in request.PermissionIds.Distinct())
        {
            var permission = await _context.Permissions.FindAsync(new object[] { permissionId }, cancellationToken);
            if (permission != null)
            {
                role.AddPermission(permission);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            nameof(Role),
            role.Id,
            AuditAction.Update,
            oldValues: oldValues,
            newValues: new { request.Name, request.Description, Permissions = request.PermissionIds },
            cancellationToken: cancellationToken);

        return Result.Success();
    }
}

public class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Role ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Role name is required.")
            .MaximumLength(100).WithMessage("Role name must not exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
    }
}
