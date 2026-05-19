using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RealWorldApi.Core.Features.Users;
using RealWorldApi.Core.Features.Users.Dto;
using RealWorldApi.Infrastructure.Data;
using Xunit.Abstractions;

namespace RealWorldApi.IntegrationTests.Users;

public class UsersRefreshTokenIntegrationTest(IntegrationTestContainerFixture fixture, ITestOutputHelper output)
    : IntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task Refresh_ReturnsOkResult()
    {
        var client = await AuthorizeUser(CreateHttpsClient());
        var oldToken = client.DefaultRequestHeaders.Authorization?.Parameter;
        var response = await client.PostAsync("/api/v1/users/refresh", null);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<RefreshTokenResponseDto>();
        Assert.NotNull(result);
        Assert.NotNull(result.Token);
        Assert.NotEmpty(result.Token);
        Assert.NotEqual(oldToken, result.Token);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", result.Token);

        var secondResponse = await client.PostAsync("/api/v1/users/refresh", null);
        secondResponse.EnsureSuccessStatusCode();
        var secondResult = await secondResponse.Content.ReadFromJsonAsync<RefreshTokenResponseDto>();
        Assert.NotNull(secondResult);
        Assert.NotNull(secondResult.Token);
        Assert.NotEmpty(secondResult.Token);
        Assert.NotEqual(result.Token, secondResult.Token);
    }

    [Fact]
    public async Task Refresh_WithExpiredAccessToken_ReturnsNewTokenThatCanBeUsed()
    {
        var client = await AuthorizeUser(CreateHttpsClient());
        var oldToken = client.DefaultRequestHeaders.Authorization?.Parameter;
        Assert.False(string.IsNullOrWhiteSpace(oldToken));
        var expiredToken = CreateExpiredAccessToken(oldToken);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", expiredToken);

        var expiredLogoutResponse = await client.DeleteAsync("/api/v1/users/logout");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, expiredLogoutResponse.StatusCode);

        var refreshResponse = await client.PostAsync("/api/v1/users/refresh", null);
        refreshResponse.EnsureSuccessStatusCode();
        var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<RefreshTokenResponseDto>();
        Assert.NotNull(refreshResult);
        Assert.NotNull(refreshResult.Token);
        Assert.NotEmpty(refreshResult.Token);
        Assert.NotEqual(oldToken, refreshResult.Token);
        Assert.NotEqual(expiredToken, refreshResult.Token);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", refreshResult.Token);

        var logoutResponse = await client.DeleteAsync("/api/v1/users/logout");
        Assert.Equal(System.Net.HttpStatusCode.NoContent, logoutResponse.StatusCode);

        var revokedRefreshResponse = await client.PostAsync("/api/v1/users/refresh", null);
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, revokedRefreshResponse.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithExpiredRefreshSession_ReturnsUnauthorized()
    {
        var client = await AuthorizeUser(CreateHttpsClient());
        var token = client.DefaultRequestHeaders.Authorization?.Parameter;
        Assert.False(string.IsNullOrWhiteSpace(token));
        var sessionId = GetSessionId(token);

        await ExpireSession(sessionId);

        var response = await client.PostAsync("/api/v1/users/refresh", null);
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }
    
    private static async Task<HttpClient> AuthorizeUser(HttpClient client)
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var details = new RegisterDetails(
            Username: $"sekiro{id}",
            Email: $"sekiro-{id}@sdt.com",
            Password: "S3k1r0"
        );

        var response = await client.PostAsJsonAsync("/api/v1/users", new RegisterRequestDto
        {
            User = details
        });
        response.EnsureSuccessStatusCode();
        var csrfToken = GetCookieValue(response, CookieHelper.CsrfCookie);
        var result = await response.Content.ReadFromJsonAsync<RegisterResponseDto>();
        Assert.NotNull(result);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", result.User.Token);
        client.DefaultRequestHeaders.Add(CookieHelper.CsrfHeader, csrfToken);
        return client;
    }

    private static string CreateExpiredAccessToken(string token)
    {
        var original = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var claims = original.Claims
            .Where(claim => claim.Type != JwtRegisteredClaimNames.Exp
                && claim.Type != JwtRegisteredClaimNames.Nbf
                && claim.Type != JwtRegisteredClaimNames.Iat
                && claim.Type != JwtRegisteredClaimNames.Aud)
            .Select(claim => new Claim(claim.Type, claim.Value))
            .ToList();

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(CustomWebApplicationFactory.JwtSecret));
        var expired = new JwtSecurityToken(
            issuer: CustomWebApplicationFactory.JwtIssuer,
            audience: CustomWebApplicationFactory.JwtAudience,
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-10),
            expires: DateTime.UtcNow.AddMinutes(-2),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(expired);
    }

    private static Guid GetSessionId(string token)
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var sessionId = jwt.Claims.FirstOrDefault(claim => claim.Type == "sid")?.Value;
        Assert.True(Guid.TryParse(sessionId, out var parsed));
        return parsed;
    }

    private async Task ExpireSession(Guid sessionId)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.PostgresConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        await using var context = new AppDbContext(options);
        var session = await context.UserSessions.SingleAsync(session => session.Id == sessionId);
        session.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await context.SaveChangesAsync();
    }
}
