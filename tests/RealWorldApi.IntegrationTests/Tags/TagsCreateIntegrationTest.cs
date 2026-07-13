using System.Net;
using System.Net.Http.Json;
using RealWorldApi.Core.Features.Tags.Dto;
using Xunit.Abstractions;

namespace RealWorldApi.IntegrationTests.Tags;

public class TagsCreateIntegrationTest(IntegrationTestContainerFixture fixture, ITestOutputHelper output) 
    : IntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task CreateTag_ReturnsOkResult()
    {
        var client = CreateHttpsClient();
        await AddDefaultAuthHeaders(client, await RegisterUser(client));
        var req = new CreateTagRequestDto
        {
            Tag = new CreateTagRequestDetails(Name: "bankai")
        };
        var response = await client.PostAsJsonAsync("/api/v1/tags", req);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetTagResponseDto>();
        Assert.NotNull(result);
        Assert.Equal("bankai", result.Name);
    }

    [Fact]
    public async Task CreateTag_ReturnsBadRequestResult()
    {
        var client = CreateHttpsClient();
        await AddDefaultAuthHeaders(client, await RegisterUser(client));
        var details = new CreateTagRequestDetails(Name: "bankai");
        var req = new CreateTagRequestDto
        {
            Tag = details
        };
        req.Tag = null!;
        var response = await client.PostAsJsonAsync("/api/v1/tags", req);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorMessage(response, "Tag", "The Tag field is required.");
        
        req.Tag = details with { Name = null! };
        response = await client.PostAsJsonAsync("/api/v1/tags", req);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorMessage(response, "Tag.Name", "The Name field is required.");
        
        req.Tag = details with { Name = "ba" };
        response = await client.PostAsJsonAsync("/api/v1/tags", req);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorMessage(response, "Tag.Name", "Tag name must be at least 3 characters long.");
        
        req.Tag = details with { Name = new string('a', 256) };
        response = await client.PostAsJsonAsync("/api/v1/tags", req);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorMessage(response, "Tag.Name", "Tag name must be at most 255 characters long.");
    }

    [Fact]
    public async Task CreateTag_FailsOnExistingTagCaseInsensitive()
    {
        var client = CreateHttpsClient();
        await AddDefaultAuthHeaders(client, await RegisterUser(client));
        var details = new CreateTagRequestDetails(Name: "bankai");
        var req = new CreateTagRequestDto
        {
            Tag = details
        };
        
        var response = await client.PostAsJsonAsync("/api/v1/tags", req);
        response.EnsureSuccessStatusCode();
        
        response = await client.PostAsJsonAsync("/api/v1/tags", req);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorMessage(response, "Tag.Name", $"Tag {details.Name} already exists.");       

        req.Tag = details with { Name = "BANKAI" };
        response = await client.PostAsJsonAsync("/api/v1/tags", req);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorMessage(response, "Tag.Name", $"Tag {req.Tag.Name} already exists.");       
    }
    
    [Fact]
    public async Task CreateTag_EnforcesAuthorization()
    {
        var client = CreateHttpsClient();
        var response = await client.PostAsJsonAsync("/api/v1/tags", new CreateTagRequestDto());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}