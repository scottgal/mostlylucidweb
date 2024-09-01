using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Umami.Net.LiveTest.Setup;
using Umami.Net.UmamiData;
using Umami.Net.UmamiData.Models.RequestObjects;

namespace Umami.Net.LiveTest;

public class PageViews_Test
{
    [Fact]
    public async Task PageViews_StartEnd()
    {
        var setup = new SetupUmamiData();
        var serviceProvider = setup.Setup();
        var websiteDataService = serviceProvider.GetRequiredService<UmamiService>();
    
      var pageViews = await websiteDataService.GetPageViews(new PageViewsRequest()
       {
           StartAtDate = DateTime.Now.AddDays(-7),
           EndAtDate = DateTime.Now
       });
       Assert.NotNull(pageViews);
       Assert.Equal( HttpStatusCode.OK, pageViews.Status);

    }
}