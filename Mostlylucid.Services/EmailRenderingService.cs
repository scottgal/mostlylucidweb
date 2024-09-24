using Mostlylucid.Services.Blog;
using Mostlylucid.Shared;
using Mostlylucid.Shared.Helpers;
using Mostlylucid.Shared.Models;
using Mostlylucid.Shared.Models.EmailSubscription;

namespace Mostlylucid.Services;

public class EmailRenderingService(IBlogService blogService)
{
    private string Host => "https://www.mostlylucid.net";
    private string GetUrl(string slug, string language)
    {
        return language == Constants.EnglishLanguage ?  
            $"{Host}/blog/{slug}" :
            $"{Host}/{language}/blog/{slug}";
    }
    
    private string GetUnsubscribeUrl(string token)
    {
        return $"{Host}/emailsubscription/unsubscribe/{token}";
    }
    
    private string GetManageSubscritioUrl(string token)
    {
        return $"{Host}/emailsubscription/manage/{token}";
    }
    public async Task<EmailRenderingModel> Get(SubscriptionType subscriptionType, 
                                                string? language=Constants.EnglishLanguage, 
                                                string[]? categories=null, 
                                                DateTime? startDate=null,
                                                DateTime? endDateTime=null, 
                                                string token="test")
    {
        var queryModel = new PostListQueryModel()
        {
            Language = language ?? Constants.EnglishLanguage,
            Categories = categories,
            StartDate = startDate,
            EndDate = endDateTime
        };
        if(subscriptionType== SubscriptionType.EveryPost)
        {
            queryModel = queryModel with { Page = 1 };
            queryModel = queryModel with { PageSize = 1 };
        }
        

        var posts = await blogService.Get(queryModel);
            var emailPostModels = posts.Data.Select(x => new EmailPostModel
            {
                Title = x.Title,
                Slug = x.Slug,
                PlainTextContent = x.PlainTextContent.TruncateAtWord(200),
                PublishedDate = x.PublishedDate,
                Url = GetUrl(x.Slug, language) ,
                 
            }).ToList();
          
            var emailRenderingModel = new EmailRenderingModel
            {
                ManageSubscriptionUrl = GetManageSubscritioUrl(token),
                UnsubscribeUrl = GetUnsubscribeUrl(token),
                Posts = emailPostModels,
                Language =language,
                SubscriptionType = subscriptionType
            };
        return emailRenderingModel;
    }

}