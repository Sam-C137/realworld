using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace RealWorldApi.Infrastructure.Data.Models;

[Index(nameof(Slug), IsUnique = true)]
[Index(nameof(AuthorId))]
public class Article: ITimestampedEntity
{
    public Guid Id { get; set; }
    [MaxLength(255)]
    public string Slug { get; set; } = null!;
    [Required, MaxLength(510)]
    public string Title { get; set; } = null!;
    [MaxLength(1024)]
    public string Description { get; set; } = null!;
    public string Body { get; set; } = null!;
    public JsonDocument? BodyJson { get; set; }
    public NpgsqlTsVector SearchVector { get; set; } = null!;

    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    
    public ICollection<ArticleTag> ArticleTags { get; set; } = new List<ArticleTag>();
}