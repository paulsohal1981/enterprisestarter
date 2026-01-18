using OrgManagement.Domain.Common;
using OrgManagement.Domain.Enums;

namespace OrgManagement.Domain.Entities;

public class Organization : BaseEntity, ISoftDelete, IAuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Code { get; private set; }
    public OrganizationStatus Status { get; private set; } = OrganizationStatus.Active;

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation properties
    public ICollection<SubOrganization> SubOrganizations { get; private set; } = new List<SubOrganization>();
    public ICollection<User> Users { get; private set; } = new List<User>();

    private Organization() { }

    public static Organization Create(string name, string? description = null, string? code = null)
    {
        return new Organization
        {
            Name = name,
            Description = description,
            Code = code ?? GenerateCode(name),
            Status = OrganizationStatus.Active
        };
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

        // Cascade deactivation to sub-organizations
        foreach (var subOrg in SubOrganizations)
        {
            subOrg.Deactivate();
        }

        // Cascade deactivation to users
        foreach (var user in Users)
        {
            user.Deactivate();
        }
    }

    public void Suspend()
    {
        Status = OrganizationStatus.Suspended;
    }

    private static string GenerateCode(string name)
    {
        var code = new string(name
            .Where(char.IsLetterOrDigit)
            .Take(10)
            .ToArray())
            .ToUpperInvariant();
        return $"{code}-{DateTime.UtcNow.Ticks % 10000:D4}";
    }
}
