namespace RealWorldApi.Core.Features.Users;


public static class UserCacheKeys
{
    public static string Session(Guid sessionId) => $"{Constants.AppCachePrefix}session:{sessionId}";
    public static string SessionVersion(Guid userId) => $"{Constants.AppCachePrefix}user_session_version:{userId}";
    public static string RevokedSession(Guid sessionId) => $"{Constants.AppCachePrefix}revoked_session:{sessionId}";
    public static string LoginEmailRateLimiter(string email) => $"{Constants.AppCachePrefix}login_email:{email}";
    public static string LoginEmailRateLimiterBackoff(string email) => $"{Constants.AppCachePrefix}login_backoff:{email}";
    public static string LoginEmailRateLimiterFails(string email) => $"{Constants.AppCachePrefix}login_fails:{email}";
}

public record CachedSessionState(
    Guid SessionId,
    Guid UserId,
    string Email,
    string[] Roles,
    int SessionVersion,
    DateTime ExpiresAt
);