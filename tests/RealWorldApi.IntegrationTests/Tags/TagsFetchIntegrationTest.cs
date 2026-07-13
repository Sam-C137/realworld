using System.Net.Http.Json;
using RealWorldApi.Core.Abstractions;
using RealWorldApi.Core.Features.Articles.Dto;
using RealWorldApi.Core.Features.Tags.Dto;
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
        var result = await response.Content.ReadFromJsonAsync<GetTagResponseDto>();
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

    [Fact]
    public async Task GetTags_CanSortByPopularity()
    {
        var client = CreateHttpsClient();
        await AddDefaultAuthHeaders(client, await RegisterUser(client));

        await CreateArticle(client,
            title: "Final Fantasy X Sphere Grid Speedrun",
            description: "Tidus learns that one more node is never just one more node",
            body: "Yuna keeps the pilgrimage on track while the grid quietly eats the evening.",
            tagList: ["blitzball", "sphere-grid", "summoner"]);
        await CreateArticle(client,
            title: "Final Fantasy X Blitzball Draft Day",
            description: "The Aurochs discover tactics, vibes, and terrifying contract math",
            body: "Wakka believes in the team, even when the goalie has the reflexes of wet cardboard.",
            tagList: ["blitzball", "summoner"]);
        await CreateArticle(client,
            title: "Final Fantasy X Calm Lands Errand Spiral",
            description: "One monster capture turns into every monster capture",
            body: "The party enters the Calm Lands and immediately invents new chores.",
            tagList: ["blitzball"]);

        var response = await client.GetAsync("/api/v1/tags?page=1&limit=-1&sort=popularity&order=desc");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<string>>();
        Assert.NotNull(result);
        Assert.Equal(3, result.Total);
        Assert.Equal(new[] { "blitzball", "summoner", "sphere-grid" }, result.Data);

        response = await client.GetAsync("/api/v1/tags?page=1&limit=-1&sort=popularity&order=asc");
        response.EnsureSuccessStatusCode();
        result = await response.Content.ReadFromJsonAsync<PaginatedResponse<string>>();
        Assert.NotNull(result);
        Assert.Equal(3, result.Total);
        Assert.Equal(new[] { "sphere-grid", "summoner", "blitzball" }, result.Data);

        response = await client.GetAsync("/api/v1/tags?page=1&limit=2&sort=popularity&order=desc");
        response.EnsureSuccessStatusCode();
        result = await response.Content.ReadFromJsonAsync<PaginatedResponse<string>>();
        Assert.NotNull(result);
        Assert.Equal(3, result.Total);
        Assert.Equal(new[] { "blitzball", "summoner" }, result.Data);
    }
    
    private static async Task<GetTagResponseDto> CreateTag(HttpClient client, string name)
    {
        var req = new CreateTagRequestDto
        {
            Tag = new CreateTagRequestDetails(Name: name)
        };
        var response = await client.PostAsJsonAsync("/api/v1/tags", req);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetTagResponseDto>();
        Assert.NotNull(result);
        return result;
    }

    private static async Task<GetArticleResponseDto> CreateArticle(
        HttpClient client,
        string title,
        string description,
        string body,
        string[] tagList)
    {
        var response = await client.PostAsJsonAsync("/api/v1/articles", new CreateArticleRequestDto
        {
            Article = new CreateArticleRequestDetails(
                Title: title,
                Description: description,
                Body: body,
                BodyJson: null,
                TagList: tagList)
        });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetArticleResponseDto>();
        Assert.NotNull(result);
        return result;
    }
}
