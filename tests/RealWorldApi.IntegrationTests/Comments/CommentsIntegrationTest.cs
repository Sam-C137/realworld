using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RealWorldApi.Core.Abstractions;
using RealWorldApi.Core.Features.Articles.Dto;
using RealWorldApi.Core.Features.Comments.Dto;
using RealWorldApi.Core.Features.Users.Dto;
using RealWorldApi.Infrastructure.Data;
using RealWorldApi.Infrastructure.Data.Models;
using Xunit.Abstractions;

namespace RealWorldApi.IntegrationTests.Comments;

public class CommentsIntegrationTest(IntegrationTestContainerFixture fixture, ITestOutputHelper output)
    : IntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task CreateComment_ReturnsCreatedResult()
    {
        var articleAuthor = CreateHttpsClient();
        await RegisterAs(articleAuthor, "laios_touden");
        var article = await CreateArticle(articleAuthor,
            title: "Dungeon Meshi Monster Menu",
            description: "A completely normal culinary dungeon audit",
            body: "Laios insists the basilisk is locally sourced and emotionally complicated.",
            tagList: ["dungeon", "meshi", "snacks"]);

        var commenter = CreateHttpsClient();
        await RegisterAs(commenter, "marcille_spellbook");
        var response = await commenter.PostAsJsonAsync($"/api/v1/articles/{article.Article.Slug}/comments",
            new CreateCommentRequestDto
            {
                Comment = new CreateCommentRequestDetails(
                    Body: "This is not a meal plan, this is a boss fight with garnish.",
                    BodyJson: """{"mood":"concerned","spiceLevel":0}""")
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<GetCommentResponseDto>();
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("This is not a meal plan, this is a boss fight with garnish.", result.Body);
        Assert.NotNull(result.BodyJson);
        Assert.Equal("marcille_spellbook", result.Author.Username);
        Assert.False(result.Author.Following);
        Assert.True(result.CreatedAt <= result.UpdatedAt);

        response = await commenter.GetAsync($"/api/v1/articles/{article.Article.Slug}/comments/{result.Id}");
        response.EnsureSuccessStatusCode();
        var fetched = await response.Content.ReadFromJsonAsync<GetCommentResponseDto>();
        Assert.NotNull(fetched);
        Assert.Equal(result.Id, fetched.Id);
        Assert.Equal(result.Body, fetched.Body);
        Assert.Equal("marcille_spellbook", fetched.Author.Username);
    }

    [Fact]
    public async Task GetComments_ReturnsCommentsForArticleWithFollowingFlag()
    {
        var articleAuthor = CreateHttpsClient();
        await RegisterAs(articleAuthor, "loid_forger");
        var targetArticle = await CreateArticle(articleAuthor,
            title: "Spy Family Dinner Diplomacy",
            description: "World peace by omelet, peanuts, and suspicious smiles",
            body: "Operation Strix survives because everyone is lying in a strangely helpful direction.",
            tagList: ["spy", "family", "waku"]);
        var decoyArticle = await CreateArticle(articleAuthor,
            title: "Bond Forger Future Sight Walkies",
            description: "A dog predicts snacks and occasionally geopolitics",
            body: "Bond sees the future and still cannot avoid bath time.",
            tagList: ["spy", "family", "bond"]);

        var reader = CreateHttpsClient();
        await RegisterAs(reader, "anya_reader");
        var yor = CreateHttpsClient();
        await RegisterAs(yor, "yor_thorn");
        var franky = CreateHttpsClient();
        await RegisterAs(franky, "franky_infos");

        var yorComment = await CreateComment(yor, targetArticle.Article.Slug,
            "I can solve this dinner problem with one extremely normal kick.");
        var frankyComment = await CreateComment(franky, targetArticle.Article.Slug,
            "Peanuts remain the strongest operational incentive.");
        await CreateComment(franky, decoyArticle.Article.Slug,
            "This comment belongs to another mission file.");
        await FollowUser("anya_reader", "yor_thorn");

        var response = await reader.GetAsync($"/api/v1/articles/{targetArticle.Article.Slug}/comments?limit=10");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CursorPaginatedResponse<GetCommentResponseDto>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Data.Count);
        Assert.Contains(result.Data, comment => comment.Id == yorComment.Id);
        Assert.Contains(result.Data, comment => comment.Id == frankyComment.Id);

        var followedAuthorComment = Assert.Single(result.Data, comment => comment.Author.Username == "yor_thorn");
        Assert.True(followedAuthorComment.Author.Following);
        var regularComment = Assert.Single(result.Data, comment => comment.Author.Username == "franky_infos");
        Assert.False(regularComment.Author.Following);
        Assert.DoesNotContain(result.Data, comment => comment.Body.Contains("another mission file"));
    }

    [Fact]
    public async Task GetComments_HasWorkingCursorPaginationAndSearch()
    {
        var articleAuthor = CreateHttpsClient();
        await RegisterAs(articleAuthor, "clive_rosfield");
        var article = await CreateArticle(articleAuthor,
            title: "Final Fantasy XVI Eikon Incident Review",
            description: "A calm report about fire, revenge, and definitely more fire",
            body: "Clive files the report under extremely normal workplace escalation.",
            tagList: ["finalfantasy", "eikon", "phoenix"]);

        var cid = CreateHttpsClient();
        await RegisterAs(cid, "cid_hideaway");
        var jill = CreateHttpsClient();
        await RegisterAs(jill, "jill_ice");
        var torgal = CreateHttpsClient();
        await RegisterAs(torgal, "torgal_goodest");

        var cidComment = await CreateComment(cid, article.Article.Slug,
            "Hideaway maintenance says the crystal budget is on fire again.");
        var jillComment = await CreateComment(jill, article.Article.Slug,
            "Ice magic is not a substitute for emotional processing.");
        var torgalComment = await CreateComment(torgal, article.Article.Slug,
            "Approved by one loyal hound with excellent battle timing.");

        var response = await cid.GetAsync($"/api/v1/articles/{article.Article.Slug}/comments?limit=2");
        response.EnsureSuccessStatusCode();
        var firstPage = await response.Content.ReadFromJsonAsync<CursorPaginatedResponse<GetCommentResponseDto>>();
        Assert.NotNull(firstPage);
        Assert.Equal(2, firstPage.Data.Count);
        Assert.NotNull(firstPage.Next);
        Assert.Null(firstPage.Previous);

        response = await cid.GetAsync($"/api/v1/articles/{article.Article.Slug}/comments?limit=2&after={firstPage.Next}");
        response.EnsureSuccessStatusCode();
        var secondPage = await response.Content.ReadFromJsonAsync<CursorPaginatedResponse<GetCommentResponseDto>>();
        Assert.NotNull(secondPage);
        Assert.Single(secondPage.Data);
        Assert.NotNull(secondPage.Previous);

        var seenIds = firstPage.Data.Concat(secondPage.Data).Select(comment => comment.Id).OrderBy(x => x).ToArray();
        Assert.Equal(new[] { cidComment.Id, jillComment.Id, torgalComment.Id }.OrderBy(x => x).ToArray(), seenIds);

        response = await cid.GetAsync($"/api/v1/articles/{article.Article.Slug}/comments?search=emotional");
        response.EnsureSuccessStatusCode();
        var searched = await response.Content.ReadFromJsonAsync<CursorPaginatedResponse<GetCommentResponseDto>>();
        Assert.NotNull(searched);
        var match = Assert.Single(searched.Data);
        Assert.Equal(jillComment.Id, match.Id);
    }

    [Fact]
    public async Task Comments_EnforceAuthorization()
    {
        var articleAuthor = CreateHttpsClient();
        await RegisterAs(articleAuthor, "sam_porter");
        var article = await CreateArticle(articleAuthor,
            title: "Death Stranding Ladder Placement Rules",
            description: "Infrastructure, rain, and deeply dramatic walking",
            body: "Sam puts a ladder over a rock and civilization briefly improves.",
            tagList: ["strand", "porter", "bbpod"]);
        var comment = await CreateComment(articleAuthor, article.Article.Slug,
            "Please like, subscribe, and return the ladder.");

        var anonymous = CreateHttpsClient();

        var response = await anonymous.GetAsync($"/api/v1/articles/{article.Article.Slug}/comments");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        response = await anonymous.GetAsync($"/api/v1/articles/{article.Article.Slug}/comments/{comment.Id}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        response = await anonymous.PostAsJsonAsync($"/api/v1/articles/{article.Article.Slug}/comments",
            new CreateCommentRequestDto
            {
                Comment = new CreateCommentRequestDetails("BB gives this route five tiny thumbs.", null)
            });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        response = await anonymous.DeleteAsync($"/api/v1/articles/{article.Article.Slug}/comments/{comment.Id}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateComment_ReturnsBadRequestAndNotFoundForInvalidRequests()
    {
        var articleAuthor = CreateHttpsClient();
        await RegisterAs(articleAuthor, "zelda_scholar");
        var article = await CreateArticle(articleAuthor,
            title: "Tears of the Kingdom Zonai Device Warranty",
            description: "Rockets taped to shields remain legally complicated",
            body: "The warranty does not cover falling upward with confidence.",
            tagList: ["zelda", "zonai", "warranty"]);

        var commenter = CreateHttpsClient();
        await RegisterAs(commenter, "purah_pad");

        var response = await commenter.PostAsJsonAsync($"/api/v1/articles/{article.Article.Slug}/comments",
            new CreateCommentRequestDto());
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorMessage(response, "Comment", "The Comment field is required.");

        response = await commenter.PostAsJsonAsync($"/api/v1/articles/{article.Article.Slug}/comments",
            new CreateCommentRequestDto
            {
                Comment = new CreateCommentRequestDetails("ok", null)
            });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorMessage(response, "Comment.Body", "Body must be at least 3 characters long.");

        response = await commenter.PostAsJsonAsync($"/api/v1/articles/{article.Article.Slug}/comments",
            new CreateCommentRequestDto
            {
                Comment = new CreateCommentRequestDetails("This JSON belongs in the Depths.", "{nope")
            });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorMessage(response, "Comment.BodyJson", "BodyJson must be a valid JSON string.");

        response = await commenter.PostAsJsonAsync("/api/v1/articles/missing-master-sword/comments",
            new CreateCommentRequestDto
            {
                Comment = new CreateCommentRequestDetails("Has anyone seen a glowing sword around here?", null)
            });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetComments_ReturnsBadRequestAndNotFoundForInvalidRequests()
    {
        var client = CreateHttpsClient();
        await RegisterAs(client, "sora_keyblade");

        var response = await client.GetAsync("/api/v1/articles/missing-gummi-route/comments");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        response = await client.GetAsync("/api/v1/articles/missing-gummi-route/comments?limit=0");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorMessage(response, "Limit", "Limit must be greater than 0.");

        response = await client.GetAsync("/api/v1/articles/missing-gummi-route/comments?after=not-a-guid");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorMessage(response, "After", "After must be a valid UUID or null.");
    }

    [Fact]
    public async Task DeleteComment_DeletesOnlyForAuthor()
    {
        var articleAuthor = CreateHttpsClient();
        await RegisterAs(articleAuthor, "phoenix_wright");
        var article = await CreateArticle(articleAuthor,
            title: "Ace Attorney Courtroom Objection Ledger",
            description: "Turnabouts, ladders, and very suspicious parrots",
            body: "Phoenix updates the ledger whenever someone yells with legal precision.",
            tagList: ["attorney", "objection", "turnabout"]);

        var maya = CreateHttpsClient();
        await RegisterAs(maya, "maya_burger");
        var edgeworth = CreateHttpsClient();
        await RegisterAs(edgeworth, "edgeworth_cravat");
        var comment = await CreateComment(maya, article.Article.Slug,
            "This testimony needs more burgers and fewer contradictions.");

        var response = await edgeworth.DeleteAsync($"/api/v1/articles/{article.Article.Slug}/comments/{comment.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        response = await maya.GetAsync($"/api/v1/articles/{article.Article.Slug}/comments/{comment.Id}");
        response.EnsureSuccessStatusCode();

        response = await maya.DeleteAsync($"/api/v1/articles/{article.Article.Slug}/comments/{comment.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        response = await maya.GetAsync($"/api/v1/articles/{article.Article.Slug}/comments/{comment.Id}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        response = await maya.GetAsync($"/api/v1/articles/{article.Article.Slug}/comments");
        response.EnsureSuccessStatusCode();
        var comments = await response.Content.ReadFromJsonAsync<CursorPaginatedResponse<GetCommentResponseDto>>();
        Assert.NotNull(comments);
        Assert.Empty(comments.Data);

        response = await maya.DeleteAsync($"/api/v1/articles/{article.Article.Slug}/comments/{Guid.CreateVersion7()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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

    private static async Task<GetCommentResponseDto> CreateComment(
        HttpClient client,
        string slug,
        string body,
        string? bodyJson = null)
    {
        var response = await client.PostAsJsonAsync($"/api/v1/articles/{slug}/comments", new CreateCommentRequestDto
        {
            Comment = new CreateCommentRequestDetails(body, bodyJson)
        });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetCommentResponseDto>();
        Assert.NotNull(result);
        return result;
    }
}
