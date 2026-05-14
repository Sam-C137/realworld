using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using RealWorldApi.Infrastructure.Data;

namespace RealWorldApi.Core.Features.Users;

[Index(nameof(Email), IsUnique = true)]
public class User: ITimestampedEntity
{
    public Guid Id { get; set; }
    [Required, MaxLength(255), EmailAddress]
    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;
    public int SessionVersion { get; set; } = 1;
    public ICollection<UserRole> Roles { get; set; } = new List<UserRole>();

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}

[PrimaryKey(nameof(UserId), nameof(Role))]
public class UserRole
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Role { get; set; } = null!;
}

[Index(nameof(UserId))]
[Index(nameof(UserId), nameof(IsRevoked))]
[Index(nameof(RefreshTokenHash), IsUnique = true)]
public class UserSession: ITimestampedEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string RefreshTokenHash { get; set; } = null!;
    public bool IsRevoked { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedReason { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string IpAddress { get; set; } = null!;
    public string UserAgent { get; set; } = null!;
    public string? DeviceName { get; set; }
    public string? DeviceFingerprintHash { get; set; }
    public string AuthStrength { get; set; } = "password";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}