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
public class RateLimitController : ControllerBase
{
    private readonly IMediator _mediator;

    public RateLimitController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{key}")]
    [ProducesResponseType(typeof(RateLimitInfoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(string key)
    {
        var result = await _mediator.Send(new GetRateLimitInfoQuery(key));
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Configure([FromBody] ConfigureRateLimitCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }
}