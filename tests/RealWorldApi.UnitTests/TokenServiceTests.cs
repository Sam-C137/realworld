using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RealWorldApi.Core.Features.Users;
using RealWorldApi.Core.Features.Users.Services;
using RealWorldApi.Infrastructure.Data.Models;

namespace RealWorldApi.UnitTests;

[Collection(RedisTestCollection.Name)]
public sealed class TokenServiceTests(RedisTestFixture redisFixture) : IAsyncLifetime
{
    private const string JwtSecret = "super-secret-test-signing-key-with-enough-bytes";
    private readonly TokenService _sut = new(CreateConfiguration(), redisFixture.Database);

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public void CreateAccessToken_IncludesExpectedClaimsAndCanBeValidated()
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "token@example.com",
            Username = "token-user",
            SessionVersion = 7,
            Roles =
            [
                new UserRole { UserId = userId, Role = "author" },
                new UserRole { UserId = userId, Role = "admin" }
            ]
        };

        var token = _sut.CreateAccessToken(user, sessionId);

        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = handler.ValidateToken(token, ValidationParameters(), out var validatedToken);
        var jwt = Assert.IsType<JwtSecurityToken>(validatedToken);

        Assert.Equal("realworld-tests", jwt.Issuer);
        Assert.Contains("realworld-tests-audience", jwt.Audiences);
        Assert.Equal(userId.ToString(), principal.FindFirstValue(JwtRegisteredClaimNames.Sub));
        Assert.Equal(user.Email, principal.FindFirstValue(JwtRegisteredClaimNames.Email));
        Assert.True(Guid.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Jti), out _));
        Assert.Equal(sessionId.ToString(), principal.FindFirstValue("sid"));
        Assert.Equal("7", principal.FindFirstValue("sv"));
        Assert.Contains(principal.Claims, claim => claim is { Type: ClaimTypes.Role, Value: "author" });
        Assert.Contains(principal.Claims, claim => claim is { Type: ClaimTypes.Role, Value: "admin" });
        Assert.InRange(jwt.ValidTo, DateTime.UtcNow.AddMinutes(14), DateTime.UtcNow.AddMinutes(15).AddSeconds(5));
    }

    [Fact]
    public void CreateRefreshToken_ReturnsUniqueBase64RawTokenAndMatchingSha256Hash()
    {
        var first = _sut.CreateRefreshToken();
        var second = _sut.CreateRefreshToken();

        Assert.NotEqual(first.Raw, second.Raw);
        Assert.NotEqual(first.Hash, second.Hash);
        Assert.Equal(64, Convert.FromBase64String(first.Raw).Length);
        Assert.Equal(TokenService.HashToken(first.Raw), first.Hash);
        Assert.Matches("^[0-9a-f]{64}$", first.Hash);
    }

    [Fact]
    public void HashToken_IsDeterministicLowercaseSha256Hex()
    {
        var expected = Convert.ToHexString(SHA256.HashData("raw-token"u8.ToArray())).ToLowerInvariant();

        var first = TokenService.HashToken("raw-token");
        var second = TokenService.HashToken("raw-token");

        Assert.Equal(expected, first);
        Assert.Equal(first, second);
        Assert.Matches("^[0-9a-f]{64}$", first);
    }

    [Fact]
    public void CreateCsrfToken_ReturnsUniqueBase64TokenWith32BytesOfEntropy()
    {
        var first = _sut.CreateCsrfToken();
        var second = _sut.CreateCsrfToken();

        Assert.NotEqual(first, second);
        Assert.Equal(32, Convert.FromBase64String(first).Length);
    }

    [Fact]
    public async Task CacheSessionAsync_StoresSessionStateWithExpiryDerivedFromSession()
    {
        var userId = Guid.NewGuid();
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            RefreshTokenHash = "hash",
            IpAddress = "127.0.0.1",
            UserAgent = "unit-test"
        };
        var user = new User
        {
            Id = userId,
            Email = "cached@example.com",
            Username = "cached-user",
            SessionVersion = 3,
            Roles = [new UserRole { UserId = userId, Role = "reader" }]
        };

        await _sut.CacheSessionAsync(session, user);

        var cached = await _sut.GetCachedSessionAsync(session.Id);
        Assert.NotNull(cached);
        Assert.Equal(session.Id, cached.SessionId);
        Assert.Equal(userId, cached.UserId);
        Assert.Equal(user.Email, cached.Email);
        Assert.Equal(new[] { "reader" }, cached.Roles);
        Assert.Equal(3, cached.SessionVersion);
        Assert.Equal(session.ExpiresAt, cached.ExpiresAt, TimeSpan.FromSeconds(1));

        var ttl = await redisFixture.Database.KeyTimeToLiveAsync(UserCacheKeys.Session(session.Id));
        Assert.True(ttl.HasValue);
        Assert.InRange(ttl.Value.TotalSeconds, 240, 300);
    }

    [Fact]
    public async Task CacheSessionAsync_WhenSessionAlreadyExpired_StoresForOneSecond()
    {
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
            RefreshTokenHash = "hash",
            IpAddress = "127.0.0.1",
            UserAgent = "unit-test"
        };
        var user = new User
        {
            Id = session.UserId,
            Email = "expired@example.com",
            Username = "expired-user"
        };

        await _sut.CacheSessionAsync(session, user);

        var ttl = await redisFixture.Database.KeyTimeToLiveAsync(UserCacheKeys.Session(session.Id));
        Assert.True(ttl.HasValue);
        Assert.InRange(ttl.Value.TotalSeconds, 0, 1);
    }

    [Fact]
    public async Task GetCachedSessionAsync_WhenKeyDoesNotExist_ReturnsNull()
    {
        var cached = await _sut.GetCachedSessionAsync(Guid.NewGuid());

        Assert.Null(cached);
    }

    [Fact]
    public async Task RevokeSessionInCacheAsync_DeletesSessionKeyAndMarksRevokedWithTtl()
    {
        var sessionId = Guid.NewGuid();
        await redisFixture.Database.StringSetAsync(UserCacheKeys.Session(sessionId), "cached", TimeSpan.FromMinutes(5));

        await _sut.RevokeSessionInCacheAsync(sessionId, TimeSpan.FromSeconds(30));

        Assert.False(await redisFixture.Database.KeyExistsAsync(UserCacheKeys.Session(sessionId)));
        Assert.True(await _sut.IsSessionRevokedInCacheAsync(sessionId));

        var ttl = await redisFixture.Database.KeyTimeToLiveAsync(UserCacheKeys.RevokedSession(sessionId));
        Assert.True(ttl.HasValue);
        Assert.InRange(ttl.Value.TotalSeconds, 1, 30);
    }

    private static IConfiguration CreateConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = JwtSecret,
                ["Jwt:Issuer"] = "realworld-tests",
                ["Jwt:Audience"] = "realworld-tests-audience"
            })
            .Build();

    private static TokenValidationParameters ValidationParameters() =>
        new()
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = "realworld-tests",
            ValidateAudience = true,
            ValidAudience = "realworld-tests-audience",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
}
