using System.Net.Http.Json;
using FluentAssertions;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using SafeBid.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace SafeBid.IntegrationTests;

[Collection("IntegrationTests")]
public class AuthRegisterTests
{
    private readonly ApiTestFixture _factory;
    private readonly HttpClient _client;

    public AuthRegisterTests(ApiTestFixture factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ValidRequest_ShouldReturn201_And_InitDefaultReputation_And_Wallet()
    {
        // Arrange
        var request = new
        {
            email = "newuser@example.com",
            password = "Password123!",
            fullName = "New User",
            phoneNumber = "0123456789",
            cccd = "001122334455"
        };

        // Act
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register");
        requestMessage.Headers.Add("X-Forwarded-For", "192.168.1.1");
        requestMessage.Content = JsonContent.Create(request);
        var response = await _client.SendAsync(requestMessage);

        // Assert HTTP
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Assert DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.email);
        user.Should().NotBeNull();
        user!.HealthScore.Should().Be(100);
        user.BuyerTier.Should().Be("Bronze");
        user.SellerTier.Should().Be("Bronze");

        var wallet = await db.Wallets.FirstOrDefaultAsync(w => w.UserId == user.Id);
        wallet.Should().NotBeNull();
        wallet!.AvailableBalance.Should().Be(0);
        wallet.HoldAmount.Should().Be(0);
        wallet.IsConfiscated.Should().BeFalse();
        wallet.RowVersion.Should().NotBeNull();
    }

    [Fact]
    public async Task Register_DuplicateEmail_ShouldReturn400()
    {
        // Arrange
        var request = new
        {
            email = "duplicate@example.com",
            password = "Password123!",
            fullName = "Duplicate User",
            phoneNumber = "0123456789",
            cccd = "001122334455"
        };
        var req1 = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register");
        req1.Headers.Add("X-Forwarded-For", "192.168.1.2");
        req1.Content = JsonContent.Create(request);
        await _client.SendAsync(req1); // First request should succeed

        // Act
        var req2 = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register");
        req2.Headers.Add("X-Forwarded-For", "192.168.1.2");
        req2.Content = JsonContent.Create(request);
        var response = await _client.SendAsync(req2); // Second request

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_Spam_ShouldTriggerRateLimit_Return429()
    {
        // Arrange
        var request = new
        {
            email = "spam@example.com",
            password = "Password123!",
            fullName = "Spam User",
            phoneNumber = "0123456789",
            cccd = "001122334455"
        };

        // Act & Assert
        for (int i = 0; i < 5; i++)
        {
            request = request with { email = $"spam{i}@example.com" };
            var req = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register");
            req.Headers.Add("X-Forwarded-For", "192.168.1.3");
            req.Content = JsonContent.Create(request);
            var res = await _client.SendAsync(req);
            if (res.StatusCode == HttpStatusCode.TooManyRequests) 
            {
                // In case rate limit is already triggered by other tests running in parallel
                return;
            }
        }

        // The 6th request should be rate limited
        request = request with { email = $"spam6@example.com" };
        var finalReq = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register");
        finalReq.Headers.Add("X-Forwarded-For", "192.168.1.3");
        finalReq.Content = JsonContent.Create(request);
        var finalResponse = await _client.SendAsync(finalReq);
        
        finalResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}
