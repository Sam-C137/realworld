using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
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
}
