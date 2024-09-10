using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Umami.Net.UmamiData;

namespace Umami.Net.Test.UmamiData;

public class UmamiData_PageViewsRequest_Test : UmamiDataBase
{
    private readonly DateTime EndDate = DateTime.ParseExact("2021-10-07", "yyyy-MM-dd", null);
    private readonly DateTime StartDate = DateTime.ParseExact("2021-10-01", "yyyy-MM-dd", null);

    [Fact]
    public async Task SetupTest_Good()
    {
        var serviceProvider = GetServiceProvider();
        var umamiDataService = serviceProvider.GetRequiredService<UmamiDataService>();
        var authLogger = serviceProvider.GetRequiredService<ILogger<AuthService>>();
        var umamiDataLogger = serviceProvider.GetRequiredService<ILogger<UmamiDataService>>();
        var result = await umamiDataService.GetPageViews(StartDate, EndDate);
        var fakeAuthLogger = (FakeLogger<AuthService>)authLogger;
        var collector = fakeAuthLogger.Collector;
        var logs = collector.GetSnapshot();
        Assert.Contains("Login successful", logs.Select(x => x.Message));

        var fakeUmamiDataLogger = (FakeLogger<UmamiDataService>)umamiDataLogger;
        var umamiDataCollector = fakeUmamiDataLogger.Collector;
        var umamiDataLogs = umamiDataCollector.GetSnapshot();
        Assert.Contains("Successfully got page views", umamiDataLogs.Select(x => x.Message));

        Assert.NotNull(result);
    }
}