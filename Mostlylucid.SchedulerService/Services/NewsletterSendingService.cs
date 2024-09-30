using Mostlylucid.Services.Email;
using Mostlylucid.Services.EmailSubscription;
using Mostlylucid.Shared;
using Mostlylucid.Shared.Config;
using Mostlylucid.Shared.Helpers;
using Mostlylucid.Shared.Models.EmailSubscription;
using Serilog;
using Serilog.Events;
using SerilogTracing;

namespace Mostlylucid.SchedulerService.Services;

public class NewsletterSendingService(
    IServiceScopeFactory scopeFactory,
    NewsletterConfig newsletterConfig,
    ILogger<NewsletterSendingService> logger)
{
    private string GetPostUrl(string language, string slug)
    {
        return language == Constants.EnglishLanguage
            ? $"{newsletterConfig.AppHostUrl}/post/{slug}"
            : $"{newsletterConfig.AppHostUrl}/{language}/post/{slug}";
    }

    public async Task SendScheduledNewsletter(SubscriptionType subscriptionType)
    {
        using var scope = scopeFactory.CreateScope();
        var activity = Log.Logger.StartActivity("SendScheduledNewsletter");
        var newsletterManagementService = scope.ServiceProvider.GetRequiredService<NewsletterManagementService>();
        var subscriptions = await newsletterManagementService.GetSubscriptions(subscriptionType);
        foreach (var subscription in subscriptions)
        {
            logger.LogInformation("Sending newsletter for subscription {Subscription}", subscription);
            await SendNewsletterForSubscription(subscription, activity);
        }

        logger.LogInformation("Updating last send for subscription type {SubscriptionType}", subscriptionType);
        await newsletterManagementService.UpdateLastSend(subscriptionType, DateTime.Now);
    }

    private async Task<bool> SendNewsletterForSubscription(EmailSubscriptionModel subscription, LoggerActivity activity)
    {
        activity?.Activity?.SetTag("subscription", subscription);
        try
        {
            using var scope = scopeFactory.CreateScope();
            var newsletterManagementService = scope.ServiceProvider.GetRequiredService<NewsletterManagementService>();
            var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSenderHostedService>();
            var posts = await newsletterManagementService.GetPostsToSend(subscription.SubscriptionType);
            var emailModel = new EmailTemplateModel()
            {
                ToEmail = subscription.Email,
                Subject = "mostlylucid newsletter",
                Posts = posts.Select(p => new EmailPostModel()
                {
                    Title = p.Title,
                    Language = p.Language,
                    PlainTextContent = p.PlainTextContent.TruncateAtWord(200),
                    Url = GetPostUrl(p.Language, p.Slug),
                    PublishedDate = p.PublishedDate
                }).ToList(),
            };
            await emailSender.SendEmailAsync(emailModel);
            activity?.Activity?.SetTag("email", subscription.Email);
            activity?.Complete(LogEventLevel.Information);
            await newsletterManagementService.UpdateLastSendForSubscription(subscription.Id, DateTime.Now);
            return true;
        }
        catch (Exception e)
        {
            activity?.Complete(LogEventLevel.Error, e);
            logger.LogError(e, "Error sending newsletter for subscription {Subscription}", subscription);
            return false;
        }
    }

    public async Task<bool> SendImmediateEmailForSubscription(string token)
    {
        using var activity = Log.Logger.StartActivity("SendImmediateEmailForSubscription");
        try
        {
            var scope = scopeFactory.CreateScope();
            var emailSubscriptionService = scope.ServiceProvider.GetRequiredService<EmailSubscriptionService>();

            var emailSubscription = await emailSubscriptionService.GetByToken(token);
            activity.AddProperty("token", token);
            if (emailSubscription == null)
            {
                logger.LogWarning("Email subscription not found for token {token}", token);
                activity.Complete(LogEventLevel.Warning);
                return false;
            }

            logger.LogInformation("Sending email for subscription {EmailSubscription}", emailSubscription);
            if (await SendNewsletterForSubscription(emailSubscription, activity))
            {
                activity.Complete(LogEventLevel.Information);
            }
            else
            {
                return false;
            }

            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error sending email for subscription");
            activity?.Complete(LogEventLevel.Error, e);
            return false;
        }
    }
}