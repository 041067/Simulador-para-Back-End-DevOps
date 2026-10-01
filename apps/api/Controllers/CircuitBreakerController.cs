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
public class CircuitBreakerController : ControllerBase
{
    private readonly IMediator _mediator;

    public CircuitBreakerController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CircuitBreakerStatusDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetAllCircuitBreakersQuery());
        return Ok(result);
    }

    [HttpGet("{name}")]
    [ProducesResponseType(typeof(CircuitBreakerStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string name)
    {
        var result = await _mediator.Send(new GetCircuitBreakerStatusQuery(name));
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Configure([FromBody] ConfigureCircuitBreakerCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }
}