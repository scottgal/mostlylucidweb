using System.Security.Claims;
using Htmx;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Mostlylucid.Helpers;
using Mostlylucid.Models;
using Mostlylucid.Services;

namespace Mostlylucid.Controllers;

public class BaseController(BaseControllerService baseControllerService, ILogger<BaseController> logger) : Controller
{
    
    protected readonly IBlogService BlogService = baseControllerService.BlogService;
    protected readonly AnalyticsSettings AnalyticsSettings = baseControllerService.AnalyticsSettings;
    protected readonly AuthSettings AuthSettings = baseControllerService.AuthSettings;

    protected string UserId => Request.GetUserId(Response);


    protected async Task<T> PopulateBaseModel<T>(T model) where T : BaseViewModel
    {
        if(!User.Identity?.IsAuthenticated ==true) return model;
      var userInfo = await GetUserInfo();
        model.Authenticated = userInfo.LoggedIn;
        model.IsAdmin = userInfo.IsAdmin;
        model.AvatarUrl = userInfo.AvatarUrl;
        model.Name = userInfo.Name;
        model.Email = userInfo.Email;
        return model;
    }
    

    public override async Task OnActionExecutionAsync(ActionExecutingContext filterContext,
        ActionExecutionDelegate next)
    {
        logger.LogInformation("OnActionExecutionAsync");
        if (!Request.IsHtmx())
        {
            ViewBag.UmamiPath = AnalyticsSettings.UmamiPath;
            ViewBag.UmamiWebsiteId = AnalyticsSettings.WebsiteId;
            ViewBag.UmamiScript = AnalyticsSettings.UmamiScript;
        }

        logger.LogInformation("Adding categories to viewbag");
        ViewBag.Categories = (await BlogService.GetCategories()).OrderBy(x => x).ToList();

        await base.OnActionExecutionAsync(filterContext, next);
    }


    protected async Task<LoginData> GetUserInfo()
    {
        var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!authenticateResult.Succeeded)
        {
            logger.LogInformation("User is not authenticated");
            return new LoginData(false, null, null, null, null);
        }

        logger.LogInformation("User is authenticated");
        var principal = authenticateResult.Principal;
        if (principal == null) return new LoginData(false, null, null, null, null);

        var nameClaim = principal.FindFirst("name") ?? principal.FindFirst(ClaimTypes.Name);

        var avatarClaim = principal.FindFirst("picture") ?? principal.FindFirst("avatar");

        var subClaim = principal.FindFirst("sub") ?? principal.FindFirst(ClaimTypes.NameIdentifier);

        var emailClaim = principal.FindFirst("email") ?? principal.FindFirst(ClaimTypes.Email);
        var sub = subClaim?.Value;
        var name = nameClaim?.Value;
        var avatarUrl = avatarClaim?.Value;
        var emailIdentifier = emailClaim?.Value;
        var isAdmin = sub == AuthSettings.AdminUserGoogleId;

        return new LoginData(true, name, avatarUrl, emailIdentifier, name, isAdmin);
    }

    public record LoginData(
        bool LoggedIn,
        string? Name,
        string? AvatarUrl,
        string? Email,
        string? Identifier,
        bool IsAdmin = false);
}