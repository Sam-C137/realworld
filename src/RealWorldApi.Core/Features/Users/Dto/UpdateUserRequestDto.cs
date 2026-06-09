using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using RealWorldApi.Infrastructure.Data;

namespace RealWorldApi.Core.Features.Users.Dto;

public class UpdateUserRequestDto
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? Bio { get; set; }
    public IFormFile? Image { get; set; }
}

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequestDto>
{
    public const long MaxFinalImageSizeBytes = 2 * 1024 * 1024; // 2 MB
    public const long MaxUploadImageSizeBytes = 5 * 1024 * 1024; // 5 MB
    private static readonly HashSet<string> AllowedImageContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];

    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;

    public UpdateUserRequestValidator(AppDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;

        RuleFor(x => x.Username)
            .Cascade(CascadeMode.Stop)
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("Username can only contain letters, numbers, and underscores.")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters long.")
            .MaximumLength(20).WithMessage("Username must be at most 20 characters long.")
            .MustAsync(BeUniqueUsername).WithMessage("Username is already taken.")
            .When(x => !string.IsNullOrEmpty(x.Username));
        
        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .MustAsync(BeUniqueEmail).WithMessage("Email is already taken.")
            .When(x => !string.IsNullOrEmpty(x.Email));
        
        RuleFor(x => x.Bio)
            .MaximumLength(255).WithMessage("Bio must be at most 255 characters long.")
            .When(x => !string.IsNullOrEmpty(x.Bio));
        
        RuleFor(x => x.Image)
            .Must(file => file is null || file.Length > 0)
            .WithMessage("Image must not be empty.")
            .Must(file => file is null || file.Length <= MaxUploadImageSizeBytes)
            .WithMessage("Image upload must be at most 5 MB.")
            .Must(file => file is null || AllowedImageContentTypes.Contains(file.ContentType.ToLowerInvariant()))
            .WithMessage("Image must be a JPEG, PNG, or WebP file.")
            .Must(HasAllowedImageSignature)
            .WithMessage("Image content must be a valid JPEG, PNG, or WebP file.");
    }

    private async Task<bool> BeUniqueUsername(string? username, CancellationToken token)
    {
        if (username is null) return false;
        var userId = GetCurrentUserId();
        return !await _db.Profiles.AnyAsync(u => u.Username == username && u.UserId != userId, token);
    }

    private async Task<bool> BeUniqueEmail(string? email, CancellationToken token)
    {
        if (email is null) return false;
        var userId = GetCurrentUserId();
        return !await _db.Users.AnyAsync(u => u.Email == email && u.Id != userId, token);
    }

    private Guid GetCurrentUserId()
    {
        return Guid.TryParse(_http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : Guid.Empty;
    }

    private static bool HasAllowedImageSignature(IFormFile? file)
    {
        if (file is null) return true;

        Span<byte> header = stackalloc byte[12];
        using var stream = file.OpenReadStream();
        var read = stream.Read(header);
        return IsJpeg(header, read) || IsPng(header, read) || IsWebp(header, read);
    }

    // Signatures follow the public PNG/JPEG/WebP container headers:
    // PNG: 89 50 4E 47 0D 0A 1A 0A, JPEG: FF D8 FF, WebP: RIFF....WEBP.
    private static bool IsJpeg(ReadOnlySpan<byte> header, int read)
    {
        return read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
    }

    private static bool IsPng(ReadOnlySpan<byte> header, int read)
    {
        return read >= 8 &&
               header[0] == 0x89 &&
               header[1] == 0x50 &&
               header[2] == 0x4E &&
               header[3] == 0x47 &&
               header[4] == 0x0D &&
               header[5] == 0x0A &&
               header[6] == 0x1A &&
               header[7] == 0x0A;
    }

    private static bool IsWebp(ReadOnlySpan<byte> header, int read)
    {
        return read >= 12 &&
               header[0] == 0x52 &&
               header[1] == 0x49 &&
               header[2] == 0x46 &&
               header[3] == 0x46 &&
               header[8] == 0x57 &&
               header[9] == 0x45 &&
               header[10] == 0x42 &&
               header[11] == 0x50;
    }
}
