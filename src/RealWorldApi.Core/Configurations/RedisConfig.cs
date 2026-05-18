using StackExchange.Redis;

namespace RealWorldApi.Core.Configurations;

public static class RedisConfig
{
    public static void AddRedisConfig(this WebApplicationBuilder builder)
    {
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = builder.Configuration.GetConnectionString("Redis");
        });
        builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(
                builder.Configuration.GetConnectionString("Redis")!));
        builder.Services.AddScoped<IDatabase>(sp =>
            sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
    }
}
