using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrgManagement.Application.Features.Organizations.Commands;
using OrgManagement.Application.Features.Organizations.Queries;
using OrgManagement.Domain.Enums;
using OrgManagement.Infrastructure.Authorization;

namespace OrgManagement.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrganizationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrganizationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [HasPermission(Permissions.Organizations.View)]
    public async Task<IActionResult> GetOrganizations(
        [FromQuery] string? searchTerm,
        [FromQuery] OrganizationStatus? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "Name",
        [FromQuery] bool sortDescending = false)
    {
        var query = new GetOrganizationsQuery(searchTerm, status, pageNumber, pageSize, sortBy, sortDescending);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Organizations.View)]
    public async Task<IActionResult> GetOrganization(Guid id)
    {
        var result = await _mediator.Send(new GetOrganizationByIdQuery(id));
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Organizations.Create)]
    public async Task<IActionResult> CreateOrganization([FromBody] CreateOrganizationCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }
        return CreatedAtAction(nameof(GetOrganization), new { id = result.Value }, new { id = result.Value });
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Organizations.Update)]
    public async Task<IActionResult> UpdateOrganization(Guid id, [FromBody] UpdateOrganizationCommand command)
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
    [HasPermission(Permissions.Organizations.Manage)]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeOrganizationStatusRequest request)
    {
        var command = new ChangeOrganizationStatusCommand(id, request.Status);
        var result = await _mediator.Send(command);
        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Organizations.Delete)]
    public async Task<IActionResult> DeleteOrganization(Guid id)
    {
        var result = await _mediator.Send(new DeleteOrganizationCommand(id));
        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }
        return NoContent();
    }

    // Sub-organization endpoints
    [HttpPost("{organizationId:guid}/sub-organizations")]
    [HasPermission(Permissions.SubOrganizations.Create)]
    public async Task<IActionResult> CreateSubOrganization(
        Guid organizationId,
        [FromBody] CreateSubOrganizationRequest request)
    {
        var command = new CreateSubOrganizationCommand(
            request.Name,
            request.Description,
            request.Code,
            organizationId,
            request.ParentSubOrganizationId);
        var result = await _mediator.Send(command);
        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }
        return CreatedAtAction(nameof(GetOrganization), new { id = organizationId }, new { id = result.Value });
    }

    [HttpPut("sub-organizations/{id:guid}")]
    [HasPermission(Permissions.SubOrganizations.Update)]
    public async Task<IActionResult> UpdateSubOrganization(Guid id, [FromBody] UpdateSubOrganizationCommand command)
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

public record ChangeOrganizationStatusRequest(OrganizationStatus Status);

public record CreateSubOrganizationRequest(
    string Name,
    string? Description,
    string? Code,
    Guid? ParentSubOrganizationId);
