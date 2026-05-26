using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealWorldApi.Infrastructure.Data.Models;

namespace RealWorldApi.Infrastructure.Data.Configurations;

public class UserSessionConfig: IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.HasIndex(x => x.RevokedAt)
            .HasFilter("\"is_revoked\" = true and \"revoked_at\" is not null");
    }
}
