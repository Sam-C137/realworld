using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RealWorldApi.Core.Workers;
using RealWorldApi.Infrastructure.Data;
using RealWorldApi.Infrastructure.Data.Models;

namespace RealWorldApi.UnitTests;

[Collection(PostgresTestCollection.Name)]
public sealed class SessionCleanupServiceTests(PostgresTestFixture postgresFixture) : IAsyncLifetime
{
    public Task InitializeAsync() => postgresFixture.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task DeleteStaleSessionsAsync_RemovesExpiredAndRevokedSessionsOlderThanCutoff()
    {
        var now = DateTime.UtcNow;
        await using var db = postgresFixture.CreateDbContext();
        var user = CreateUser();
        var staleExpired = CreateSession(user, expiresAt: now.AddDays(-8));
        var staleRevoked = CreateSession(user, expiresAt: now.AddDays(20), isRevoked: true, revokedAt: now.AddDays(-8));
        var freshExpired = CreateSession(user, expiresAt: now.AddDays(-6).AddHours(-23));
        var active = CreateSession(user, expiresAt: now.AddDays(20));

        db.Users.Add(user);
        db.UserSessions.AddRange(staleExpired, staleRevoked, freshExpired, active);
        await db.SaveChangesAsync();

        var deletedCount = await new SessionCleanupService(db).DeleteStaleSessionsAsync(now);

        Assert.Equal(2, deletedCount);
        var remainingIds = await db.UserSessions.Select(session => session.Id).ToListAsync();
        Assert.DoesNotContain(staleExpired.Id, remainingIds);
        Assert.DoesNotContain(staleRevoked.Id, remainingIds);
        Assert.Contains(freshExpired.Id, remainingIds);
        Assert.Contains(active.Id, remainingIds);
    }

    [Fact]
    public async Task DeleteStaleSessionsAsync_PreservesSessionsAtCutoffAndRevokedSessionsWithoutRevokedAt()
    {
        var now = DateTime.UtcNow;
        await using var db = postgresFixture.CreateDbContext();
        var user = CreateUser();
        var expiresAtCutoff = CreateSession(user, expiresAt: now.AddDays(-7));
        var revokedAtCutoff = CreateSession(user, expiresAt: now.AddDays(20), isRevoked: true, revokedAt: now.AddDays(-7));
        var revokedWithoutTimestamp = CreateSession(user, expiresAt: now.AddDays(20), isRevoked: true);

        db.Users.Add(user);
        db.UserSessions.AddRange(expiresAtCutoff, revokedAtCutoff, revokedWithoutTimestamp);
        await db.SaveChangesAsync();

        var deletedCount = await new SessionCleanupService(db).DeleteStaleSessionsAsync(now);

        Assert.Equal(0, deletedCount);
        Assert.Equal(3, await db.UserSessions.CountAsync());
    }

    [Fact]
    public void SessionCleanupWorker_CanBeConstructedByDependencyInjectionWithScopedDbContext()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(postgresFixture.ConnectionString).UseSnakeCaseNamingConvention());
        services.AddScoped<SessionCleanupService>();
        services.AddHostedService<SessionCleanupWorker>();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        var hostedServices = provider.GetServices<IHostedService>();

        Assert.Contains(hostedServices, service => service is SessionCleanupWorker);
    }
    private static User CreateUser() =>
        new()
        {
            Id = Guid.NewGuid(),
            Email = $"cleanup-{Guid.NewGuid():N}@example.com",
            PasswordHash = "hash"
        };

    private static UserSession CreateSession(
        User user,
        DateTime expiresAt,
        bool isRevoked = false,
        DateTime? revokedAt = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            RefreshTokenHash = Guid.NewGuid().ToString("N"),
            IsRevoked = isRevoked,
            RevokedAt = revokedAt,
            ExpiresAt = expiresAt,
            IpAddress = "127.0.0.1",
            UserAgent = "unit-test"
        };
}
