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
public class PurchasesController : ControllerBase
{
    private readonly IMediator _mediator;

    public PurchasesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PurchaseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPurchases([FromQuery] Guid? eventId, [FromQuery] Guid? userId)
    {
        if (eventId.HasValue)
        {
            var result = await _mediator.Send(new GetPurchasesByEventQuery(eventId.Value));
            return Ok(result);
        }
        if (userId.HasValue)
        {
            var result = await _mediator.Send(new GetPurchasesByUserQuery(userId.Value));
            return Ok(result);
        }

        var allResult = await _mediator.Send(new GetPendingPurchasesQuery());
        return Ok(allResult);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PurchaseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPurchase(Guid id)
    {
        var result = await _mediator.Send(new GetPurchaseQuery(id));
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreatePurchase([FromBody] CreatePurchaseCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetPurchase), new { id = result.Value }, result.Value)
            : result.ErrorCode == "IDEMPOTENCY_KEY_EXISTS" ? Conflict(result.Error) : BadRequest(result.Error);
    }

    [HttpPost("{id:guid}/process")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ProcessPurchase(Guid id)
    {
        var result = await _mediator.Send(new ProcessPurchaseCommand(id));
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }

    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompletePurchase(Guid id)
    {
        var result = await _mediator.Send(new CompletePurchaseCommand(id));
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }

    [HttpPost("{id:guid}/fail")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> FailPurchase(Guid id, [FromBody] FailPurchaseRequest request)
    {
        var result = await _mediator.Send(new FailPurchaseCommand(id, request.Reason));
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelPurchase(Guid id)
    {
        var result = await _mediator.Send(new CancelPurchaseCommand(id));
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }
}

public record FailPurchaseRequest(string Reason);