using RealWorldApi.Core.Configurations;
using RealWorldApi.Core.Domain.Middleware;
using RealWorldApi.Core.Features.Users.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
        .AddLoggerConfig();
builder.AddRedisConfig();
builder.AddCorsConfig(builder.Services);
builder.AddDatabaseConfig(builder.Services);
builder.Services.AddOpenApiConfig();
builder.Services.AddMapsterConfig();
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllerConfig();
builder.Services.AddProblemDetails();

builder.Services.AddScoped<IUsersService, UsersService>();


var app = builder.Build();

app.MapDefaultEndpoints();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors("Frontend");
app.UseRateLimiter();
app.MapControllers();
await app.UseMigrateAndSeedDatabaseOnStart();


app.Run();
