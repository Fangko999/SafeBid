using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace SafeBid.IntegrationTests;

public class SpikeWebhookTests : IClassFixture<ApiTestFixture>
{
    private readonly HttpClient _client;
    private const string Secret = "my_super_secret_webhook_key"; // Same as in app config

    public SpikeWebhookTests(ApiTestFixture fixture)
    {
        _client = fixture.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        Environment.SetEnvironmentVariable("WebhookSecret", Secret);
    }

    private string GenerateHmacSignature(string payload, string secret)
    {
        var encoding = new UTF8Encoding();
        var keyByte = encoding.GetBytes(secret);
        using var hmac = new HMACSHA256(keyByte);
        var messageBytes = encoding.GetBytes(payload);
        var hashMessage = hmac.ComputeHash(messageBytes);
        return Convert.ToHexString(hashMessage).ToLowerInvariant();
    }

    [Fact]
    public async Task ValidSignature_ShouldReturn200()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = $$"""{"transactionId":"tx-123","amount":1000,"timestamp":{{timestamp}}}""";
        var signature = GenerateHmacSignature(payload, Secret);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/spikes/webhook");
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        request.Headers.Add("X-Signature", signature);

        var response = await _client.SendAsync(request);
        var error = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, error);
    }

    [Fact]
    public async Task InvalidBody_ShouldReturn401()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var originalPayload = $$"""{"transactionId":"tx-spoofed","amount":1000,"timestamp":{{timestamp}}}""";
        var signature = GenerateHmacSignature(originalPayload, Secret);

        // Tampering the payload after signature generation
        var tamperedPayload = $$"""{"transactionId":"tx-spoofed","amount":9999,"timestamp":{{timestamp}}}""";

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/spikes/webhook");
        request.Content = new StringContent(tamperedPayload, Encoding.UTF8, "application/json");
        request.Headers.Add("X-Signature", signature);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DuplicateTransaction_ShouldNotProcessTwice()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = $$"""{"transactionId":"tx-duplicate","amount":500,"timestamp":{{timestamp}}}""";
        var signature = GenerateHmacSignature(payload, Secret);

        // First request
        var req1 = new HttpRequestMessage(HttpMethod.Post, "/api/spikes/webhook");
        req1.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        req1.Headers.Add("X-Signature", signature);
        var res1 = await _client.SendAsync(req1);
        res1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Second request (identical)
        var req2 = new HttpRequestMessage(HttpMethod.Post, "/api/spikes/webhook");
        req2.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        req2.Headers.Add("X-Signature", signature);
        var res2 = await _client.SendAsync(req2);
        
        res2.StatusCode.Should().Be(HttpStatusCode.OK); // Return 200 so provider doesn't retry
        var content = await res2.Content.ReadAsStringAsync();
        content.Should().Contain("Idempotency triggered");
    }

    [Fact]
    public async Task ExpiredTimestamp_ShouldReturn401()
    {
        var expiredTimestamp = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds(); // 10 minutes ago
        var payload = $$"""{"transactionId":"tx-expired","amount":100,"timestamp":{{expiredTimestamp}}}""";
        var signature = GenerateHmacSignature(payload, Secret);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/spikes/webhook");
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        request.Headers.Add("X-Signature", signature);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
