namespace RealWorldApi.Core.Features.Users;

public static class CookieHelper
{
    public const string RefreshTokenCookie = "rt";
    public const string CsrfCookie = "csrf";
    public const string CsrfHeader = "X-CSRF-TOKEN";

    public static void SetRefreshTokenCookie(HttpResponse response, string token, DateTime expires)
    {
        response.Cookies.Append(RefreshTokenCookie, token, new CookieOptions
        {
            HttpOnly  = true,               
            Secure    = true,
            SameSite  = SameSiteMode.None,  // cross-site capable; CSRF cookie mitigates risk
            Expires   = expires,
            Path      = "/api"              
        });
    }

    public static void SetCsrfCookie(HttpResponse response, string csrfToken, bool secure = true)
    {
        response.Cookies.Append(CsrfCookie, csrfToken, new CookieOptions
        {
            HttpOnly  = false,              
            Secure    = secure,
            SameSite  = SameSiteMode.None,
            Path      = "/"
        });
    }

    public static void ClearAuthCookies(HttpResponse response)
    {
        foreach (var name in new[] { RefreshTokenCookie, CsrfCookie })
            response.Cookies.Delete(name, new CookieOptions { Path = name == RefreshTokenCookie ? "/api" : "/" });
    }
}
