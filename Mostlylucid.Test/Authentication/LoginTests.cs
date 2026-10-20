using System.Security.Claims;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Mostlylucid.Controllers;
using Mostlylucid.Services;
using Mostlylucid.Shared.Config;

namespace Mostlylucid.Test.Authentication;

public class LoginTests
{
    [Fact]
    public async Task ForgedTokenIsRejectedByRealGoogleValidator()
    {
        var validator = new GoogleIdTokenValidator(new AuthSettings { GoogleClientId = "expected-client" });
        // HS256, expired, wrong issuer/audience, and an invented signature.
        const string token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJub3QtZ29vZ2xlIiwiYXVkIjoid3JvbmciLCJleHAiOjEsInN1YiI6ImZha2UifQ.aW52YWxpZHNpZ25hdHVyZQ";
        await Assert.ThrowsAsync<InvalidJwtException>(() => validator.ValidateAsync(token));
    }

    [Fact]
    public async Task InvalidTokenNeverIssuesAuthenticationCookie()
    {
        var validator = new Mock<IGoogleIdTokenValidator>();
        validator.Setup(v => v.ValidateAsync("forged")).ThrowsAsync(new InvalidJwtException("Invalid signature"));
        var (controller, authentication) = CreateController(validator.Object);
        Assert.IsType<UnauthorizedObjectResult>(await controller.HandleGoogleCallback(new() { IdToken = "forged" }));
        authentication.Verify(a => a.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(),
            It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()), Times.Never);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ValidatedLoginNormalizesIdentityAndOnlyTrustsVerifiedEmail(bool verified)
    {
        var validator = new Mock<IGoogleIdTokenValidator>();
        validator.Setup(v => v.ValidateAsync("validated")).ReturnsAsync(new GoogleJsonWebSignature.Payload
        {
            Subject = "google-subject", Email = "writer@example.com", EmailVerified = verified,
            Name = "Writer", Picture = "https://example.com/avatar.png"
        });
        var (controller, authentication) = CreateController(validator.Object);
        ClaimsPrincipal? signedIn = null;
        authentication.Setup(a => a.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()))
            .Callback<HttpContext, string, ClaimsPrincipal, AuthenticationProperties>((_, _, principal, _) => signedIn = principal)
            .Returns(Task.CompletedTask);
        Assert.IsType<OkResult>(await controller.HandleGoogleCallback(new() { IdToken = "validated" }));
        Assert.True(signedIn!.Identity!.IsAuthenticated);
        Assert.Equal("google-subject", signedIn.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal("google-subject", signedIn.FindFirstValue("sub"));
        Assert.Equal(verified ? "writer@example.com" : null, signedIn.FindFirstValue(ClaimTypes.Email));
    }

    [Fact]
    public async Task VerificationOutageFailsClosed()
    {
        var validator = new Mock<IGoogleIdTokenValidator>();
        validator.Setup(v => v.ValidateAsync("token")).ThrowsAsync(new HttpRequestException("Unavailable"));
        var (controller, _) = CreateController(validator.Object);
        var result = Assert.IsType<ObjectResult>(await controller.HandleGoogleCallback(new() { IdToken = "token" }));
        Assert.Equal(503, result.StatusCode);
    }

    private static (LoginController Controller, Mock<IAuthenticationService> Authentication) CreateController(IGoogleIdTokenValidator validator)
    {
        var authentication = new Mock<IAuthenticationService>();
        var services = new ServiceCollection().AddSingleton(authentication.Object).BuildServiceProvider();
        var controller = new LoginController(validator, NullLogger<LoginController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { RequestServices = services } }
        };
        return (controller, authentication);
    }
}
