using OrgManagement.Domain.Common;
using OrgManagement.Domain.Enums;

namespace OrgManagement.Domain.Entities;

public class Permission : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public PermissionCategory Category { get; private set; }
    public bool IsSystemPermission { get; private set; }

    // Navigation properties
    public ICollection<RolePermission> RolePermissions { get; private set; } = new List<RolePermission>();

    private Permission() { }

    public static Permission Create(
        string name,
        string code,
        PermissionCategory category,
        string? description = null,
        bool isSystemPermission = true)
    {
        return new Permission
        {
            Name = name,
            Code = code,
            Category = category,
            Description = description,
            IsSystemPermission = isSystemPermission
        };
    }
}
