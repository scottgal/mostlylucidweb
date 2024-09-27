using Htmx;
using Microsoft.AspNetCore.Mvc;
using Mostlylucid.Blog.ViewServices;
using Mostlylucid.Controllers;
using Mostlylucid.EmailSubscription.Mappers;
using Mostlylucid.EmailSubscription.Models;
using Mostlylucid.Services;
using Mostlylucid.Services.Email;
using Mostlylucid.Services.Markdown;
using Mostlylucid.Shared.Models.Email;
using Mostlylucid.Shared.Models.EmailSubscription;
using EmailSubscriptionService = Mostlylucid.Services.EmailSubscription.EmailSubscriptionService;

namespace Mostlylucid.EmailSubscription.Controller;

[Route("/newsletter")]
public class EmailSubscriptionController(BaseControllerService baseControllerService,IEmailSenderHostedService emailService,
    EmailSubscriptionService emailSubscriptionService, IBlogViewService blogViewService, ILogger<EmailSubscriptionController> logger) 
    : BaseController(baseControllerService,logger)
{
    [HttpGet]
    [Route("")]
    public async Task<IActionResult> Show(string language = "en", string[]? categories=null)

    {
        var posts = await blogViewService.GetPostsForRange(DateTime.Now.AddDays(-7), DateTime.Now,categories, language:language);
        var emailPostModels = posts.Select(x => new EmailPostModel
        {
            Title = x.Title,
            Slug = x.Slug,
            PlainTextContent = x.Summary,
            PublishedDate = x.PublishedDate,
            Url = language == MarkdownBaseService.EnglishLanguage ?  
                Url.ActionLink("Show", "Blog", new { x.Slug }, "https", Request.Host.Value) :
                Url.ActionLink("Language", "Blog", new { x.Slug, language }, "https", Request.Host.Value)
        }).ToList();
      
        var emailSubscriptionModel = new EmailRenderingModel
        {
            Posts = emailPostModels,
            Language = language
        };
        return View("WeeklyTemplate", emailSubscriptionModel);
    }
    
    [HttpGet]
    [Route("preview/{token}")]
    public async Task<IActionResult> Preview(string token)
    {
        var emailSubscription = await emailSubscriptionService.GetByToken(token);
        if (emailSubscription == null)
        {
            return NotFound();
        }
        var posts = await blogViewService.GetPostsForRange(DateTime.Now.AddDays(-7), DateTime.Now,emailSubscription.Categories.ToArray(), language:emailSubscription.Language);
        var emailPostModels = posts.Select(x => new EmailPostModel
        {
            Title = x.Title,
            Slug = x.Slug,
            PlainTextContent = x.Summary,
            PublishedDate = x.PublishedDate,
            Url = emailSubscription.Language == MarkdownBaseService.EnglishLanguage ?  
                Url.ActionLink("Show", "Blog", new { x.Slug }, "https", Request.Host.Value) :
                Url.ActionLink("Language", "Blog", new { x.Slug, emailSubscription.Language }, "https", Request.Host.Value)
        }).ToList();
      
        var emailRenderingModel = new EmailRenderingModel
        {
            ManageSubscriptionUrl = Url.ActionLink("Manage", "EmailSubscription", new { token }, "https"),
            UnsubscribeUrl = Url.ActionLink("Unsubscribe", "EmailSubscription", new { token }, "https"),
            Posts = emailPostModels,
            Language = emailSubscription.Language,
           SubscriptionType = emailSubscription.SubscriptionType
        };
        return View("EmailTemplate", emailRenderingModel);
    }

    [HttpGet]
    [Route("subscribe")]
    public async Task<IActionResult> Subscribe()
    {
        var categories = await blogViewService.GetCategories(true);
       
        
        var subscribeModel = new EmailSubscribeViewModel
        {
            Categories = categories,
            DaysOfWeek = Enum.GetValues<DayOfWeek>().ToList(),
            
        };
        var userInSession = await GetUserInfo();
        if (userInSession.LoggedIn)
        {
         subscribeModel.Email = userInSession?.Email ?? string.Empty;   
        }
       subscribeModel=await  PopulateBaseModel(subscribeModel);
       if(Request.IsHtmx())
           return PartialView("Subscribe", subscribeModel);
        return View("Subscribe", subscribeModel);
    }
    
    [HttpPost]
    [Route("save")]
    public async Task<IActionResult> Save(EmailSubscribeViewModel model)
    {
        if (!ModelState.IsValid)
        {
            if(Request.IsHtmx())
                return PartialView("Subscribe", model);
            return View("Subscribe", model);
        }
        
        var saveModel = model.ToModel(emailSubscriptionService.GetToken());
        var emailModel = new ConfirmEmailModel();
        emailModel.ConfirmUrl = Url.ActionLink("Confirm", "EmailSubscription", new { saveModel.Token}, "https", Request.Host.Value);
        emailModel.UnsubscribeUrl = Url.ActionLink("Unsubscribe", "EmailSubscription", new { saveModel.Token}, "https", Request.Host.Value);
        emailModel.ManageSubscriptionUrl = Url.ActionLink("Manage", "EmailSubscription", new { saveModel.Token}, "https", Request.Host.Value);
        emailModel.SubscriptionType = saveModel.SubscriptionType;
        await emailService.SendEmailAsync(emailModel);
        await emailSubscriptionService.Create(saveModel);
        if(Request.IsHtmx())
            return PartialView("Save", model);
        return View("Save", model);
    }
    
    [HttpGet]
    [Route("unsubscribe/{token}")]
    public async Task<IActionResult> Unsubscribe(string token)
    {
        var emailSubscription = await emailSubscriptionService.Delete(token);
        return View("Unsubscribe");
    }
    [HttpGet]
    [Route("manage/{token}")]
    public async Task<IActionResult> Manage(string token)
    {
        var emailSubscription = await emailSubscriptionService.GetByToken(token);
        if (emailSubscription == null)
        {
            return NotFound();
        }
        var categories = await blogViewService.GetCategories(true);
        var manageModel = emailSubscription.MapToEmailSubscribeViewModel();
        manageModel.Categories = categories;
       manageModel.DaysOfWeek = Enum.GetValues<DayOfWeek>().ToList();
        manageModel=await  PopulateBaseModel(manageModel);
        manageModel.PageType = PageType.Manage;
        return View("Save", manageModel);
    }
    
    
    [HttpGet]
    [Route("confirm/{token}")]
    public async Task<IActionResult> Confirm(string token)
    {
        var emailSubscription = await emailSubscriptionService.GetByToken(token);
        if (emailSubscription == null)
        {
            return NotFound();
        }
        await emailSubscriptionService.UpdateEmailConfirmed(token);
        return View("Confirm");
    }
}