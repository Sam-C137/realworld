using RealWorldApi.Core.Features.Users;
using RealWorldApi.Core.Features.Users.Services;

namespace RealWorldApi.UnitTests;

[Collection(RedisTestCollection.Name)]
public sealed class EmailRateLimitServiceTests(RedisTestFixture redisFixture) : IAsyncLifetime
{
    private readonly string _email = $"person-{Guid.NewGuid():N}@example.com";
    private readonly EmailRateLimitService _sut = new(redisFixture.Database);

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CheckAndRecordAsync_FirstAttempt_AllowsAndStartsEmailWindow()
    {
        var result = await _sut.CheckAndRecordAsync(_email);

        Assert.True(result.Allowed);
        Assert.Equal(0, result.RetryAfterSeconds);
        Assert.Equal(1, (long)(await redisFixture.Database.StringGetAsync(UserCacheKeys.LoginEmailRateLimiter(_email)))!);

        var ttl = await redisFixture.Database.KeyTimeToLiveAsync(UserCacheKeys.LoginEmailRateLimiter(_email));
        AssertTtlInRange(ttl, TimeSpan.FromMinutes(14), TimeSpan.FromMinutes(15));
    }

    [Fact]
    public async Task CheckAndRecordAsync_TenthAttempt_IsAllowedButEleventhIsRejected()
    {
        for (var i = 0; i < 10; i++)
        {
            var allowedResult = await _sut.CheckAndRecordAsync(_email);
            Assert.True(allowedResult.Allowed);
            Assert.Equal(0, allowedResult.RetryAfterSeconds);
        }

        var rejectedResult = await _sut.CheckAndRecordAsync(_email);

        Assert.False(rejectedResult.Allowed);
        Assert.Equal(900, rejectedResult.RetryAfterSeconds);
        Assert.Equal(11, (long)(await redisFixture.Database.StringGetAsync(UserCacheKeys.LoginEmailRateLimiter(_email)))!);
    }

    [Fact]
    public async Task CheckAndRecordAsync_ActiveBackoff_RejectsWithoutIncrementingEmailWindow()
    {
        await redisFixture.Database.StringSetAsync(
            UserCacheKeys.LoginEmailRateLimiterBackoff(_email),
            "1",
            TimeSpan.FromSeconds(30));

        var result = await _sut.CheckAndRecordAsync(_email);

        Assert.False(result.Allowed);
        Assert.InRange(result.RetryAfterSeconds, 1, 30);
        Assert.False(await redisFixture.Database.KeyExistsAsync(UserCacheKeys.LoginEmailRateLimiter(_email)));
    }

    [Fact]
    public async Task RecordFailureAsync_IncrementsFailuresAndAppliesExponentialBackoff()
    {
        var expectedBackoffs = new[] { 1, 2, 4, 8, 16 };

        foreach (var expectedBackoff in expectedBackoffs)
        {
            await _sut.RecordFailureAsync(_email);

            var backoffTtl = await redisFixture.Database.KeyTimeToLiveAsync(UserCacheKeys.LoginEmailRateLimiterBackoff(_email));
            AssertTtlInRange(backoffTtl, TimeSpan.FromSeconds(Math.Max(0, expectedBackoff - 1)), TimeSpan.FromSeconds(expectedBackoff));
        }

        Assert.Equal(expectedBackoffs.Length, (long)(await redisFixture.Database.StringGetAsync(UserCacheKeys.LoginEmailRateLimiterFails(_email)))!);

        var failuresTtl = await redisFixture.Database.KeyTimeToLiveAsync(UserCacheKeys.LoginEmailRateLimiterFails(_email));
        AssertTtlInRange(failuresTtl, TimeSpan.FromMinutes(29), TimeSpan.FromMinutes(30));
    }

    [Fact]
    public async Task RecordFailureAsync_BackoffIsCappedAt512Seconds()
    {
        for (var i = 0; i < 11; i++)
        {
            await _sut.RecordFailureAsync(_email);
        }

        var backoffTtl = await redisFixture.Database.KeyTimeToLiveAsync(UserCacheKeys.LoginEmailRateLimiterBackoff(_email));

        AssertTtlInRange(backoffTtl, TimeSpan.FromSeconds(500), TimeSpan.FromSeconds(512));
    }

    [Fact]
    public async Task ClearFailuresAsync_DeletesFailureAndBackoffKeysOnly()
    {
        await _sut.CheckAndRecordAsync(_email);
        await _sut.RecordFailureAsync(_email);

        await _sut.ClearFailuresAsync(_email);

        Assert.False(await redisFixture.Database.KeyExistsAsync(UserCacheKeys.LoginEmailRateLimiterFails(_email)));
        Assert.False(await redisFixture.Database.KeyExistsAsync(UserCacheKeys.LoginEmailRateLimiterBackoff(_email)));
        Assert.True(await redisFixture.Database.KeyExistsAsync(UserCacheKeys.LoginEmailRateLimiter(_email)));
    }

    private static void AssertTtlInRange(TimeSpan? actual, TimeSpan min, TimeSpan max)
    {
        Assert.True(actual.HasValue, "Expected key to have a TTL.");
        Assert.InRange(actual.Value.TotalSeconds, min.TotalSeconds, max.TotalSeconds);
    }
}
