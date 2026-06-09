using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using RealWorldApi.Core.Features.Users.Dto;
using Xunit.Abstractions;

namespace RealWorldApi.IntegrationTests.Users;

public class UserCurrentIntegrationTest(IntegrationTestContainerFixture fixture, ITestOutputHelper output)
    : IntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task GetUser_ReturnsCurrentUser()
    {
        var client = CreateHttpsClient();
        await AddDefaultAuthHeaders(client, await RegisterUser(client, new RegisterDetails(
            Username: "miles_morales",
            Password: "Dragon1",
            Email: "miles@spiderverse.test")));

        var response = await client.GetAsync("/api/v1/user");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(result);
        Assert.Equal("miles_morales", result.User.Username);
        Assert.Equal("miles@spiderverse.test", result.User.Email);
        Assert.NotEmpty(result.User.Token);
    }

    [Fact]
    public async Task GetUser_EnforcesAuthorization()
    {
        var client = CreateHttpsClient();

        var response = await client.GetAsync("/api/v1/user");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_AcceptsMultipartFormData()
    {
        var client = CreateHttpsClient();
        await AddDefaultAuthHeaders(client, await RegisterUser(client, new RegisterDetails(
            Username: "shuri_panther",
            Password: "Dragon1",
            Email: "shuri@wakanda.test")));
        using var form = CreateUpdateForm(
            username: "shuri_tech",
            email: "shuri.lab@wakanda.test",
            bio: "Vibranium QA lead, part-time roast dispenser.",
            imageBytes: ValidPngBytes(),
            imageContentType: "image/png",
            imageFileName: "kimoyo.png");

        var response = await client.PutAsync("/api/v1/user", form);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(result);
        Assert.Equal("shuri_tech", result.User.Username);
        Assert.Equal("shuri.lab@wakanda.test", result.User.Email);
        Assert.Equal("Vibranium QA lead, part-time roast dispenser.", result.User.Bio);
        Assert.NotNull(result.User.Image);
        Assert.NotEqual("kimoyo.png", result.User.Image);
        Assert.StartsWith(TestObjectStorageService.BaseUrl, result.User.Image);
        Assert.Contains("/profiles/", result.User.Image);
        Assert.EndsWith(".webp", result.User.Image);

        var storage = Factory.Services.GetRequiredService<TestObjectStorageService>();
        var uploaded = Assert.Single(storage.Objects);
        Assert.Equal("image/webp", uploaded.ContentType);
        Assert.NotEmpty(uploaded.Bytes);
        Assert.Equal($"{TestObjectStorageService.BaseUrl}/{uploaded.Key}", result.User.Image);
    }

    [Fact]
    public async Task UpdateUser_AllowsTextOnlyMultipartFormData()
    {
        var client = CreateHttpsClient();
        await AddDefaultAuthHeaders(client, await RegisterUser(client, new RegisterDetails(
            Username: "jinwoo_ranker",
            Password: "Dragon1",
            Email: "jinwoo@hunter.test")));
        using var form = CreateUpdateForm(
            username: "jinwoo_shadow",
            email: null,
            bio: "Arise, but make it strongly typed.",
            imageBytes: null,
            imageContentType: null,
            imageFileName: null);

        var response = await client.PutAsync("/api/v1/user", form);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(result);
        Assert.Equal("jinwoo_shadow", result.User.Username);
        Assert.Equal("jinwoo@hunter.test", result.User.Email);
        Assert.Equal("Arise, but make it strongly typed.", result.User.Bio);
        Assert.Null(result.User.Image);
    }

    [Fact]
    public async Task UpdateUser_ValidatesMultipartFormData()
    {
        var client = CreateHttpsClient();
        await AddDefaultAuthHeaders(client, await RegisterUser(client, new RegisterDetails(
            Username: "ellie_strings",
            Password: "Dragon1",
            Email: "ellie@jackson.test")));

        using var invalidType = CreateUpdateForm(
            username: null,
            email: null,
            bio: null,
            imageBytes: [0x25, 0x50, 0x44, 0x46],
            imageContentType: "application/pdf",
            imageFileName: "not-a-clicker.pdf");
        var response = await client.PutAsync("/api/v1/user", invalidType);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorMessage(response, "Image", "Image must be a JPEG, PNG, or WebP file.");

        using var spoofedType = CreateUpdateForm(
            username: null,
            email: null,
            bio: null,
            imageBytes: [0x25, 0x50, 0x44, 0x46],
            imageContentType: "image/png",
            imageFileName: "joel-promised-this-was-a-png.png");
        response = await client.PutAsync("/api/v1/user", spoofedType);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorMessage(response, "Image", "Image content must be a valid JPEG, PNG, or WebP file.");

        using var tooLarge = CreateUpdateForm(
            username: null,
            email: null,
            bio: null,
            imageBytes: new byte[(5 * 1024 * 1024) + 1],
            imageContentType: "image/png",
            imageFileName: "bloater.png");
        response = await client.PutAsync("/api/v1/user", tooLarge);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorMessage(response, "Image", "Image upload must be at most 5 MB.");

        using var badUsername = CreateUpdateForm(
            username: "jo",
            email: "joelo@adz.test",
            bio: null,
            imageBytes: null,
            imageContentType: null,
            imageFileName: null);
        response = await client.PutAsync("/api/v1/user", badUsername);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorMessage(response, "Username", "Username must be at least 3 characters long.");

        using var badEmail = CreateUpdateForm(
            username: "joel",
            email: "not-email",
            bio: null,
            imageBytes: null,
            imageContentType: null,
            imageFileName: null);
        response = await client.PutAsync("/api/v1/user", badEmail);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorMessage(response, "Email", "Email must be a valid email address.");
    }

    [Fact]
    public async Task UpdateUser_RejectsTakenUsernameAndEmail()
    {
        var client = CreateHttpsClient();
        await AddDefaultAuthHeaders(client, await RegisterUser(client, new RegisterDetails(
            Username: "ripley_nostromo",
            Password: "Dragon1",
            Email: "ripley@nostromo.test")));

        var other = CreateHttpsClient();
        await AddDefaultAuthHeaders(other, await RegisterUser(other, new RegisterDetails(
            Username: "newt_survivor",
            Password: "Dragon1",
            Email: "newt@lv426.test")));

        using var usernameForm = CreateUpdateForm(
            username: "newt_survivor",
            email: null,
            bio: null,
            imageBytes: null,
            imageContentType: null,
            imageFileName: null);
        var response = await client.PutAsync("/api/v1/user", usernameForm);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorMessage(response, "Username", "Username is already taken.");

        using var emailForm = CreateUpdateForm(
            username: null,
            email: "newt@lv426.test",
            bio: null,
            imageBytes: null,
            imageContentType: null,
            imageFileName: null);
        response = await client.PutAsync("/api/v1/user", emailForm);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorMessage(response, "Email", "Email is already taken.");
    }

    private static MultipartFormDataContent CreateUpdateForm(
        string? username,
        string? email,
        string? bio,
        byte[]? imageBytes,
        string? imageContentType,
        string? imageFileName)
    {
        var form = new MultipartFormDataContent();
        if (username is not null) form.Add(new StringContent(username), "Username");
        if (email is not null) form.Add(new StringContent(email), "Email");
        if (bio is not null) form.Add(new StringContent(bio), "Bio");
        if (imageBytes is null) return form;

        var image = new ByteArrayContent(imageBytes);
        if (imageContentType is not null)
        {
            image.Headers.ContentType = new MediaTypeHeaderValue(imageContentType);
        }
        form.Add(image, "Image", imageFileName ?? "profile-image");
        return form;
    }

    private static byte[] ValidPngBytes() =>
        Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAAAAAA6fptVAAAACklEQVR4nGNgAAAAAgABSK+kcQAAAABJRU5ErkJggg==");
}
