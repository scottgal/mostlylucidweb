using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Umami.Net.Config;
using Umami.Net.Models;

namespace Umami.Net;

public class PayloadService(
    IHttpContextAccessor httpContextAccessor,
    UmamiClientSettings settings,
    ILogger<PayloadService> logger)
{

    public static string DefaultUserAgent => _userAgent ??= GetVersion();

    private static string? _userAgent;
    private static string GetVersion()
    {
        var informationalVersion = typeof(UmamiClient).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        var subVersion = informationalVersion?.Split('+')[0];
        return $"Mozilla/5.0 (Windows 11) Umami.Net/{subVersion}";
    }

    
    public UmamiPayload PopulateFromPayload(UmamiPayload? payload, UmamiEventData? data)
    {
        var newPayload = GetPayload(data: data);

        if (payload == null) return newPayload;

        if (payload.Hostname != null)
            newPayload.Hostname = payload.Hostname;

        if (payload.Language != null)
            newPayload.Language = payload.Language;

        if (payload.Referrer != null)
            newPayload.Referrer = payload.Referrer;

        if (payload.Screen != null)
            newPayload.Screen = payload.Screen;

        if (payload.Title != null)
            newPayload.Title = payload.Title;

        if (payload.Url != null)
            newPayload.Url = payload.Url;

        if (payload.Name != null)
            newPayload.Name = payload.Name;

        if (payload.Data != null)
            newPayload.Data = payload.Data;

        if (payload.SessionId != null)
            newPayload.SessionId = payload.SessionId;


        newPayload.UserAgent = payload.UserAgent ?? DefaultUserAgent;

        if (payload.UseDefaultUserAgent)
        {
            var userData = newPayload.Data ?? new UmamiEventData();
            userData.TryAdd("OriginalUserAgent", newPayload.UserAgent ?? "");
            newPayload.UserAgent = DefaultUserAgent;
            newPayload.Data = userData;
        }


        logger.LogInformation("Using UserAgent: {UserAgent}", newPayload.UserAgent);


        if (payload.Hostname != null)
            newPayload.Hostname = payload.Hostname;

        newPayload.PayloadPopulated = true;
        return newPayload;
    }

    private UmamiPayload GetPayload(string? url = null, UmamiEventData? data = null)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var request = httpContext?.Request;

        var payload = new UmamiPayload
        {
            Website = settings.WebsiteId,
            Data = data,
            Url = url ?? httpContext?.Request?.Path.Value,
            IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString(),
            UserAgent = request?.Headers["User-Agent"].FirstOrDefault(),
            Referrer = request?.Headers["Referer"].FirstOrDefault(),
            Hostname = request?.Host.Host
        };

        return payload;
    }
}