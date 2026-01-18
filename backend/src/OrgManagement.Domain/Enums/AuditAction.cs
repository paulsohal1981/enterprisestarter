namespace OrgManagement.Domain.Enums;

public enum AuditAction
{
    Create = 1,
    Update = 2,
    Delete = 3,
    Activate = 4,
    Deactivate = 5,
    Login = 6,
    Logout = 7,
    PasswordChange = 8,
    RoleAssignment = 9,
    PermissionChange = 10
}
