using Microsoft.EntityFrameworkCore;
using RealWorldApi.Infrastructure.Data;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Testcontainers.PostgreSql;

namespace RealWorldApi.IntegrationTests;

public sealed class IntegrationTestContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres:15-alpine")
        .WithDatabase("realworld-dotnet")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .WithCleanUp(true)
        .Build();

    private readonly IContainer _redisContainer = new ContainerBuilder("redis:7-alpine")
        .WithPortBinding(6379, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(6379))
        .WithCleanUp(true)
        .Build();

    public string PostgresConnectionString => _postgresContainer.GetConnectionString();
    public string RedisConnectionString =>
        $"{_redisContainer.Hostname}:{_redisContainer.GetMappedPublicPort(6379)},abortConnect=false";

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _postgresContainer.StartAsync(),
            _redisContainer.StartAsync());

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(PostgresConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        await using var context = new AppDbContext(options);
        await context.Database.MigrateAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(PostgresConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        await using var context = new AppDbContext(options);

        var tables = context.Model.GetEntityTypes()
            .Select(entityType => new
            {
                Schema = entityType.GetSchema(),
                Table = entityType.GetTableName()
            })
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Table))
            .Distinct()
            .ToList();

        if (tables.Count == 0) return;

        var tableList = string.Join(", ", tables.Select(entry =>
        {
            if (string.IsNullOrWhiteSpace(entry.Schema))
            {
                return $"\"{entry.Table}\"";
            }

            return $"\"{entry.Schema}\".\"{entry.Table}\"";
        }));

        var sql = $"TRUNCATE TABLE {tableList} RESTART IDENTITY CASCADE;";
        await context.Database.ExecuteSqlRawAsync(sql);
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
        await _redisContainer.DisposeAsync();
    }
}
