using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SafeBid.Domain;
using SafeBid.Infrastructure;

namespace SafeBid.IntegrationTests;

[Collection("IntegrationTests")]
public class MediaUploadTests
{
    private readonly ApiTestFixture _factory;
    private readonly HttpClient _client;

    public MediaUploadTests(ApiTestFixture factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> SetupUserAndLogin()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var password = "TestPassword123!";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"media_test_{Guid.NewGuid()}@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FullName = "Media Test User",
            PhoneNumber = $"09{Guid.NewGuid().ToString().Substring(0, 8)}",
            EmailConfirmed = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = user.Email,
            password = password
        });
        
        var setCookieHeaders = loginResponse.Headers.GetValues("Set-Cookie").ToList();
        return setCookieHeaders.First().Split(';')[0];
    }

    [Fact]
    public async Task UploadMedia_ValidImage_Returns200AndUrl()
    {
        var cookie = await SetupUserAndLogin();
        
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[1024 * 10]); // 10KB
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(fileContent, "file", "test-image.jpg");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/media/upload");
        request.Headers.Add("Cookie", cookie);
        request.Content = content;

        var response = await _client.SendAsync(request);
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("url").GetString().Should().StartWith("http");
        json.GetProperty("url").GetString().Should().Contain("temp-media");
    }

    [Fact]
    public async Task UploadMedia_ExeFile_Returns400()
    {
        var cookie = await SetupUserAndLogin();
        
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[1024]);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-msdownload");
        content.Add(fileContent, "file", "virus.exe");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/media/upload");
        request.Headers.Add("Cookie", cookie);
        request.Content = content;

        var response = await _client.SendAsync(request);
        
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadMedia_Exceeds5MB_Returns400()
    {
        var cookie = await SetupUserAndLogin();
        
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[1024 * 1024 * 6]); // 6MB
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
        content.Add(fileContent, "file", "large.png");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/media/upload");
        request.Headers.Add("Cookie", cookie);
        request.Content = content;

        var response = await _client.SendAsync(request);
        
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadMedia_NoCookie_Returns401()
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[1024]);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
        content.Add(fileContent, "file", "test.png");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/media/upload");
        request.Content = content;

        var response = await _client.SendAsync(request);
        
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
