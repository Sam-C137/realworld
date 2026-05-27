using Microsoft.EntityFrameworkCore;

namespace RealWorldApi.Infrastructure.Data.Models;

[Index(nameof(UserId), nameof(ArticleId), IsUnique = true)]
[Index(nameof(ArticleId))]
public class Likes: ITimestampedEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ArticleId { get; set; }
    
    public User User { get; set; } = null!;
    public Article Article { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
