using System.Net.Http.Json;
using RealWorldApi.Core.Abstractions;
using RealWorldApi.Core.Features.Tags.Dto;
using RealWorldApi.Infrastructure.Data.Models;
using Xunit.Abstractions;

namespace RealWorldApi.IntegrationTests.Tags;

public class TagsFetchIntegrationTest(IntegrationTestContainerFixture fixture, ITestOutputHelper output) 
    : IntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task GetTag_ReturnsOkResult()
    {
        var client = CreateHttpsClient();
        await AddDefaultAuthHeaders(client, await RegisterUser(client));
        var tag = await CreateTag(client, "light-light");
        var response = await client.GetAsync($"/api/v1/tags/{tag.Name}");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Tag>();
        Assert.NotNull(result);
        Assert.Equal(tag.Name, result.Name);
    }
    
    [Fact]
    public async Task GetTag_ReturnsNotFoundResult()
    {
        var client = CreateHttpsClient();
        await AddDefaultAuthHeaders(client, await RegisterUser(client));
        var response = await client.GetAsync("/api/v1/tags/spring-spring");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }
    
    [Fact]
    public async Task GetTags_ReturnsOkResult()
    {
        var client = CreateHttpsClient();
        await AddDefaultAuthHeaders(client, await RegisterUser(client));
        var tag1 = await CreateTag(client, "flame-flame");
        var tag2 = await CreateTag(client, "gum-gum");
        var response = await client.GetAsync("/api/v1/tags");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<string>>();
        Assert.NotNull(result);
        Assert.Contains(result.Data, name => name == tag1.Name);
        Assert.Contains(result.Data, name => name == tag2.Name);
    }
    
    [Fact]
    public async Task GetTags_ReturnsEmptyResult()
    {
        var client = CreateHttpsClient();
        await AddDefaultAuthHeaders(client, await RegisterUser(client));
        var response = await client.GetAsync("/api/v1/tags");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<string>>();
        Assert.NotNull(result);
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task GetTags_HasWorkingFilters()
    {
        var client = CreateHttpsClient();
        await AddDefaultAuthHeaders(client, await RegisterUser(client));
        await Task.WhenAll(
            new List<string>(["paw-paw", "snake-snake", "ice-ice", "lava-lava", "string-string"])
                .Select(name => CreateTag(client, name))
        );
        var response = await client.GetAsync("/api/v1/tags?page=1&limit=2");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<string>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal(5, result.Total);
        
        response = await client.GetAsync("/api/v1/tags?page=1&limit=2&sort=name&order=desc");
        response.EnsureSuccessStatusCode();
        result = await response.Content.ReadFromJsonAsync<PaginatedResponse<string>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal(5, result.Total);
        Assert.Equal(new[] { "string-string", "snake-snake" }, result.Data);
        
        response = await client.GetAsync("/api/v1/tags?page=1&limit=2&sort=name&order=asc");
        response.EnsureSuccessStatusCode();
        result = await response.Content.ReadFromJsonAsync<PaginatedResponse<string>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal(5, result.Total);
        Assert.Equal(new[] { "ice-ice", "lava-lava" }, result.Data);       
        
        response = await client.GetAsync("/api/v1/tags?page=1&limit=1&sort=name&order=desc&search=ice");
        response.EnsureSuccessStatusCode();
        result = await response.Content.ReadFromJsonAsync<PaginatedResponse<string>>();
        Assert.NotNull(result);
        Assert.Single(result.Data);
        Assert.Equal(1, result.Total);
        Assert.Equal(new[] { "ice-ice" }, result.Data);  
        
        response = await client.GetAsync("/api/v1/tags?page=1&limit=-1");
        response.EnsureSuccessStatusCode();
        result = await response.Content.ReadFromJsonAsync<PaginatedResponse<string>>();
        Assert.NotNull(result);
        Assert.Equal(5, result.Data.Count);
        Assert.Equal(5, result.Total);       
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