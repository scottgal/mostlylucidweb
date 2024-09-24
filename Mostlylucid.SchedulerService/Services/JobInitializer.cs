using Hangfire;
using Mostlylucid.Shared;

namespace Mostlylucid.SchedulerService.Services;


public static class JobInitializer
{
    private const string AutoNewsletterJob = "AutoNewsletterJob";
    private const string DailyNewsletterJob = "DailyNewsletterJob";
    private const string WeeklyNewsletterJob = "WeeklyNewsletterJob";
    private const string MonthlyNewsletterJob = "MonthlyNewsletterJob";
    public static void InitializeJobs(this IApplicationBuilder app)
    {
      var scope=  app.ApplicationServices.CreateScope();
        var recurringJobManager = scope.ServiceProvider.GetRequiredService<RecurringJobManager>();
      
        recurringJobManager.AddOrUpdate<NewsletterSendingService>(AutoNewsletterJob,  x =>  x.SendNewsletter(SubscriptionType.EveryPost), Cron.Hourly);
        recurringJobManager.AddOrUpdate<NewsletterSendingService>(DailyNewsletterJob,  x =>  x.SendNewsletter(SubscriptionType.Daily), "0 17 * * *");
        recurringJobManager.AddOrUpdate<NewsletterSendingService>(WeeklyNewsletterJob,  x =>  x.SendNewsletter(SubscriptionType.Weekly), "0 17 * * *");
        recurringJobManager.AddOrUpdate<NewsletterSendingService>(MonthlyNewsletterJob,  x => x.SendNewsletter(SubscriptionType.Monthly), "0 17 * * *");
    }
}