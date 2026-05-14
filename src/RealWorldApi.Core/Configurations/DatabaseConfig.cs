using Microsoft.EntityFrameworkCore;
using RealWorldApi.Infrastructure.Data;

namespace RealWorldApi.Core.Configurations;

public static class DatabaseConfig
{
    public static IServiceCollection AddDatabaseConfig(this WebApplicationBuilder builder, IServiceCollection services)
    {
        services.AddDbContextPool<AppDbContext>(options =>
        {
            var npgsql = options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
                .UseSnakeCaseNamingConvention();

            if (!builder.Environment.IsDevelopment()) return;
            npgsql.EnableSensitiveDataLogging();
            npgsql.EnableDetailedErrors();
        });
        return services;
    }
}