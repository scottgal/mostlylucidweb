using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Umami.Net;
using Umami.Net.Helpers;
using Umami.Net.Models;

namespace Mostlylucid.Umami;

public interface IUmamiUserInfoService
{
    Task<UmamiResponse?> GetUserInfo(string userId);
}

public class UmamiUserInfoService(IMemoryCache cache, UmamiClient umamiClient, ILogger<UmamiUserInfoService> logger) : IUmamiUserInfoService
{
    
    public async Task<UmamiResponse?> GetUserInfo(string userId)
    {
        var cacheKey = $"UserData_{userId}";
        if (cache.TryGetValue(cacheKey, out UmamiResponse? userInfo))
        {
            return userInfo;
        }
         userInfo = await umamiClient.IdentifySessionAndDecode(userId);
        if(userInfo == null)
        {
            logger.LogWarning("Failed to get user info for {UserId}", userId);
            return null;
        }
        cache.Set(cacheKey, userInfo, TimeSpan.FromHours(6));
        return userInfo;
    }
}