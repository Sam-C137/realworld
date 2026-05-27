using System.Net;
using System.Net.Http.Json;
using RealWorldApi.Core.Abstractions;
using RealWorldApi.Core.Features.Articles.Dto;
using RealWorldApi.Core.Features.Tags.Dto;
using Xunit.Abstractions;

namespace RealWorldApi.IntegrationTests.Articles;

public class ArticlesCreateIntegrationTest(IntegrationTestContainerFixture fixture, ITestOutputHelper output) 
    : IntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task CreateArticle_ReturnsOkResult()
    {
        var client = CreateHttpsClient();
        await AddDefaultAuthHeaders(client, await RegisterUser(client));
        var req = new CreateArticleRequestDto
        {
            Article = new CreateArticleRequestDetails(
                Title: "How to train your dragon",
                Description: "Ever wonder how?",
                Body: "You have to believe",
                BodyJson: null,
                TagList: ["dragons", "training"]
            )
        };
        var response = await client.PostAsJsonAsync("/api/v1/articles", req);
        response.EnsureSuccessStatusCode();
        await InspectResponse(response);
        var result = await response.Content.ReadFromJsonAsync<GetArticleResponseDto>();
        Assert.NotNull(result);
        Assert.Equal("How to train your dragon", result.Article.Title);
        Assert.Equal("Ever wonder how?", result.Article.Description);
        Assert.Equal("You have to believe", result.Article.Body);
        Assert.Equal(2, result.Article.TagList.Length);
        Assert.Contains(result.Article.TagList, tag => tag == "dragons");
        Assert.Contains(result.Article.TagList, tag => tag == "training");
        Assert.NotNull(result.Article.Author);
        Assert.Contains("ryuuma", result.Article.Author.Username);
    }

    [Fact]
    public async Task CreateArticle_ReturnsBadRequestResult()
    {
        var client = CreateHttpsClient();
        await AddDefaultAuthHeaders(client, await RegisterUser(client));
        var details = new CreateArticleRequestDetails(
            Title: "How to train your dragon",
            Description: "Ever wonder how?",
            Body: "You have to believe",
            BodyJson: null,
            TagList: ["dragons", "training"]
        );
        var req = new CreateArticleRequestDto
        {
            Article = details
        };
        req.Article = null!;
        var response = await client.PostAsJsonAsync("/api/v1/articles", req);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorMessage(response, "Article", "The Article field is required.");
        
        req.Article = details with { Title = null! };
        response = await client.PostAsJsonAsync("/api/v1/articles", req);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorMessage(response, "Article.Title", "The Title field is required.");
        
        req.Article = details with { Title = " " };
        response = await client.PostAsJsonAsync("/api/v1/articles", req);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorMessage(response, "Article.Title", "Title is required.");
        
        req.Article = details with { Description = " " };
        response = await client.PostAsJsonAsync("/api/v1/articles", req);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorMessage(response, "Article.Description", "Description is required.");
        
        req.Article = details with { Body = " " };
        response = await client.PostAsJsonAsync("/api/v1/articles", req);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorMessage(response, "Article.Body", "Body is required.");
        
        req.Article = details with { TagList = ["a"] };
        response = await client.PostAsJsonAsync("/api/v1/articles", req);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorMessage(response, "Article.TagList", "Tag must be at least 3 characters long.");
    }

    [Fact]
    public async Task CreateArticle_EnforcesAuthorization()
    {
        var client = CreateHttpsClient();
        var response = await client.PostAsJsonAsync("/api/v1/articles", new CreateArticleRequestDto());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateArticle_HandlesSlugCollision()
    {
        var client = CreateHttpsClient();
        await AddDefaultAuthHeaders(client, await RegisterUser(client));
        var details = new CreateArticleRequestDetails(
            Title: "The Man Who's Gonna Be King of the Pirates",
            Description: "I'm the one who's going to best you and become",
            Body: "king of the pirates",
            BodyJson: null,
            TagList: ["onigashima", "wano"]
        );
        var req = new CreateArticleRequestDto
        {
            Article = details
        };
        var response = await client.PostAsJsonAsync("/api/v1/articles", req);
        response.EnsureSuccessStatusCode();
        var result1 = await response.Content.ReadFromJsonAsync<GetArticleResponseDto>();
        Assert.NotNull(result1);
        Assert.NotEmpty(result1.Article.Slug);
        
        response = await client.PostAsJsonAsync("/api/v1/articles", req);
        response.EnsureSuccessStatusCode();
        var result2 = await response.Content.ReadFromJsonAsync<GetArticleResponseDto>();
        Assert.NotNull(result2);
        Assert.NotEmpty(result2.Article.Slug);
        Assert.NotEqual(result1.Article.Slug, result2.Article.Slug);       
    }

    [Fact]
    public async Task CreateArticle_HandlesNewAndExistingTags()
    {
        var client = CreateHttpsClient();
        await AddDefaultAuthHeaders(client, await RegisterUser(client));
        var response = await client.PostAsJsonAsync("/api/v1/tags", new CreateTagRequestDto
        {
            Tag = new CreateTagRequestDetails(Name: "geas")
        });
        response.EnsureSuccessStatusCode();
        response = await client.GetAsync("/api/v1/tags");
        response.EnsureSuccessStatusCode();
        var tags = await response.Content.ReadFromJsonAsync<PaginatedResponse<string>>();
        Assert.NotNull(tags);
        Assert.Single(tags.Data);
        
        var req = new CreateArticleRequestDto
        {
            Article = new CreateArticleRequestDetails(
                Title: "Lelouch of the rebellion",
                Description: "The world is a game, where the only thing that matters is winning",
                Body: "If I win, I will change the world. If I lose, I will still change the world.",
                BodyJson: null,
                TagList: ["geas", "rebellion"]
            )
        };
        response = await client.PostAsJsonAsync("/api/v1/articles", req);
        response.EnsureSuccessStatusCode();
        response = await client.GetAsync("/api/v1/tags");
        response.EnsureSuccessStatusCode();
        tags = await response.Content.ReadFromJsonAsync<PaginatedResponse<string>>();
        Assert.NotNull(tags);
        Assert.Equal(2, tags.Data.Count);
        Assert.Contains(tags.Data, name => name == "geas");
        Assert.Contains(tags.Data, name => name == "rebellion");       
    }
}