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
public class SimulationController : ControllerBase
{
    private readonly IMediator _mediator;

    public SimulationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("metrics")]
    [ProducesResponseType(typeof(SimulationResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMetrics([FromQuery] SimulationScenario scenario)
    {
        var result = await _mediator.Send(new GetSimulationMetricsQuery(scenario));
        return Ok(result);
    }

    [HttpGet("queue-metrics")]
    [ProducesResponseType(typeof(QueueMetricsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQueueMetrics([FromQuery] string queueName)
    {
        var result = await _mediator.Send(new GetQueueMetricsQuery(queueName));
        return Ok(result);
    }

    [HttpGet("health")]
    [ProducesResponseType(typeof(HealthCheckDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHealth()
    {
        var result = await _mediator.Send(new GetSystemHealthQuery());
        return Ok(result);
    }

    [HttpGet("snapshot")]
    [ProducesResponseType(typeof(MetricsSnapshotDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSnapshot()
    {
        var result = await _mediator.Send(new GetMetricsSnapshotQuery());
        return Ok(result);
    }
}