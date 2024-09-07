using Htmx;
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
public class ContactController(
    AuthSettings authSettingsSettings,
    AnalyticsSettings analyticsSettings,
    IBlogService blogService,
    CommentService commentService,
    IEmailSenderHostedService sender,
    ILogger<BaseController> logger) : BaseController(authSettingsSettings, analyticsSettings, blogService, logger)
{
    [Route("")]
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        
        ViewBag.Title = "Contact";
        ;
        var model = new ContactViewModel();
        var user =await GetUserInfo();
        model.Authenticated = user.LoggedIn;
        model.Name = user.Name;
        model.Email = user.Email;
        model.AvatarUrl = user.AvatarUrl;
        return View("Contact", model);
    }

    [HttpPost]
    [Route("submit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit([Bind(Prefix = "")] ContactViewModel comment)
    {
        ViewBag.Title = "Contact";
        //Only allow HTMX requests
        if(!Request.IsHtmx())
        {
            return RedirectToAction("Index", "Contact");
        }
      
        if (!ModelState.IsValid)
        {
            return PartialView("_ContactForm", comment);
        }

        var commentHtml = commentService.ProcessComment(comment.Comment);
        var contactModel = new ContactEmailModel()
        {
            SenderEmail = string.IsNullOrEmpty(comment.Email) ? "Anonymous" : comment.Email,
            SenderName = string.IsNullOrEmpty(comment.Name) ? "Anonymous" : comment.Name,
            Comment = commentHtml,
        };
        await sender.SendEmailAsync(contactModel);
        return PartialView("_Response",
            new ContactViewModel() { Email = comment.Email, Name = comment.Name, Comment = commentHtml });

        return RedirectToAction("Index", "Home");
    }
}