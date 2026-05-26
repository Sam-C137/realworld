using Microsoft.EntityFrameworkCore;
using RealWorldApi.Infrastructure.Data;
using Testcontainers.PostgreSql;

namespace RealWorldApi.UnitTests;

public sealed class PostgresTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres:15-alpine")
        .WithDatabase("realworld-unit-tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .WithCleanUp(true)
        .Build();

    public string ConnectionString => _postgresContainer.GetConnectionString();

    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AppDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        await using var context = CreateDbContext();
        await context.Database.MigrateAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        await using var context = CreateDbContext();

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

        await context.Database.ExecuteSqlRawAsync($"TRUNCATE TABLE {tableList} RESTART IDENTITY CASCADE;");
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresTestCollection : ICollectionFixture<PostgresTestFixture>
{
    public const string Name = "Postgres unit tests";
}
