using System.Security.Claims;
using ErrorOr;
using Mapster;
using Microsoft.EntityFrameworkCore;
using RealWorldApi.Core.Features.Articles.Services;
using RealWorldApi.Core.Features.Users.Dto;
using RealWorldApi.Infrastructure.Data;
using RealWorldApi.Infrastructure.ObjectStorage;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace RealWorldApi.Core.Features.Users.Services;

public class UserService(AppDbContext db, IHttpContextAccessor http, IObjectStorageService r2, ArticleCacheService acs)
    : IUserService
{
    public async Task<ErrorOr<LoginResponseDto>> GetUser()
    {
        var userId = Guid.TryParse(http.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsed) ? parsed
            : Guid.Empty;
        var token = http.HttpContext?.Request.Headers.Authorization.ToString().Split(" ").LastOrDefault();
        var csrfToken = http.HttpContext?.Request.Headers[CookieHelper.CsrfHeader].ToString();
        if (userId == Guid.Empty || token is null) return Error.Unauthorized();

        var user = await db.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return Error.NotFound();

        return user.
            BuildAdapter()
            .AddParameters("token", token)
            .AddParameters("csrfToken", csrfToken!)
            .AdaptToType<LoginResponseDto>();
    }

    public async Task<ErrorOr<LoginResponseDto>> UpdateUser(UpdateUserRequestDto request)
    {
        var userId = Guid.TryParse(http.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsed) ? parsed
            : Guid.Empty;
        var token = http.HttpContext?.Request.Headers.Authorization.ToString().Split(" ").LastOrDefault();
        var csrfToken = http.HttpContext?.Request.Headers[CookieHelper.CsrfHeader].ToString();
        if (userId == Guid.Empty || token is null) return Error.Unauthorized();

        var user = await db.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return Error.NotFound();

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            user.Email = request.Email;
        }

        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            user.Profile.Username = request.Username;
        }

        if (request.Bio is not null)
        {
            user.Profile.Bio = string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio;
        }

        if (request.Image is not null)
        {
            var processed = await PrepareProfileImageForUpload(user.Id, request.Image);
            if (processed.IsError) return processed.Errors;
            var (key, file) = processed.Value;
            await using var stream = new MemoryStream(file);
            var url = await r2.UploadAsync(key, stream, "image/webp");
            if (url is null) return Error.Failure(description: "Failed to upload profile image");
            
            user.Profile.Image = url;
        }

        await db.SaveChangesAsync();
        await acs.BumpVersionAsync();

        return user
            .BuildAdapter()
            .AddParameters("token", token)
            .AddParameters("csrfToken", csrfToken!)
            .AdaptToType<LoginResponseDto>();
    }

    /// <summary>
    /// Converts a user-uploaded profile image into the normalized asset shape we want to upload later:
    /// a generated profile-image key and WebP bytes capped by <see cref="UpdateUserRequestValidator.MaxFinalImageSizeBytes"/>.
    /// The initial resize keeps large originals from wasting encode work while preserving enough pixels for profile UI.
    /// </summary>
    private static async Task<ErrorOr<(string Key, byte[] File)>> PrepareProfileImageForUpload(Guid userId, IFormFile image)
    {
        try
        {
            await using var input = image.OpenReadStream();
            using var loaded = await Image.LoadAsync(input);

            // if image is larger than 1024 * 1024 (arbitrary for avatar) resize down to 1024 * 1024
            if (loaded.Width > 1024 || loaded.Height > 1024)
            {
                loaded.Mutate(ctx => ctx.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(1024, 1024)
                }));
            }

            var output = await EncodeWebpWithinLimit(loaded);
            if (output.Length > UpdateUserRequestValidator.MaxFinalImageSizeBytes)
            {
                return Error.Validation(description: "Image must be at most 2 MB after processing.");
            }

            var key = $"profiles/{userId}/{Guid.CreateVersion7()}.webp";
            return (key, output);
        }
        catch (Exception)
        {
            return Error.Validation(description: "Image content must be a valid JPEG, PNG, or WebP file.");
        }
    }

    /// <summary>
    /// Attempts to keep the image at its current dimensions first by lowering WebP quality in small steps.
    /// If quality-only compression still misses the final size cap, it retries with smaller max edges
    /// (768, 512, then 256 pixels) at a middle quality before returning one final low-quality encode for the caller to reject.
    /// </summary>
    private static async Task<byte[]> EncodeWebpWithinLimit(Image image)
    {
        foreach (var quality in new[] {82, 74, 66, 58})
        {
            var bytes = await EncodeWebp(image, quality);
            if (bytes.Length <= UpdateUserRequestValidator.MaxFinalImageSizeBytes) return bytes;
        }

        for (var edge = 768; edge >= 256; edge -= 256)
        {
            var e = edge;
            using var clone = image.Clone(ctx => ctx.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(e, e)
            }));
            var bytes = await EncodeWebp(clone, 66);
            if (bytes.Length <= UpdateUserRequestValidator.MaxFinalImageSizeBytes) return bytes;
        }

        return await EncodeWebp(image, 50);
    }

    /// <summary>
    /// Encodes the provided image as WebP at the requested quality and returns the resulting upload bytes.
    /// The caller owns deciding whether those bytes are small enough for the profile-image policy.
    /// </summary>
    private static async Task<byte[]> EncodeWebp(Image image, int quality)
    {
        await using var output = new MemoryStream();
        await image.SaveAsWebpAsync(output, new WebpEncoder
        {
            Quality = quality
        });
        return output.ToArray();
    }
}
