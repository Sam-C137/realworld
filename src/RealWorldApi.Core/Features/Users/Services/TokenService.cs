using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using RealWorldApi.Infrastructure.Data.Models;
using StackExchange.Redis;

namespace RealWorldApi.Core.Features.Users.Services;


public class TokenService(IConfiguration config, IDatabase redis)
{
    private static readonly TimeSpan AccessTokenTtl = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan RefreshTokenTtl = TimeSpan.FromDays(30);

    public string CreateAccessToken(User user, Guid sessionId)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(config["Jwt:Secret"]!));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("sid", sessionId.ToString()),        
            new("sv",  user.SessionVersion.ToString())
        };
        claims.AddRange(user.Roles.Select(r => new Claim(ClaimTypes.Role, r.Role)));

        var token = new JwtSecurityToken(
            issuer:   config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims:   claims,
            expires:  DateTime.UtcNow.Add(AccessTokenTtl),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
    /// <summary>
    /// Creates new session and refresh tokens
    /// </summary>
    /// <returns>Returns (rawToken, hash) — store hash in DB, send raw in cookie</returns>
    public (string Raw, string Hash) CreateRefreshToken()
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var hash = HashToken(raw);
        return (raw, hash);
    }

    public static string HashToken(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Creates a random CSRF token. This should be stored in a secure, HttpOnly cookie and sent in a
    /// custom header on state-changing requests to prevent CSRF attacks.
    /// </summary>
    /// <returns> A random CSRF token. </returns>
    public string CreateCsrfToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    public async Task CacheSessionAsync(UserSession session, User user)
    {
        var state = new CachedSessionState(
            session.Id, user.Id, user.Email,
            user.Roles.Select(r => r.Role).ToArray(),
            user.SessionVersion,
            session.ExpiresAt
        );
        var ttl = session.ExpiresAt - DateTime.UtcNow;
        await redis.StringSetAsync(
            UserCacheKeys.Session(session.Id),
            JsonSerializer.Serialize(state),
            ttl > TimeSpan.Zero ? ttl : TimeSpan.FromSeconds(1)
        );
    }

    public async Task<CachedSessionState?> GetCachedSessionAsync(Guid sessionId)
    {
        var raw = await redis.StringGetAsync(UserCacheKeys.Session(sessionId));
        return raw.HasValue switch
        {
            true => JsonSerializer.Deserialize<CachedSessionState>((ReadOnlySpan<byte>)raw),
            _ => null
        };
    }

    public async Task RevokeSessionInCacheAsync(Guid sessionId, TimeSpan ttl)
    {
        await redis.KeyDeleteAsync(UserCacheKeys.Session(sessionId));
        await redis.StringSetAsync(UserCacheKeys.RevokedSession(sessionId), "1", ttl);
    }

    public async Task<bool> IsSessionRevokedInCacheAsync(Guid sessionId) =>
        await redis.KeyExistsAsync(UserCacheKeys.RevokedSession(sessionId));
}
