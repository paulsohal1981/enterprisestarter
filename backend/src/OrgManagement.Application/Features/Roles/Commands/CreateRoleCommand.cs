using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrgManagement.Application.Common.Interfaces;
using OrgManagement.Application.Common.Models;
using OrgManagement.Domain.Entities;
using OrgManagement.Domain.Enums;
using OrgManagement.Application.Common.Interfaces;

namespace OrgManagement.Application.Features.Roles.Commands;

public record CreateRoleCommand(
    string Name,
    string? Description,
    IEnumerable<Guid> PermissionIds) : IRequest<Result<Guid>>;

public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public CreateRoleCommandHandler(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<Result<Guid>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var nameExists = await _context.Roles
            .AnyAsync(r => r.Name.ToLower() == request.Name.ToLower(), cancellationToken);

        if (nameExists)
        {
            return Result.Failure<Guid>("A role with this name already exists.");
        }

        var role = Role.Create(request.Name, request.Description, isSystemRole: false);

        // Assign permissions
        foreach (var permissionId in request.PermissionIds.Distinct())
        {
            var permission = await _context.Permissions.FindAsync(new object[] { permissionId }, cancellationToken);
            if (permission != null)
            {
                role.AddPermission(permission);
            }
        }

        await _context.Roles.AddAsync(role, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            nameof(Role),
            role.Id,
            AuditAction.Create,
            newValues: new { request.Name, request.Description, PermissionCount = request.PermissionIds.Count() },
            cancellationToken: cancellationToken);

        return Result.Success(role.Id);
    }
}

public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Role name is required.")
            .MaximumLength(100).WithMessage("Role name must not exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
    }
}
