using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mostlylucid.Blog;
using Mostlylucid.Blog.Markdown;
using Mostlylucid.Config;
using Mostlylucid.Email;
using Mostlylucid.Email.Models;
using Mostlylucid.Models.Contact;

namespace Mostlylucid.Controllers;

[Route("contact")]
public class ContactController(AuthSettings authSettingsSettings, AnalyticsSettings analyticsSettings,IBlogService blogService,
    CommentService commentService, EmailSenderHostedService sender, ILogger<BaseController> logger) : BaseController(authSettingsSettings,analyticsSettings, blogService, logger)
{
   [Route("")]
    public IActionResult Index()
    {
        var user = GetUserInfo();
        var model = new ContactViewModel(){Email = user.Email, Name = user.Name, Authenticated = user.LoggedIn};
        return View("Contact", model);
    }
    
    [HttpPost]
    [Route("submit")]
    [Authorize]
    public async Task<IActionResult> Submit(string comment)
    {
        var user = GetUserInfo();
            var commentHtml = commentService.ProcessComment(comment);
            var contactModel = new ContactEmailModel()
            {
                SenderEmail = user.Email,
                SenderName =user.Name,
                Comment = commentHtml,
            };
            await sender.SendEmailAsync(contactModel);
            return PartialView("_Response", new ContactViewModel(){Email = user.Email, Name = user.Name, Comment = commentHtml, Authenticated = user.LoggedIn});

        return RedirectToAction("Index", "Home");
    }
    
}