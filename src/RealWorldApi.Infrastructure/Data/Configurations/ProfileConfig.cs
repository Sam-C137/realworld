using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealWorldApi.Infrastructure.Data.Models;

namespace RealWorldApi.Infrastructure.Data.Configurations;

public class ProfileConfig: IEntityTypeConfiguration<Profile>
{
    public void Configure(EntityTypeBuilder<Profile> builder)
    {
        builder.HasMany(x => x.Follows)
            .WithOne(x => x.Follower)
            .HasForeignKey(x => x.FollowerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.FollowedBy)
            .WithOne(x => x.Followee)
            .HasForeignKey(x => x.FolloweeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.Bio)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops")
            .HasFilter("\"bio\" is not null and \"bio\" <> ''");
    }
}
