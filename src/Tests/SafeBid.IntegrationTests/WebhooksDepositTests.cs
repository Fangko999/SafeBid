using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SafeBid.Infrastructure;
using SafeBid.Domain;
using Microsoft.EntityFrameworkCore;

namespace SafeBid.IntegrationTests;

[Collection("IntegrationTests")]
public class WebhooksDepositTests
{
    private readonly ApiTestFixture _factory;
    private readonly HttpClient _client;
    private const string Secret = "my_super_secret_webhook_key";

    public WebhooksDepositTests(ApiTestFixture factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
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
    public async Task DepositWebhook_ValidPayload_ShouldAddFunds_And_CreateLedgerEntry()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "webhook_valid@example.com",
            PasswordHash = "hash",
            FullName = "Valid Webhook User",
            PhoneNumber = "0123456781",
            EmailConfirmed = true
        };
        db.Users.Add(user);
        
        var wallet = new Wallet
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            AvailableBalance = 0,
            HoldAmount = 0,
            IsConfiscated = false
        };
        db.Wallets.Add(wallet);
        await db.SaveChangesAsync();

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = $$"""{"transactionId":"tx-12345","referenceId":"{{user.Id}}","amount":100000,"status":"SUCCESS","timestamp":{{timestamp}}}""";
        var signature = GenerateHmacSignature(payload, Secret);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/deposit");
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        request.Headers.Add("X-Signature", signature);

        // Act
        var response = await _client.SendAsync(request);

        // Assert HTTP
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert DB
        var updatedWallet = await db.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.Id == wallet.Id);
        updatedWallet.Should().NotBeNull();
        updatedWallet!.AvailableBalance.Should().Be(100000);
        var ledgerEntry = await db.LedgerEntries.FirstOrDefaultAsync(l => l.WalletId == wallet.Id);
        ledgerEntry.Should().NotBeNull();
        ledgerEntry!.Amount.Should().Be(100000);
        ledgerEntry.Type.Should().Be("DEPOSIT");
        ledgerEntry.Status.Should().Be("COMPLETED");
    }
    [Fact]
    public async Task DepositWebhook_ConfiscatedUser_ShouldRedirectToSystemFund()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "webhook_confiscated@example.com",
            PasswordHash = "hash",
            FullName = "Confiscated User",
            PhoneNumber = "0123456782",
            EmailConfirmed = true
        };
        db.Users.Add(user);
        
        var wallet = new Wallet
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            AvailableBalance = 0,
            HoldAmount = 0,
            IsConfiscated = true
        };
        db.Wallets.Add(wallet);
        await db.SaveChangesAsync();

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = $$"""{"transactionId":"tx-12346","referenceId":"{{user.Id}}","amount":50000,"status":"SUCCESS","timestamp":{{timestamp}}}""";
        var signature = GenerateHmacSignature(payload, Secret);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/deposit");
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        request.Headers.Add("X-Signature", signature);

        // Act
        var response = await _client.SendAsync(request);

        // Assert HTTP
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert DB
        var updatedWallet = await db.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.Id == wallet.Id);
        updatedWallet.Should().NotBeNull();
        updatedWallet!.AvailableBalance.Should().Be(0); // Should not increase

        var ledgerEntry = await db.LedgerEntries.FirstOrDefaultAsync(l => l.Type == "DEPOSIT_CONFISCATION");
        ledgerEntry.Should().NotBeNull();
        ledgerEntry!.WalletId.Should().BeNull();
        ledgerEntry.Amount.Should().Be(50000);
        ledgerEntry.Status.Should().Be("COMPLETED");
    }

    [Fact]
    public async Task DepositWebhook_InvalidSignature_ShouldReturn401()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = $$"""{"transactionId":"tx-12347","referenceId":"{{Guid.NewGuid()}}","amount":10000,"status":"SUCCESS","timestamp":{{timestamp}}}""";
        var signature = "invalid_signature";

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/deposit");
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        request.Headers.Add("X-Signature", signature);

        // Act
        var response = await _client.SendAsync(request);

        // Assert HTTP
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
