using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SafeBid.Domain;
using SafeBid.Infrastructure;

namespace SafeBid.IntegrationTests;

[Collection("IntegrationTests")]
public class WalletTransactionsTests
{
    private readonly ApiTestFixture _factory;
    private readonly HttpClient _client;

    public WalletTransactionsTests(ApiTestFixture factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(User User, Wallet Wallet, string Cookie)> SetupUserWalletAndLogin(decimal availableBalance, int entryCount)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var password = "TestPassword123!";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"tx_test_{Guid.NewGuid()}@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FullName = "Transaction Test User",
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

        // Add Ledger Entries
        for (int i = 0; i < entryCount; i++)
        {
            db.LedgerEntries.Add(new LedgerEntry
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                Amount = 1000,
                Type = "DEPOSIT",
                Status = "COMPLETED",
                CreatedAt = DateTime.UtcNow.AddMinutes(-entryCount + i) // Oldest first, so we can test descending
            });
        }
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
    public async Task GetTransactions_ShouldReturnPaginatedResult_WhenValid()
    {
        // Arrange
        var (_, _, cookie) = await SetupUserWalletAndLogin(100000, 15);
        
        var requestPage1 = new HttpRequestMessage(HttpMethod.Get, "/api/wallet/transactions?page=1&pageSize=10");
        requestPage1.Headers.Add("Cookie", cookie);

        var requestPage2 = new HttpRequestMessage(HttpMethod.Get, "/api/wallet/transactions?page=2&pageSize=10");
        requestPage2.Headers.Add("Cookie", cookie);

        // Act
        var responsePage1 = await _client.SendAsync(requestPage1);
        var responsePage2 = await _client.SendAsync(requestPage2);

        // Assert
        responsePage1.StatusCode.Should().Be(HttpStatusCode.OK);
        responsePage2.StatusCode.Should().Be(HttpStatusCode.OK);

        var content1 = await responsePage1.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var content2 = await responsePage2.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();

        content1.GetProperty("totalCount").GetInt32().Should().Be(15);
        content1.GetProperty("totalPages").GetInt32().Should().Be(2);
        content1.GetProperty("currentPage").GetInt32().Should().Be(1);
        content1.GetProperty("hasNext").GetBoolean().Should().BeTrue();
        content1.GetProperty("items").GetArrayLength().Should().Be(10);

        // First item of Page 1 should be the most recent one (index 14)
        var firstItem = content1.GetProperty("items")[0];
        firstItem.GetProperty("amount").GetDecimal().Should().Be(1000);

        content2.GetProperty("totalCount").GetInt32().Should().Be(15);
        content2.GetProperty("currentPage").GetInt32().Should().Be(2);
        content2.GetProperty("hasNext").GetBoolean().Should().BeFalse();
        content2.GetProperty("items").GetArrayLength().Should().Be(5);
    }

    [Fact]
    public async Task GetTransactions_ShouldReturnUnauthorized_WhenNoCookie()
    {
        var response = await _client.GetAsync("/api/wallet/transactions?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
