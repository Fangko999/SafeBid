using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using SafeBid.Application;
using SafeBid.Domain;

namespace SafeBid.IntegrationTests;

[Collection("IntegrationTests")]
public class GetAuctionsApiTests : IAsyncLifetime
{
    private readonly ApiTestFixture _fixture;
    private readonly HttpClient _client;

    public GetAuctionsApiTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(Guid activeId, Guid draftId)> SetupAuctions()
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        
        var category = Category.Create("Test Category");
        db.Categories.Add(category);
        
        var password = "TestPassword123!";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"seller_{Guid.NewGuid()}@safebid.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FullName = "Test Seller",
            PhoneNumber = $"09{new Random().Next(10000000, 99999999)}",
            HealthScore = 100,
            EmailConfirmed = true
        };
        db.Users.Add(user);

        var activeAuction = Auction.CreateDraft(
            user.Id,
            "Active Auction",
            category.Id,
            1000m,
            10m,
            null,
            null,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2)
        ).Value;

        var draftAuction = Auction.CreateDraft(
            user.Id,
            "Draft Auction",
            category.Id,
            1000m,
            10m,
            null,
            null,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2)
        ).Value;

        db.Auctions.Add(activeAuction);
        db.Auctions.Add(draftAuction);

        db.AuctionMedia.Add(AuctionMedia.Create(activeAuction.Id, "http://image1.jpg", 1));
        db.AuctionMedia.Add(AuctionMedia.Create(activeAuction.Id, "http://image2.jpg", 2));

        await db.SaveChangesAsync();

        // Force active
        await ((DbContext)db).Database.ExecuteSqlRawAsync($"UPDATE Auctions SET Status = {(int)AuctionStatus.Active} WHERE Id = '{activeAuction.Id}'");

        return (activeAuction.Id, draftAuction.Id);
    }

    [Fact]
    public async Task GetAuctions_ReturnsActiveOnly_Paginated()
    {
        await SetupAuctions();
        
        var response = await _client.GetAsync("/api/auctions");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<AuctionDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
        result.Items.Should().OnlyContain(a => a.Status == "Active");
        
        // Ensure names are fetched
        var active = result.Items.First(a => a.Title == "Active Auction");
        active.CategoryName.Should().Be("Test Category");
        active.SellerName.Should().Be("Test Seller");
        active.MainImageUrl.Should().Be("http://image1.jpg");
    }

    [Fact]
    public async Task GetAuctionById_Success_ReturnsDetail()
    {
        var (activeId, _) = await SetupAuctions();
        
        var response = await _client.GetAsync($"/api/auctions/{activeId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await response.Content.ReadFromJsonAsync<AuctionDetailDto>();
        detail.Should().NotBeNull();
        detail!.Id.Should().Be(activeId);
        detail.Title.Should().Be("Active Auction");
        detail.SellerName.Should().Be("Test Seller");
        detail.CategoryName.Should().Be("Test Category");
        detail.MediaUrls.Should().HaveCount(2);
        detail.MediaUrls[0].Should().Be("http://image1.jpg");
    }

    [Fact]
    public async Task GetAuctionById_NotFound()
    {
        var response = await _client.GetAsync($"/api/auctions/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

public class AuctionDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal StartPrice { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string MainImageUrl { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string SellerName { get; set; } = string.Empty;
}

public class AuctionDetailDto : AuctionDto
{
    public decimal StepPrice { get; set; }
    public decimal? ReservePrice { get; set; }
    public decimal? BuyNowPrice { get; set; }
    public List<string> MediaUrls { get; set; } = new();
}
