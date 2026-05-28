using System.Net;
using System.Net.Http.Json;
using RealWorldApi.Core.Abstractions;
using RealWorldApi.Core.Features.Articles.Dto;
using RealWorldApi.Core.Features.Users.Dto;
using Xunit.Abstractions;

namespace RealWorldApi.IntegrationTests.Articles;

public class ArticlesDeleteIntegrationTest(IntegrationTestContainerFixture fixture, ITestOutputHelper output)
    : IntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task DeleteArticle_ReturnsNoContentAndRemovesArticleFromFetches()
    {
        var client = CreateHttpsClient();
        await RegisterAs(client, "samus_aran");
        var article = await CreateArticle(client,
            title: "Metroid Dread EMMI Escape Route",
            description: "Running is research when the robot has opinions",
            body: "Samus routes the cold room, parries the impossible, and keeps the mission moving.",
            tagList: ["metroid", "dread", "zdr"]);

        var response = await client.DeleteAsync($"/api/v1/articles/{article.Article.Slug}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        response = await client.GetAsync($"/api/v1/articles/{article.Article.Slug}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        response = await client.GetAsync("/api/v1/articles?limit=-1");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<GetArticleResponseDto>>();
        Assert.NotNull(result);
        Assert.Empty(result.Data);
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public async Task DeleteArticle_ReturnsNotFoundResult()
    {
        var client = CreateHttpsClient();
        await RegisterAs(client, "kiryu_kazuma");

        var response = await client.DeleteAsync("/api/v1/articles/ten-years-in-the-joint-made-this-slug-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteArticle_EnforcesAuthorization()
    {
        var client = CreateHttpsClient();

        var response = await client.DeleteAsync("/api/v1/articles/chrono-trigger-campfire-save-point");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteArticle_ReturnsForbiddenIfAuthorIsDifferent()
    {
        var author = CreateHttpsClient();
        await RegisterAs(author, "cloud_strife");
        var article = await CreateArticle(author,
            title: "Final Fantasy VII Materia Loadout",
            description: "When every slot is somehow still not enough",
            body: "Cloud debates whether lightning materia belongs next to existential dread.",
            tagList: ["ffvii", "materia", "midgar"]);

        var sephiroth = CreateHttpsClient();
        await RegisterAs(sephiroth, "sephiroth_onewing");
        var response = await sephiroth.DeleteAsync($"/api/v1/articles/{article.Article.Slug}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        response = await author.GetAsync($"/api/v1/articles/{article.Article.Slug}");
        response.EnsureSuccessStatusCode();
        var fetched = await response.Content.ReadFromJsonAsync<GetArticleResponseDto>();
        Assert.NotNull(fetched);
        Assert.Equal(article.Article.Slug, fetched.Article.Slug);
    }

    private async Task RegisterAs(HttpClient client, string username)
    {
        await AddDefaultAuthHeaders(client, await RegisterUser(client, new RegisterDetails(
            Username: username,
            Password: "Dragon1",
            Email: $"{username}@realworld.test")));
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
