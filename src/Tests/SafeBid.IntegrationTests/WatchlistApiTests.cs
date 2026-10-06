using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SafeBid.Application;
using Xunit;

namespace SafeBid.IntegrationTests;

[Collection("IntegrationTests")]
public class WatchlistApiTests : IAsyncLifetime
{
    private readonly ApiTestFixture _fixture;
    private HttpClient _client = default!;

    public WatchlistApiTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SafeBid.Infrastructure.AppDbContext>();
        var user = new SafeBid.Domain.User
        {
            Id = Guid.NewGuid(),
            Email = $"test{Guid.NewGuid()}@safebid.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            FullName = "Test",
            PhoneNumber = "0900000000",
            HealthScore = 100,
            EmailConfirmed = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var loginClient = _fixture.CreateClient();
        var loginResponse = await loginClient.PostAsJsonAsync("/api/auth/login", new { email = user.Email, password = "123456" });
        var cookie = loginResponse.Headers.GetValues("Set-Cookie").First().Split(';')[0];

        _client = _fixture.CreateClient();
        _client.DefaultRequestHeaders.Add("Cookie", cookie);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ToggleWatchlist_ReturnsOk()
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SafeBid.Infrastructure.AppDbContext>();
        var category = SafeBid.Domain.Category.Create("Tech");
        var user = new SafeBid.Domain.User { Id = Guid.NewGuid(), Email = "test@test.com", FullName = "Test User" };
        db.Categories.Add(category);
        db.Users.Add(user);
        
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
        await db.SaveChangesAsync();

        // Act - POST (Add)
        var postResponse = await _client.PostAsync($"/api/auctions/{auction.Id}/watchlist", null);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);
        var json = await postResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.True(json.GetProperty("isWatched").GetBoolean());

        // Act - POST again (Remove)
        var postResponse2 = await _client.PostAsync($"/api/auctions/{auction.Id}/watchlist", null);
        Assert.Equal(HttpStatusCode.OK, postResponse2.StatusCode);
        var json2 = await postResponse2.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.False(json2.GetProperty("isWatched").GetBoolean());
    }
}
