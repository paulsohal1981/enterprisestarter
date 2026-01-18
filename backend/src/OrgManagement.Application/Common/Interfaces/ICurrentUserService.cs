namespace OrgManagement.Application.Common.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? Email { get; }
    Guid? OrganizationId { get; }
    bool IsSuperAdmin { get; }
    IEnumerable<string> Permissions { get; }
    IEnumerable<string> Roles { get; }
}
