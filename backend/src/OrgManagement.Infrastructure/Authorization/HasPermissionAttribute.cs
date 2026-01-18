using Microsoft.AspNetCore.Authorization;

namespace OrgManagement.Infrastructure.Authorization;

public class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission)
        : base($"Permission:{permission}")
    {
    }
}
