using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using MediatR;
using SafeBid.Application;
using SafeBid.Domain;

namespace SafeBid.IntegrationTests;

[Collection("IntegrationTests")]
public class CategoryApiTests : IAsyncLifetime
{
    private readonly ApiTestFixture _fixture;
    private readonly HttpClient _client;

    public CategoryApiTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetCategories_ReturnsTreeStructure()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Create categories
        var catA = await mediator.Send(new CreateCategoryCommand("A", null));
        var catB = await mediator.Send(new CreateCategoryCommand("B", catA.Value));
        var catC = await mediator.Send(new CreateCategoryCommand("C", catB.Value));

        // Act
        var response = await _client.GetAsync("/api/categories");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var categories = await response.Content.ReadFromJsonAsync<List<CategoryNode>>();
        categories.Should().NotBeNull();
        
        var a = categories!.FirstOrDefault(x => x.Id == catA.Value);
        a.Should().NotBeNull();
        a!.Children.Should().HaveCount(1);
        
        var b = a.Children.First();
        b.Id.Should().Be(catB.Value);
        b.Children.Should().HaveCount(1);

        var c = b.Children.First();
        c.Id.Should().Be(catC.Value);
    }

    [Fact]
    public async Task CreateCategory_WithCircularReference_ReturnsFailure()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var catX = await mediator.Send(new CreateCategoryCommand("X", null));
        var catY = await mediator.Send(new CreateCategoryCommand("Y", catX.Value));

        // Act
        var result = await mediator.Send(new UpdateCategoryParentCommand(catX.Value, catY.Value));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Category.CircularReference");
    }
}

public class CategoryNode
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public List<CategoryNode> Children { get; set; } = new();
}
