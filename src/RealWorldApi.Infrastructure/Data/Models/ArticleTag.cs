using Microsoft.EntityFrameworkCore;

namespace RealWorldApi.Infrastructure.Data.Models;

[Index(nameof(ArticleId), nameof(TagId), IsUnique = true)]
public class ArticleTag: ITimestampedEntity
{
    public Guid Id { get; set; }
    public Guid ArticleId { get; set; }
    public Article Article { get; set; } = null!;
    
    public Guid TagId { get; set; }
    public Tag Tag { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}