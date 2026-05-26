using System.Net;
using System.Net.Http.Json;
using RealWorldApi.Core.Features.Articles.Dto;
using Xunit.Abstractions;

namespace RealWorldApi.IntegrationTests.Articles;

public class ArticlesUpdateIntegrationTest(IntegrationTestContainerFixture fixture, ITestOutputHelper output) 
    : IntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task UpdateArticle_ReturnsOkResult()
    {
        var client = CreateHttpsClient();
        await AddDefaultAuthHeaders(client, await RegisterUser(client));
        var response = await client.PostAsJsonAsync("/api/v1/articles", new CreateArticleRequestDto
        {
            Article = new CreateArticleRequestDetails(
                Title: "American Psycho",
                Description:
                "A wealthy New York investment banking executive hides his psychopathic ego from his circle of friends.",
                Body: """
                      Patrick Bateman is a wealthy New York investment banking executive who hides his psychopathic ego from his 
                      circle of friends. He indulges in a violent hedonistic lifestyle that leads to a downward spiral of increasing violence and insanity.
                      """,
                BodyJson: null,
                TagList: ["american", "psycho", "bateman", "patrick"]
            )
        });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetArticleResponseDto>();
        Assert.NotNull(result);

        response = await client.PutAsJsonAsync($"/api/v1/articles/{result.Article.Slug}", new UpdateArticleRequestDto
        {
            Article = new UpdateArticleRequestDetails(
                Title: "American Gigachad",
                Description: "Womp womp gigachad is here",
                Body: "Patrick Gigachad is rides a fly to wall street",
                BodyJson: null,
                TagList: ["american", "gigachad", "wall-street", "fly", "patrick"]
            )
        });
        response.EnsureSuccessStatusCode();
        result = await response.Content.ReadFromJsonAsync<GetArticleResponseDto>();
        Assert.NotNull(result);
        Assert.Equal("American Gigachad", result.Article.Title);
        Assert.Equal("Womp womp gigachad is here", result.Article.Description);
        Assert.Equal("Patrick Gigachad is rides a fly to wall street", result.Article.Body);
        Assert.Equal(5, result.Article.TagList.Length);
        Assert.Contains(result.Article.TagList, tag => tag == "fly");   
    }
    
    [Fact]
    public async Task UpdateArticle_ReturnsNotFoundResult()
    {
        var client = CreateHttpsClient();
        await AddDefaultAuthHeaders(client, await RegisterUser(client));
        var response = await client.PutAsJsonAsync("/api/v1/articles/non-existent-slug", new UpdateArticleRequestDto
        {
            Article = new UpdateArticleRequestDetails(
                Title: "American Psycho",
                Description: null,
                Body: null,
                BodyJson: null,
                TagList: []
            )
        });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateArticle_ReturnsForbiddenIfAuthorIsDifferent()
    {
        var client = CreateHttpsClient();
        await AddDefaultAuthHeaders(client, await RegisterUser(client));
        var req = new CreateArticleRequestDto
        {
            Article = new CreateArticleRequestDetails(
                Title: "American Psycho",
                Description:
                "A wealthy New York investment banking executive hides his psychopathic ego from his circle of friends.",
                Body: """
                      Patrick Bateman is a wealthy New York investment banking executive who hides his psychopathic ego from his 
                      circle of friends. He indulges in a violent hedonistic lifestyle that leads to a downward spiral of increasing violence and insanity.
                      """,
                BodyJson: null,
                TagList: ["american", "new-york", "psycho", "bateman", "patrick"]
            )
        };
        var response = await client.PostAsJsonAsync("/api/v1/articles", req);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetArticleResponseDto>();
        Assert.NotNull(result);

        var anotherClient = CreateHttpsClient();
        await AddDefaultAuthHeaders(anotherClient, await RegisterUser(anotherClient));
        response = await anotherClient.PutAsJsonAsync($"/api/v1/articles/{result.Article.Slug}", req);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateArticle_DoesNotUpdateNullFields()
    {
        var client = CreateHttpsClient();
        await AddDefaultAuthHeaders(client, await RegisterUser(client));
        var req = new CreateArticleRequestDto
        {
            Article = new CreateArticleRequestDetails(
                Title: "American Psycho",
                Description:
                "A wealthy New York investment banking executive hides his psychopathic ego from his circle of friends.",
                Body: """
                      Patrick Bateman is a wealthy New York investment banking executive who hides his psychopathic ego from his 
                      circle of friends. He indulges in a violent hedonistic lifestyle that leads to a downward spiral of increasing violence and insanity.
                      """,
                BodyJson: null,
                TagList: ["american", "new-york", "psycho", "bateman", "patrick"]
            )
        };
        var response = await client.PostAsJsonAsync("/api/v1/articles", req);
        response.EnsureSuccessStatusCode();
        var result1 = await response.Content.ReadFromJsonAsync<GetArticleResponseDto>();
        Assert.NotNull(result1);
        
        response = await client.PutAsJsonAsync($"/api/v1/articles/{result1.Article.Slug}", new UpdateArticleRequestDto
        {
            Article = new UpdateArticleRequestDetails(
                Title: "American Gigachad",
                Description: null,
                Body: null,
                BodyJson: null,
                TagList: null
            )
        });
        var result2 = await response.Content.ReadFromJsonAsync<GetArticleResponseDto>();
        Assert.NotNull(result2);
        Assert.Equal("American Gigachad", result2.Article.Title);
        Assert.NotEqual(result1.Article.Title, result2.Article.Title);
        Assert.Equal(result1.Article.Description, result2.Article.Description);
        Assert.Equal(result1.Article.Body, result2.Article.Body);
        Assert.Equal(result1.Article.TagList.OrderBy(x => x).ToArray(), result2.Article.TagList.OrderBy(x => x).ToArray());
    }

    [Fact]
    public async Task UpdateArticle_UpdatesSlugIfTitleChanges()
    {
        var client = CreateHttpsClient();
        await AddDefaultAuthHeaders(client, await RegisterUser(client));
        var req = new CreateArticleRequestDto
        {
            Article = new CreateArticleRequestDetails(
                Title: "American Psycho",
                Description:
                "A wealthy New York investment banking executive hides his psychopathic ego from his circle of friends.",
                Body: """
                      Patrick Bateman is a wealthy New York investment banking executive who hides his psychopathic ego from his 
                      circle of friends. He indulges in a violent hedonistic lifestyle that leads to a downward spiral of increasing violence and insanity.
                      """,
                BodyJson: null,
                TagList: ["american", "new-york", "psycho", "bateman", "patrick"]
            )
        };
        var response = await client.PostAsJsonAsync("/api/v1/articles", req);
        response.EnsureSuccessStatusCode();
        var result1 = await response.Content.ReadFromJsonAsync<GetArticleResponseDto>();
        Assert.NotNull(result1);
        
        response = await client.PutAsJsonAsync($"/api/v1/articles/{result1.Article.Slug}", new UpdateArticleRequestDto
        {
            Article = new UpdateArticleRequestDetails(
                Title: "American Gigachad",
                Description: null,
                Body: null,
                BodyJson: null,
                TagList: null
            )
        });
        var result2 = await response.Content.ReadFromJsonAsync<GetArticleResponseDto>();
        Assert.NotNull(result2);
        Assert.Equal("American Gigachad", result2.Article.Title);
        Assert.NotEqual(result1.Article.Slug, result2.Article.Slug);
    }
}