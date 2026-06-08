using System.Net;
using System.Net.Http.Json;
using RealWorldApi.Core.Features.Articles.Dto;
using RealWorldApi.Core.Features.Profiles.Dto;
using RealWorldApi.Core.Features.Users.Dto;
using Xunit.Abstractions;

namespace RealWorldApi.IntegrationTests.Profiles;

public class ProfilesFollowIntegrationTest(IntegrationTestContainerFixture fixture, ITestOutputHelper output)
    : IntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task GetProfile_ReturnsProfileForAnonymousAndAuthenticatedReaders()
    {
        var tanjiro = CreateHttpsClient();
        await RegisterAs(tanjiro, "tanjiro_hanafuda");
        var nezuko = CreateHttpsClient();
        await RegisterAs(nezuko, "nezuko_box");

        var anonymous = CreateHttpsClient();
        var response = await anonymous.GetAsync("/api/v1/profiles/nezuko_box");

        response.EnsureSuccessStatusCode();
        var profile = await response.Content.ReadFromJsonAsync<GetProfileResponseDto>();
        Assert.NotNull(profile);
        Assert.Equal("nezuko_box", profile.Profile.Username);
        Assert.False(profile.Profile.Following);

        response = await tanjiro.GetAsync("/api/v1/profiles/nezuko_box");
        response.EnsureSuccessStatusCode();
        profile = await response.Content.ReadFromJsonAsync<GetProfileResponseDto>();
        Assert.NotNull(profile);
        Assert.Equal("nezuko_box", profile.Profile.Username);
        Assert.False(profile.Profile.Following);
    }

    [Fact]
    public async Task FollowProfile_ReturnsFollowingAndIsIdempotent()
    {
        var joseph = CreateHttpsClient();
        await RegisterAs(joseph, "joseph_joestar");
        var caesar = CreateHttpsClient();
        await RegisterAs(caesar, "caesar_zeppeli");

        var followed = await FollowProfile(joseph, "caesar_zeppeli");
        Assert.Equal("caesar_zeppeli", followed.Profile.Username);
        Assert.True(followed.Profile.Following);

        followed = await FollowProfile(joseph, "caesar_zeppeli");
        Assert.True(followed.Profile.Following);

        var fetchedByJoseph = await GetProfile(joseph, "caesar_zeppeli");
        Assert.True(fetchedByJoseph.Profile.Following);

        var fetchedByAnonymous = await GetProfile(CreateHttpsClient(), "caesar_zeppeli");
        Assert.False(fetchedByAnonymous.Profile.Following);
    }

    [Fact]
    public async Task FollowProfile_InvalidatesArticleFollowingMetadata()
    {
        var reader = CreateHttpsClient();
        await RegisterAs(reader, "usopp_reader");
        var author = CreateHttpsClient();
        await RegisterAs(author, "sanji_cook");
        var article = await CreateArticle(author,
            title: "Baratie Soup Timing Protocol",
            description: "Kicks, cuisine, and absolutely no wasted food",
            body: "Sanji explains why the soup is late only if you enjoy being kicked into exposition.",
            tagList: ["baratie", "cooking", "onepiece"]);

        var beforeFollow = await GetArticle(reader, article.Article.Slug);
        Assert.False(beforeFollow.Article.Author.Following);

        await FollowProfile(reader, "sanji_cook");

        var afterFollow = await GetArticle(reader, article.Article.Slug);
        Assert.True(afterFollow.Article.Author.Following);
    }

    [Fact]
    public async Task UnfollowProfile_ReturnsNotFollowingAndIsIdempotent()
    {
        var yuji = CreateHttpsClient();
        await RegisterAs(yuji, "yuji_vessel");
        var megumi = CreateHttpsClient();
        await RegisterAs(megumi, "megumi_shadows");

        await FollowProfile(yuji, "megumi_shadows");

        var unfollowed = await UnfollowProfile(yuji, "megumi_shadows");
        Assert.Equal("megumi_shadows", unfollowed.Profile.Username);
        Assert.False(unfollowed.Profile.Following);

        unfollowed = await UnfollowProfile(yuji, "megumi_shadows");
        Assert.False(unfollowed.Profile.Following);

        var fetched = await GetProfile(yuji, "megumi_shadows");
        Assert.False(fetched.Profile.Following);
    }

    [Fact]
    public async Task FollowProfile_EnforcesAuthorizationNotFoundAndSelfFollowRules()
    {
        var anonymous = CreateHttpsClient();

        var response = await anonymous.PostAsync("/api/v1/profiles/missing_senpai/follow", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        response = await anonymous.DeleteAsync("/api/v1/profiles/missing_senpai/follow");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var mob = CreateHttpsClient();
        await RegisterAs(mob, "mob_psychic");

        response = await mob.GetAsync("/api/v1/profiles/reigen_master");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        response = await mob.PostAsync("/api/v1/profiles/reigen_master/follow", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        response = await mob.DeleteAsync("/api/v1/profiles/reigen_master/follow");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        response = await mob.PostAsync("/api/v1/profiles/mob_psychic/follow", null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        response = await mob.DeleteAsync("/api/v1/profiles/mob_psychic/follow");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task RegisterAs(HttpClient client, string username)
    {
        await AddDefaultAuthHeaders(client, await RegisterUser(client, new RegisterDetails(
            Username: username,
            Password: "Dragon1",
            Email: $"{username}@realworld.test")));
    }

    private static async Task<GetProfileResponseDto> GetProfile(HttpClient client, string username)
    {
        var response = await client.GetAsync($"/api/v1/profiles/{username}");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetProfileResponseDto>();
        Assert.NotNull(result);
        return result;
    }

    private static async Task<GetProfileResponseDto> FollowProfile(HttpClient client, string username)
    {
        var response = await client.PostAsync($"/api/v1/profiles/{username}/follow", null);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetProfileResponseDto>();
        Assert.NotNull(result);
        return result;
    }

    private static async Task<GetProfileResponseDto> UnfollowProfile(HttpClient client, string username)
    {
        var response = await client.DeleteAsync($"/api/v1/profiles/{username}/follow");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetProfileResponseDto>();
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

    private static async Task<GetArticleResponseDto> GetArticle(HttpClient client, string slug)
    {
        var response = await client.GetAsync($"/api/v1/articles/{slug}");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetArticleResponseDto>();
        Assert.NotNull(result);
        return result;
    }
}
