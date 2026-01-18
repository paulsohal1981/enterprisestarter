using MediatR;
using Microsoft.EntityFrameworkCore;
using OrgManagement.Application.Common.Exceptions;
using OrgManagement.Domain.Entities;
using OrgManagement.Domain.Enums;
using OrgManagement.Application.Common.Interfaces;

namespace OrgManagement.Application.Features.Roles.Queries;

public record GetRoleByIdQuery(Guid Id) : IRequest<RoleDetailDto>;

public record RoleDetailDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystemRole,
    bool IsActive,
    DateTime CreatedAt,
    IEnumerable<RolePermissionDto> Permissions);

public record RolePermissionDto(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    PermissionCategory Category);

public class GetRoleByIdQueryHandler : IRequestHandler<GetRoleByIdQuery, RoleDetailDto>
{
    private readonly IApplicationDbContext _context;

    public GetRoleByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RoleDetailDto> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
    {
        var role = await _context.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (role == null)
        {
            throw new NotFoundException(nameof(Role), request.Id);
        }

        return new RoleDetailDto(
            Id: role.Id,
            Name: role.Name,
            Description: role.Description,
            IsSystemRole: role.IsSystemRole,
            IsActive: role.IsActive,
            CreatedAt: role.CreatedAt,
            Permissions: role.RolePermissions.Select(rp => new RolePermissionDto(
                rp.Permission.Id,
                rp.Permission.Name,
                rp.Permission.Code,
                rp.Permission.Description,
                rp.Permission.Category)));
    }
}
