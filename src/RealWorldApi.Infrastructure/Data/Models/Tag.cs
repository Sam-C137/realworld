using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace RealWorldApi.Infrastructure.Data.Models;

[Index(nameof(Name), IsUnique = true)]
public class Tag: ITimestampedEntity
{
    public Guid Id { get; set; }
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    
    public ICollection<ArticleTag> ArticleTags { get; set; } = new List<ArticleTag>();
}