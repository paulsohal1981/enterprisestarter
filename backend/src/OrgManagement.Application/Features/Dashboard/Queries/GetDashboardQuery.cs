using MediatR;
using Microsoft.EntityFrameworkCore;
using OrgManagement.Domain.Enums;
using OrgManagement.Application.Common.Interfaces;

namespace OrgManagement.Application.Features.Dashboard.Queries;

public record GetDashboardQuery() : IRequest<DashboardDto>;

public record DashboardDto(
    int TotalOrganizations,
    int ActiveOrganizations,
    int TotalSubOrganizations,
    int TotalUsers,
    int ActiveUsers,
    int InactiveUsers,
    int LockedUsers,
    int TotalRoles,
    int SystemRoles,
    int CustomRoles,
    IEnumerable<RecentActivityDto> RecentActivity,
    IEnumerable<UsersByOrganizationDto> UsersByOrganization);

public record RecentActivityDto(
    Guid Id,
    string EntityType,
    AuditAction Action,
    string? UserEmail,
    DateTime CreatedAt);

public record UsersByOrganizationDto(
    Guid OrganizationId,
    string OrganizationName,
    int UserCount);

public class GetDashboardQueryHandler : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    private readonly IApplicationDbContext _context;

    public GetDashboardQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        // Organization stats
        var totalOrgs = await _context.Organizations.CountAsync(cancellationToken);
        var activeOrgs = await _context.Organizations.CountAsync(o => o.Status == OrganizationStatus.Active, cancellationToken);
        var totalSubOrgs = await _context.SubOrganizations.CountAsync(cancellationToken);

        // User stats
        var totalUsers = await _context.Users.CountAsync(cancellationToken);
        var activeUsers = await _context.Users.CountAsync(u => u.Status == UserStatus.Active, cancellationToken);
        var inactiveUsers = await _context.Users.CountAsync(u => u.Status == UserStatus.Inactive, cancellationToken);
        var lockedUsers = await _context.Users.CountAsync(u => u.Status == UserStatus.Locked, cancellationToken);

        // Role stats
        var totalRoles = await _context.Roles.CountAsync(cancellationToken);
        var systemRoles = await _context.Roles.CountAsync(r => r.IsSystemRole, cancellationToken);
        var customRoles = totalRoles - systemRoles;

        // Recent activity
        var recentActivity = await _context.AuditLogs
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .Select(a => new RecentActivityDto(
                a.Id,
                a.EntityType,
                a.Action,
                a.UserEmail,
                a.CreatedAt))
            .ToListAsync(cancellationToken);

        // Users by organization (top 5)
        var usersByOrg = await _context.Organizations
            .Select(o => new UsersByOrganizationDto(
                o.Id,
                o.Name,
                o.Users.Count(u => !u.IsDeleted)))
            .OrderByDescending(x => x.UserCount)
            .Take(5)
            .ToListAsync(cancellationToken);

        return new DashboardDto(
            TotalOrganizations: totalOrgs,
            ActiveOrganizations: activeOrgs,
            TotalSubOrganizations: totalSubOrgs,
            TotalUsers: totalUsers,
            ActiveUsers: activeUsers,
            InactiveUsers: inactiveUsers,
            LockedUsers: lockedUsers,
            TotalRoles: totalRoles,
            SystemRoles: systemRoles,
            CustomRoles: customRoles,
            RecentActivity: recentActivity,
            UsersByOrganization: usersByOrg);
    }
}
