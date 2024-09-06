using Mostlylucid.Blog;
using Umami.Net.UmamiData.Models.ResponseObjects;

namespace Mostlylucid.Test.Extensions;

public class UmamiDataSortFake : IUmamiDataSortService
{
    public async Task<List<MetricsResponseModels>?> GetMetrics(DateTime startAt, DateTime endAt, string prefix = "")
    {
        return new List<MetricsResponseModels>();
    }
}