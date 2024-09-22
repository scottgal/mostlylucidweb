using Microsoft.AspNetCore.Mvc;
using Mostlylucid.Blog.ViewServices;
using Mostlylucid.Controllers;
using Mostlylucid.EmailSubscription.Models;
using Mostlylucid.EmailSubscription.Services;
using Mostlylucid.Services;
using Mostlylucid.Services.Interfaces;
using Mostlylucid.Services.Markdown;

namespace Mostlylucid.EmailSubscription;

[Route("/emailsubscription")]
public class EmailSubscriptionController(BaseControllerService baseControllerService,EmailSubscriptionService emailSubscriptionService, IBlogViewService blogViewService, ILogger<EmailSubscriptionController> logger) 
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
            Summary = x.Summary,
            PublishedDate = x.PublishedDate,
            Url = language == MarkdownBaseService.EnglishLanguage ?  
                Url.ActionLink("Show", "Blog", new { x.Slug }, "https", Request.Host.Value) :
                Url.ActionLink("Language", "Blog", new { x.Slug, language }, "https", Request.Host.Value)
        }).ToList();
      
        var emailSubscriptionModel = new EmailRenderingModel
        {
            Posts = emailPostModels,
            UnsubscribeUrl = Url.ActionLink("Unsubscribe", "EmailSubscription", new {token="test"}, "https", Request.Host.Value),
            Language = language
        };
        return View("WeeklyTemplate", emailSubscriptionModel);
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
        return View("Subscribe", subscribeModel);
    }
    
    [HttpPost]
    [Route("save")]
    public async Task<IActionResult> Save(EmailSubscribeViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Subscribe", model);
        }
        
        
        var saveModel = model.ToModel(emailSubscriptionService.GetToken());
        await emailSubscriptionService.Create(saveModel);
        return View("Save", model);
    }
    
    [HttpGet]
    [Route("unsubscribe/{token}")]
    public async Task<IActionResult> Unsubscribe(string token)
    {
        return View("Unsubscribe");
    }
}