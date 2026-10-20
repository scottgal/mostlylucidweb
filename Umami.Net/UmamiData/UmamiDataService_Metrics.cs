using Umami.Net.UmamiData.Models;
using Umami.Net.UmamiData.Models.RequestObjects;
using Umami.Net.UmamiData.Models.ResponseObjects;

namespace Umami.Net.UmamiData;

public partial class UmamiDataService
{
    public Task<UmamiResult<MetricsResponseModels[]>> GetMetrics(MetricsRequest metricsRequest)
    {
        ArgumentNullException.ThrowIfNull(metricsRequest);
        return RequestAsync<MetricsResponseModels[]>(HttpMethod.Get, WebsitePath("metrics") + Query(metricsRequest));
    }

    public Task<UmamiResult<ExpandedMetricsResponseModel[]>> GetExpandedMetrics(MetricsRequest metricsRequest)
    {
        ArgumentNullException.ThrowIfNull(metricsRequest);
        return RequestAsync<ExpandedMetricsResponseModel[]>(HttpMethod.Get, WebsitePath("metrics/expanded") + Query(metricsRequest));
    }
}
