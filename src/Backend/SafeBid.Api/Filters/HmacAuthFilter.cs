using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using StackExchange.Redis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SafeBid.Api.Filters;

public class HmacAuthFilter : IAsyncAuthorizationFilter
{
    private readonly string _secret;
    private readonly IConnectionMultiplexer _redis;

    public HmacAuthFilter(IConfiguration config, IConnectionMultiplexer redis)
    {
        _secret = config["WebhookSecret"] ?? Environment.GetEnvironmentVariable("WebhookSecret") ?? "my_super_secret_webhook_key";
        _redis = redis;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var request = context.HttpContext.Request;

        if (!request.Headers.TryGetValue("X-Signature", out var signatureValues))
        {
            context.Result = new UnauthorizedObjectResult(new { message = "Missing signature" });
            return;
        }
        var signature = signatureValues.ToString();

        request.EnableBuffering();
        using var ms = new MemoryStream();
        await request.Body.CopyToAsync(ms);
        var messageBytes = ms.ToArray();
        request.Body.Position = 0; // Rewind stream for model binding

        // Verify HMAC
        var encoding = new UTF8Encoding();
        var keyByte = encoding.GetBytes(_secret);
        using var hmac = new HMACSHA256(keyByte);
        var expectedHash = Convert.ToHexString(hmac.ComputeHash(messageBytes)).ToLowerInvariant();

        var rawBody = Encoding.UTF8.GetString(messageBytes);

        if (!string.Equals(signature, expectedHash, StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new UnauthorizedObjectResult(new { message = "Invalid signature" });
            return;
        }

        try
        {
            var json = JsonSerializer.Deserialize<JsonElement>(rawBody);
            
            // Verify Timestamp
            if (json.TryGetProperty("timestamp", out var tsProperty))
            {
                var timestamp = tsProperty.GetInt64();
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (Math.Abs(now - timestamp) > 300) // 5 minutes SLA
                {
                    context.Result = new UnauthorizedObjectResult(new { message = "Request expired" });
                    return;
                }
            }

            // Verify Idempotency
            if (json.TryGetProperty("transactionId", out var txProperty))
            {
                var txId = txProperty.GetString();
                if (!string.IsNullOrEmpty(txId))
                {
                    var db = _redis.GetDatabase();
                    var key = $"webhook:idempotency:{txId}";
                    var isSet = await db.StringSetAsync(key, "processed", TimeSpan.FromMinutes(6), When.NotExists);
                    if (!isSet)
                    {
                        context.Result = new OkObjectResult(new { message = "Idempotency triggered" });
                        return;
                    }
                }
            }
        }
        catch (JsonException)
        {
            context.Result = new BadRequestObjectResult(new { message = "Invalid JSON payload" });
        }
    }
}
