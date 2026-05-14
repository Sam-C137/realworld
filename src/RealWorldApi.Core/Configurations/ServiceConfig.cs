using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;

namespace RealWorldApi.Core.Configurations;
using OpenTelemetry;

/// <summary>
/// Provides extension methods for configuring common services and middleware for the application,
/// such as OpenTelemetry, health checks, and service discovery.
/// </summary>
public static class ServiceConfig
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    extension<TBuilder>(TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        public TBuilder AddServiceDefaults()
        {
            builder.ConfigureOpenTelemetry();
    
            builder.AddDefaultHealthChecks();
    
            builder.Services.AddServiceDiscovery();
            
            builder.ConfigureRateLimiting();
    
            builder.Services.ConfigureHttpClientDefaults(http =>
            {
                // Turn on resilience by default
                http.AddStandardResilienceHandler();
    
                // Turn on service discovery by default
                http.AddServiceDiscovery();
            });
    
            // Uncomment the following to restrict the allowed schemes for service discovery.
            // builder.Services.Configure<ServiceDiscoveryOptions>(options =>
            // {
            //     options.AllowedSchemes = ["https"];
            // });
    
            return builder;
        }
        
        private TBuilder ConfigureOpenTelemetry()
        {
            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
            });
            builder.Services.AddOpenTelemetry()
                .WithMetrics(metrics =>
                {
                    metrics.AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation();
                })
                .WithTracing(tracing =>
                {
                    tracing.AddSource(builder.Environment.ApplicationName)
                        .AddAspNetCoreInstrumentation(tracing =>
                            // Exclude health check requests from tracing
                            tracing.Filter = context =>
                                !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                                && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath)
                        )
                        // Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                        //.AddGrpcClientInstrumentation()
                        .AddHttpClientInstrumentation();
                });
        
            builder.AddOpenTelemetryExporters();
        
            return builder;
        }
        
        private TBuilder AddOpenTelemetryExporters()
        {
            var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
    
            if (useOtlpExporter)
            {
                builder.Services.AddOpenTelemetry().UseOtlpExporter();
            }
    
            // Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
            //if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
            //{
            //    builder.Services.AddOpenTelemetry()
            //       .UseAzureMonitor();
            //}
    
            return builder;
        }
        
        private TBuilder AddDefaultHealthChecks()
        {
            builder.Services.AddHealthChecks()
                // Add a default liveness check to ensure app is responsive
                .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
    
            return builder;
        }

        private TBuilder ConfigureRateLimiting()
        {
            builder.Services.AddRateLimiter(options =>
            {
                options.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    
                    var problemDetails = new ProblemDetails
                    {
                        Type = "https://tools.ietf.org/html/rfc6585#section-4",
                        Title = "Too Many Requests",
                        Status = StatusCodes.Status429TooManyRequests,
                        Detail = "Rate limit exceeded. Please try again later.",
                        Instance = context.HttpContext.Request.Path.Value
                    };

                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        context.HttpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString(CultureInfo.InvariantCulture);
                    }

                    context.HttpContext.Response.ContentType = "application/problem+json";
                    await context.HttpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
                };

                options.AddPolicy("default_sliding", context =>
                {
                    var userIdentifier = context.User?.Identity?.Name;
                    
                    if (string.IsNullOrEmpty(userIdentifier))
                    {
                        userIdentifier = context.Request.Headers.Host.ToString();
                    }
                    
                    if (string.IsNullOrEmpty(userIdentifier))
                    {
                        userIdentifier = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    }

                    return RateLimitPartition.GetSlidingWindowLimiter(userIdentifier, _ => new SlidingWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1),
                        PermitLimit = 10,
                        QueueLimit = 0,
                        SegmentsPerWindow = 1
                    });
                });

                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    var userIdentifier = context.User?.Identity?.Name;
                
                    if (string.IsNullOrEmpty(userIdentifier))
                    {
                        userIdentifier = context.Request.Headers.Host.ToString();
                    }
                
                    if (string.IsNullOrEmpty(userIdentifier))
                    {
                        userIdentifier = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    }

                    return RateLimitPartition.GetFixedWindowLimiter(userIdentifier, _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        Window = TimeSpan.FromSeconds(1),
                        PermitLimit = 100,
                        QueueLimit = 0
                    });
                });
            });
            
            return builder;
        }
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Adding health checks endpoints to applications in non-development environments has security implications.
        // See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
        if (!app.Environment.IsDevelopment()) return app;
        // All health checks must pass for app to be considered ready to accept traffic after starting
        app.MapHealthChecks(HealthEndpointPath);

        // Only health checks tagged with the "live" tag must pass for app to be considered alive
        app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });
        
        // OpenAPI endpoints
        app.MapOpenApi();
        app.MapScalarApiReference("/docs");

        return app;
    }
}
