namespace RealWorldApi.Core.Domain.Middleware;


public static class ExceptionHandlingMiddleware
{
    public static void UseExceptionHandling(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            return;
        }

        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/problem+json";

                var problemDetails = Results.Problem(
                    title: "Unexpected error",
                    detail: "An unexpected error occurred.",
                    statusCode: StatusCodes.Status500InternalServerError);

                await problemDetails.ExecuteAsync(context);
            });
        });
    }
}