using System.Diagnostics.CodeAnalysis;
using Mostlylucid.DbContext.EntityFramework;
using Mostlylucid.Services.Email;
using Mostlylucid.Services.EmailSubscription;
using Mostlylucid.Shared;
using Mostlylucid.Shared.Models.EmailSubscription;

namespace Mostlylucid.SchedulerService.Services;

public class NewsletterSendingService(IServiceScopeFactory scopeFactory)
{
    public async Task SendNewsletter(SubscriptionType subscriptionType)
    {
        
        var scope = scopeFactory.CreateScope();
        var newsletterManagementService = scope.ServiceProvider.GetRequiredService<NewsletterManagementService>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSenderHostedService>();
  
       var subscriptions =await newsletterManagementService.GetSubscriptions(subscriptionType);
         var posts =await newsletterManagementService.GetPostsToSend(subscriptionType);
            foreach (var subscription in subscriptions)
            {
                // foreach (var post in posts)
                // {
                //     
                //    await emailSender.SendEmailAsync(subscription.Email, emailModel);
                // }
                await newsletterManagementService.UpdateLastSendForSubscription(subscription.Id, DateTime.Now);
            }
    }

    public async Task SendImmediateEmailForSubscription(string token)
    {
        var scope = scopeFactory.CreateScope();
        var emailSubscriptionService = scope.ServiceProvider.GetRequiredService<EmailSubscriptionService>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSenderHostedService>();
        var emailSubscription = await emailSubscriptionService.GetByToken(token);
        if (emailSubscription == null)
        {
            return;
        }
      
        
    }
    
    public async Task SendEmail(SubscriptionType subscriptionType, DateTime fromDateTime, DateTime toDateTime,
        string email)
    {
        
        var emailTemplageModel = new EmailTemplateModel
        {
            ToEmail = email
        };
    }
    
}