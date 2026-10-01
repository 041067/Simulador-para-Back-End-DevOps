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
public class WorkersController : ControllerBase
{
    private readonly IMediator _mediator;

    public WorkersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WorkerStatusDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWorkers([FromQuery] string? type)
    {
        var result = await _mediator.Send(new GetWorkersQuery(type));
        return Ok(result);
    }

    [HttpGet("available")]
    [ProducesResponseType(typeof(IReadOnlyList<WorkerStatusDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailableWorkers([FromQuery] string type)
    {
        var result = await _mediator.Send(new GetAvailableWorkersQuery(type));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WorkerStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWorker(Guid id)
    {
        var result = await _mediator.Send(new GetWorkerQuery(id));
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateWorker([FromBody] CreateWorkerCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess ? CreatedAtAction(nameof(GetWorker), new { id = result.Value }, result.Value) : BadRequest(result.Error);
    }

    [HttpPost("{id:guid}/heartbeat")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Heartbeat(Guid id)
    {
        var result = await _mediator.Send(new WorkerHeartbeatCommand(id));
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }

    [HttpPost("{id:guid}/start-job")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartJob(Guid id, [FromBody] StartJobRequest request)
    {
        var result = await _mediator.Send(new WorkerStartJobCommand(id, request.JobId));
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }

    [HttpPost("{id:guid}/complete-job")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteJob(Guid id, [FromBody] CompleteJobRequest request)
    {
        var result = await _mediator.Send(new WorkerCompleteJobCommand(id, request.Success));
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }

    [HttpPost("{id:guid}/fail")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> FailWorker(Guid id, [FromBody] FailWorkerRequest request)
    {
        var result = await _mediator.Send(new WorkerFailCommand(id, request.Error));
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }

    [HttpPost("{id:guid}/stop")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StopWorker(Guid id)
    {
        var result = await _mediator.Send(new WorkerStopCommand(id));
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }
}

public record StartJobRequest(string JobId);
public record CompleteJobRequest(bool Success);
public record FailWorkerRequest(string Error);