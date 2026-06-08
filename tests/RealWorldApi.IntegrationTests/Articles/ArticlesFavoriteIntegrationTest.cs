using System.Net;
using System.Net.Http.Json;
using RealWorldApi.Core.Features.Articles.Dto;
using RealWorldApi.Core.Features.Users.Dto;
using Xunit.Abstractions;

namespace RealWorldApi.IntegrationTests.Articles;

public class ArticlesFavoriteIntegrationTest(IntegrationTestContainerFixture fixture, ITestOutputHelper output)
    : IntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task FavoriteArticle_ReturnsFavoritedArticleAndIsIdempotent()
    {
        var author = CreateHttpsClient();
        await RegisterAs(author, "vash_stampede");
        var article = await CreateArticle(author,
            title: "Trigun Donut Ammunition Ledger",
            description: "Peace, love, and suspiciously expensive pastries",
            body: "Vash insists the donut budget is non-negotiable and morally load-bearing.",
            tagList: ["trigun", "donuts", "pacifist"]);

        var meryl = CreateHttpsClient();
        await RegisterAs(meryl, "meryl_reporter");

        var favorited = await FavoriteArticle(meryl, article.Article.Slug);
        Assert.True(favorited.Article.Favorited);
        Assert.Equal(1, favorited.Article.FavoritesCount);

        favorited = await FavoriteArticle(meryl, article.Article.Slug);
        Assert.True(favorited.Article.Favorited);
        Assert.Equal(1, favorited.Article.FavoritesCount);

        var fetchedByMeryl = await GetArticle(meryl, article.Article.Slug);
        Assert.True(fetchedByMeryl.Article.Favorited);
        Assert.Equal(1, fetchedByMeryl.Article.FavoritesCount);

        var anonymous = CreateHttpsClient();
        var fetchedByAnonymous = await GetArticle(anonymous, article.Article.Slug);
        Assert.False(fetchedByAnonymous.Article.Favorited);
        Assert.Equal(1, fetchedByAnonymous.Article.FavoritesCount);
    }

    [Fact]
    public async Task UnfavoriteArticle_ReturnsUnfavoritedArticleAndIsIdempotent()
    {
        var author = CreateHttpsClient();
        await RegisterAs(author, "aang_air");
        var article = await CreateArticle(author,
            title: "Avatar Cabbage Cart Damage Report",
            description: "A municipal tragedy in many acts",
            body: "Every nation denies responsibility, but the cabbages remember.",
            tagList: ["avatar", "cabbage", "omashu"]);

        var sokka = CreateHttpsClient();
        await RegisterAs(sokka, "sokka_boomerang");
        var toph = CreateHttpsClient();
        await RegisterAs(toph, "toph_metal");
        await FavoriteArticle(sokka, article.Article.Slug);
        await FavoriteArticle(toph, article.Article.Slug);

        var unfavorited = await UnfavoriteArticle(sokka, article.Article.Slug);
        Assert.False(unfavorited.Article.Favorited);
        Assert.Equal(1, unfavorited.Article.FavoritesCount);

        unfavorited = await UnfavoriteArticle(sokka, article.Article.Slug);
        Assert.False(unfavorited.Article.Favorited);
        Assert.Equal(1, unfavorited.Article.FavoritesCount);

        var fetchedByToph = await GetArticle(toph, article.Article.Slug);
        Assert.True(fetchedByToph.Article.Favorited);
        Assert.Equal(1, fetchedByToph.Article.FavoritesCount);
    }

    [Fact]
    public async Task FavoriteArticle_EnforcesAuthorizationAndNotFound()
    {
        var anonymous = CreateHttpsClient();

        var response = await anonymous.PostAsync("/api/v1/articles/missing-hunter-license/favorite", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        response = await anonymous.DeleteAsync("/api/v1/articles/missing-hunter-license/favorite");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var gon = CreateHttpsClient();
        await RegisterAs(gon, "gon_fishingrod");

        response = await gon.PostAsync("/api/v1/articles/missing-hunter-license/favorite", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        response = await gon.DeleteAsync("/api/v1/articles/missing-hunter-license/favorite");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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

    private static async Task<GetArticleResponseDto> FavoriteArticle(HttpClient client, string slug)
    {
        var response = await client.PostAsync($"/api/v1/articles/{slug}/favorite", null);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetArticleResponseDto>();
        Assert.NotNull(result);
        return result;
    }

    private static async Task<GetArticleResponseDto> UnfavoriteArticle(HttpClient client, string slug)
    {
        var response = await client.DeleteAsync($"/api/v1/articles/{slug}/favorite");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetArticleResponseDto>();
        Assert.NotNull(result);
        return result;
    }

    private static async Task<GetArticleResponseDto> GetArticle(HttpClient client, string slug)
    {
        var response = await client.GetAsync($"/api/v1/articles/{slug}");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetArticleResponseDto>();
        Assert.NotNull(result);
        return result;
    }
}
