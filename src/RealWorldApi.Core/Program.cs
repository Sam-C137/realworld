using RealWorldApi.Core.Configurations;
using RealWorldApi.Core.Domain.Middleware;
using RealWorldApi.Core.Features.Articles.Services;
using RealWorldApi.Core.Features.Comments.Services;
using RealWorldApi.Core.Features.Profiles.Services;
using RealWorldApi.Core.Features.Tags.Services;
using RealWorldApi.Core.Features.Users.Services;
using RealWorldApi.Core.Workers;
using RealWorldApi.Infrastructure.ObjectStorage;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
        .AddLoggerConfig();
builder.AddRedisConfig();
builder.AddCorsConfig(builder.Services);
builder.AddDatabaseConfig(builder.Services);
builder.AddAuthConfig();
builder.AddR2Config();
builder.Services.AddOpenApiConfig();
builder.Services.AddMapsterConfig();
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllerConfig();
builder.Services.AddProblemDetails();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHostedService<SessionCleanupService>();

builder.Services.AddScoped<IUsersService, UsersService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<EmailRateLimitService>();
builder.Services.AddScoped<ITagsService, TagsService>();
builder.Services.AddScoped<TagsCacheService>();
builder.Services.AddScoped<IArticlesService, ArticlesService>();
builder.Services.AddScoped<ArticleCacheService>();
builder.Services.AddScoped<ICommentsService, CommentsService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddSingleton<IObjectStorageService, ObjectStorageService>();


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
app.AddFrontendConfig();
await app.UseMigrateAndSeedDatabaseOnStart();


app.Run();
