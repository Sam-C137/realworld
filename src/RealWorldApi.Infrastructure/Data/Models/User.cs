using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace RealWorldApi.Infrastructure.Data.Models;

[Index(nameof(Email), IsUnique = true)]
[Index(nameof(Username), IsUnique = true)]
public class User: ITimestampedEntity
{
    public Guid Id { get; set; }
    [Required, MaxLength(255), EmailAddress]
    public string Email { get; set; } = null!;
    [Required, MaxLength(255)]
    public string Username { get; set; } = null!;
    [MaxLength(255)]
    public string? FirstName { get; set; }
    [MaxLength(255)]
    public string? LastName { get; set; }
    [MaxLength(1024)]
    public string? Bio { get; set; }
    [MaxLength(510)]
    public string? Image { get; set; }

    [MaxLength(510)]
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
[Index(nameof(ExpiresAt))]
[Index(nameof(RefreshTokenHash))]
public class UserSession: ITimestampedEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    [MaxLength(510)]
    public string RefreshTokenHash { get; set; } = null!;
    public bool IsRevoked { get; set; }
    public int SessionVersion { get; set; } = 1;
    public DateTime? LastUsedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    [MaxLength(255)]
    public string? RevokedReason { get; set; }
    public DateTime ExpiresAt { get; set; }
    [MaxLength(255)]
    public string IpAddress { get; set; } = null!;
    [MaxLength(255)]
    public string UserAgent { get; set; } = null!;
    [MaxLength(255)]
    public string? DeviceFingerprintHash { get; set; }
    [MaxLength(255)]
    public string AuthStrength { get; set; } = "password";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
