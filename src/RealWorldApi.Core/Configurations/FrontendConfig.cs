namespace RealWorldApi.Core.Configurations;

public static class FrontendConfig
{
    public static void AddFrontendConfig(this WebApplication app)
    {
        app.UseDefaultFiles();
        app.UseStaticFiles();

        if (app.Environment.IsDevelopment()) return;

        var webRoot = app.Environment.WebRootPath ??
                      Path.Combine(app.Environment.ContentRootPath, "wwwroot");
        var indexHtml = Path.Combine(webRoot, "index.html");

        app.MapFallback(async context =>
        {
            if (IsApiOrToolingPath(context.Request.Path))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            if (!File.Exists(indexHtml))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.SendFileAsync(indexHtml);
        });
    }

    private static bool IsApiOrToolingPath(PathString path)
    {
        return path.StartsWithSegments("/api") ||
               path.StartsWithSegments("/docs") ||
               path.StartsWithSegments("/openapi") ||
               path.StartsWithSegments("/health");
    }
}
