using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using StackExchange.Redis;

namespace RealWorldApi.UnitTests;

public sealed class RedisTestFixture : IAsyncLifetime
{
    private readonly IContainer _redisContainer = new ContainerBuilder("redis:7-alpine")
        .WithPortBinding(6379, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(6379))
        .WithCleanUp(true)
        .Build();

    private ConnectionMultiplexer _connection = null!;

    public IDatabase Database => _connection.GetDatabase();

    public async Task InitializeAsync()
    {
        await _redisContainer.StartAsync();

        var connectionString = $"{_redisContainer.Hostname}:{_redisContainer.GetMappedPublicPort(6379)},abortConnect=false";
        _connection = await ConnectionMultiplexer.ConnectAsync(connectionString);
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
        await _redisContainer.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class RedisTestCollection : ICollectionFixture<RedisTestFixture>
{
    public const string Name = "Redis unit tests";
}
