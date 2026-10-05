using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SafeBid.Infrastructure;
using Microsoft.EntityFrameworkCore;
using SafeBid.Domain;

namespace SafeBid.IntegrationTests;

[Collection("IntegrationTests")]
public class AuthLoginTests : IAsyncLifetime
{
    private readonly ApiTestFixture _factory;
    private readonly HttpClient _client;

    public AuthLoginTests(ApiTestFixture factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        if (!await db.Users.AnyAsync(u => u.Email == "loginuser@example.com"))
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "loginuser@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("LoginPass123!"),
                FullName = "Login User",
                PhoneNumber = "0987654321",
                EmailConfirmed = true,
                HealthScore = 100,
                BuyerTier = "Bronze",
                SellerTier = "Bronze",
                SevereViolationCount = 0,
                IsBanned = false,
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(user);
            
            var wallet = new Wallet 
            { 
                Id = Guid.NewGuid(), 
                UserId = user.Id, 
                AvailableBalance = 0, 
                HoldAmount = 0 
            };
            db.Wallets.Add(wallet);
            
            await db.SaveChangesAsync();
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Login_ValidCredentials_ShouldReturn200_And_SetHttpOnlyCookie()
    {
        // Arrange
        var request = new { email = "loginuser@example.com", password = "LoginPass123!" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var setCookieHeaders = response.Headers.GetValues("Set-Cookie").ToList();
        setCookieHeaders.Should().ContainSingle();
        var cookie = setCookieHeaders.First();
        
        cookie.Should().Contain("jwt=");
        cookie.Should().ContainEquivalentOf("HttpOnly");
        cookie.Should().ContainEquivalentOf("SameSite=Lax");
        
        // Ensure that using the returned cookie we can fetch /api/users/me
        var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        meRequest.Headers.Add("Cookie", cookie.Split(';')[0]); // Add the jwt=... part
        
        var meResponse = await _client.SendAsync(meRequest);
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var userContent = await meResponse.Content.ReadFromJsonAsync<UserDto>();
        userContent.Should().NotBeNull();
        userContent!.Email.Should().Be("loginuser@example.com");
    }

    [Fact]
    public async Task Login_InvalidPassword_ShouldReturn401()
    {
        // Arrange
        var request = new { email = "loginuser@example.com", password = "WrongPassword!" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    
    [Fact]
    public async Task GetMe_WithoutCookie_ShouldReturn401()
    {
        // Act
        var response = await _client.GetAsync("/api/users/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private record UserDto(Guid Id, string Email, string FullName);
}
