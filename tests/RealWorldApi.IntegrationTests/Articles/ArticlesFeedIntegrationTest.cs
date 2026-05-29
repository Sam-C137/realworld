using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RealWorldApi.Core.Abstractions;
using RealWorldApi.Core.Features.Articles.Dto;
using RealWorldApi.Core.Features.Users.Dto;
using RealWorldApi.Infrastructure.Data;
using RealWorldApi.Infrastructure.Data.Models;
using Xunit.Abstractions;

namespace RealWorldApi.IntegrationTests.Articles;

public class ArticlesFeedIntegrationTest(IntegrationTestContainerFixture fixture, ITestOutputHelper output)
    : IntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task GetFeed_EnforcesAuthorization()
    {
        var client = CreateHttpsClient();

        var response = await client.GetAsync("/api/v1/articles/feed");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetFeed_ReturnsEmptyResultWhenUserFollowsNobody()
    {
        var reader = CreateHttpsClient();
        await RegisterAs(reader, "tanjiro_reader");

        var author = CreateHttpsClient();
        await RegisterAs(author, "nezuko_writer");
        await CreateArticle(author,
            title: "Demon Slayer Box Travel Etiquette",
            description: "Sunlight avoidance with premium sibling logistics",
            body: "Nezuko rates every box by comfort, stealth, and dramatic entrance potential.",
            tagList: ["demon", "slayer", "hashira"]);

        var response = await reader.GetAsync("/api/v1/articles/feed");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<GetArticleResponseDto>>();
        Assert.NotNull(result);
        Assert.Empty(result.Data);
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public async Task GetFeed_ReturnsOnlyArticlesFromFollowedAuthors()
    {
        var reader = CreateHttpsClient();
        await RegisterAs(reader, "luffy_reader");
        var zoro = CreateHttpsClient();
        await RegisterAs(zoro, "zoro_swords");
        var nami = CreateHttpsClient();
        await RegisterAs(nami, "nami_maps");
        var buggy = CreateHttpsClient();
        await RegisterAs(buggy, "buggy_clown");

        var zoroArticle = await CreateArticle(zoro,
            title: "Three Sword Style Pantry Audit",
            description: "When the kitchen is somehow north, south, and lost",
            body: "Zoro labels every rice ball with intense seriousness and still exits through the pantry.",
            tagList: ["onepiece", "swords", "snacks"]);
        var namiArticle = await CreateArticle(nami,
            title: "Grand Line Weather Scam Detection",
            description: "Clouds, currents, and invoices no pirate read carefully enough",
            body: "Nami explains why the map costs extra when the ocean starts acting weird.",
            tagList: ["onepiece", "maps", "weather"]);
        await CreateArticle(buggy,
            title: "Buggy Ball Brand Strategy",
            description: "Explosions, branding, and extremely loud confidence",
            body: "Buggy mistakes market research for applause and somehow gets both.",
            tagList: ["onepiece", "circus", "chaos"]);

        await FollowUser("luffy_reader", "zoro_swords");
        await FollowUser("luffy_reader", "nami_maps");

        var response = await reader.GetAsync("/api/v1/articles/feed?limit=-1&sort=title&order=asc");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<GetArticleResponseDto>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Total);
        Assert.Equal(new[] { namiArticle.Article.Slug, zoroArticle.Article.Slug }.OrderBy(x => x).ToArray(),
            result.Data.Select(x => x.Article.Slug).OrderBy(x => x).ToArray());
        Assert.All(result.Data, article => Assert.True(article.Article.Author.Following));
        Assert.DoesNotContain(result.Data, article => article.Article.Author.Username == "buggy_clown");
    }

    [Fact]
    public async Task GetFeed_HasWorkingFiltersPaginationSortingAndSearch()
    {
        var reader = CreateHttpsClient();
        await RegisterAs(reader, "frieren_reader");
        var fern = CreateHttpsClient();
        await RegisterAs(fern, "fern_deadpan");
        var stark = CreateHttpsClient();
        await RegisterAs(stark, "stark_axeman");
        var himmel = CreateHttpsClient();
        await RegisterAs(himmel, "himmel_hero");

        var mimic = await CreateArticle(fern,
            title: "Frieren Mimic Chest Safety Memo",
            description: "A field guide for apprentices who enjoy living",
            body: "Fern documents mimic bite radius, suspicious treasure rooms, and why Frieren needs supervision.",
            tagList: ["frieren", "magic", "mimic"]);
        var laundry = await CreateArticle(fern,
            title: "Spellbook Laundry Day Incident",
            description: "When clean robes meet forbidden detergent magic",
            body: "Fern discovers that laundry spells should never be tested near rare grimoires.",
            tagList: ["frieren", "magic", "laundry"]);
        var dragon = await CreateArticle(stark,
            title: "Red Dragon Courage Rehearsal",
            description: "Heroic trembling with excellent axe form",
            body: "Stark practices dragon speeches until the mountain gets embarrassed for him.",
            tagList: ["frieren", "dragon", "axe"]);
        await CreateArticle(himmel,
            title: "Hero Statue Maintenance Budget",
            description: "Selfless posing, polish invoices, and completely normal vanity",
            body: "Himmel files a heroic memo about why every village needs flattering lighting.",
            tagList: ["frieren", "hero", "statue"]);

        await FollowUser("frieren_reader", "fern_deadpan");
        await FollowUser("frieren_reader", "stark_axeman");

        var response = await reader.GetAsync("/api/v1/articles/feed?page=1&limit=2&sort=title&order=asc");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<GetArticleResponseDto>>();
        Assert.NotNull(result);
        Assert.Equal(3, result.Total);
        Assert.Equal(new[] { "Frieren Mimic Chest Safety Memo", "Red Dragon Courage Rehearsal" },
            result.Data.Select(x => x.Article.Title).ToArray());

        response = await reader.GetAsync("/api/v1/articles/feed?page=2&limit=2&sort=title&order=asc");
        response.EnsureSuccessStatusCode();
        result = await response.Content.ReadFromJsonAsync<PaginatedResponse<GetArticleResponseDto>>();
        Assert.NotNull(result);
        Assert.Equal(3, result.Total);
        Assert.Equal(laundry.Article.Slug, Assert.Single(result.Data).Article.Slug);

        response = await reader.GetAsync("/api/v1/articles/feed?limit=-1&author=fern");
        response.EnsureSuccessStatusCode();
        result = await response.Content.ReadFromJsonAsync<PaginatedResponse<GetArticleResponseDto>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Total);
        Assert.All(result.Data, article => Assert.Equal("fern_deadpan", article.Article.Author.Username));

        response = await reader.GetAsync("/api/v1/articles/feed?limit=-1&tag=dragon");
        response.EnsureSuccessStatusCode();
        result = await response.Content.ReadFromJsonAsync<PaginatedResponse<GetArticleResponseDto>>();
        Assert.NotNull(result);
        Assert.Equal(1, result.Total);
        Assert.Equal(dragon.Article.Slug, Assert.Single(result.Data).Article.Slug);

        response = await reader.GetAsync("/api/v1/articles/feed?limit=-1&search=mimic");
        response.EnsureSuccessStatusCode();
        result = await response.Content.ReadFromJsonAsync<PaginatedResponse<GetArticleResponseDto>>();
        Assert.NotNull(result);
        Assert.Equal(1, result.Total);
        Assert.Equal(mimic.Article.Slug, Assert.Single(result.Data).Article.Slug);

        response = await reader.GetAsync("/api/v1/articles/feed?limit=-1&search=statue");
        response.EnsureSuccessStatusCode();
        result = await response.Content.ReadFromJsonAsync<PaginatedResponse<GetArticleResponseDto>>();
        Assert.NotNull(result);
        Assert.Empty(result.Data);
        Assert.Equal(0, result.Total);

        response = await reader.GetAsync("/api/v1/articles/feed?limit=-1&favorited=himmel");
        response.EnsureSuccessStatusCode();
        result = await response.Content.ReadFromJsonAsync<PaginatedResponse<GetArticleResponseDto>>();
        Assert.NotNull(result);
        Assert.Empty(result.Data);
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public async Task GetFeed_ReturnsBadRequestForInvalidQuery()
    {
        var reader = CreateHttpsClient();
        await RegisterAs(reader, "korra_reader");

        var response = await reader.GetAsync("/api/v1/articles/feed?page=0");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorMessage(response, "Page", "Page number must be greater than 0.");

        response = await reader.GetAsync("/api/v1/articles/feed?limit=101");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorMessage(response, "Limit", "Limit must be less than or equal to 100 or -1 for all results.");
    }

    private async Task RegisterAs(HttpClient client, string username)
    {
        await AddDefaultAuthHeaders(client, await RegisterUser(client, new RegisterDetails(
            Username: username,
            Password: "Dragon1",
            Email: $"{username}@realworld.test")));
    }

    private async Task FollowUser(string followerUsername, string followeeUsername)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var follower = await db.Profiles.SingleAsync(p => p.Username == followerUsername);
        var followee = await db.Profiles.SingleAsync(p => p.Username == followeeUsername);
        var alreadyFollowing = await db.Follows.AnyAsync(f =>
            f.FollowerId == follower.Id &&
            f.FolloweeId == followee.Id);

        if (alreadyFollowing) return;

        db.Follows.Add(new Follow
        {
            FollowerId = follower.Id,
            FolloweeId = followee.Id
        });
        await db.SaveChangesAsync();
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
