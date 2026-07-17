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
    public async Task SessionCleanupService_RemovesExpiredAndRevokedSessionsOlderThanCutoff()
    {
        var timeProvider = new ManualTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var cleanupTime = timeProvider.GetUtcNow().UtcDateTime.Date.AddDays(1);
        await using var db = postgresFixture.CreateDbContext();
        var user = CreateUser();
        var staleExpired = CreateSession(user, expiresAt: cleanupTime.AddDays(-8));
        var staleRevoked = CreateSession(user, expiresAt: cleanupTime.AddDays(20), isRevoked: true, revokedAt: cleanupTime.AddDays(-8));
        var freshExpired = CreateSession(user, expiresAt: cleanupTime.AddDays(-6).AddHours(-23));
        var active = CreateSession(user, expiresAt: cleanupTime.AddDays(20));

        db.Users.Add(user);
        db.UserSessions.AddRange(staleExpired, staleRevoked, freshExpired, active);
        await db.SaveChangesAsync();

        await using var provider = CreateServiceProvider(timeProvider);
        var service = provider.GetServices<IHostedService>().OfType<SessionCleanupService>().Single();
        await service.StartAsync(CancellationToken.None);
        await WaitForAsync(() => Task.FromResult(timeProvider.HasTimers));
        timeProvider.AdvanceTo(cleanupTime);
        await WaitForAsync(async () => await db.UserSessions.CountAsync() == 2);
        await service.StopAsync(CancellationToken.None);

        var remainingIds = await db.UserSessions.Select(session => session.Id).ToListAsync();
        Assert.DoesNotContain(staleExpired.Id, remainingIds);
        Assert.DoesNotContain(staleRevoked.Id, remainingIds);
        Assert.Contains(freshExpired.Id, remainingIds);
        Assert.Contains(active.Id, remainingIds);
    }

    [Fact]
    public async Task SessionCleanupService_PreservesSessionsAtCutoffAndRevokedSessionsWithoutRevokedAt()
    {
        var timeProvider = new ManualTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var cleanupTime = timeProvider.GetUtcNow().UtcDateTime.Date.AddDays(1);
        await using var db = postgresFixture.CreateDbContext();
        var user = CreateUser();
        var staleExpired = CreateSession(user, expiresAt: cleanupTime.AddDays(-8));
        var expiresAtCutoff = CreateSession(user, expiresAt: cleanupTime.AddDays(-7));
        var revokedAtCutoff = CreateSession(user, expiresAt: cleanupTime.AddDays(20), isRevoked: true, revokedAt: cleanupTime.AddDays(-7));
        var revokedWithoutTimestamp = CreateSession(user, expiresAt: cleanupTime.AddDays(20), isRevoked: true);

        db.Users.Add(user);
        db.UserSessions.AddRange(staleExpired, expiresAtCutoff, revokedAtCutoff, revokedWithoutTimestamp);
        await db.SaveChangesAsync();

        await using var provider = CreateServiceProvider(timeProvider);
        var service = provider.GetServices<IHostedService>().OfType<SessionCleanupService>().Single();
        await service.StartAsync(CancellationToken.None);
        await WaitForAsync(() => Task.FromResult(timeProvider.HasTimers));
        timeProvider.AdvanceTo(cleanupTime);
        await WaitForAsync(async () => await db.UserSessions.CountAsync() == 3);
        await service.StopAsync(CancellationToken.None);

        var remainingIds = await db.UserSessions.Select(session => session.Id).ToListAsync();
        Assert.DoesNotContain(staleExpired.Id, remainingIds);
        Assert.Contains(expiresAtCutoff.Id, remainingIds);
        Assert.Contains(revokedAtCutoff.Id, remainingIds);
        Assert.Contains(revokedWithoutTimestamp.Id, remainingIds);
    }

    [Fact]
    public void SessionCleanupService_CanBeConstructedByDependencyInjectionWithScopedDbContext()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(postgresFixture.ConnectionString).UseSnakeCaseNamingConvention());
        services.AddHostedService<SessionCleanupService>();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        var hostedServices = provider.GetServices<IHostedService>();

        Assert.Contains(hostedServices, service => service is SessionCleanupService);
    }

    private ServiceProvider CreateServiceProvider(TimeProvider timeProvider)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<TimeProvider>(timeProvider);
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(postgresFixture.ConnectionString).UseSnakeCaseNamingConvention());
        services.AddHostedService<SessionCleanupService>();

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
    }

    private static async Task WaitForAsync(Func<Task<bool>> condition)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(20);
        }

        Assert.True(await condition());
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

    private sealed class ManualTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private readonly object _lock = new();
        private readonly List<ManualTimer> _timers = [];
        private DateTimeOffset _utcNow = utcNow;

        public bool HasTimers
        {
            get
            {
                lock (_lock)
                {
                    return _timers.Count > 0;
                }
            }
        }

        public override DateTimeOffset GetUtcNow()
        {
            lock (_lock)
            {
                return _utcNow;
            }
        }

        public void AdvanceTo(DateTime utcNow)
        {
            List<ManualTimer> timers;

            lock (_lock)
            {
                _utcNow = new DateTimeOffset(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc));
                timers = _timers.ToList();
            }

            foreach (var timer in timers)
            {
                timer.TryInvoke(_utcNow);
            }
        }

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new ManualTimer(this, callback, state, dueTime, period);

            lock (_lock)
            {
                _timers.Add(timer);
            }

            return timer;
        }

        private sealed class ManualTimer : ITimer
        {
            private readonly ManualTimeProvider _timeProvider;
            private readonly TimerCallback _callback;
            private readonly object? _state;
            private TimeSpan _period;
            private DateTimeOffset _dueAt;
            private bool _disposed;

            public ManualTimer(
                ManualTimeProvider timeProvider,
                TimerCallback callback,
                object? state,
                TimeSpan dueTime,
                TimeSpan period)
            {
                _timeProvider = timeProvider;
                _callback = callback;
                _state = state;
                _period = period;
                _dueAt = dueTime == Timeout.InfiniteTimeSpan
                    ? DateTimeOffset.MaxValue
                    : timeProvider.GetUtcNow().Add(dueTime);
            }

            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                lock (_timeProvider._lock)
                {
                    if (_disposed)
                    {
                        return false;
                    }

                    _period = period;
                    _dueAt = dueTime == Timeout.InfiniteTimeSpan
                        ? DateTimeOffset.MaxValue
                        : _timeProvider._utcNow.Add(dueTime);
                    return true;
                }
            }

            public void TryInvoke(DateTimeOffset utcNow)
            {
                lock (_timeProvider._lock)
                {
                    if (_disposed || utcNow < _dueAt)
                    {
                        return;
                    }

                    if (_period == Timeout.InfiniteTimeSpan)
                    {
                        _disposed = true;
                    }
                    else
                    {
                        _dueAt = utcNow.Add(_period);
                    }
                }

                _callback(_state);
            }

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }

            public void Dispose()
            {
                lock (_timeProvider._lock)
                {
                    _disposed = true;
                }
            }
        }
    }
}
