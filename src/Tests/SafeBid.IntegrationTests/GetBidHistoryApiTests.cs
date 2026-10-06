using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SafeBid.Application;
using Xunit;

namespace SafeBid.IntegrationTests;

[Collection("IntegrationTests")]
public class GetBidHistoryApiTests : IAsyncLifetime
{
    private readonly ApiTestFixture _fixture;
    private HttpClient _client = default!;

    public GetBidHistoryApiTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _client = _fixture.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetBidHistory_ReturnsMaskedNames()
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SafeBid.Infrastructure.AppDbContext>();
        
        var user = new SafeBid.Domain.User
        {
            Id = Guid.NewGuid(),
            Email = $"bidder_{Guid.NewGuid()}@safebid.com",
            PasswordHash = "hash",
            FullName = "Nguyễn Văn Anh",
            PhoneNumber = Guid.NewGuid().ToString().Substring(0, 10),
            HealthScore = 100,
            EmailConfirmed = true
        };
        db.Users.Add(user);

        var category = SafeBid.Domain.Category.Create("Tech");
        db.Categories.Add(category);

        var auctionResult = SafeBid.Domain.Auction.CreateDraft(
            user.Id,
            "Test Auction",
            category.Id,
            100,
            10,
            null,
            null,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2)
        );
        var auction = auctionResult.Value;
        auction.Publish();
        db.Auctions.Add(auction);

        db.Bids.Add(new SafeBid.Domain.Bid
        {
            Id = Guid.NewGuid(),
            AuctionId = auction.Id,
            BidderId = user.Id,
            Amount = 150000,
            Timestamp = DateTime.UtcNow,
            IsWinning = true
        });

        await db.SaveChangesAsync();
        
        // Act
        var response = await _client.GetAsync($"/api/auctions/{auction.Id}/bids");
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var items = json.GetProperty("items").EnumerateArray().ToList();
        
        Assert.Single(items);
        Assert.Equal("Nguyễn A***", items[0].GetProperty("maskedBidderName").GetString());
        Assert.Equal(150000, items[0].GetProperty("amount").GetDecimal());
    }
}
