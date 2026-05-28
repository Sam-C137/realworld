using System.Net;
using System.Net.Http.Json;
using RealWorldApi.Core.Abstractions;
using RealWorldApi.Core.Features.Articles.Dto;
using RealWorldApi.Core.Features.Users.Dto;
using Xunit.Abstractions;

namespace RealWorldApi.IntegrationTests.Articles;

public class ArticlesFetchIntegrationTest(IntegrationTestContainerFixture fixture, ITestOutputHelper output)
    : IntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task GetArticle_ReturnsOkResultForAnonymousReader()
    {
        var author = CreateHttpsClient();
        await RegisterAs(author, "spike_cowboy");
        var created = await CreateArticle(author,
            title: "Cowboy Bebop and the Ballad of Fallen Angels",
            description: "A jazz-soaked duel under cathedral glass",
            body: "Spike walks into memory with roses, rain, and a past that refuses to stay buried.",
            tagList: ["bebop", "jazz", "vicious"]);

        var reader = CreateHttpsClient();
        var response = await reader.GetAsync($"/api/v1/articles/{created.Article.Slug}");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetArticleResponseDto>();
        Assert.NotNull(result);
        Assert.Equal(created.Article.Slug, result.Article.Slug);
        Assert.Equal("Cowboy Bebop and the Ballad of Fallen Angels", result.Article.Title);
        Assert.Equal("A jazz-soaked duel under cathedral glass", result.Article.Description);
        Assert.Equal("Spike walks into memory with roses, rain, and a past that refuses to stay buried.", result.Article.Body);
        Assert.Equal(new[] { "bebop", "jazz", "vicious" }, result.Article.TagList.OrderBy(x => x).ToArray());
        Assert.Equal("spike_cowboy", result.Article.Author.Username);
        Assert.False(result.Article.Favorited);
        Assert.False(result.Article.Author.Following);
        Assert.Equal(0, result.Article.FavoritesCount);
        Assert.True(result.Article.CreatedAt <= result.Article.UpdatedAt);
    }

    [Fact]
    public async Task GetArticle_ReturnsReaderSpecificDefaultsForAuthenticatedReader()
    {
        var author = CreateHttpsClient();
        await RegisterAs(author, "makoto_niijima");
        var created = await CreateArticle(author,
            title: "Persona 5 Palace Notes",
            description: "Calling cards, velvet rooms, and suspicious pancakes",
            body: "The phantom thieves test every palace route before the deadline clock starts yelling.",
            tagList: ["persona", "phantom", "palace"]);

        var reader = CreateHttpsClient();
        await RegisterAs(reader, "futaba_sakura");
        var response = await reader.GetAsync($"/api/v1/articles/{created.Article.Slug}");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetArticleResponseDto>();
        Assert.NotNull(result);
        Assert.False(result.Article.Favorited);
        Assert.False(result.Article.Author.Following);
        Assert.Equal(0, result.Article.FavoritesCount);
        Assert.Equal("makoto_niijima", result.Article.Author.Username);
    }

    [Fact]
    public async Task GetArticle_ReturnsNotFoundResult()
    {
        var client = CreateHttpsClient();

        var response = await client.GetAsync("/api/v1/articles/the-cake-is-a-lie-but-the-slug-is-not-here");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetArticles_ReturnsEmptyResult()
    {
        var client = CreateHttpsClient();

        var response = await client.GetAsync("/api/v1/articles");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<GetArticleResponseDto>>();
        Assert.NotNull(result);
        Assert.Empty(result.Data);
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public async Task GetArticles_HasWorkingFiltersPaginationAndSorting()
    {
        var ghibli = CreateHttpsClient();
        await RegisterAs(ghibli, "nausicaa_wind");
        var cyberpunk = CreateHttpsClient();
        await RegisterAs(cyberpunk, "motoko_major");
        var hyrule = CreateHttpsClient();
        await RegisterAs(hyrule, "link_between");

        var mononoke = await CreateArticle(ghibli,
            title: "Princess Mononoke Forest Treaty",
            description: "Kodama logistics for angry gods and iron towns",
            body: "San negotiates with wolves, spirits, and a town that keeps choosing louder weapons.",
            tagList: ["ghibli", "forest", "spirits"]);
        var nausicaa = await CreateArticle(ghibli,
            title: "Nausicaa Toxic Jungle Field Guide",
            description: "How to read spores without starting another apocalypse",
            body: "A glider, a mask, and a patient heart make the toxic jungle less mysterious.",
            tagList: ["ghibli", "jungle", "glider"]);
        var ghost = await CreateArticle(cyberpunk,
            title: "Ghost in the Shell Puppet Master Briefing",
            description: "Cyberbrain whispers from Section 9",
            body: "Motoko tracks a puppet master through mirrors, networks, and borrowed bodies.",
            tagList: ["cyberpunk", "anime", "section9"]);
        var akira = await CreateArticle(cyberpunk,
            title: "Akira Capsule Gang Incident Report",
            description: "Neo Tokyo motorcycles and psychic pressure cookers",
            body: "Kaneda keeps shouting while Tetsuo becomes everyone's emergency meeting.",
            tagList: ["cyberpunk", "neo-tokyo", "capsules"]);
        var zelda = await CreateArticle(hyrule,
            title: "Tears of the Kingdom Sky Island Cartography",
            description: "Ultrahand field notes for suspicious floating rocks",
            body: "Link tapes rockets to almost anything and calls it archaeology.",
            tagList: ["zelda", "hyrule", "sky"]);

        var response = await ghibli.GetAsync("/api/v1/articles?page=1&limit=2&sort=title&order=asc");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<GetArticleResponseDto>>();
        Assert.NotNull(result);
        Assert.Equal(5, result.Total);
        Assert.Equal(new[] { "Akira Capsule Gang Incident Report", "Ghost in the Shell Puppet Master Briefing" },
            result.Data.Select(x => x.Article.Title).ToArray());

        response = await ghibli.GetAsync("/api/v1/articles?page=2&limit=2&sort=title&order=asc");
        response.EnsureSuccessStatusCode();
        result = await response.Content.ReadFromJsonAsync<PaginatedResponse<GetArticleResponseDto>>();
        Assert.NotNull(result);
        Assert.Equal(5, result.Total);
        Assert.Equal(new[] { "Nausicaa Toxic Jungle Field Guide", "Princess Mononoke Forest Treaty" },
            result.Data.Select(x => x.Article.Title).ToArray());

        response = await ghibli.GetAsync("/api/v1/articles?limit=-1&tag=Ghib");
        response.EnsureSuccessStatusCode();
        result = await response.Content.ReadFromJsonAsync<PaginatedResponse<GetArticleResponseDto>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Total);
        Assert.Equal(new[] { mononoke.Article.Slug, nausicaa.Article.Slug }.OrderBy(x => x).ToArray(),
            result.Data.Select(x => x.Article.Slug).OrderBy(x => x).ToArray());

        response = await ghibli.GetAsync("/api/v1/articles?limit=-1&author=major");
        response.EnsureSuccessStatusCode();
        result = await response.Content.ReadFromJsonAsync<PaginatedResponse<GetArticleResponseDto>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Total);
        Assert.All(result.Data, article => Assert.Equal("motoko_major", article.Article.Author.Username));

        response = await ghibli.GetAsync("/api/v1/articles?limit=-1&favorited=faye");
        response.EnsureSuccessStatusCode();
        result = await response.Content.ReadFromJsonAsync<PaginatedResponse<GetArticleResponseDto>>();
        Assert.NotNull(result);
        Assert.Empty(result.Data);
        Assert.Equal(0, result.Total);

        response = await ghibli.GetAsync("/api/v1/articles?limit=-1&tag=cyber&author=motoko");
        response.EnsureSuccessStatusCode();
        result = await response.Content.ReadFromJsonAsync<PaginatedResponse<GetArticleResponseDto>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Total);
        Assert.All(result.Data, article =>
        {
            Assert.Equal("motoko_major", article.Article.Author.Username);
            Assert.Contains(article.Article.TagList, tag => tag == "cyberpunk");
        });

        response = await ghibli.GetAsync("/api/v1/articles?limit=-1&sort=title&order=desc");
        response.EnsureSuccessStatusCode();
        result = await response.Content.ReadFromJsonAsync<PaginatedResponse<GetArticleResponseDto>>();
        Assert.NotNull(result);
        Assert.Equal(5, result.Total);
        Assert.Equal("Tears of the Kingdom Sky Island Cartography", result.Data.First().Article.Title);
    }

    [Fact]
    public async Task GetArticles_SearchMatchesTextAcrossArticleFieldsAndStemmedTerms()
    {
        var client = CreateHttpsClient();
        await RegisterAs(client, "gordon_freeman");
        var silentCart = await CreateArticle(client,
            title: "Half Life Resonance Cascade Cleanup",
            description: "Crowbars, headcrabs, and suspiciously quiet scientists",
            body: "The resonance cascade turns Black Mesa into a physics exam with worse snacks.",
            tagList: ["halflife", "xen", "crowbar"]);
        var portal = await CreateArticle(client,
            title: "Aperture Science Cake Procurement",
            description: "A portal test chamber memo with morally flexible promises",
            body: "Chell solves portal puzzles while GLaDOS mentions cake, science, and momentum.",
            tagList: ["portal", "aperture", "cake"]);
        var metalGear = await CreateArticle(client,
            title: "Metal Gear Codec Snack Review",
            description: "Stealth missions rated by cardboard box comfort",
            body: "Snake learns that a codec call can interrupt any dramatic hallway.",
            tagList: ["stealth", "codec", "box"]);

        var response = await client.GetAsync("/api/v1/articles?limit=-1&search=portal%20science");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<GetArticleResponseDto>>();
        Assert.NotNull(result);
        Assert.Equal(1, result.Total);
        Assert.Equal(portal.Article.Slug, Assert.Single(result.Data).Article.Slug);

        response = await client.GetAsync("/api/v1/articles?limit=-1&search=physics");
        response.EnsureSuccessStatusCode();
        result = await response.Content.ReadFromJsonAsync<PaginatedResponse<GetArticleResponseDto>>();
        Assert.NotNull(result);
        Assert.Equal(1, result.Total);
        Assert.Equal(silentCart.Article.Slug, Assert.Single(result.Data).Article.Slug);

        response = await client.GetAsync("/api/v1/articles?limit=-1&search=scientist");
        response.EnsureSuccessStatusCode();
        result = await response.Content.ReadFromJsonAsync<PaginatedResponse<GetArticleResponseDto>>();
        Assert.NotNull(result);
        Assert.Equal(1, result.Total);
        Assert.Equal(silentCart.Article.Slug, Assert.Single(result.Data).Article.Slug);

        response = await client.GetAsync("/api/v1/articles?limit=-1&search=cardboard");
        response.EnsureSuccessStatusCode();
        result = await response.Content.ReadFromJsonAsync<PaginatedResponse<GetArticleResponseDto>>();
        Assert.NotNull(result);
        Assert.Equal(1, result.Total);
        Assert.Equal(metalGear.Article.Slug, Assert.Single(result.Data).Article.Slug);

        response = await client.GetAsync("/api/v1/articles?limit=-1&search=vortigaunt");
        response.EnsureSuccessStatusCode();
        result = await response.Content.ReadFromJsonAsync<PaginatedResponse<GetArticleResponseDto>>();
        Assert.NotNull(result);
        Assert.Empty(result.Data);
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public async Task GetArticles_ReturnsBadRequestForInvalidQuery()
    {
        var client = CreateHttpsClient();

        var response = await client.GetAsync("/api/v1/articles?page=0");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorMessage(response, "Page", "Page number must be greater than 0.");

        response = await client.GetAsync("/api/v1/articles?limit=0");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorMessage(response, "Limit", "Limit must be greater than 0 or equal to -1 (fetch all).");

        response = await client.GetAsync($"/api/v1/articles?search={new string('x', 101)}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorMessage(response, "Search", "Search query cannot be longer than 100 characters.");
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
