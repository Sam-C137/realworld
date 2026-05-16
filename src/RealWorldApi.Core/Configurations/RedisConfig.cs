using StackExchange.Redis;

namespace RealWorldApi.Core.Configurations;

public static class RedisConfig
{
    public static void AddRedisConfig(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(
                builder.Configuration.GetConnectionString("Redis")!));
    }
}