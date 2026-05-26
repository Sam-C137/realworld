using System.Net.Http.Json;
using RealWorldApi.Core.Features.Tags.Dto;
using RealWorldApi.Infrastructure.Data.Models;
using Xunit.Abstractions;

namespace RealWorldApi.IntegrationTests.Tags;

public class TagsDeleteIntegrationTest(IntegrationTestContainerFixture fixture, ITestOutputHelper output) 
    : IntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task DeleteTag_ReturnsNoContentResult()
    {
        var client = CreateHttpsClient();
        await AddDefaultAuthHeaders(client, await RegisterUser(client));
        var tag = await CreateTag(client, "mirror-mirror");
        var response = await client.DeleteAsync($"/api/v1/tags/{tag.Name}");
        Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);
        
        var getResponse = await client.GetAsync($"/api/v1/tags/{tag.Name}");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, getResponse.StatusCode);
    }
    
    [Fact]
    public async Task DeleteTag_ReturnsNotFoundResult()
    {
        var client = CreateHttpsClient();
        await AddDefaultAuthHeaders(client, await RegisterUser(client));
        var response = await client.DeleteAsync("/api/v1/tags/spring-spring");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }
    
    private static async Task<Tag> CreateTag(HttpClient client, string name)
    {
        var req = new CreateTagRequestDto
        {
            Tag = new CreateTagRequestDetails(Name: name)
        };
        var response = await client.PostAsJsonAsync("/api/v1/tags", req);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Tag>();
        Assert.NotNull(result);
        return result;
    }
}