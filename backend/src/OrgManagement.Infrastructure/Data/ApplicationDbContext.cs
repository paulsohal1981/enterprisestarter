using Microsoft.EntityFrameworkCore;
using OrgManagement.Application.Common.Interfaces;
using OrgManagement.Domain.Common;
using OrgManagement.Domain.Entities;
using System.Reflection;

namespace OrgManagement.Infrastructure.Data;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly Guid? _currentOrganizationId;
    private readonly bool _isSuperAdmin;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        Guid? currentOrganizationId,
        bool isSuperAdmin = false)
        : base(options)
    {
        _currentOrganizationId = currentOrganizationId;
        _isSuperAdmin = isSuperAdmin;
    }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<SubOrganization> SubOrganizations => Set<SubOrganization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Apply soft delete query filters
        ApplySoftDeleteQueryFilters(modelBuilder);

        // Apply multi-tenant query filters (if not super admin)
        if (!_isSuperAdmin && _currentOrganizationId.HasValue)
        {
            ApplyMultiTenantQueryFilters(modelBuilder);
        }
    }

    private static void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
    {
        // Organization
        modelBuilder.Entity<Organization>()
            .HasQueryFilter(e => !e.IsDeleted);

        // SubOrganization
        modelBuilder.Entity<SubOrganization>()
            .HasQueryFilter(e => !e.IsDeleted);

        // User
        modelBuilder.Entity<User>()
            .HasQueryFilter(e => !e.IsDeleted);

        // Role
        modelBuilder.Entity<Role>()
            .HasQueryFilter(e => !e.IsDeleted);
    }

    private void ApplyMultiTenantQueryFilters(ModelBuilder modelBuilder)
    {
        // SubOrganization - filter by organization
        modelBuilder.Entity<SubOrganization>()
            .HasQueryFilter(e => !e.IsDeleted && e.OrganizationId == _currentOrganizationId);

        // User - filter by organization
        modelBuilder.Entity<User>()
            .HasQueryFilter(e => !e.IsDeleted && e.OrganizationId == _currentOrganizationId);

        // AuditLog - filter by organization
        modelBuilder.Entity<AuditLog>()
            .HasQueryFilter(e => e.OrganizationId == _currentOrganizationId);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.ModifiedAt = DateTime.UtcNow;
                    break;
            }
        }

        foreach (var entry in ChangeTracker.Entries<ISoftDelete>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
