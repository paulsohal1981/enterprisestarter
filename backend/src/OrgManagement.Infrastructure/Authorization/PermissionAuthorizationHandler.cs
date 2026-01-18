using Microsoft.AspNetCore.Authorization;
using OrgManagement.Application.Common.Interfaces;

namespace OrgManagement.Infrastructure.Authorization;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ICurrentUserService _currentUserService;

    public PermissionAuthorizationHandler(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // Super Admin has all permissions
        if (_currentUserService.IsSuperAdmin)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Check if user has the required permission
        if (_currentUserService.Permissions.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
