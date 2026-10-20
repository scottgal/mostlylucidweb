using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mostlylucid.Resume;

namespace Mostlylucid.Test.Authentication;

public class ResumeWriterTests
{
    [Theory]
    [InlineData("writer@example.com", true, "/resume/api/career-record", 200, null, true)]
    [InlineData("WRITER@example.com", true, "/resume/api/career-record", 200, null, true)]
    [InlineData("other@example.com", true, "/resume/api/career-record", 403, null, true)]
    [InlineData("writer@example.com", false, "/resume/api/career-record", 401, null, true)]
    [InlineData(null, false, "/resume/api/compile", 200, null, true)]
    [InlineData(null, true, "/resume/api/career-record", 200, "existing-admin", false)]
    [InlineData(null, true, "/resume/api/career-record", 403, "other-account", false)]
    [InlineData(null, false, "/resume/api/career-record", 401, "existing-admin", false)]
    public async Task GatewayOnlyAllowsAuthenticatedWriterToChangeRecord(string? email, bool authenticated, string path, int expected, string? subject, bool configuredEmail)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["LucidResumeProxy:UpstreamUri"] = "http://compiler.invalid/",
            ["LucidResumeProxy:WriterEmail"] = configuredEmail ? "writer@example.com" : null,
            ["Auth:AdminUserGoogleId"] = "existing-admin"
        });
        builder.Services.AddLucidResumeProxy(builder.Configuration);
        builder.Services.AddHttpClient("LucidResumeCompiler")
            .ConfigurePrimaryHttpMessageHandler(() => new CompilerHandler());
        await using var app = builder.Build();
        app.Use(async (context, next) =>
        {
            var claims = new List<Claim>();
            if (email != null) claims.Add(new Claim(ClaimTypes.Email, email));
            if (subject != null) claims.Add(new Claim(ClaimTypes.NameIdentifier, subject));
            context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticated ? "Google" : null));
            await next();
        });
        app.MapLucidResumeProxy();
        await app.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        using var response = await client.PostAsync(path, new StringContent("test"));
        Assert.Equal(expected, (int)response.StatusCode);
    }

    private sealed class CompilerHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("upstream reached") });
    }
}
