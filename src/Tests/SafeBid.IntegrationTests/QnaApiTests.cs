using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace SafeBid.IntegrationTests;

[Collection("IntegrationTests")]
public class QnaApiTests : IAsyncLifetime
{
    private readonly ApiTestFixture _fixture;
    private readonly HttpClient _client;
    private string _buyerToken = default!;
    private string _sellerToken = default!;
    private Guid _buyerId;
    private Guid _sellerId;
    private Guid _auctionId;
    private Guid _questionId;

    public QnaApiTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SafeBid.Infrastructure.AppDbContext>();

        // Clear DB
        db.Users.RemoveRange(db.Users);
        db.Categories.RemoveRange(db.Categories);
        db.Auctions.RemoveRange(db.Auctions);
        await db.SaveChangesAsync();

        // Create Seller
        _sellerId = Guid.NewGuid();
        var seller = new SafeBid.Domain.User
        {
            Id = _sellerId,
            Email = "seller_qna@safebid.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            FullName = "Nguyễn Văn Seller",
            PhoneNumber = "0988000000",
            HealthScore = 100,
            EmailConfirmed = true
        };
        db.Users.Add(seller);

        // Create Buyer
        _buyerId = Guid.NewGuid();
        var buyer = new SafeBid.Domain.User
        {
            Id = _buyerId,
            Email = "buyer_qna@safebid.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            FullName = "Nguyễn Văn Buyer",
            PhoneNumber = "0977000000",
            HealthScore = 100,
            EmailConfirmed = true
        };
        db.Users.Add(buyer);

        // Create Category
        var category = SafeBid.Domain.Category.Create("Tech");
        db.Categories.Add(category);

        // Create Auction
        var startTime = DateTime.UtcNow.AddMinutes(5);
        var endTime = startTime.AddHours(2);
        var auctionResult = SafeBid.Domain.Auction.CreateDraft(
            _sellerId, "IPhone 15", category.Id, 10000, 5000, 50000, 200000, 
            startTime, endTime);
        var auction = auctionResult.Value!;
        auction.Publish(); // Make ACTIVE
        _auctionId = auction.Id;
        db.Auctions.Add(auction);
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task LoginAsBuyer()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { Email = "buyer_qna@safebid.com", Password = "123456" });
        var cookie = loginResponse.Headers.GetValues("Set-Cookie").FirstOrDefault();
        if (cookie != null) _client.DefaultRequestHeaders.Add("Cookie", cookie.Split(';')[0]);
    }

    private async Task LoginAsSeller()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { Email = "seller_qna@safebid.com", Password = "123456" });
        var cookie = loginResponse.Headers.GetValues("Set-Cookie").FirstOrDefault();
        if (cookie != null) _client.DefaultRequestHeaders.Add("Cookie", cookie.Split(';')[0]);
    }

    [Fact]
    public async Task AddQuestion_Returns201()
    {
        await LoginAsBuyer();
        var response = await _client.PostAsJsonAsync($"/api/auctions/{_auctionId}/questions", new { Content = "Sản phẩm xước xát gì không?" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task GetQuestions_ReturnsMaskedNames()
    {
        // First we add one question
        await LoginAsBuyer();
        await _client.PostAsJsonAsync($"/api/auctions/{_auctionId}/questions", new { Content = "Test Question?" });

        // Get questions
        var response = await _client.GetAsync($"/api/auctions/{_auctionId}/questions");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var items = json.GetProperty("items").EnumerateArray().ToList();
        
        Assert.NotEmpty(items);
        var q = items.First();
        Assert.Equal("Nguyễn B***", q.GetProperty("maskedAskerName").GetString());
        Assert.Equal("Test Question?", q.GetProperty("content").GetString());
    }

    [Fact]
    public async Task AnswerQuestion_Forbidden_IfNotSeller()
    {
        // Buyer adds a question
        await LoginAsBuyer();
        var qRes = await _client.PostAsJsonAsync($"/api/auctions/{_auctionId}/questions", new { Content = "Test Question?" });
        var qJson = await qRes.Content.ReadFromJsonAsync<JsonElement>();
        var qId = qJson.GetProperty("id").GetString();

        // Buyer tries to answer it
        var ansRes = await _client.PostAsJsonAsync($"/api/auctions/{_auctionId}/questions/{qId}/answer", new { Answer = "Hàng like new nhé" });
        Assert.Equal(HttpStatusCode.Forbidden, ansRes.StatusCode);
    }

    [Fact]
    public async Task AnswerQuestion_Success_IfSeller()
    {
        // Buyer adds a question
        await LoginAsBuyer();
        var qRes = await _client.PostAsJsonAsync($"/api/auctions/{_auctionId}/questions", new { Content = "Test Question?" });
        var qJson = await qRes.Content.ReadFromJsonAsync<JsonElement>();
        var qId = qJson.GetProperty("id").GetString();

        // Login as seller and clear headers
        _client.DefaultRequestHeaders.Remove("Cookie");
        await LoginAsSeller();

        // Seller answers
        var ansRes = await _client.PostAsJsonAsync($"/api/auctions/{_auctionId}/questions/{qId}/answer", new { Answer = "Hàng like new nhé" });
        Assert.Equal(HttpStatusCode.OK, ansRes.StatusCode);

        // Check if answer is visible
        var getRes = await _client.GetAsync($"/api/auctions/{_auctionId}/questions");
        var json = await getRes.Content.ReadFromJsonAsync<JsonElement>();
        var item = json.GetProperty("items").EnumerateArray().First(x => x.GetProperty("id").GetString() == qId);
        
        Assert.Equal("Hàng like new nhé", item.GetProperty("answer").GetString());
    }

    [Fact]
    public async Task AddQuestion_RateLimitExceeded()
    {
        await LoginAsBuyer();

        HttpResponseMessage lastResponse = null!;
        for (int i = 0; i < 11; i++)
        {
            lastResponse = await _client.PostAsJsonAsync($"/api/auctions/{_auctionId}/questions", new { Content = $"Spam {i}" });
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, lastResponse.StatusCode);
    }
}
