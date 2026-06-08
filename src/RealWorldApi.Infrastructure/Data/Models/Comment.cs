using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace RealWorldApi.Infrastructure.Data.Models;

[Index(nameof(ArticleId))]
[Index(nameof(AuthorId))]
public class Comment: ITimestampedEntity
{
    public Guid Id { get; set; }
    [Required, MaxLength(1024)]
    public string Body { get; set; } = null!;
    public JsonDocument? BodyJson { get; set; }
    public Guid ArticleId { get; set; }
    public Article Article { get; set; } = null!;
    public Guid AuthorId { get; set; }
    public Profile Author { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}