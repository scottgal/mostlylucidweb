using Hangfire;

namespace Mostlylucid.SchedulerService.Services;


public static class JobInitializer
{
    
    private const string HourlyNewsletterJob = "HourlyNewsletterJob";
    private const string DailyNewsletterJob = "DailyNewsletterJob";
    private const string WeeklyNewsletterJob = "WeeklyNewsletterJob";
    private const string MonthlyNewsletterJob = "MonthlyNewsletterJob";
    

    
    public static void InitializeJobs(this IApplicationBuilder app)
    {
      var scope=  app.ApplicationServices.CreateScope();
        var recurringJobManager = scope.ServiceProvider.GetRequiredService<RecurringJobManager>();
      
        recurringJobManager.AddOrUpdate<NewsletterSendingService>(HourlyNewsletterJob, x => x.SendHourlyNewsletter(), Cron.Hourly);
        recurringJobManager.AddOrUpdate<NewsletterSendingService>(DailyNewsletterJob, x => x.SendDailyNewsletter(), Cron.Daily);
        recurringJobManager.AddOrUpdate<NewsletterSendingService>(WeeklyNewsletterJob, x => x.SendWeeklyNewsletter(), Cron.Weekly);
        recurringJobManager.AddOrUpdate<NewsletterSendingService>(MonthlyNewsletterJob, x => x.SendMonthlyNewsletter(), Cron.Monthly);
    }
}