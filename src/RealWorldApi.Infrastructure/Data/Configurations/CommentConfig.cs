using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealWorldApi.Infrastructure.Data.Models;

namespace RealWorldApi.Infrastructure.Data.Configurations;

public class CommentConfig: IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.Property(x => x.BodyJson)
            .HasColumnType("jsonb");

        builder.HasIndex(x => new { x.ArticleId, x.Id })
            .HasFilter("\"deleted_at\" IS NULL")
            .HasDatabaseName("ix_comments_articles_active");
        
        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}
