using Xunit;

namespace SafeBid.IntegrationTests;

[CollectionDefinition("IntegrationTests")]
public class SharedTestCollection : ICollectionFixture<ApiTestFixture>
{
}
