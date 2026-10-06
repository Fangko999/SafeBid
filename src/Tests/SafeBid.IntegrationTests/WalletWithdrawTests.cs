using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SafeBid.Domain;
using SafeBid.Infrastructure;

namespace SafeBid.IntegrationTests;

[Collection("IntegrationTests")]
public class WalletWithdrawTests
{
    private readonly ApiTestFixture _factory;
    private readonly HttpClient _client;

    public WalletWithdrawTests(ApiTestFixture factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(User User, Wallet Wallet, string Cookie)> SetupUserWalletAndLogin(decimal availableBalance)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var password = "TestPassword123!";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"wallet_test_{Guid.NewGuid()}@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FullName = "Wallet Test User",
            PhoneNumber = $"09{Guid.NewGuid().ToString().Substring(0, 8)}",
            EmailConfirmed = true
        };
        db.Users.Add(user);
        
        var wallet = new Wallet
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            AvailableBalance = availableBalance,
            HoldAmount = 0,
            IsConfiscated = false
        };
        db.Wallets.Add(wallet);
        await db.SaveChangesAsync();

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = user.Email,
            password = password
        });
        
        var setCookieHeaders = loginResponse.Headers.GetValues("Set-Cookie").ToList();
        var cookie = setCookieHeaders.First().Split(';')[0];
        
        return (user, wallet, cookie);
    }

    [Fact]
    public async Task GetBalance_ReturnsCorrectAmounts()
    {
        var (user, _, cookie) = await SetupUserWalletAndLogin(100000m);
        
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/wallet/balance");
        request.Headers.Add("Cookie", cookie);

        var response = await _client.SendAsync(request);
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        
        json.GetProperty("availableBalance").GetDecimal().Should().Be(100000m);
        json.GetProperty("holdAmount").GetDecimal().Should().Be(0m);
    }

    [Fact]
    public async Task Withdraw_Success_MovesMoneyToHold_And_CreatesLedger_And_WithdrawalRequest()
    {
        var (user, wallet, cookie) = await SetupUserWalletAndLogin(100000m);
        
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/wallet/withdraw");
        request.Headers.Add("Cookie", cookie);
        request.Content = JsonContent.Create(new { amount = 50000m });

        var response = await _client.SendAsync(request);
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Check DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var updatedWallet = await db.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.Id == wallet.Id);
        updatedWallet!.AvailableBalance.Should().Be(50000m);
        updatedWallet.HoldAmount.Should().Be(50000m);

        var ledgerEntries = await db.LedgerEntries.Where(l => l.WalletId == wallet.Id).ToListAsync();
        ledgerEntries.Should().HaveCount(2); // -50000 Available, +50000 Hold
        
        var req = await db.Set<WithdrawalRequest>().FirstOrDefaultAsync(r => r.WalletId == wallet.Id);
        req.Should().NotBeNull();
        req!.Amount.Should().Be(50000m);
        req.Status.Should().Be("PENDING");
    }

    [Fact]
    public async Task Withdraw_ThrowsInsufficientFunds_WhenBalanceTooLow()
    {
        var (_, _, cookie) = await SetupUserWalletAndLogin(40000m); // Minimum is 10k but we try to withdraw 50k
        
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/wallet/withdraw");
        request.Headers.Add("Cookie", cookie);
        request.Content = JsonContent.Create(new { amount = 50000m });

        var response = await _client.SendAsync(request);
        
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Withdraw_ThrowsConflict_WhenRaceCondition()
    {
        var (user, wallet, cookie) = await SetupUserWalletAndLogin(100000m);

        // Simulate Race Condition: Modify RowVersion in DB right before API call
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // Simulating another thread updating the wallet by modifying AvailableBalance which changes RowVersion
        await db.Database.ExecuteSqlRawAsync($"UPDATE Wallets SET AvailableBalance = 90000 WHERE Id = '{wallet.Id}'");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/wallet/withdraw");
        request.Headers.Add("Cookie", cookie);
        // This request will load the Wallet, but EF doesn't know it was changed underneath if the API uses standard DbContext tracking.
        // Wait, the API call is a completely new HTTP request. It will read the NEW RowVersion from the DB, so it WILL succeed if we just do standard update!
        // To truly simulate Concurrency Exception, we need 2 parallel HTTP requests hitting the exact same endpoint at the same time.
        // Let's fire two requests concurrently.
        
        var request1 = new HttpRequestMessage(HttpMethod.Post, "/api/wallet/withdraw");
        request1.Headers.Add("Cookie", cookie);
        request1.Content = JsonContent.Create(new { amount = 50000m });

        var request2 = new HttpRequestMessage(HttpMethod.Post, "/api/wallet/withdraw");
        request2.Headers.Add("Cookie", cookie);
        request2.Content = JsonContent.Create(new { amount = 50000m });

        var task1 = _client.SendAsync(request1);
        var task2 = _client.SendAsync(request2);

        var responses = await Task.WhenAll(task1, task2);

        // One must succeed (200 OK) and one must fail with 409 Conflict (or 500, but we expect to map to 409)
        var statuses = responses.Select(r => r.StatusCode).ToList();
        
        statuses.Should().Contain(HttpStatusCode.OK);
        statuses.Should().Contain(HttpStatusCode.Conflict);
        
        var updatedWallet = await db.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.Id == wallet.Id);
        updatedWallet!.AvailableBalance.Should().Be(40000m); // Started at 90k, one 50k succeeded => 40k
    }
}
