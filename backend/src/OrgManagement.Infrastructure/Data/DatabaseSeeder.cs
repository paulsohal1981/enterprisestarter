using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrgManagement.Application.Common.Interfaces;
using OrgManagement.Domain.Entities;
using OrgManagement.Domain.Enums;

namespace OrgManagement.Infrastructure.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            await context.Database.EnsureCreatedAsync();

            // Seed permissions if not exist
            if (!await context.Permissions.AnyAsync())
            {
                await SeedPermissionsAsync(context);
                logger.LogInformation("Permissions seeded successfully");
            }

            // Seed roles if not exist
            if (!await context.Roles.AnyAsync())
            {
                await SeedRolesAsync(context);
                logger.LogInformation("Roles seeded successfully");
            }

            // Seed default organization and super admin if not exist
            if (!await context.Organizations.AnyAsync())
            {
                await SeedDefaultOrganizationAndSuperAdminAsync(context, passwordService);
                logger.LogInformation("Default organization and super admin seeded successfully");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private static async Task SeedPermissionsAsync(ApplicationDbContext context)
    {
        var permissions = new List<Permission>
        {
            // Organizations
            Permission.Create("View Organizations", "organizations.view", PermissionCategory.Organizations, "View organization list and details"),
            Permission.Create("Create Organizations", "organizations.create", PermissionCategory.Organizations, "Create new organizations"),
            Permission.Create("Update Organizations", "organizations.update", PermissionCategory.Organizations, "Update organization information"),
            Permission.Create("Delete Organizations", "organizations.delete", PermissionCategory.Organizations, "Delete organizations"),
            Permission.Create("Manage Organizations", "organizations.manage", PermissionCategory.Organizations, "Full organization management including status changes"),

            // Sub-Organizations
            Permission.Create("View Sub-Organizations", "suborgs.view", PermissionCategory.Organizations, "View sub-organization list and details"),
            Permission.Create("Create Sub-Organizations", "suborgs.create", PermissionCategory.Organizations, "Create new sub-organizations"),
            Permission.Create("Update Sub-Organizations", "suborgs.update", PermissionCategory.Organizations, "Update sub-organization information"),
            Permission.Create("Delete Sub-Organizations", "suborgs.delete", PermissionCategory.Organizations, "Delete sub-organizations"),
            Permission.Create("Manage Sub-Organizations", "suborgs.manage", PermissionCategory.Organizations, "Full sub-organization management"),

            // Users
            Permission.Create("View Users", "users.view", PermissionCategory.Users, "View user list and details"),
            Permission.Create("Create Users", "users.create", PermissionCategory.Users, "Create new users"),
            Permission.Create("Update Users", "users.update", PermissionCategory.Users, "Update user information"),
            Permission.Create("Delete Users", "users.delete", PermissionCategory.Users, "Delete users"),
            Permission.Create("Manage Users", "users.manage", PermissionCategory.Users, "Full user management including status changes"),
            Permission.Create("Assign Roles", "users.assignroles", PermissionCategory.Users, "Assign roles to users"),

            // Roles
            Permission.Create("View Roles", "roles.view", PermissionCategory.Roles, "View role list and details"),
            Permission.Create("Create Roles", "roles.create", PermissionCategory.Roles, "Create new roles"),
            Permission.Create("Update Roles", "roles.update", PermissionCategory.Roles, "Update role information"),
            Permission.Create("Delete Roles", "roles.delete", PermissionCategory.Roles, "Delete roles"),
            Permission.Create("Manage Roles", "roles.manage", PermissionCategory.Roles, "Full role management"),

            // Audit Logs
            Permission.Create("View Audit Logs", "auditlogs.view", PermissionCategory.AuditLogs, "View audit log entries"),

            // Dashboard
            Permission.Create("View Dashboard", "dashboard.view", PermissionCategory.Dashboard, "View dashboard metrics"),

            // Settings
            Permission.Create("View Settings", "settings.view", PermissionCategory.Settings, "View system settings"),
            Permission.Create("Manage Settings", "settings.manage", PermissionCategory.Settings, "Manage system settings")
        };

        await context.Permissions.AddRangeAsync(permissions);
        await context.SaveChangesAsync();
    }

    private static async Task SeedRolesAsync(ApplicationDbContext context)
    {
        var allPermissions = await context.Permissions.ToListAsync();

        // Super Admin - All permissions
        var superAdmin = Role.Create("Super Admin", "Full system access across all organizations", isSystemRole: true);
        foreach (var permission in allPermissions)
        {
            superAdmin.AddPermission(permission);
        }

        // Organization Admin - Full access within assigned organization
        var orgAdmin = Role.Create("Organization Admin", "Full access within assigned organization", isSystemRole: true);
        var orgAdminPermissions = allPermissions.Where(p =>
            p.Category != PermissionCategory.Settings ||
            p.Code == "settings.view").ToList();
        foreach (var permission in orgAdminPermissions)
        {
            orgAdmin.AddPermission(permission);
        }

        // Sub-Organization Manager - Manage assigned sub-organization
        var subOrgManager = Role.Create("Sub-Organization Manager", "Manage assigned sub-organization and its users", isSystemRole: true);
        var subOrgPermissions = allPermissions.Where(p =>
            p.Code.StartsWith("suborgs.") ||
            p.Code.StartsWith("users.") ||
            p.Code == "dashboard.view").ToList();
        foreach (var permission in subOrgPermissions)
        {
            subOrgManager.AddPermission(permission);
        }

        // User Manager - Create and manage users
        var userManager = Role.Create("User Manager", "Create and manage users within organization", isSystemRole: true);
        var userManagerPermissions = allPermissions.Where(p =>
            p.Code.StartsWith("users.") ||
            p.Code == "roles.view" ||
            p.Code == "dashboard.view").ToList();
        foreach (var permission in userManagerPermissions)
        {
            userManager.AddPermission(permission);
        }

        // Viewer - Read-only access
        var viewer = Role.Create("Viewer", "Read-only access to organization data", isSystemRole: true);
        var viewerPermissions = allPermissions.Where(p =>
            p.Code.EndsWith(".view")).ToList();
        foreach (var permission in viewerPermissions)
        {
            viewer.AddPermission(permission);
        }

        await context.Roles.AddRangeAsync(superAdmin, orgAdmin, subOrgManager, userManager, viewer);
        await context.SaveChangesAsync();
    }

    private static async Task SeedDefaultOrganizationAndSuperAdminAsync(
        ApplicationDbContext context,
        IPasswordService passwordService)
    {
        // Create default organization
        var defaultOrg = Organization.Create(
            "System Organization",
            "Default system organization for super administrators",
            "SYSTEM-ORG");

        await context.Organizations.AddAsync(defaultOrg);
        await context.SaveChangesAsync();

        // Create super admin user
        var superAdminRole = await context.Roles
            .FirstOrDefaultAsync(r => r.Name == "Super Admin");

        var passwordHash = passwordService.HashPassword("Password1,");
        var superAdmin = User.Create(
            email: "superadmin@system.local",
            passwordHash: passwordHash,
            firstName: "Super",
            lastName: "Admin",
            organizationId: defaultOrg.Id,
            mustChangePassword: true);

        if (superAdminRole != null)
        {
            superAdmin.AddRole(superAdminRole);
        }

        await context.Users.AddAsync(superAdmin);
        await context.SaveChangesAsync();
    }
}
