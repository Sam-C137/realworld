using Amazon.S3;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RealWorldApi.Infrastructure.Data;
using RealWorldApi.Infrastructure.ObjectStorage;

namespace RealWorldApi.IntegrationTests;

public class CustomWebApplicationFactory(IntegrationTestContainerFixture fixture)
    : WebApplicationFactory<Program>
{
    public const string JwtSecret = "integration-test-secret-with-enough-bytes";
    public const string JwtIssuer = "https://localhost";
    public const string JwtAudience = "https://localhost";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Redis"] = fixture.RedisConnectionString,
                ["Jwt:Secret"] = JwtSecret,
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();

            services.AddDbContextPool<AppDbContext>(options =>
            {
                options.UseNpgsql(fixture.PostgresConnectionString)
                    .UseSnakeCaseNamingConvention();
            });

            services.RemoveAll<IObjectStorageService>();
            services.RemoveAll<IAmazonS3>();
            services.AddSingleton<TestObjectStorageService>();
            services.AddSingleton<IObjectStorageService>(sp => sp.GetRequiredService<TestObjectStorageService>());
        });
    }
}
