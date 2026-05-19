namespace RealWorldApi.IntegrationTests;

[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<IntegrationTestContainerFixture>
{
    public const string Name = "Integration tests";
}
