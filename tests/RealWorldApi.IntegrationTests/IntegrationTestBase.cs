using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using RealWorldApi.Core.Features.Users;
using RealWorldApi.Core.Features.Users.Dto;
using Xunit.Abstractions;

namespace RealWorldApi.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public abstract class IntegrationTestBase(IntegrationTestContainerFixture fixture, ITestOutputHelper output)
    : IAsyncLifetime
{
    protected readonly CustomWebApplicationFactory Factory = new(fixture);

    public Task InitializeAsync() => fixture.ResetDatabaseAsync();

    public Task DisposeAsync()
    {
        Factory.Dispose();
        return Task.CompletedTask;
    }
    
    protected async Task InspectResponse(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        output.WriteLine($"Status: {(int)response.StatusCode} {response.StatusCode}");
        output.WriteLine($"Content-Type: {response.Content.Headers.ContentType}");
        output.WriteLine($"Body: {body}");
        output.WriteLine("Headers:");
        foreach (var header in response.Headers) {
            output.WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
        }
    }

    protected static async Task AssertErrorMessage(HttpResponseMessage response, string key, string message)
    {
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(result);
        result.Extensions.TryGetValue("errors", out var obj);
        Assert.NotNull(obj);
        if (obj is JsonElement element)
        {
            var dict = element.Deserialize<Dictionary<string, string[]>>();
            Assert.NotNull(dict);
            Assert.True(dict.ContainsKey(key));
            Assert.Contains(message, dict[key]);
        }
        else
        {
            Assert.Fail("Expected 'errors' to be a JSON object");
        }
    }
    
    protected HttpClient CreateHttpsClient() =>
        Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    
    protected static string GetCookieValue(HttpResponseMessage response, string cookieName)
    {
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));

        var cookieValue = cookies
            .Where(cookie => cookie.StartsWith($"{cookieName}=", StringComparison.Ordinal))
            .Select(cookie => cookie.Split(';', 2)[0][(cookieName.Length + 1)..])
            .FirstOrDefault();

        Assert.False(string.IsNullOrWhiteSpace(cookieValue), $"Expected '{cookieName}' cookie to be set.");
        return Uri.UnescapeDataString(cookieValue);
    }

    protected static async Task<HttpResponseMessage> RegisterUser(HttpClient client, RegisterDetails? details = null)
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        details ??= new RegisterDetails(
            Username: $"ryuuma{id}",
            Email: $"ryuuma-{id}@op.com",
            Password: "Dragon1"
        );

        var response = await client.PostAsJsonAsync("/api/v1/users", new RegisterRequestDto
        {
            User = details
        });
        response.EnsureSuccessStatusCode();
        return response;
    }
    
    protected static async Task AddDefaultAuthHeaders(HttpClient client, HttpResponseMessage registerResponse)
    {
        var csrfToken = GetCookieValue(registerResponse, CookieHelper.CsrfCookie);
        var result = await registerResponse.Content.ReadFromJsonAsync<RegisterResponseDto>();
        Assert.NotNull(result);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.User.Token);
        client.DefaultRequestHeaders.Add(CookieHelper.CsrfHeader, csrfToken);
    }
}
