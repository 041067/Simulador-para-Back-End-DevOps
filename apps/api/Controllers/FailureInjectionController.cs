namespace BackOps.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using MediatR;
using BackOps.Application.Commands;
using BackOps.Application.Queries;
using BackOps.Application.DTOs;
using BackOps.Application.Common;
using BackOps.Domain.Enums;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class FailureInjectionController : ControllerBase
{
    private readonly IMediator _mediator;

    public FailureInjectionController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FailureConfigDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFailures()
    {
        var result = await _mediator.Send(new GetAllFailuresQuery());
        return Ok(result);
    }

    [HttpGet("{type}")]
    [ProducesResponseType(typeof(FailureConfigDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFailure(FailureType type)
    {
        var result = await _mediator.Send(new GetFailureConfigQuery(type));
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InjectFailure([FromBody] InjectFailureCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    [HttpDelete("{type}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ClearFailure(FailureType type)
    {
        var result = await _mediator.Send(new ClearFailureCommand(type));
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ClearAllFailures()
    {
        var result = await _mediator.Send(new ClearAllFailuresCommand());
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }
}