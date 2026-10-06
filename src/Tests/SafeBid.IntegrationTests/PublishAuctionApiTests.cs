using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using SafeBid.Application;
using SafeBid.Domain;

namespace SafeBid.IntegrationTests;

[Collection("IntegrationTests")]
public class PublishAuctionApiTests : IAsyncLifetime
{
    private readonly ApiTestFixture _fixture;
    private Guid _categoryId;

    public PublishAuctionApiTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        
        var category = Category.Create("Test Category");
        db.Categories.Add(category);
        _categoryId = category.Id;
        
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(HttpClient Client, User User, Wallet Wallet, Auction Auction, string Cookie)> SetupAuctionAndLogin(decimal walletBalance, decimal startPrice, decimal? reservePrice, AuctionStatus status = AuctionStatus.Draft)
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        
        var password = "TestPassword123!";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"test_publish_{Guid.NewGuid()}@safebid.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FullName = "Test Publisher",
            PhoneNumber = $"09{new Random().Next(10000000, 99999999)}",
            HealthScore = 100,
            EmailConfirmed = true
        };
        db.Users.Add(user);

        var wallet = new Wallet
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            AvailableBalance = walletBalance,
            HoldAmount = 0
        };
        db.Wallets.Add(wallet);

        var auction = Auction.CreateDraft(
            user.Id,
            "Test Publish Auction",
            _categoryId,
            startPrice,
            10m,
            reservePrice,
            null,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2)
        ).Value;

        if (status != AuctionStatus.Draft)
        {
            // Reflection or directly modifying status if possible. 
            // In C# it's a private set, so we can't change it directly without a method.
            // Let's assume we can change it via DbContext.
        }

        db.Auctions.Add(auction);

        var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var uploadedUrl = await storageService.UploadFileAsync(stream, $"test_{Guid.NewGuid()}.jpg", "image/jpeg", CancellationToken.None);
        
        var media = AuctionMedia.Create(auction.Id, uploadedUrl, 1);
        db.AuctionMedia.Add(media);

        await db.SaveChangesAsync();

        if (status != AuctionStatus.Draft)
        {
            // Force change status using raw SQL
            await ((DbContext)db).Database.ExecuteSqlRawAsync($"UPDATE Auctions SET Status = {(int)status} WHERE Id = '{auction.Id}'");
        }

        var loginClient = _fixture.CreateClient();
        var loginResponse = await loginClient.PostAsJsonAsync("/api/auth/login", new
        {
            email = user.Email,
            password = password
        });
        
        var setCookieHeaders = loginResponse.Headers.GetValues("Set-Cookie").ToList();
        var cookie = setCookieHeaders.First().Split(';')[0];
        
        var client = _fixture.CreateClient();
        return (client, user, wallet, auction, cookie);
    }

    [Fact]
    public async Task Publish_Success_Returns200_And_DeductsFee()
    {
        // Max(20k, 2% of ReservePrice). ReservePrice = 2000000 => 2% = 40000
        var (client, user, wallet, auction, cookie) = await SetupAuctionAndLogin(100000m, 1000000m, 2000000m);
        
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/auctions/{auction.Id}/publish");
        request.Headers.Add("Cookie", cookie);

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        
        var updatedWallet = await db.Wallets.FirstAsync(w => w.Id == wallet.Id);
        updatedWallet.AvailableBalance.Should().Be(60000m); // 100000 - 40000

        var updatedAuction = await db.Auctions.FirstAsync(a => a.Id == auction.Id);
        updatedAuction.Status.Should().Be(AuctionStatus.Active);

        var ledger = await db.LedgerEntries.FirstOrDefaultAsync(l => l.WalletId == wallet.Id && l.Type == "LISTING_FEE");
        ledger.Should().NotBeNull();
        ledger!.Amount.Should().Be(-40000m);
        
        var media = await db.AuctionMedia.FirstAsync(m => m.AuctionId == auction.Id);
        media.MediaUrl.Should().Contain("/auction-media/");
    }

    [Fact]
    public async Task Publish_InsufficientFunds_Returns400()
    {
        // Fee = 40000, Balance = 30000
        var (client, user, wallet, auction, cookie) = await SetupAuctionAndLogin(30000m, 1000000m, 2000000m);
        
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/auctions/{auction.Id}/publish");
        request.Headers.Add("Cookie", cookie);

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        
        var updatedWallet = await db.Wallets.FirstAsync(w => w.Id == wallet.Id);
        updatedWallet.AvailableBalance.Should().Be(30000m); // No change

        var updatedAuction = await db.Auctions.FirstAsync(a => a.Id == auction.Id);
        updatedAuction.Status.Should().Be(AuctionStatus.Draft); // No change
    }

    [Fact]
    public async Task Publish_InvalidState_Returns400()
    {
        var (client, user, wallet, auction, cookie) = await SetupAuctionAndLogin(100000m, 100000m, null, AuctionStatus.Active);
        
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/auctions/{auction.Id}/publish");
        request.Headers.Add("Cookie", cookie);

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Publish_UnauthorizedUser_Returns403()
    {
        var (client, user, wallet, auction, _) = await SetupAuctionAndLogin(100000m, 100000m, null);
        
        // Create a different user and login
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        
        var password = "TestPassword123!";
        var anotherUser = new User
        {
            Id = Guid.NewGuid(),
            Email = $"hacker_{Guid.NewGuid()}@safebid.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FullName = "Hacker",
            PhoneNumber = $"09{new Random().Next(10000000, 99999999)}",
            HealthScore = 100,
            EmailConfirmed = true
        };
        db.Users.Add(anotherUser);
        await db.SaveChangesAsync();

        var loginClient = _fixture.CreateClient();
        var loginResponse = await loginClient.PostAsJsonAsync("/api/auth/login", new
        {
            email = anotherUser.Email,
            password = password
        });
        
        var setCookieHeaders = loginResponse.Headers.GetValues("Set-Cookie").ToList();
        var cookie = setCookieHeaders.First().Split(';')[0];
        
        var hackerClient = _fixture.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/auctions/{auction.Id}/publish");
        request.Headers.Add("Cookie", cookie);

        var response = await hackerClient.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
