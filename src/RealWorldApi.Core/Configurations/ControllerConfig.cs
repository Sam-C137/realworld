using FluentValidation;
using RealWorldApi.Core.Domain.Validation;

namespace RealWorldApi.Core.Configurations;

public static class ControllerConfig
{
    public static IServiceCollection AddControllerConfig(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<Program>();
        services.AddControllers(options =>
        {
            options.Filters.Add<FluentValidationFilter>();
            options.Conventions.Add(new LowercaseRouteConvention());
        }).AddJsonOptions(options =>
        {
            // send enums as strings
            options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        });
        return services;
    }
}
