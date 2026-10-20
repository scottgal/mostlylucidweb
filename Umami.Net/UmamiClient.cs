using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Umami.Net.Config;
using Umami.Net.Helpers;
using Umami.Net.Models;

namespace Umami.Net;

public class UmamiClient(
    HttpClient client,
    PayloadService payloadService,
    JwtDecoder jwtDecoder,
    ILogger<UmamiClient> logger,
    UmamiClientSettings settings)
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = new LowerCaseNamingPolicy(), // Custom naming policy for lower-cased properties
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public Task<HttpResponseMessage> Send(UmamiPayload? payload = null, UmamiEventData? eventData = null, string type = "event") =>
        SendAsync(payload, eventData, type);

    public async Task<HttpResponseMessage> SendAsync(UmamiPayload? payload = null, UmamiEventData? eventData = null,
        string type = "event", CancellationToken cancellationToken = default)
    {
        if (type != "event" && type != "identify" && type != "performance")
            throw new ArgumentException("Type must be 'event', 'identify' or 'performance'");
        payload ??= new UmamiPayload();
        payload = payloadService.PopulateFromPayload(payload, eventData);

        var jsonPayload = new { type, payload };
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/send");
        request.Headers.Remove("User-Agent");
        request.Headers.TryAddWithoutValidation("User-Agent", payload.UserAgent);
        if (!string.IsNullOrWhiteSpace(payload.CacheToken))
            request.Headers.TryAddWithoutValidation("x-umami-cache", payload.CacheToken);
        request.Content = JsonContent.Create(jsonPayload, options: Options);
        var response = await client.SendAsync(request, cancellationToken);


        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Failed to send data to Umami: {StatusCode}", response.StatusCode);
        }
        else logger.LogDebug("Successfully sent data to Umami: {StatusCode}", response.StatusCode);

        return response;
    }


    public async Task<UmamiDataResponse?> SendAndDecode(
        UmamiPayload? payload = null,
        UmamiEventData? eventData = null,
        string type = "event")
    {
        using var response = await Send(payload, eventData, type);
        return await DecodeResponse(response);
    }

    public async Task<UmamiDataResponse?> TrackPageViewAndDecode(
        string? url = "",
        string? title = "",
        UmamiPayload? payload = null,
        UmamiEventData? eventData = null,
        string? distinctId = null)
    {
        using var response = await TrackPageView(url, title, payload, eventData, distinctId);
        return await DecodeResponse(response);
    }


    public async Task<HttpResponseMessage> TrackPageView(
        string? url = "",
        string? title = "",
        UmamiPayload? payload = null,
        UmamiEventData? eventData = null,
        string? distinctId = null)
    {
        var sendPayload = payload ?? new UmamiPayload();
        sendPayload.Data = eventData ?? sendPayload.Data;
        if (!string.IsNullOrEmpty(url))
            sendPayload.Url = url;
        if (!string.IsNullOrEmpty(title))
            sendPayload.Title = title;
        if (!string.IsNullOrEmpty(distinctId))
            sendPayload.DistinctId = distinctId;
        return await Send(sendPayload);
    }


    public async Task<UmamiDataResponse?> TrackAndDecode(
        string eventName,
        UmamiEventData? eventData = null,
        string? distinctId = null)
    {
        using var response = await Track(eventName, eventData, distinctId);
        return await DecodeResponse(response);
    }

    public async Task<UmamiDataResponse?> TrackAndDecode(
        UmamiPayload eventObj,
        UmamiEventData? eventData = null,
        string? distinctId = null)
    {
        using var response = await Track(eventObj, eventData, distinctId);
        return await DecodeResponse(response);
    }

    public async Task<HttpResponseMessage> Track(
        string eventName,
        UmamiEventData? eventData = null,
        string? distinctId = null)
    {
        var thisPayload = new UmamiPayload
        {
            Name = eventName,
            Data = eventData ?? new UmamiEventData(),
            DistinctId = distinctId
        };
        return await Track(thisPayload);
    }

    public async Task<HttpResponseMessage> Track(UmamiPayload eventObj,
        UmamiEventData? eventData = null,
        string? distinctId = null)
    {
        var payload = eventObj;
        payload.Data = eventData ?? payload.Data ?? new UmamiEventData();
        payload.Website = settings.WebsiteId;
        if (!string.IsNullOrEmpty(distinctId))
            payload.DistinctId = distinctId;
        return await Send(payload);
    }

    private async Task<UmamiDataResponse?> DecodeResponse(HttpResponseMessage responseMessage)
    {
        var responseString = await responseMessage.Content.ReadAsStringAsync();

        if (!responseMessage.IsSuccessStatusCode) return new(UmamiDataResponse.ResponseStatus.Failed);
        if (responseString.TrimStart().StartsWith('{'))
        {
            try
            {
                using var document = JsonDocument.Parse(responseString);
                var root = document.RootElement;
                if (root.TryGetProperty("beep", out var beep) && beep.GetString() == "boop")
                    return new(UmamiDataResponse.ResponseStatus.BotDetected);
                if (!root.TryGetProperty("sessionId", out var session) || !Guid.TryParse(session.GetString(), out var sessionId)
                    || !root.TryGetProperty("visitId", out var visit) || !Guid.TryParse(visit.GetString(), out var visitId))
                    return new(UmamiDataResponse.ResponseStatus.Failed);
                return new(UmamiDataResponse.ResponseStatus.Success)
                {
                    Id = sessionId, SessionId = sessionId, VisitId = visitId,
                    Cache = root.TryGetProperty("cache", out var cache) ? cache.GetString() : null
                };
            }
            catch (Exception exception) when (exception is JsonException or InvalidOperationException)
            { return new(UmamiDataResponse.ResponseStatus.Failed); }
        }
        // Legacy Umami servers returned a bare JWT; it is decoded only as analytics metadata.
        var decoded = await jwtDecoder.DecodeResponse(responseString);
        try { return decoded == null ? null : UmamiDataResponse.Decode(decoded); }
        catch (FormatException) { return new(UmamiDataResponse.ResponseStatus.Failed); }
    }

    public Task<HttpResponseMessage> TrackPerformanceAsync(UmamiPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        foreach (var (value, maximum, name) in new[]
        {
            (payload.Lcp, 60000d, "lcp"), (payload.Inp, 60000d, "inp"), (payload.Cls, 100d, "cls"),
            (payload.Fcp, 60000d, "fcp"), (payload.Ttfb, 60000d, "ttfb")
        })
            if (value is { } number && (!double.IsFinite(number) || number < 0 || number > maximum))
                throw new ArgumentOutOfRangeException(name);
        return Send(payload, type: "performance");
    }


    public async Task<UmamiDataResponse?> IdentifyAndDecode()
    {
        return await IdentifyAndDecode(new UmamiPayload());
    }


    public async Task<UmamiDataResponse?> IdentifyAndDecode(UmamiPayload payload, UmamiEventData? eventData = null)
    {
        using var response = await Identify(payload, eventData);
        return await DecodeResponse(response);
    }

    public async Task<UmamiDataResponse?> IdentifyAndDecode(string sessionId, string? email = null,
        string? username = null,
        string? userId = null,
        UmamiEventData? eventData = null,
        string? distinctId = null)
    {
        using var response = await Identify(email, username, sessionId, userId, eventData, distinctId);
        return await DecodeResponse(response);
    }

    public async Task<HttpResponseMessage> Identify(UmamiPayload payload, UmamiEventData? eventData = null)
    {
        eventData ??= payload.Data ?? new UmamiEventData();
        var sendPayload = payloadService.PopulateFromPayload(payload, eventData);
        return await Send(sendPayload, eventData, "identify");
    }

    public async Task<HttpResponseMessage> Identify(string? email = null, string? username = null,
        string? sessionId = null, string? userId = null, UmamiEventData? eventData = null, string? distinctId = null)
    {
        var emailData = BuildEventData(email, username, userId, eventData);
        var payload = new UmamiPayload
        {
            SessionId = sessionId,
            DistinctId = distinctId,
            Data = emailData
        };

        return await Identify(payload, eventData);
    }

    public async Task<HttpResponseMessage> IdentifySession(string sessionId, string? distinctId = null)
    {
        return await Identify(sessionId: sessionId, distinctId: distinctId);
    }

    public async Task<UmamiDataResponse?> IdentifySessionAndDecode(string sessionId)
    {
        return await IdentifyAndDecode(sessionId);
    }

// Helper methods to reduce redundancy and centralize logic

    private static UmamiEventData BuildEventData(string? email, string? username, string? userId, UmamiEventData? eventData)
    {
        eventData ??= new UmamiEventData();

        if (!string.IsNullOrEmpty(email))
            eventData.TryAdd("email", email);
        if (!string.IsNullOrEmpty(username))
            eventData.TryAdd("username", username);
        if (!string.IsNullOrEmpty(userId))
            eventData.TryAdd("userId", userId);

        return eventData;
    }
}