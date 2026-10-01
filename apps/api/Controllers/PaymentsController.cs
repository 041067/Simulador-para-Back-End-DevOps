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
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PaymentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PaymentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPayments([FromQuery] PaymentStatus? status)
    {
        var result = await _mediator.Send(new GetPaymentsQuery(status));
        return Ok(result);
    }

    [HttpGet("pending")]
    [ProducesResponseType(typeof(IReadOnlyList<PaymentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingPayments()
    {
        var result = await _mediator.Send(new GetPendingPaymentsQuery());
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPayment(Guid id)
    {
        var result = await _mediator.Send(new GetPaymentQuery(id));
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpGet("by-reference/{reference}")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentByReference(string reference)
    {
        var result = await _mediator.Send(new GetPaymentByExternalReferenceQuery(reference));
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpGet("by-idempotency/{key}")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentByIdempotencyKey(string key)
    {
        var result = await _mediator.Send(new GetPaymentByIdempotencyKeyQuery(key));
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetPayment), new { id = result.Value }, result.Value)
            : result.ErrorCode == "IDEMPOTENCY_KEY_EXISTS" ? Conflict(result.Error) : BadRequest(result.Error);
    }

    [HttpPost("{id:guid}/process")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ProcessPayment(Guid id)
    {
        var result = await _mediator.Send(new ProcessPaymentCommand(id));
        return result.IsSuccess
            ? NoContent()
            : result.ErrorCode == "CIRCUIT_BREAKER_OPEN" || result.ErrorCode == "BANK_UNAVAILABLE"
                ? StatusCode(StatusCodes.Status503ServiceUnavailable, result.Error)
                : NotFound(result.Error);
    }

    [HttpPost("{id:guid}/authorize")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AuthorizePayment(Guid id, [FromBody] AuthorizeRequest request)
    {
        var result = await _mediator.Send(new AuthorizePaymentCommand(id, request.AuthorizationCode));
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }

    [HttpPost("{id:guid}/capture")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CapturePayment(Guid id)
    {
        var result = await _mediator.Send(new CapturePaymentCommand(id));
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }

    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompletePayment(Guid id)
    {
        var result = await _mediator.Send(new CompletePaymentCommand(id));
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }

    [HttpPost("{id:guid}/fail")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> FailPayment(Guid id, [FromBody] FailPaymentRequest request)
    {
        var result = await _mediator.Send(new FailPaymentCommand(id, request.Reason));
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }

    [HttpPost("{id:guid}/refund")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RefundPayment(Guid id, [FromBody] RefundRequest request)
    {
        var result = await _mediator.Send(new RefundPaymentCommand(id, request.Amount));
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelPayment(Guid id)
    {
        var result = await _mediator.Send(new CancelPaymentCommand(id));
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }

    [HttpPost("{id:guid}/retry")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RetryPayment(Guid id)
    {
        var result = await _mediator.Send(new RetryPaymentCommand(id));
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }
}

public record AuthorizeRequest(string AuthorizationCode);
public record FailPaymentRequest(string Reason);
public record RefundRequest(decimal Amount);