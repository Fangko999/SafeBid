using MediatR;
using Microsoft.AspNetCore.Mvc;
using SafeBid.Api.Filters;
using SafeBid.Application;

namespace SafeBid.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhooksController : ControllerBase
{
    private readonly IMediator _mediator;

    public WebhooksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("deposit")]
    [ServiceFilter(typeof(HmacAuthFilter))]
    public async Task<IActionResult> Deposit([FromBody] DepositWebhookCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(new { message = "Processed successfully", transactionId = command.TransactionId });
        }

        // According to webhook best practices, even if the user is not found, we shouldn't necessarily
        // return 400/500 if we want the provider to stop retrying, but returning BadRequest makes it clear to us.
        // For security, often returning 200 is better to avoid leaking user existence, but for our case returning BadRequest is fine.
        return BadRequest(new { error = new { code = result.Error.Code, message = result.Error.Message } });
    }
}
