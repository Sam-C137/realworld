using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealWorldApi.Infrastructure.Data.Models;

namespace RealWorldApi.Infrastructure.Data.Configurations;

public class TagConfig: IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        // builder.HasIndex(x => x.Name)
        //     .HasMethod("gin")
        //     .HasOperators("gin_trgm_ops");
    }
}