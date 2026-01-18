namespace OrgManagement.Infrastructure.Authorization;

public static class Permissions
{
    public static class Organizations
    {
        public const string View = "organizations.view";
        public const string Create = "organizations.create";
        public const string Update = "organizations.update";
        public const string Delete = "organizations.delete";
        public const string Manage = "organizations.manage";
    }

    public static class SubOrganizations
    {
        public const string View = "suborgs.view";
        public const string Create = "suborgs.create";
        public const string Update = "suborgs.update";
        public const string Delete = "suborgs.delete";
        public const string Manage = "suborgs.manage";
    }

    public static class Users
    {
        public const string View = "users.view";
        public const string Create = "users.create";
        public const string Update = "users.update";
        public const string Delete = "users.delete";
        public const string Manage = "users.manage";
        public const string AssignRoles = "users.assignroles";
    }

    public static class Roles
    {
        public const string View = "roles.view";
        public const string Create = "roles.create";
        public const string Update = "roles.update";
        public const string Delete = "roles.delete";
        public const string Manage = "roles.manage";
    }

    public static class AuditLogs
    {
        public const string View = "auditlogs.view";
    }

    public static class Dashboard
    {
        public const string View = "dashboard.view";
    }

    public static class Settings
    {
        public const string View = "settings.view";
        public const string Manage = "settings.manage";
    }

    public static IEnumerable<string> GetAllPermissions()
    {
        yield return Organizations.View;
        yield return Organizations.Create;
        yield return Organizations.Update;
        yield return Organizations.Delete;
        yield return Organizations.Manage;
        yield return SubOrganizations.View;
        yield return SubOrganizations.Create;
        yield return SubOrganizations.Update;
        yield return SubOrganizations.Delete;
        yield return SubOrganizations.Manage;
        yield return Users.View;
        yield return Users.Create;
        yield return Users.Update;
        yield return Users.Delete;
        yield return Users.Manage;
        yield return Users.AssignRoles;
        yield return Roles.View;
        yield return Roles.Create;
        yield return Roles.Update;
        yield return Roles.Delete;
        yield return Roles.Manage;
        yield return AuditLogs.View;
        yield return Dashboard.View;
        yield return Settings.View;
        yield return Settings.Manage;
    }
}
