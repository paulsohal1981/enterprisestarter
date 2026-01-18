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

public record AssignUserRolesCommand(Guid UserId, IEnumerable<Guid> RoleIds) : IRequest<Result>;

public class AssignUserRolesCommandHandler : IRequestHandler<AssignUserRolesCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public AssignUserRolesCommandHandler(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<Result> Handle(AssignUserRolesCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException(nameof(User), request.UserId);
        }

        var oldRoles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

        // Clear existing roles
        user.ClearRoles();

        // Assign new roles
        foreach (var roleId in request.RoleIds.Distinct())
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);
            if (role != null && role.IsActive)
            {
                user.AddRole(role);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        var newRoles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

        await _auditService.LogAsync(
            nameof(User),
            user.Id,
            AuditAction.RoleAssignment,
            oldValues: new { Roles = oldRoles },
            newValues: new { Roles = newRoles },
            cancellationToken: cancellationToken);

        return Result.Success();
    }
}

public class AssignUserRolesCommandValidator : AbstractValidator<AssignUserRolesCommand>
{
    public AssignUserRolesCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.RoleIds)
            .NotEmpty().WithMessage("At least one role must be assigned.");
    }
}
