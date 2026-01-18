using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using OrgManagement.Application.Common.Interfaces;

namespace OrgManagement.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? Email => User?.FindFirstValue(ClaimTypes.Email);

    public Guid? OrganizationId
    {
        get
        {
            var orgIdClaim = User?.FindFirstValue("organization_id");
            return Guid.TryParse(orgIdClaim, out var orgId) ? orgId : null;
        }
    }

    public bool IsSuperAdmin => Roles.Contains("Super Admin");

    public IEnumerable<string> Permissions =>
        User?.FindAll("permission").Select(c => c.Value) ?? Enumerable.Empty<string>();

    public IEnumerable<string> Roles =>
        User?.FindAll(ClaimTypes.Role).Select(c => c.Value) ?? Enumerable.Empty<string>();
}
