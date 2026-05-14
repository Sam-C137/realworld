namespace RealWorldApi.Core.Features.Users;


public static class UserCacheKeys
{
    // Cached session auth state — avoids DB on every request
    public static string Session(Guid sessionId) => $"{Constants.AppCachePrefix}session:{sessionId}";
    // Per-user version — checked lazily on access token refresh, not every request
    public static string SessionVersion(Guid userId) => $"{Constants.AppCachePrefix}user_session_version:{userId}";
    // Fast revocation check — set on logout/revoke
    public static string RevokedSession(Guid sessionId) => $"{Constants.AppCachePrefix}revoked_session:{sessionId}";
}

// Auth/CachedSessionState.cs
public record CachedSessionState(
    Guid SessionId,
    Guid UserId,
    string Email,
    string[] Roles,
    int SessionVersion,
    DateTime ExpiresAt
);