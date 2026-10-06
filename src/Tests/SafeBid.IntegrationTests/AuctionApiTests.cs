using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using MediatR;
using SafeBid.Application;
using SafeBid.Domain;

namespace SafeBid.IntegrationTests;

[Collection("IntegrationTests")]
public class AuctionApiTests : IAsyncLifetime
{
    private readonly ApiTestFixture _fixture;
    private readonly HttpClient _client;
    private Guid _userId;
    private Guid _categoryId;
    private string _token = "";

    public AuctionApiTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        
        // Setup Category
        var category = Category.Create("Test Category");
        db.Categories.Add(category);
        _categoryId = category.Id;
        
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(HttpClient Client, string Cookie)> GetAuthenticatedClient(int healthScore)
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        
        var password = "TestPassword123!";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"test{Guid.NewGuid()}@safebid.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FullName = "Test User",
            PhoneNumber = $"09{new Random().Next(10000000, 99999999)}",
            HealthScore = healthScore,
            EmailConfirmed = true
        };
        
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var loginClient = _fixture.CreateClient();
        var loginResponse = await loginClient.PostAsJsonAsync("/api/auth/login", new
        {
            email = user.Email,
            password = password
        });
        
        var setCookieHeaders = loginResponse.Headers.GetValues("Set-Cookie").ToList();
        var cookie = setCookieHeaders.First().Split(';')[0];
        
        var client = _fixture.CreateClient();
        return (client, cookie);
    }

    [Fact]
    public async Task CreateDraft_Success_Returns201()
    {
        var (client, cookie) = await GetAuthenticatedClient(100);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auctions");
        request.Headers.Add("Cookie", cookie);
        request.Content = JsonContent.Create(new
        {
            Title = "Test Auction",
            CategoryId = _categoryId,
            StartPrice = 100m,
            StepPrice = 10m,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(2),
            MediaUrls = new[] { "http://minio/temp-media/1.jpg" }
        });

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateDraft_LowHealthScore_Returns403()
    {
        var (client, cookie) = await GetAuthenticatedClient(50);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auctions");
        request.Headers.Add("Cookie", cookie);
        request.Content = JsonContent.Create(new
        {
            Title = "Test Auction",
            CategoryId = _categoryId,
            StartPrice = 100m,
            StepPrice = 10m,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(2),
            MediaUrls = new string[0]
        });

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateDraft_InvalidTime_Returns400()
    {
        var (client, cookie) = await GetAuthenticatedClient(100);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auctions");
        request.Headers.Add("Cookie", cookie);
        request.Content = JsonContent.Create(new
        {
            Title = "Test Auction",
            CategoryId = _categoryId,
            StartPrice = 100m,
            StepPrice = 10m,
            StartTime = DateTime.UtcNow.AddHours(-1), // Past
            EndTime = DateTime.UtcNow.AddMinutes(30), // < 1 hour
            MediaUrls = new string[0]
        });

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateDraft_InvalidPrice_Returns400()
    {
        var (client, cookie) = await GetAuthenticatedClient(100);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auctions");
        request.Headers.Add("Cookie", cookie);
        request.Content = JsonContent.Create(new
        {
            Title = "Test Auction",
            CategoryId = _categoryId,
            StartPrice = 100m,
            StepPrice = -5m, // Invalid
            ReservePrice = 50m, // Invalid (less than StartPrice)
            BuyNowPrice = 50m, // Invalid (less than StartPrice)
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(2),
            MediaUrls = new string[0]
        });

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
