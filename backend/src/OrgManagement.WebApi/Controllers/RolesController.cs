using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrgManagement.Application.Features.Roles.Commands;
using OrgManagement.Application.Features.Roles.Queries;
using OrgManagement.Infrastructure.Authorization;

namespace OrgManagement.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IMediator _mediator;

    public RolesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [HasPermission(Permissions.Roles.View)]
    public async Task<IActionResult> GetRoles(
        [FromQuery] string? searchTerm,
        [FromQuery] bool? isSystemRole,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetRolesQuery(searchTerm, isSystemRole, pageNumber, pageSize);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Roles.View)]
    public async Task<IActionResult> GetRole(Guid id)
    {
        var result = await _mediator.Send(new GetRoleByIdQuery(id));
        return Ok(result);
    }

    [HttpGet("permissions")]
    [HasPermission(Permissions.Roles.View)]
    public async Task<IActionResult> GetPermissions()
    {
        var result = await _mediator.Send(new GetPermissionsQuery());
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Roles.Create)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }
        return CreatedAtAction(nameof(GetRole), new { id = result.Value }, new { id = result.Value });
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Roles.Update)]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest(new { error = "ID mismatch" });
        }
        var result = await _mediator.Send(command);
        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }
        return NoContent();
    }
}
