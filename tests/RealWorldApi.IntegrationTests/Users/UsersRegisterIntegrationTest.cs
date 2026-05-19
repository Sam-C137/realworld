using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RealWorldApi.Core.Features.Users.Dto;
using Xunit.Abstractions;

namespace RealWorldApi.IntegrationTests.Users;

public class UsersRegisterIntegrationTest(IntegrationTestContainerFixture fixture, ITestOutputHelper output) 
    : IntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task Register_ReturnsOkResult()
    {
        var req = new RegisterRequestDto
        {
            User = new RegisterDetails(
                Username: "outkast",
                Email: "sofreshsoclean@stankonia.com",
                Password: "0utKast"
            )
        };
        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/users", req);
        await InspectResponse(response);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<RegisterResponseDto>();
        Assert.NotNull(result);
        Assert.Equal("outkast", result.User.Username);
        Assert.Equal("sofreshsoclean@stankonia.com", result.User.Email);
        Assert.NotNull(result.User.Token);
        Assert.NotEmpty(result.User.Token);
    }

    [Fact]
    public async Task Register_ReturnsBadRequestResult()
    {
        var details = new RegisterDetails(
            Username: "outkast",
            Email: "sofreshsoclean@stankonia.com",
            Password: "0utKast"
        );
        var req = new RegisterRequestDto
        {
            User = details 
        };
        req.User = null!;
        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/users", req);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorMessage(response, "User", "The User field is required.");
        
        req.User = details with { Username = null! };
        response = await client.PostAsJsonAsync("/api/v1/users", req);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorMessage(response, "User.Username", "The Username field is required.");
        
        req.User = details with { Password = "foo" };
        response = await client.PostAsJsonAsync("/api/v1/users", req);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorMessage(response, "User.Password", "Password must be at least 6 characters long.");
        
        req.User = details with { Email = "bar" };
        response = await client.PostAsJsonAsync("/api/v1/users", req);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorMessage(response, "User.Email", "Email must be a valid email address.");
    }

    [Fact]
    public async Task Register_FailsOnExistingUser()
    {
        var details = new RegisterDetails(
            Username: "kendrick",
            Email: "moneytrees@tpab.com",
            Password: "Kendr1k"
        );
        var req = new RegisterRequestDto
        {
            User = details 
        };
        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/users", req);
        response.EnsureSuccessStatusCode();
        
        response = await client.PostAsJsonAsync("/api/v1/users", req);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorMessage(response, "User.Username", "Username is already taken.");

        req.User = details with { Username = "kendricklamar" };
        response = await client.PostAsJsonAsync("/api/v1/users", req);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("User already exists", body);
    }
}
