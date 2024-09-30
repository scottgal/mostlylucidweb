using Microsoft.AspNetCore.Mvc.Razor;
using Mostlylucid.Services.EmailSubscription;
using Mostlylucid.Shared.Config;

namespace Mostlylucid.EmailSubscription;

    public static class EmailProcessorSetup
    {
        public static void ConfigureEmailProcessor(this IServiceCollection services, ConfigurationManager configurationManager)
        {
            services.AddScoped<EmailSubscriptionService>();
          
            services.ConfigurePOCO<NewsletterConfig>(configurationManager); 
            
            services.AddHttpClient<NewsletterClient>((provider, client)=>
            {
                var settings = provider.GetRequiredService<NewsletterConfig>();
                client.BaseAddress = new Uri(settings.SchedulerServiceUrl);
            }).SetHandlerLifetime(TimeSpan.FromMinutes(5)) //Set lifetime to five minutes
            .AddPolicyHandler(RetryPolicyExtension.GetRetryPolicy());;
        }
    }