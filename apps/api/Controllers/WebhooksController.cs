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
public class WebhooksController : ControllerBase
{
    private readonly IMediator _mediator;

    public WebhooksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WebhookDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWebhooks([FromQuery] Guid? paymentId)
    {
        if (paymentId.HasValue)
        {
            var result = await _mediator.Send(new GetWebhooksByPaymentQuery(paymentId.Value));
            return Ok(result);
        }

        var result = await _mediator.Send(new GetPendingWebhooksQuery());
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WebhookDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWebhook(Guid id)
    {
        var result = await _mediator.Send(new GetWebhookQuery(id));
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPost("{id:guid}/send")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendWebhook(Guid id)
    {
        var result = await _mediator.Send(new SendWebhookCommand(id));
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }

    [HttpPost("{id:guid}/retry")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RetryWebhook(Guid id)
    {
        var result = await _mediator.Send(new RetryWebhookCommand(id));
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }
}