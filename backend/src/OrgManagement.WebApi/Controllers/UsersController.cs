using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrgManagement.Application.Features.Users.Commands;
using OrgManagement.Application.Features.Users.Queries;
using OrgManagement.Domain.Enums;
using OrgManagement.Infrastructure.Authorization;

namespace OrgManagement.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [HasPermission(Permissions.Users.View)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? searchTerm,
        [FromQuery] UserStatus? status,
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? subOrganizationId,
        [FromQuery] Guid? roleId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "LastName",
        [FromQuery] bool sortDescending = false)
    {
        var query = new GetUsersQuery(
            searchTerm, status, organizationId, subOrganizationId,
            roleId, pageNumber, pageSize, sortBy, sortDescending);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Users.View)]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var result = await _mediator.Send(new GetUserByIdQuery(id));
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Users.Create)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }
        return CreatedAtAction(nameof(GetUser), new { id = result.Value }, new { id = result.Value });
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Users.Update)]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserCommand command)
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

    [HttpPatch("{id:guid}/status")]
    [HasPermission(Permissions.Users.Manage)]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeUserStatusRequest request)
    {
        var command = new ChangeUserStatusCommand(id, request.Status);
        var result = await _mediator.Send(command);
        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }
        return NoContent();
    }

    [HttpPut("{id:guid}/roles")]
    [HasPermission(Permissions.Users.AssignRoles)]
    public async Task<IActionResult> AssignRoles(Guid id, [FromBody] AssignRolesRequest request)
    {
        var command = new AssignUserRolesCommand(id, request.RoleIds);
        var result = await _mediator.Send(command);
        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }
        return NoContent();
    }
}

public record ChangeUserStatusRequest(UserStatus Status);
public record AssignRolesRequest(IEnumerable<Guid> RoleIds);
