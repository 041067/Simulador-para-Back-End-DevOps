namespace BackOps.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using MediatR;
using BackOps.Application.Commands;
using BackOps.Application.Queries;
using BackOps.Application.DTOs;
using BackOps.Application.Common;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class EventsController : ControllerBase
{
    private readonly IMediator _mediator;

    public EventsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvents([FromQuery] bool activeOnly = true)
    {
        var result = await _mediator.Send(new GetEventsQuery(activeOnly));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEvent(Guid id)
    {
        var result = await _mediator.Send(new GetEventQuery(id));
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpGet("{eventId:guid}/inventory")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInventory(Guid eventId)
    {
        var result = await _mediator.Send(new GetEventInventoryQuery(eventId));
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess ? CreatedAtAction(nameof(GetEvent), new { id = result.Value }, result.Value) : BadRequest(result.Error);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] UpdateEventCommand command)
    {
        if (id != command.Id) return BadRequest("ID mismatch");
        var result = await _mediator.Send(command);
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }

    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateEvent(Guid id)
    {
        var result = await _mediator.Send(new ActivateEventCommand(id));
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }

    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateEvent(Guid id)
    {
        var result = await _mediator.Send(new DeactivateEventCommand(id));
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }
}