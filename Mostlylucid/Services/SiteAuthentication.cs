namespace Mostlylucid.Services;

public static class SiteAuthentication
{
    // Changing the scheme also changes ticket encryption purposes, rejecting renamed legacy cookies.
    public const string CookieScheme = "Mostlylucid.ValidatedGoogle.v2";
    public const string CookieName = ".Mostlylucid.Auth.v2";
}
