using System.Security.Claims;
using RealWorldApi.Core.Features.Users.Services;
using RealWorldApi.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

namespace RealWorldApi.Core.Extensions;

/// <summary>
/// Provides extension methods called inside AddJwtBearer's OnTokenValidated.
/// </summary>
public static class SessionValidationExtensions
{
    /// <summary>
    /// Validates the session associated with a JWT token during token validation following these steps:
    /// <li>Fast revocation check from redis cache - fails if is revoked</li>
    /// <li>Gets a user session from cache or DB - fails if not found or expired</li>
    /// <li>Session version check, sv is incremented on password change - fails if mismatch (handles password-change invalidation)</li>
    /// </summary>
    /// <param name="ctx">
    /// The context for a token that has been validated. Contains information about
    /// the security principal, claims, and the HTTP request.
    /// </param>
    /// <returns>
    /// <see cref="Task"/>
    /// </returns>
    public static async Task ValidateSessionAsync(TokenValidatedContext ctx)
    {
        var sessionIdClaim = ctx.Principal?.FindFirstValue("sid");
        var svClaim        = ctx.Principal?.FindFirstValue("sv");

        if (!Guid.TryParse(sessionIdClaim, out var sessionId) || svClaim is null)
        {
            ctx.Fail("Missing session claims.");
            return;
        }

        var tokens = ctx.HttpContext.RequestServices.GetRequiredService<TokenService>();

        if (await tokens.IsSessionRevokedInCacheAsync(sessionId))
        {
            ctx.Fail("Session revoked.");
            return;
        }

        var cached = await tokens.GetCachedSessionAsync(sessionId);
        if (cached is null)
        {
            var db = ctx.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var session = await db.UserSessions
                .Include(s => s.User).ThenInclude(u => u.Roles)
                .FirstOrDefaultAsync(s => s.Id == sessionId && !s.IsRevoked);

            if (session is null || session.ExpiresAt < DateTime.UtcNow)
            {
                ctx.Fail("Session not found or expired.");
                return;
            }
            await tokens.CacheSessionAsync(session, session.User);
            cached = await tokens.GetCachedSessionAsync(sessionId);
        }

        if (!int.TryParse(svClaim, out var tokenSv) || tokenSv != cached!.SessionVersion)
        {
            ctx.Fail("Session version mismatch.");
        }
    }
}