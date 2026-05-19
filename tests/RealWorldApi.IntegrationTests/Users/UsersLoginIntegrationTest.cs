using System.Net;
using System.Net.Http.Json;
using RealWorldApi.Core.Features.Users.Dto;
using Xunit.Abstractions;

namespace RealWorldApi.IntegrationTests.Users;

public class UsersLoginIntegrationTest(IntegrationTestContainerFixture fixture, ITestOutputHelper output)
    :IntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task Login_ReturnsOkResult()
    {
        await RegisterUser();
        var req = new LoginRequestDto
        {
            User = new LoginDetails(
                Email: "son@dbz.com",
                Password: "SonG0ku"
            )
        };
        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/users/login", req);
        await InspectResponse(response);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(result);
        Assert.Equal("songoku", result.User.Username);
        Assert.NotNull(result.User.Token);
        Assert.NotEmpty(result.User.Token);
    }

    [Fact]
    public async Task Login_FailsIfUserDoesNotExist()
    {
        var req = new LoginRequestDto
        {
            User = new LoginDetails(
                Email: "bulma@dbz.com",
                Password: "Bulma123"
            )
        };
        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/users/login", req);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync();
        Assert.NotNull(result);
        Assert.Contains("Invalid credentials", result);
    }

    [Fact]
    public async Task Login_FailsOnInvalidCredentials()
    {
        await RegisterUser();
        var req = new LoginRequestDto
        {
            User = new LoginDetails(
                Email: "son@dbz.com",
                Password: "Vegeta123"
            )
        };
        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/users/login", req);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync();
        Assert.NotNull(result);
        Assert.Contains("Invalid credentials", result);
    }

    [Fact]
    public async Task Login_ReturnsBadRequestResult()
    {
        await RegisterUser();
        var details = new LoginDetails(
            Email: "son@dbz.com",
            Password: "Vegeta123"
        );
        var req = new LoginRequestDto
        {
            User = details
        };
        req.User = null!;
        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/users/login", req);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorMessage(response, "User", "The User field is required.");
        
        req.User = details with { Email = null! };
        response = await client.PostAsJsonAsync("/api/v1/users/login", req);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorMessage(response, "User.Email", "The Email field is required.");
        
        req.User = details with { Email = "son" };
        response = await client.PostAsJsonAsync("/api/v1/users/login", req);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorMessage(response, "User.Email", "Email must be a valid email address.");
        
        req.User = details with { Password = null! };
        response = await client.PostAsJsonAsync("/api/v1/users/login", req);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorMessage(response, "User.Password", "The Password field is required.");
        
        req.User = details with { Password = " " };
        response = await client.PostAsJsonAsync("/api/v1/users/login", req);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorMessage(response, "User.Password", "Password is required.");
    }
    
    private async Task RegisterUser()
    {
        var details = new RegisterDetails(
            Username: "songoku",
            Email: "son@dbz.com",
            Password: "SonG0ku"
        );

        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/users", new RegisterRequestDto
        {
            User = details
        });
        response.EnsureSuccessStatusCode();
    }
}