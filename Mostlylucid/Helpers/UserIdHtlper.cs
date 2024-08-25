namespace Mostlylucid.Helpers;

public static class UserIdHtlper
{
    public  static string GetUserId(this HttpRequest request, HttpResponse response)
    {
        var userId = request.Cookies["UserIdentifier"];
        if (userId != null) return userId;
        userId = Guid.NewGuid().ToString();
        var cookieOptions = new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddHours(6),
            HttpOnly = true,
            Secure = false,
            SameSite = SameSiteMode.Strict
        };
        response.Cookies.Append("UserIdentifier", userId, cookieOptions);

        return userId;
    }
}