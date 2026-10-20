using Google.Apis.Auth;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Mostlylucid.Services;

namespace Mostlylucid.Controllers;

[Route("/")]
public class LoginController(IGoogleIdTokenValidator tokens, ILogger<LoginController> logger) : Controller
{
    [Route("challenge")]
    [HttpGet]
    public IActionResult Login(string returnUrl = "/")
    {
        // Challenge the user using Google authentication
        var properties = new AuthenticationProperties { RedirectUri = returnUrl };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [Route("logout")]
    [HttpGet]
    public async Task<IActionResult> Logout(string returnUrl = "/")
    {
        await HttpContext.SignOutAsync(SiteAuthentication.CookieScheme);
        return Redirect(returnUrl);
    }

    [Route("login")]
    [HttpPost]
    public async Task<IActionResult> HandleGoogleCallback([FromBody] GoogleLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken)) return BadRequest("A Google ID token is required.");

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await tokens.ValidateAsync(request.IdToken);
        }
        catch (InvalidJwtException)
        {
            return Unauthorized(new { error = "Invalid Google ID token." });
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Google sign-in verification is unavailable");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Google sign-in verification is unavailable." });
        }

        if (string.IsNullOrWhiteSpace(payload.Subject)) return Unauthorized(new { error = "Invalid Google ID token." });
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, payload.Subject),
            new("sub", payload.Subject)
        };
        if (!string.IsNullOrEmpty(payload.Name)) claims.Add(new Claim(ClaimTypes.Name, payload.Name));
        if (!string.IsNullOrEmpty(payload.Picture)) claims.Add(new Claim("picture", payload.Picture));
        if (payload.EmailVerified && !string.IsNullOrEmpty(payload.Email))
            claims.Add(new Claim(ClaimTypes.Email, payload.Email));
        var claimsIdentity = new ClaimsIdentity(claims, GoogleDefaults.AuthenticationScheme);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true
        };


        await HttpContext.SignInAsync(
            SiteAuthentication.CookieScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        return Ok();
    }
}

public class GoogleLoginRequest
{
    public string? IdToken { get; set; }
}
