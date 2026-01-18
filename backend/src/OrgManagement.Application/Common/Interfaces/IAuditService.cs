using OrgManagement.Domain.Enums;

namespace OrgManagement.Application.Common.Interfaces;

public interface IAuditService
{
    Task LogAsync(
        string entityType,
        Guid entityId,
        AuditAction action,
        object? oldValues = null,
        object? newValues = null,
        CancellationToken cancellationToken = default);
}
