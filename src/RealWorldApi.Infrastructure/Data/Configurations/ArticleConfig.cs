using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealWorldApi.Infrastructure.Data.Models;

namespace RealWorldApi.Infrastructure.Data.Configurations;

public class ArticleConfig: IEntityTypeConfiguration<Article>
{
    public void Configure(EntityTypeBuilder<Article> builder)
    {
        builder.Property(x => x.BodyJson)
            .HasColumnType("jsonb");
        
        builder.HasGeneratedTsVectorColumn(
                p => p.SearchVector,
                "english",
                p => new { p.Title, p.Description, p.Body, p.Slug })
            .HasIndex(p => p.SearchVector)
            .HasMethod("gin");
    }
}
