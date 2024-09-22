using Microsoft.AspNetCore.Mvc.Razor;
using Mostlylucid.EmailSubscription.Services;

namespace Mostlylucid.EmailSubscription;

    public static class EmailProcessorSetup
    {
        public static void ConfigureEmailProcessor(this IServiceCollection services)
        {
       
            services.AddTransient<TemplateProcessorService>();
            services.AddScoped<EmailSubscriptionService>();
        }
    }