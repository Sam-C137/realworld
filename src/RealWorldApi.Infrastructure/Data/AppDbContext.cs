using Microsoft.EntityFrameworkCore;
using RealWorldApi.Infrastructure.Data.Models;

namespace RealWorldApi.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<Article> Articles { get; set; }
    public DbSet<ArticleTag> ArticleTags { get; set; }
    public DbSet<Profile> Profiles { get; set; }
    public DbSet<Follow> Follows { get; set; }
    public DbSet<Likes> Likes { get; set; }
    public DbSet<Comment> Comments { get; set; }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasPostgresExtension("pg_trgm");
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
    
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetId();
        SetTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        SetId();
        SetTimestamps();
        return base.SaveChanges();
    }

    private void SetTimestamps()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<ITimestampedEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }
    }

    private void SetId()
    {
        foreach (var entry in ChangeTracker.Entries<IEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (string.IsNullOrWhiteSpace(entry.Entity.Id.ToString()))
                {
                    entry.Entity.Id = Guid.CreateVersion7();
                }
            }
        }
    }
}