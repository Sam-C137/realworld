using Microsoft.EntityFrameworkCore;
using RealWorldApi.Infrastructure.Data;

namespace RealWorldApi.Core.Domain.Middleware;

public static class MigrateAndSeedDatabaseMiddleware
{
    extension(WebApplication app)
    {
        public async Task UseMigrateAndSeedDatabaseOnStart()
        {
            await app.MigrateDataBase();
            await app.SeedDataBase();
        }
        
        private async Task MigrateDataBase()
        {
            using var scope = app.Services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            var shouldRun = app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup");
            if (!shouldRun) return;
            try
            {
                logger.LogInformation("Applying database migrations...");
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await context.Database.MigrateAsync();
                logger.LogInformation("Database migrations applied successfully");
            }
            catch (Exception e)
            {
                logger.LogError(e, "An error occurred migrating the DB. {exceptionMessage}", e.Message);
                throw; // Re-throw to make startup fail if migrations fail
            }
        }

        private async Task SeedDataBase()
        {
            var scope = app.Services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            var shouldRun = app.Configuration.GetValue<bool>("Database:SeedDatabaseOnStartup") && app.Environment.IsDevelopment();
            if (!shouldRun) return;
            try
            {
                logger.LogInformation("Resetting database...");
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await ResetDatabaseAsync(context);
                logger.LogInformation("Seeding database...");
                await SeedNothing(context);
                logger.LogInformation("Database seeded successfully...");
            }
            catch (Exception e)
            {
                logger.LogError(e, "An error occurred while seeding the DB. {exceptionMessage}", e.Message);
                throw;
            }
        }
    }

    private static async Task SeedNothing(AppDbContext context)
    {
        await Task.CompletedTask;
    }
    
    private static async Task ResetDatabaseAsync(AppDbContext context)
    {
        var tables = context.Model.GetEntityTypes()
            .Select(entityType => new
            {
                Schema = entityType.GetSchema(),
                Table = entityType.GetTableName()
            })
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Table))
            .Distinct()
            .ToList();

        if (tables.Count == 0) return;

        var tableList = string.Join(", ", tables.Select(entry =>
        {
            if (string.IsNullOrWhiteSpace(entry.Schema))
            {
                return $"\"{entry.Table}\"";
            }

            return $"\"{entry.Schema}\".\"{entry.Table}\"";
        }));

        var sql = $"TRUNCATE TABLE {tableList} RESTART IDENTITY CASCADE;";

        await context.Database.ExecuteSqlRawAsync(sql);
    }
}