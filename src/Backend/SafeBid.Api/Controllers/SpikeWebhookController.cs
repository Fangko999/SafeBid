using Microsoft.AspNetCore.Mvc;
using SafeBid.Api.Filters;

namespace SafeBid.Api.Controllers;

public class WebhookPayload
{
    public string TransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public long Timestamp { get; set; }
}

[ApiController]
[Route("api/spikes/webhook")]
public class SpikeWebhookController : ControllerBase
{
    [HttpPost]
    [ServiceFilter(typeof(HmacAuthFilter))]
    public IActionResult Post([FromBody] WebhookPayload payload)
    {
        // Business logic here, safe from spoofing, replay, and duplicates
        return Ok(new { message = "Processed successfully", transactionId = payload.TransactionId });
    }
}
