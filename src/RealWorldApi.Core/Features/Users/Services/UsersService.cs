using ErrorOr;
using Microsoft.EntityFrameworkCore;
using RealWorldApi.Core.Features.Users.Dto;
using RealWorldApi.Infrastructure.Data;
using RealWorldApi.Infrastructure.Data.Models;

namespace RealWorldApi.Core.Features.Users.Services;

public class UsersService(AppDbContext db, TokenService tokens, IHttpContextAccessor http, ILogger<Program> logger) 
    : IUsersService
{
    public async Task<ErrorOr<(User, string accessToken, string csrfToken)>> Register(RegisterRequestDto request)
    {
        try
        {
            var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == request.User.Email);
            if (existing is not null) return Error.Conflict(description: "User already exists");

            var user = new User
            {
                Email = request.User.Email,
                Profile = new Profile { Username = request.User.Username },
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.User.Password),
                SessionVersion = 1
            };
            user.Roles.Add(new UserRole { UserId = user.Id, Role = "User" });

            db.Users.Add(user);
            await db.SaveChangesAsync();

            return await IssueTokensAsync(user)
                .Then(t => (user, t.accessToken, t.csrfToken));
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error registering user with email {email}", request.User.Email);
            return Error.Failure(description: "An error occurred while registering user");
        }
    }
    
    public async Task<ErrorOr<(User, string accessToken, string csrfToken)>> Login(LoginRequestDto request)
    {
        try
        {
            var user = await db.Users
                .Include(u => u.Roles)
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Email == request.User.Email);

            if (user is null) return Error.NotFound(description: "User not found");

            if (!BCrypt.Net.BCrypt.Verify(request.User.Password, user.PasswordHash))
            {
                return Error.Unauthorized(description: "Invalid credentials");
            }

            return await IssueTokensAsync(user)
                .Then(t => (user, t.accessToken, t.csrfToken));
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error logging in user with email {email}", request.User.Email);
            return Error.Failure(description: "An error occurred while logging in");
        }
    }

    public async Task<ErrorOr<string>> RefreshToken()
    {
        var ctx = http.HttpContext!;

        var token = ctx.Request.Cookies[CookieHelper.RefreshTokenCookie];
        if (string.IsNullOrEmpty(token))
        {
            return Error.Unauthorized(description: "Refresh token not found");
        }

        var tokenHash = TokenService.HashToken(token);

        var session = await db.UserSessions
            .Include(s => s.User)
            .ThenInclude(u => u.Roles)
            .FirstOrDefaultAsync(s => s.RefreshTokenHash == tokenHash);
        
        if (session is null) return Error.Unauthorized(description: "Invalid refresh token");

        if (session.ExpiresAt < DateTime.UtcNow)
        {
            return Error.Unauthorized(description: "Refresh token expired");
        }

        if (session.IsRevoked)
        {
            await RevokeAllUserSessionsAsync(session.UserId); // possible token theft
            return Error.Unauthorized(description: "Please login to continue");
        }
        
        var sessionVersion = await db.Users
            .Where(u => u.Id == session.UserId)
            .Select(u => u.SessionVersion)
            .FirstOrDefaultAsync();

        if (session.SessionVersion != sessionVersion)
        {
            return Error.Unauthorized(description: "Invalid session");
        }
        var (refreshToken, refreshHash) = tokens.CreateRefreshToken();
        session.RefreshTokenHash = refreshHash;
        session.LastUsedAt = DateTime.UtcNow;
        session.ExpiresAt = DateTime.UtcNow.AddDays(30);
        await db.SaveChangesAsync();
        
        await tokens.CacheSessionAsync(session, session.User);
        
        var accessToken = tokens.CreateAccessToken(session.User, session.Id);
        CookieHelper.SetRefreshTokenCookie(ctx.Response, refreshToken, session.ExpiresAt);
        return accessToken;
    }
    
    public async Task Logout(Guid sessionId)
    {
        var session = await db.UserSessions.FindAsync(sessionId);
        if (session is { IsRevoked: false })
        {
            session.IsRevoked  = true;
            session.RevokedAt  = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        var remaining = session?.ExpiresAt - DateTime.UtcNow ?? TimeSpan.FromDays(30);
        await tokens.RevokeSessionInCacheAsync(sessionId, remaining > TimeSpan.Zero ? remaining : TimeSpan.FromSeconds(1));
        CookieHelper.ClearAuthCookies(http.HttpContext!.Response);
    }

    /// <summary>
    /// Issues a new access token, refresh token, and CSRF token for the specified user.
    /// Creates a user session in db and cache, sets refresh and csrf cookies on the response, and returns access token.
    /// </summary>
    /// <param name="user">The user object for whom the tokens are being issued.</param>
    /// <returns>An access token as a string, or an error if token issuance fails.</returns>
    private async Task<ErrorOr<(string accessToken, string csrfToken)>> IssueTokensAsync(User user)
    {
        try
        {
            var ctx = http.HttpContext!;
            var (refreshToken, refreshHash) = tokens.CreateRefreshToken();
            var csrfToken = tokens.CreateCsrfToken();

            var session = new UserSession
            {
                UserId           = user.Id,
                RefreshTokenHash = refreshHash,
                SessionVersion   = user.SessionVersion,
                UserAgent        = ctx.Request.Headers.UserAgent.ToString()[..Math.Min(200, ctx.Request.Headers.UserAgent.ToString().Length)],
                IpAddress        = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                CreatedAt        = DateTime.UtcNow,
                ExpiresAt        = DateTime.UtcNow.AddDays(30),
                LastUsedAt       = DateTime.UtcNow,
                AuthStrength     = "password"
            };

            db.UserSessions.Add(session);
            await db.SaveChangesAsync();
            await tokens.CacheSessionAsync(session, user);

            var accessToken = tokens.CreateAccessToken(user, session.Id);

            CookieHelper.SetRefreshTokenCookie(ctx.Response, refreshToken, session.ExpiresAt);
            CookieHelper.SetCsrfCookie(ctx.Response, csrfToken);

            return (accessToken, csrfToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error issuing tokens for user {userId}", user.Id);
            return Error.Failure(description: "An error occurred while issuing tokens");
        }
    }
    
    private async Task RevokeAllUserSessionsAsync(Guid userId)
    {
        var sessions = await db.UserSessions
            .Where(s => s.UserId == userId && !s.IsRevoked)
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var s in sessions)
        {
            s.IsRevoked = true;
            s.RevokedAt = now;
            var remaining = s.ExpiresAt - now;
            await tokens.RevokeSessionInCacheAsync(s.Id, remaining > TimeSpan.Zero ? remaining : TimeSpan.FromSeconds(1));
        }
        await db.SaveChangesAsync();
    }
}
