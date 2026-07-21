using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using RealWorldApi.Core.Features.Users;
using RealWorldApi.Core.Features.Users.Dto;
using Xunit.Abstractions;

namespace RealWorldApi.IntegrationTests.Users;

public class UsersLogoutIntegrationTest(IntegrationTestContainerFixture fixture, ITestOutputHelper output)
    : IntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task Logout_ReturnsNoContent()
    {
        var client = CreateHttpsClient();
        var registerResponse = await RegisterUser(client);
        await AddDefaultAuthHeaders(client, registerResponse);

        var response = await client.DeleteAsync("/api/v1/users/logout");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact(Skip = "Csrf is temporarily disabled")]
    public async Task Logout_WithoutCsrfHeader_ReturnsForbidden()
    {
        var client = CreateHttpsClient();
        var registerResponse = await RegisterUser(client);
        var result = await registerResponse.Content.ReadFromJsonAsync<RegisterResponseDto>();
        Assert.NotNull(result);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", result.User.Token);

        var response = await client.DeleteAsync("/api/v1/users/logout");

        await InspectResponse(response);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact(Skip = "Csrf is temporarily disabled")]
    public async Task Logout_WithMismatchedCsrfHeader_ReturnsForbidden()
    {
        var client = CreateHttpsClient();
        var registerResponse = await RegisterUser(client);
        var result = await registerResponse.Content.ReadFromJsonAsync<RegisterResponseDto>();
        Assert.NotNull(result);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", result.User.Token);
        client.DefaultRequestHeaders.Add(CookieHelper.CsrfHeader, "not-the-csrf-token");

        var response = await client.DeleteAsync("/api/v1/users/logout");

        await InspectResponse(response);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
