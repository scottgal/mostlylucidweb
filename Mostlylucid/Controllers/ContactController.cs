using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mostlylucid.Config;
using Mostlylucid.Email;
using Mostlylucid.Email.Models;
using Mostlylucid.Models.Contact;
using Mostlylucid.Services.Markdown;

namespace Mostlylucid.Controllers;

[Route("contact")]
public class ContactController(AuthSettings authSettingsSettings,BlogService blogService,
    CommentService commentService, EmailSenderHostedService sender, ILogger<BaseController> logger) : BaseController(authSettingsSettings,blogService, logger)
{
   [Route("")]
    public IActionResult Index()
    {
        var user = GetUserInfo();
        var model = new ContactViewModel(){Email = user.email, Name = user.name, Authenticated = user.loggedIn};
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
                SenderEmail = user.email,
                SenderName =user.name,
                Comment = commentHtml,
            };
            await sender.SendEmailAsync(contactModel);
            return PartialView("_Response", new ContactViewModel(){Email = user.email, Name = user.name, Comment = commentHtml, Authenticated = user.loggedIn});

        return RedirectToAction("Index", "Home");
    }
    
}