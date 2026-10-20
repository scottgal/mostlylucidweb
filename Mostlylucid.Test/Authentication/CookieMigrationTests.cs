using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mostlylucid.Services;

namespace Mostlylucid.Test.Authentication;

public class CookieMigrationTests
{
    [Fact]
    public async Task LegacyCookieCannotBeReplayedUnderNewCookieName()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Services.AddAuthentication(SiteAuthentication.CookieScheme)
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(SiteAuthentication.CookieScheme, options => options.Cookie.Name = SiteAuthentication.CookieName);
        await using var app = builder.Build();
        app.UseAuthentication();
        app.MapGet("/issue/{version}", async (HttpContext context, string version) =>
        {
            var scheme = version == "legacy" ? CookieAuthenticationDefaults.AuthenticationScheme : SiteAuthentication.CookieScheme;
            await context.SignInAsync(scheme, new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, "migration-test") }, "Google")));
        });
        app.MapGet("/status", (HttpContext context) => context.User.Identity?.IsAuthenticated == true
            ? Results.Ok() : Results.Unauthorized());
        app.MapGet("/legacy-status", async (HttpContext context) =>
            (await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme)).Succeeded
                ? Results.Ok() : Results.Unauthorized());
        await app.StartAsync();
        using var client = new HttpClient(new HttpClientHandler { UseCookies = false }) { BaseAddress = new Uri(app.Urls.Single()) };
        var oldTicket = await IssueAsync("legacy");
        Assert.Equal(HttpStatusCode.OK, await StatusAsync("/legacy-status", ".AspNetCore.Cookies", oldTicket));
        Assert.Equal(HttpStatusCode.Unauthorized, await StatusAsync("/status", SiteAuthentication.CookieName, oldTicket));
        Assert.Equal(HttpStatusCode.OK, await StatusAsync("/status", SiteAuthentication.CookieName, await IssueAsync("current")));

        async Task<string> IssueAsync(string version)
        {
            using var response = await client.GetAsync("/issue/" + version);
            var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
            return cookie.Split(';')[0].Split('=', 2)[1];
        }

        async Task<HttpStatusCode> StatusAsync(string path, string name, string ticket)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Add("Cookie", $"{name}={ticket}");
            using var response = await client.SendAsync(request);
            return response.StatusCode;
        }
    }
}
