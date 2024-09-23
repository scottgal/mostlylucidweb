using Mostlylucid.DbContext.EntityFramework;
using Mostlylucid.Services.Email;
using Mostlylucid.Shared;

namespace Mostlylucid.SchedulerService.Services;

public class NewsletterSendingService(IServiceScopeFactory scopeFactory)
{
    public async Task SendNewsletter(SubscriptionType subscriptionType)
    {
        
        var scope = scopeFactory.CreateScope();
        var newsletterManagementService = scope.ServiceProvider.GetRequiredService<NewsletterManagementService>();
        var emailSender = scope.ServiceProvider.GetRequiredService<EmailSenderHostedService>();
  
       var subscriptions =await newsletterManagementService.GetSubscriptions(subscriptionType);
         var posts =await newsletterManagementService.GetPostsToSend(subscriptionType);
            foreach (var subscription in subscriptions)
            {
                // foreach (var post in posts)
                // {
                //     
                //    await emailSender.SendEmailAsync(subscription.Email, emailModel);
                // }
            }
    }
    
}