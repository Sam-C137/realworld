using System.Reflection;
using Mapster;

namespace RealWorldApi.Core.Configurations;

public static class MapsterConfig
{
    public static IServiceCollection AddMapsterConfig(this IServiceCollection services)
    {
        TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());
        services.AddMapster();
        return services;
    }
}