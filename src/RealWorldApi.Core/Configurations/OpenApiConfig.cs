using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace RealWorldApi.Core.Configurations;

public static class OpenApiConfig
{
    public static IServiceCollection AddOpenApiConfig(this IServiceCollection services)
    {
        services.AddOpenApi("v1");
        services.AddEndpointsApiExplorer();
        services.AddOpenApi(options =>
        {
            options.AddSchemaTransformer((schema, context, _) =>
            {
                var type = Nullable.GetUnderlyingType(context.JsonTypeInfo.Type) ?? context.JsonTypeInfo.Type;

                if (!type.IsEnum)
                {
                    return Task.CompletedTask;
                }

                schema.Type = JsonSchemaType.String;
                schema.Format = null;
                schema.Enum = type.GetEnumNames()
                    .Select(name => JsonValue.Create(name))
                    .Cast<JsonNode>()
                    .ToList();

                return Task.CompletedTask;
            });
        });
        
        return services;
    }
}