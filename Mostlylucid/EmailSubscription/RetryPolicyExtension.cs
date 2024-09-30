using System.Net;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;

namespace Mostlylucid.EmailSubscription;

public static class RetryPolicyExtension
{
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        var delay = Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromSeconds(1), 3);
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.ServiceUnavailable)
            .WaitAndRetryAsync(delay);
    }
}