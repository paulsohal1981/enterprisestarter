using OrgManagement.Domain.Common;
using OrgManagement.Domain.Enums;

namespace OrgManagement.Domain.Entities;

public class AuditLog : BaseEntity
{
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public AuditAction Action { get; private set; }
    public string? OldValues { get; private set; }
    public string? NewValues { get; private set; }
    public string? UserId { get; private set; }
    public string? UserEmail { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public Guid? OrganizationId { get; private set; }

    private AuditLog() { }

    public static AuditLog Create(
        string entityType,
        Guid entityId,
        AuditAction action,
        string? oldValues = null,
        string? newValues = null,
        string? userId = null,
        string? userEmail = null,
        string? ipAddress = null,
        string? userAgent = null,
        Guid? organizationId = null)
    {
        return new AuditLog
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            OldValues = oldValues,
            NewValues = newValues,
            UserId = userId,
            UserEmail = userEmail,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            OrganizationId = organizationId
        };
    }
}
