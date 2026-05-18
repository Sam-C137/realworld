using System.Security.Cryptography;
using System.Text;
using RealWorldApi.Core.Features.Users;

namespace RealWorldApi.Core.Domain.Middleware;

/// <summary>
///  Validates X-CSRF-TOKEN header matches the csrf cookie on mutating requests.
///  Runs AFTER JWT authentication, so we have the session available.
/// </summary>
public class CsrfValidationMiddleware(RequestDelegate next, ILogger<Program> logger)
{
    private static readonly HashSet<string> SafeMethods = ["GET", "HEAD", "OPTIONS", "TRACE"];

    public async Task InvokeAsync(HttpContext ctx)
    {
        if (!SafeMethods.Contains(ctx.Request.Method)
            && ctx.User.Identity?.IsAuthenticated == true)
        {
            var cookieValue  = ctx.Request.Cookies[CookieHelper.CsrfCookie];
            var headerValue  = ctx.Request.Headers[CookieHelper.CsrfHeader].FirstOrDefault();

            if (string.IsNullOrEmpty(cookieValue)
                || !CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(cookieValue),
                    Encoding.UTF8.GetBytes(headerValue ?? "")))
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                logger.LogInformation("CSRF validation failed. {cookieValue} != {headerValue}", cookieValue, headerValue);
                await ctx.Response.WriteAsJsonAsync(new { error = "CSRF validation failed." });
                return;
            }
        }
        await next(ctx);
    }
}