using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var db = builder.AddConnectionString("DefaultConnection");
var redis = builder.AddConnectionString("Redis");

var api = builder.AddProject<Projects.RealWorldApi_Core>("api")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithReference(db)
    .WithReference(redis)
    .WaitFor(db)
    .WaitFor(redis);

if (builder.Environment.IsDevelopment())
{
    api.WithHttpHealthCheck("/health");
}
else
{
    api.WithHttpEndpoint(port: 8080, name: "http")
        .WithHttpEndpoint(port: 8081, name: "health")
        .WithHttpHealthCheck("/health", endpointName: "health");
}
    
builder.Build().Run();    