﻿using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Umami.Net.LiveTest.Setup;
using Umami.Net.UmamiData;
using Umami.Net.UmamiData.Models.RequestObjects;

namespace Umami.Net.LiveTest;

public class Stats_Test
{
    [Fact]
    public async Task Stats_StartEndForUrl()
    {
        var setup = new SetupUmamiData();
        var serviceProvider = setup.Setup();
        var websiteDataService = serviceProvider.GetRequiredService<UmamiDataService>();

        var metrics = await websiteDataService.GetStats(new StatsRequest
        {
            StartAtDate = DateTime.Now.AddDays(-7),
            EndAtDate = DateTime.Now,
            Url = "/"
        });
        Assert.NotNull(metrics);
        Assert.Equal(HttpStatusCode.OK, metrics.Status);
    }
}