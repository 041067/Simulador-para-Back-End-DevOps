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
public class VideoJobsController : ControllerBase
{
    private readonly IMediator _mediator;

    public VideoJobsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<VideoJobDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVideoJobs([FromQuery] JobStatus? status)
    {
        var result = await _mediator.Send(new GetVideoJobsQuery(status));
        return Ok(result);
    }

    [HttpGet("queued")]
    [ProducesResponseType(typeof(IReadOnlyList<VideoJobDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQueuedJobs([FromQuery] int count = 100)
    {
        var result = await _mediator.Send(new GetQueuedVideoJobsQuery(count));
        return Ok(result);
    }

    [HttpGet("queue-depth")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQueueDepth()
    {
        var result = await _mediator.Send(new GetQueueDepthQuery());
        return Ok(result);
    }

    [HttpGet("by-worker/{workerId}")]
    [ProducesResponseType(typeof(IReadOnlyList<VideoJobDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetJobsByWorker(string workerId)
    {
        var result = await _mediator.Send(new GetVideoJobsByWorkerQuery(workerId));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(VideoJobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVideoJob(Guid id)
    {
        var result = await _mediator.Send(new GetVideoJobQuery(id));
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateVideoJob([FromBody] CreateVideoJobCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess ? CreatedAtAction(nameof(GetVideoJob), new { id = result.Value }, result.Value) : BadRequest(result.Error);
    }

    [HttpPost("{id:guid}/assign")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignJob(Guid id, [FromBody] AssignJobRequest request)
    {
        var result = await _mediator.Send(new StartVideoJobCommand(id, request.WorkerId));
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }

    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteJob(Guid id)
    {
        var result = await _mediator.Send(new CompleteVideoJobCommand(id));
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }

    [HttpPost("{id:guid}/fail")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> FailJob(Guid id, [FromBody] FailJobRequest request)
    {
        var result = await _mediator.Send(new FailVideoJobCommand(id, request.Error));
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }

    [HttpPost("{id:guid}/retry")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RetryJob(Guid id)
    {
        var result = await _mediator.Send(new RetryVideoJobCommand(id));
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelJob(Guid id)
    {
        var result = await _mediator.Send(new CancelVideoJobCommand(id));
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }
}

public record AssignJobRequest(string WorkerId);
public record FailJobRequest(string Error);