namespace Mostlylucid.Helpers;

public static class UserIdHelper
{
    public  static string GetUserId(this HttpRequest request, HttpResponse response)
    {
        var userId = request.Cookies["UserIdentifier"];
        if (userId != null) return userId;
        userId = Guid.NewGuid().ToString();
        var cookieOptions = new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddHours(24),
            HttpOnly = true,
            Secure = false,
            SameSite = SameSiteMode.Strict
        };
        response.Cookies.Append("UserIdentifier", userId, cookieOptions);

        return userId;
    }
}