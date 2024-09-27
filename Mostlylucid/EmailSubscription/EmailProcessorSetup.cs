using Microsoft.AspNetCore.Mvc.Razor;
using Mostlylucid.Services.EmailSubscription;

namespace Mostlylucid.EmailSubscription;

    public static class EmailProcessorSetup
    {
        public static void ConfigureEmailProcessor(this IServiceCollection services)
        {
            services.AddScoped<EmailSubscriptionService>();
            services.AddHttpClient<NewsletterClient>();
        }
    }