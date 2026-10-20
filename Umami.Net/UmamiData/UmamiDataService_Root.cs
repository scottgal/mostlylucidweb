using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Umami.Net.Config;
using Umami.Net.UmamiData.Models;
using Umami.Net.UmamiData.Models.ResponseObjects;

namespace Umami.Net.UmamiData;

public partial class UmamiDataService(
    UmamiDataSettings analyticsSettings,
    AuthService authService,
    ILogger<UmamiDataService> logger)
{
    private string WebsiteId => analyticsSettings.WebsiteId;


    public async Task<UmamiResult<ActiveUsersResponse>> GetActiveUsers()
    {
        return await RequestAsync<ActiveUsersResponse>(HttpMethod.Get, WebsitePath("active"));
    }
}
