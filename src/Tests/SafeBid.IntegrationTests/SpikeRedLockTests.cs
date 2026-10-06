using System.Text.Json.Nodes;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Testcontainers.MsSql;
using Testcontainers.Minio;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace SafeBid.IntegrationTests;

public class ApiTestFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly IContainer _redisContainer = new ContainerBuilder()
        .WithImage("redis:7-alpine")
        .WithPortBinding(6379, true)
        .Build();

    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    private readonly MinioContainer _minioContainer = new MinioBuilder()
        .WithImage("elestio/minio:latest")
        .WithUsername("admin")
        .WithPassword("password123")
        .Build();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_redisContainer.StartAsync(), _dbContainer.StartAsync(), _minioContainer.StartAsync());
        Environment.SetEnvironmentVariable("REDIS_CONNECTION", $"{_redisContainer.Hostname}:{_redisContainer.GetMappedPublicPort(6379)}");
        Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", _dbContainer.GetConnectionString());
        Environment.SetEnvironmentVariable("MINIO_ENDPOINT", $"{_minioContainer.Hostname}:{_minioContainer.GetMappedPublicPort(9000)}");
        Environment.SetEnvironmentVariable("MINIO_ACCESS_KEY", "admin");
        Environment.SetEnvironmentVariable("MINIO_SECRET_KEY", "password123");
    }

    public new async Task DisposeAsync()
    {
        await Task.WhenAll(_redisContainer.DisposeAsync().AsTask(), _dbContainer.DisposeAsync().AsTask(), _minioContainer.DisposeAsync().AsTask());
    }
}

[Collection("IntegrationTests")]
public class SpikeRedLockTests
{
    private readonly HttpClient _client;

    public SpikeRedLockTests(ApiTestFixture fixture)
    {
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task SequentialRequests_ShouldIncrementProperly()
    {
        // Arrange
        // We will reset the counter to 0 just in case
        await _client.DeleteAsync("/api/spikes/redlock");

        // Act
        for (int i = 0; i < 5; i++)
        {
            var response = await _client.PostAsync("/api/spikes/redlock", null);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonNode.Parse(content);
            var count = (int)json!["count"]!;
            count.Should().Be(i + 1);
        }
    }

    [Fact]
    public async Task ConcurrentRequests_ShouldNotSufferFromRaceCondition()
    {
        // Arrange
        await _client.DeleteAsync("/api/spikes/redlock");
        var numRequests = 100;
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act
        for (int i = 0; i < numRequests; i++)
        {
            tasks.Add(_client.PostAsync("/api/spikes/redlock", null));
        }

        await Task.WhenAll(tasks);

        // Assert
        var checkResponse = await _client.GetAsync("/api/spikes/redlock");
        var content = await checkResponse.Content.ReadAsStringAsync();
        var json = JsonNode.Parse(content);
        var finalCount = (int)json!["count"]!;
        
        finalCount.Should().Be(numRequests);
    }
}
