using System.Security.Claims;
using Htmx;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Mostlylucid.Blog;
using Mostlylucid.Config;

namespace Mostlylucid.Controllers;

public class BaseController : Controller
{
    private readonly AuthSettings? _authSettingsSettings;
    private readonly IBlogService? _blogService;
    private readonly ILogger<BaseController> _logger;
    private readonly AnalyticsSettings _analyticsSettings;

    public BaseController(AnalyticsSettings analyticsSettings, ILogger<BaseController> logger)
    {
        _analyticsSettings = analyticsSettings;
        _logger = logger;
    }

    
    public BaseController(AuthSettings authSettingsSettings, AnalyticsSettings analyticsSettings, IBlogService blogService, ILogger<BaseController> logger) : this(analyticsSettings, logger)
    {
        _authSettingsSettings = authSettingsSettings;
        _blogService = blogService;
       
    }
    

    public override async Task OnActionExecutionAsync(ActionExecutingContext filterContext, ActionExecutionDelegate next)
    {
        if (!Request.IsHtmx())
        {
         
            ViewBag.UmamiPath = _analyticsSettings.UmamiPath;
            ViewBag.UmamiWebsiteId = _analyticsSettings.WebsiteId;
            ViewBag.UmamiScript = _analyticsSettings.UmamiScript;
        }

        if (_blogService != null)
        {

            _logger.LogInformation("Adding categories to viewbag");
            ViewBag.Categories = (await _blogService.GetCategories()).OrderBy(x => x).ToList();
        }

        await base.OnActionExecutionAsync(filterContext, next);
    }
    
    public record LoginData(bool LoggedIn, string? Name, string? AvatarUrl, string? Email, string? Identifier, bool IsAdmin = false);
    

    protected LoginData GetUserInfo()
    {
        var authenticateResult = HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme).Result;
        if (!authenticateResult.Succeeded)
        {
            _logger.LogInformation("User is not authenticated");
            return new LoginData(false, null, null, null, null);
        }
        _logger.LogInformation("User is authenticated");
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
        var isAdmin = sub == _authSettingsSettings.AdminUserGoogleId;    
            
        return new LoginData(true, name, avatarUrl, emailIdentifier, name, isAdmin);
    }
}