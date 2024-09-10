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
        if (await authService.Login() == false)
            return new UmamiResult<ActiveUsersResponse>(HttpStatusCode.Unauthorized, "Failed to login", null);
        /*GET /api/websites/:websiteId/active*/
        var metrics = await authService.HttpClient.GetAsync($"/api/websites/{WebsiteId}/active");
        if (!metrics.IsSuccessStatusCode)
            return new UmamiResult<ActiveUsersResponse>(metrics.StatusCode,
                metrics.ReasonPhrase ?? "Failed to get active users", null);
        var content = await metrics.Content.ReadFromJsonAsync<ActiveUsersResponse>();
        return new UmamiResult<ActiveUsersResponse>(metrics.StatusCode, metrics.ReasonPhrase ?? "Success",
            content ?? null);
    }
}