using MediatR;
using Microsoft.EntityFrameworkCore;
using OrgManagement.Application.Common.Models;
using OrgManagement.Domain.Enums;
using OrgManagement.Application.Common.Interfaces;

namespace OrgManagement.Application.Features.AuditLogs.Queries;

public record GetAuditLogsQuery(
    string? EntityType = null,
    Guid? EntityId = null,
    AuditAction? Action = null,
    string? UserId = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedList<AuditLogDto>>;

public record AuditLogDto(
    Guid Id,
    string EntityType,
    Guid EntityId,
    AuditAction Action,
    string? OldValues,
    string? NewValues,
    string? UserId,
    string? UserEmail,
    string? IpAddress,
    DateTime CreatedAt);

public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, PaginatedList<AuditLogDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAuditLogsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.EntityType))
        {
            query = query.Where(a => a.EntityType == request.EntityType);
        }

        if (request.EntityId.HasValue)
        {
            query = query.Where(a => a.EntityId == request.EntityId.Value);
        }

        if (request.Action.HasValue)
        {
            query = query.Where(a => a.Action == request.Action.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.UserId))
        {
            query = query.Where(a => a.UserId == request.UserId);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(a => a.CreatedAt >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(a => a.CreatedAt <= request.ToDate.Value);
        }

        var projectedQuery = query
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AuditLogDto(
                a.Id,
                a.EntityType,
                a.EntityId,
                a.Action,
                a.OldValues,
                a.NewValues,
                a.UserId,
                a.UserEmail,
                a.IpAddress,
                a.CreatedAt));

        return await PaginatedList<AuditLogDto>.CreateAsync(projectedQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}
