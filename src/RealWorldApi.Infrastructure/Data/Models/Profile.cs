using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace RealWorldApi.Infrastructure.Data.Models;

[Index(nameof(Username), IsUnique = true)]
[Index(nameof(UserId), IsUnique = true)]
public class Profile: ITimestampedEntity
{
    public Guid Id { get; set; }

    [Required, MaxLength(255)]
    public string Username { get; set; } = null!;
    [MaxLength(1024)]
    public string? Bio { get; set; }
    [MaxLength(510)]
    public string? Image { get; set; }
    
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public ICollection<Follow> Follows { get; set; } = new List<Follow>();
    public ICollection<Follow> FollowedBy { get; set; } = new List<Follow>();

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
