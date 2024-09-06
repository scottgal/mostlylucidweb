﻿using Umami.Net.UmamiData.Models.ResponseObjects;

namespace Mostlylucid.Blog;

public interface IUmamiDataSortService
{
    Task<List<MetricsResponseModels>?> GetMetrics(DateTime startAt, DateTime endAt, string prefix="" );
}