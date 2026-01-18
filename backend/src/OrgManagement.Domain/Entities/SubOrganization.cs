using OrgManagement.Domain.Common;
using OrgManagement.Domain.Enums;

namespace OrgManagement.Domain.Entities;

/// <summary>
/// Sub-organization with materialized path pattern for efficient hierarchy queries.
/// Path format: "/org-id/sub1-id/sub2-id/" enables O(1) ancestor/descendant queries.
/// Maximum depth is 5 levels.
/// </summary>
public class SubOrganization : BaseEntity, ISoftDelete, IAuditableEntity
{
    public const int MaxLevel = 5;

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Code { get; private set; }
    public OrganizationStatus Status { get; private set; } = OrganizationStatus.Active;

    /// <summary>
    /// Materialized path for hierarchy. Format: "/parentId/childId/..."
    /// </summary>
    public string Path { get; private set; } = string.Empty;

    /// <summary>
    /// Level in hierarchy (1-5). Level 1 is direct child of Organization.
    /// </summary>
    public int Level { get; private set; }

    // Foreign keys
    public Guid OrganizationId { get; private set; }
    public Guid? ParentSubOrganizationId { get; private set; }

    // Navigation properties
    public Organization Organization { get; private set; } = null!;
    public SubOrganization? ParentSubOrganization { get; private set; }
    public ICollection<SubOrganization> ChildSubOrganizations { get; private set; } = new List<SubOrganization>();
    public ICollection<User> Users { get; private set; } = new List<User>();

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    private SubOrganization() { }

    public static SubOrganization Create(
        string name,
        Guid organizationId,
        SubOrganization? parent = null,
        string? description = null,
        string? code = null)
    {
        var level = parent == null ? 1 : parent.Level + 1;

        if (level > MaxLevel)
        {
            throw new InvalidOperationException($"Cannot create sub-organization beyond level {MaxLevel}");
        }

        var subOrg = new SubOrganization
        {
            Name = name,
            Description = description,
            Code = code ?? GenerateCode(name),
            OrganizationId = organizationId,
            ParentSubOrganizationId = parent?.Id,
            Level = level,
            Status = OrganizationStatus.Active
        };

        // Set materialized path after Id is generated
        subOrg.Path = parent == null
            ? $"/{subOrg.Id}/"
            : $"{parent.Path}{subOrg.Id}/";

        return subOrg;
    }

    public void UpdatePath(SubOrganization? newParent)
    {
        if (newParent != null)
        {
            var newLevel = newParent.Level + 1;
            if (newLevel > MaxLevel)
            {
                throw new InvalidOperationException($"Cannot move sub-organization beyond level {MaxLevel}");
            }

            ParentSubOrganizationId = newParent.Id;
            Level = newLevel;
            Path = $"{newParent.Path}{Id}/";
        }
        else
        {
            ParentSubOrganizationId = null;
            Level = 1;
            Path = $"/{Id}/";
        }

        // Recursively update children paths
        UpdateChildrenPaths();
    }

    private void UpdateChildrenPaths()
    {
        foreach (var child in ChildSubOrganizations)
        {
            child.Level = Level + 1;
            child.Path = $"{Path}{child.Id}/";
            child.UpdateChildrenPaths();
        }
    }

    public void Update(string name, string? description, string? code)
    {
        Name = name;
        Description = description;
        Code = code;
    }

    public void Activate()
    {
        Status = OrganizationStatus.Active;
    }

    public void Deactivate()
    {
        Status = OrganizationStatus.Inactive;

        // Cascade deactivation to child sub-organizations
        foreach (var child in ChildSubOrganizations)
        {
            child.Deactivate();
        }

        // Cascade deactivation to users
        foreach (var user in Users)
        {
            user.Deactivate();
        }
    }

    /// <summary>
    /// Check if this sub-organization is a descendant of the given sub-organization.
    /// </summary>
    public bool IsDescendantOf(SubOrganization ancestor)
    {
        return Path.StartsWith(ancestor.Path);
    }

    /// <summary>
    /// Check if this sub-organization is an ancestor of the given sub-organization.
    /// </summary>
    public bool IsAncestorOf(SubOrganization descendant)
    {
        return descendant.Path.StartsWith(Path);
    }

    /// <summary>
    /// Get all ancestor IDs from the path.
    /// </summary>
    public IEnumerable<Guid> GetAncestorIds()
    {
        return Path.Trim('/')
            .Split('/')
            .Where(s => !string.IsNullOrEmpty(s) && s != Id.ToString())
            .Select(Guid.Parse);
    }

    private static string GenerateCode(string name)
    {
        var code = new string(name
            .Where(char.IsLetterOrDigit)
            .Take(10)
            .ToArray())
            .ToUpperInvariant();
        return $"SUB-{code}-{DateTime.UtcNow.Ticks % 10000:D4}";
    }
}
