using OrgManagement.Domain.Common;

namespace OrgManagement.Domain.Entities;

public class Role : BaseEntity, ISoftDelete, IAuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsSystemRole { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation properties
    public ICollection<RolePermission> RolePermissions { get; private set; } = new List<RolePermission>();
    public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();

    private Role() { }

    public static Role Create(string name, string? description = null, bool isSystemRole = false)
    {
        return new Role
        {
            Name = name,
            Description = description,
            IsSystemRole = isSystemRole,
            IsActive = true
        };
    }

    public void Update(string name, string? description)
    {
        if (IsSystemRole)
        {
            throw new InvalidOperationException("Cannot modify system roles");
        }

        Name = name;
        Description = description;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        if (IsSystemRole)
        {
            throw new InvalidOperationException("Cannot deactivate system roles");
        }

        IsActive = false;
    }

    public void AddPermission(Permission permission)
    {
        if (!RolePermissions.Any(rp => rp.PermissionId == permission.Id))
        {
            RolePermissions.Add(new RolePermission { RoleId = Id, PermissionId = permission.Id });
        }
    }

    public void RemovePermission(Guid permissionId)
    {
        var rolePermission = RolePermissions.FirstOrDefault(rp => rp.PermissionId == permissionId);
        if (rolePermission != null)
        {
            RolePermissions.Remove(rolePermission);
        }
    }

    public void ClearPermissions()
    {
        RolePermissions.Clear();
    }
}
