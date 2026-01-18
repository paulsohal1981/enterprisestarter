using System.Text.Json;
using Microsoft.AspNetCore.Http;
using OrgManagement.Application.Common.Interfaces;
using OrgManagement.Domain.Entities;
using OrgManagement.Domain.Enums;
using OrgManagement.Infrastructure.Data;

namespace OrgManagement.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(
        ApplicationDbContext context,
        ICurrentUserService currentUserService,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _currentUserService = currentUserService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(
        string entityType,
        Guid entityId,
        AuditAction action,
        object? oldValues = null,
        object? newValues = null,
        CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        var auditLog = AuditLog.Create(
            entityType: entityType,
            entityId: entityId,
            action: action,
            oldValues: oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
            newValues: newValues != null ? JsonSerializer.Serialize(newValues) : null,
            userId: _currentUserService.UserId,
            userEmail: _currentUserService.Email,
            ipAddress: GetIpAddress(httpContext),
            userAgent: httpContext?.Request.Headers["User-Agent"].ToString(),
            organizationId: _currentUserService.OrganizationId);

        await _context.AuditLogs.AddAsync(auditLog, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string? GetIpAddress(HttpContext? context)
    {
        if (context == null) return null;

        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',').First().Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }
}
