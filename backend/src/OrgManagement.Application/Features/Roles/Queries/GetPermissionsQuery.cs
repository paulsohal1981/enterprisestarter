using MediatR;
using Microsoft.EntityFrameworkCore;
using OrgManagement.Domain.Enums;
using OrgManagement.Application.Common.Interfaces;

namespace OrgManagement.Application.Features.Roles.Queries;

public record GetPermissionsQuery() : IRequest<IEnumerable<PermissionGroupDto>>;

public record PermissionGroupDto(
    PermissionCategory Category,
    IEnumerable<PermissionItemDto> Permissions);

public record PermissionItemDto(
    Guid Id,
    string Name,
    string Code,
    string? Description);

public class GetPermissionsQueryHandler : IRequestHandler<GetPermissionsQuery, IEnumerable<PermissionGroupDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPermissionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PermissionGroupDto>> Handle(GetPermissionsQuery request, CancellationToken cancellationToken)
    {
        var permissions = await _context.Permissions
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return permissions
            .GroupBy(p => p.Category)
            .Select(g => new PermissionGroupDto(
                g.Key,
                g.Select(p => new PermissionItemDto(p.Id, p.Name, p.Code, p.Description))));
    }
}
