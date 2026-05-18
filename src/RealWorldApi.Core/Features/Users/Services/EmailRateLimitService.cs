using ErrorOr;
using StackExchange.Redis;

namespace RealWorldApi.Core.Features.Users.Services;

/// <summary>
/// Handles per-email rate limiting + exponential backoff via Redis.
/// </summary>
public class EmailRateLimitService(IDatabase redis)
{
    private const int EmailWindowSeconds  = 15 * 60; // 15 min
    private const int EmailMaxAttempts    = 10;
    private const int BackoffBaseSeconds  = 2;
    private const int BackoffMaxSeconds   = 512;     // ~8.5 min cap

    /// <summary>
    /// Checks if the provided email is allowed to log in based on rate limiting and exponential backoff rules.
    /// If the email exceeds the allowed limit, returns the time (in seconds) to wait before retrying.
    /// </summary>
    /// <param name="email">The email address to check for rate-limiting restrictions.</param>
    /// <returns>A tuple containing a boolean allowed and the number of seconds to wait before retrying.
    /// If allowed, the retry time will be 0.</returns>
    public async Task<(bool Allowed, int RetryAfterSeconds)> CheckAndRecordAsync(string email)
    {
        // Exponential backoff: is there an active backoff penalty?
        var backoffTtl = await redis.KeyTimeToLiveAsync(UserCacheKeys.LoginEmailRateLimiterBackoff(email));
        if (backoffTtl.HasValue && backoffTtl.Value > TimeSpan.Zero)
            return (false, (int)backoffTtl.Value.TotalSeconds);

        var count = await redis.StringIncrementAsync(UserCacheKeys.LoginEmailRateLimiter(email));
        if (count == 1)
            await redis.KeyExpireAsync(UserCacheKeys.LoginEmailRateLimiter(email), TimeSpan.FromSeconds(EmailWindowSeconds));

        if (count > EmailMaxAttempts)
            return (false, EmailWindowSeconds);

        return (true, 0);
    }

    /// <summary>
    /// Records a failed login attempt for the provided email address.
    /// Call on failed login to apply exponential backoff
    /// </summary>
    /// <param name="email"></param>
    public async Task RecordFailureAsync(string email)
    {
        var fails    = await redis.StringIncrementAsync(UserCacheKeys.LoginEmailRateLimiterFails(email));
        await redis.KeyExpireAsync(UserCacheKeys.LoginEmailRateLimiterFails(email), TimeSpan.FromMinutes(30));

        var backoff  = (int)Math.Min(
            Math.Pow(BackoffBaseSeconds, fails - 1),   // 1s, 2s, 4s, 8s … 512s
            BackoffMaxSeconds);
        if (backoff > 0)
            await redis.StringSetAsync(UserCacheKeys.LoginEmailRateLimiterBackoff(email), "1", TimeSpan.FromSeconds(backoff));
    }

    public async Task ClearFailuresAsync(string email)
    {
        await redis.KeyDeleteAsync(UserCacheKeys.LoginEmailRateLimiterFails(email));
        await redis.KeyDeleteAsync(UserCacheKeys.LoginEmailRateLimiterBackoff(email));
    }
}