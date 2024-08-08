using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Mostlylucid.Config;
using Mostlylucid.Services.Markdown;

namespace Mostlylucid.Controllers;

public class BaseController : Controller
{
    private readonly AuthSettings _authSettingsSettings;
    private readonly BlogService _blogService;

    public BaseController(AuthSettings authSettingsSettings, BlogService blogService, ILogger<BaseController> logger)
    {
        _authSettingsSettings = authSettingsSettings;
        _blogService = blogService;
       
    }

    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
     ViewBag.Categories = _blogService.GetCategories();
        base.OnActionExecuting(filterContext);
    }
    
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
            
            
            if(sub == _authSettingsSettings.AdminUserGoogleId)
            {
                return new LoginData(true, name, avatarUrl, emailIdentifier, name, true);
            }
            return new LoginData(true, name, avatarUrl, emailIdentifier, name);
        }
        return new LoginData(false,null,null,null,null);
    }
}