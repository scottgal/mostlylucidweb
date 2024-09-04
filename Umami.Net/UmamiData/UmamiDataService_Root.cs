using Microsoft.Extensions.Logging;
using Umami.Net.Config;

namespace Umami.Net.UmamiData;

public partial class UmamiDataService(
    UmamiDataSettings analyticsSettings,
    AuthService authService,
    ILogger<UmamiDataService> logger)
{
    private string WebsiteId => analyticsSettings.WebsiteId;
}