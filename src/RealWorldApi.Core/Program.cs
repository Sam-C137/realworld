using RealWorldApi.Core.Configurations;
using RealWorldApi.Core.Domain.Middleware;
using RealWorldApi.Core.Features.Articles.Services;
using RealWorldApi.Core.Features.Tags.Services;
using RealWorldApi.Core.Features.Users.Services;
using RealWorldApi.Core.Workers;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
        .AddLoggerConfig();
builder.AddRedisConfig();
builder.AddCorsConfig(builder.Services);
builder.AddDatabaseConfig(builder.Services);
builder.AddAuthConfig();
builder.Services.AddOpenApiConfig();
builder.Services.AddMapsterConfig();
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllerConfig();
builder.Services.AddProblemDetails();

builder.Services.AddHostedService<SessionCleanupWorker>();

builder.Services.AddScoped<IUsersService, UsersService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<EmailRateLimitService>();
builder.Services.AddScoped<SessionCleanupService>();
builder.Services.AddScoped<ITagsService, TagsService>();
builder.Services.AddScoped<TagsCacheService>();
builder.Services.AddScoped<IArticlesService, ArticlesService>();
builder.Services.AddScoped<ArticleCacheService>();


var app = builder.Build();

app.MapDefaultEndpoints();
app.UseExceptionHandling();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors("Frontend");
app.UseRateLimiter();
app.UseMiddleware<CsrfValidationMiddleware>(); 
app.MapControllers();
app.MapOpenApi();
app.MapScalarApiReference("/docs");
await app.UseMigrateAndSeedDatabaseOnStart();


app.Run();
