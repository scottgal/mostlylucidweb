using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Mostlylucid.Config;

namespace Mostlylucid.Controllers;

public class BaseController(AuthSettings authSettingsSettings, ILogger<BaseController> logger) : Controller
{
    public record LoginData(bool loggedIn, string? name, string? avatarUrl, string? email, string? identifier, bool isAdmin = false);
    
    protected LoginData GetUserInfo()
    {
        var authenticateResult = HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme).Result;
        if (authenticateResult.Succeeded)
        {
            var principal = authenticateResult.Principal;
            if(principal == null)
            {
                return new LoginData(false, null, null, null, null);
            }

            var nameClaim = principal.FindFirst("name");
            if (nameClaim == null)
            {
                nameClaim = principal.FindFirst(ClaimTypes.Name);
            }

            var avatarClaim = principal.FindFirst("picture");
            if (avatarClaim == null)
            {
                avatarClaim = principal.FindFirst("avatar");
            }

            var subClaim = principal.FindFirst("sub");
            if (subClaim == null)
            {
                subClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
            }

            var emailClaim = principal.FindFirst("email");
            if (emailClaim == null)
            {
                emailClaim = principal.FindFirst(ClaimTypes.Email);
            }
            var sub = subClaim?.Value;
            var name = nameClaim?.Value;
            var avatarUrl = avatarClaim?.Value;
            var emailIdentifier = emailClaim?.Value;
            
            
            if(sub == authSettingsSettings.AdminUserGoogleId)
            {
                return new LoginData(true, name, avatarUrl, emailIdentifier, name, true);
            }
            return new LoginData(true, name, avatarUrl, emailIdentifier, name);
        }
        return new LoginData(false,null,null,null,null);
    }
}