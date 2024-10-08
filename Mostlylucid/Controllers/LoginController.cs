﻿using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Mostlylucid.Controllers;

[Route("/")]
public class LoginController : Controller
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
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect(returnUrl);
    }

    [Route("login")]
    [HttpPost]
    public async Task<IActionResult> HandleGoogleCallback([FromBody] GoogleLoginRequest request)
    {
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadToken(request.IdToken) as JwtSecurityToken;

        if (jsonToken == null) return BadRequest("Invalid token");

        var claimsIdentity = new ClaimsIdentity(
            jsonToken.Claims,
            GoogleDefaults.AuthenticationScheme);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true
        };


        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        return Ok();
    }
}

public class GoogleLoginRequest
{
    public string IdToken { get; set; }
}