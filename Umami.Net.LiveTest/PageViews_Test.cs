using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Umami.Net.LiveTest.Setup;
using Umami.Net.UmamiData;
using Umami.Net.UmamiData.Models.RequestObjects;

namespace Umami.Net.LiveTest;

public class PageViews_Test
{
    [Fact]
    public async Task PageViews_StartEnd_Hour()
    {
        var setup = new SetupUmamiData();
        var serviceProvider = setup.Setup();
        var websiteDataService = serviceProvider.GetRequiredService<UmamiDataService>();

        var pageViews = await websiteDataService.GetPageViews(new PageViewsRequest
        {
            StartAtDate = DateTime.Now.AddDays(-7),
            EndAtDate = DateTime.Now,
            Unit = Unit.hour
        });
        Assert.NotNull(pageViews);
        Assert.Equal(HttpStatusCode.OK, pageViews.Status);
    }

    [Fact]
    public async Task PageViews_StartEnd_Day()
    {
        var setup = new SetupUmamiData();
        var serviceProvider = setup.Setup();
        var websiteDataService = serviceProvider.GetRequiredService<UmamiDataService>();

        var pageViews = await websiteDataService.GetPageViews(new PageViewsRequest
        {
            StartAtDate = DateTime.Now.AddDays(-7),
            EndAtDate = DateTime.Now,
            Unit = Unit.day
        });
        Assert.NotNull(pageViews);
        Assert.Equal(HttpStatusCode.OK, pageViews.Status);
    }

    [Fact]
    public async Task PageViews_StartEnd_Day_Url()
    {
        var setup = new SetupUmamiData();
        var serviceProvider = setup.Setup();
        var websiteDataService = serviceProvider.GetRequiredService<UmamiDataService>();

        var pageViews = await websiteDataService.GetPageViews(new PageViewsRequest
        {
            StartAtDate = DateTime.Now.AddDays(-7),
            EndAtDate = DateTime.Now,
            Unit = Unit.day,
            Url = "/blog"
        });
        Assert.NotNull(pageViews);
        Assert.Equal(HttpStatusCode.OK, pageViews.Status);
    }

    [Fact]
    public async Task PageViews_StartEnd_Month_Url()
    {
        var setup = new SetupUmamiData();
        var serviceProvider = setup.Setup();
        var websiteDataService = serviceProvider.GetRequiredService<UmamiDataService>();

        var pageViews = await websiteDataService.GetPageViews(new PageViewsRequest
        {
            StartAtDate = DateTime.Now.AddDays(-90),
            EndAtDate = DateTime.Now,
            Unit = Unit.month,
            Url = "/blog"
        });
        Assert.NotNull(pageViews);
        Assert.Equal(HttpStatusCode.OK, pageViews.Status);
    }
}