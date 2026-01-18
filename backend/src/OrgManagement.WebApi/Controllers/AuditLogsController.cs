using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrgManagement.Application.Features.AuditLogs.Queries;
using OrgManagement.Domain.Enums;
using OrgManagement.Infrastructure.Authorization;

namespace OrgManagement.WebApi.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize]
public class AuditLogsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuditLogsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [HasPermission(Permissions.AuditLogs.View)]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] string? entityType,
        [FromQuery] Guid? entityId,
        [FromQuery] AuditAction? action,
        [FromQuery] string? userId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetAuditLogsQuery(
            entityType, entityId, action, userId,
            fromDate, toDate, pageNumber, pageSize);
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}
