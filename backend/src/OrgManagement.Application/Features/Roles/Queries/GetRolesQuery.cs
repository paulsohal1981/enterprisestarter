using MediatR;
using Microsoft.EntityFrameworkCore;
using OrgManagement.Application.Common.Models;
using OrgManagement.Application.Common.Interfaces;

namespace OrgManagement.Application.Features.Roles.Queries;

public record GetRolesQuery(
    string? SearchTerm = null,
    bool? IsSystemRole = null,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<PaginatedList<RoleDto>>;

public record RoleDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystemRole,
    bool IsActive,
    int PermissionCount,
    int UserCount);

public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, PaginatedList<RoleDto>>
{
    private readonly IApplicationDbContext _context;

    public GetRolesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Roles
            .Include(r => r.RolePermissions)
            .Include(r => r.UserRoles)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(r => r.Name.ToLower().Contains(searchTerm));
        }

        if (request.IsSystemRole.HasValue)
        {
            query = query.Where(r => r.IsSystemRole == request.IsSystemRole.Value);
        }

        var projectedQuery = query
            .OrderByDescending(r => r.IsSystemRole)
            .ThenBy(r => r.Name)
            .Select(r => new RoleDto(
                r.Id,
                r.Name,
                r.Description,
                r.IsSystemRole,
                r.IsActive,
                r.RolePermissions.Count,
                r.UserRoles.Count));

        return await PaginatedList<RoleDto>.CreateAsync(projectedQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}
