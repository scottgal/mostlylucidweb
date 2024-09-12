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

    public async Task<HttpResponseMessage> Send(
        UmamiPayload? payload = null,
        UmamiEventData? eventData = null,
        string type = "event")
    {
        if (type != "event" && type != "identify")
            throw new ArgumentException("Type must be either 'event' or 'identify'");
        payload ??= new UmamiPayload();
        if (!payload.PayloadPopulated)
            payload = payloadService.PopulateFromPayload(payload, eventData);

        var jsonPayload = new { type, payload };
        var request = new HttpRequestMessage(HttpMethod.Post, "api/send");
        request.Headers.Remove("User-Agent");
        request.Headers.TryAddWithoutValidation("User-Agent", payload.UserAgent);
        request.Content = JsonContent.Create(jsonPayload, options: Options);
        var response = await client.SendAsync(request);


        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            logger.LogError("Failed to send data to Umami: {StatusCode}, {ReasonPhrase} , {Content}",
                response.StatusCode, response.ReasonPhrase, content);
        }
        else if (logger.IsEnabled(LogLevel.Information))
        {
            var content = await response.Content.ReadAsStringAsync();
            logger.LogInformation("Successfully sent data to Umami: {StatusCode}, {ReasonPhrase}, {Content}",
                response.StatusCode, response.ReasonPhrase, content);
        }

        return response;
    }


    public async Task<UmamiDataResponse?> SendAndDecode(
        UmamiPayload? payload = null,
        UmamiEventData? eventData = null,
        string type = "event")
    {
        var response = await Send(payload, eventData, type);
        return await DecodeResponse(response);
    }

    public async Task<UmamiDataResponse?> TrackPageViewAndDecode(
        string? url = "",
        string? title = "",
        UmamiPayload? payload = null,
        UmamiEventData? eventData = null)
    {
        var response = await TrackPageView(url, title, payload, eventData);
        return await DecodeResponse(response);
    }


    public async Task<HttpResponseMessage> TrackPageView(
        string? url = "",
        string? title = "",
        UmamiPayload? payload = null,
        UmamiEventData? eventData = null)
    {
        var sendPayload = payload ?? new UmamiPayload();
        sendPayload.Data = eventData;
        if (!string.IsNullOrEmpty(url))
            sendPayload.Url = url;
        if (!string.IsNullOrEmpty(title))
            sendPayload.Title = title;
        return await Send(sendPayload);
    }


    public async Task<UmamiDataResponse?> TrackAndDecode(
        string eventName,
        UmamiEventData? eventData = null)
    {
        var response = await Track(eventName, eventData);
        return await DecodeResponse(response);
    }

    public async Task<UmamiDataResponse?> TrackAndDecode(
        UmamiPayload eventObj,
        UmamiEventData? eventData = null)
    {
        var response = await Track(eventObj, eventData);
        return await DecodeResponse(response);
    }

    public async Task<HttpResponseMessage> Track(
        string eventName,
        UmamiEventData? eventData = null)
    {
        var thisPayload = new UmamiPayload
        {
            Name = eventName,
            Data = eventData ?? new UmamiEventData()
        };
        return await Track(thisPayload);
    }

    public async Task<HttpResponseMessage> Track(UmamiPayload eventObj,
        UmamiEventData? eventData = null)
    {
        var payload = eventObj;
        payload.Data = eventData ?? new UmamiEventData();
        payload.Website = settings.WebsiteId;
        return await Send(payload);
    }

    private async Task<UmamiDataResponse?> DecodeResponse(HttpResponseMessage responseMessage)
    {
        var responseString = await responseMessage.Content.ReadAsStringAsync();

        switch (responseMessage.IsSuccessStatusCode)
        {
            case false:
                return new UmamiDataResponse(UmamiDataResponse.ResponseStatus.Failed);
            case true when responseString.Contains("beep") && responseString.Contains("boop"):
                logger.LogWarning("Bot detected data not stored in Umami");
                return new UmamiDataResponse(UmamiDataResponse.ResponseStatus.BotDetected);

            case true:
                var decoded = await jwtDecoder.DecodeResponse(responseString);
                if (decoded == null)
                {
                    logger.LogError("Failed to decode response from Umami");
                    return null;
                }

                var payload = UmamiDataResponse.Decode(decoded);

                return payload;
        }
    }


    public async Task<UmamiDataResponse?> IdentifyAndDecode()
    {
        return await IdentifyAndDecode(new UmamiPayload());
    }


    public async Task<UmamiDataResponse?> IdentifyAndDecode(UmamiPayload payload, UmamiEventData? eventData = null)
    {
        var response = await Identify(payload, eventData);
        return await DecodeResponse(response);
    }

    public async Task<UmamiDataResponse?> IdentifyAndDecode(string sessionId, string? email = null,
        string? username = null,
        string? userId = null, UmamiEventData? eventData = null)
    {
        var response = await Identify(email, username, sessionId, userId, eventData);
        return await DecodeResponse(response);
    }

    public async Task<HttpResponseMessage> Identify(UmamiPayload payload, UmamiEventData? eventData = null)
    {
        eventData ??= new UmamiEventData();
        var sendPayload = payloadService.PopulateFromPayload(payload, eventData);
        return await Send(sendPayload, eventData, "identify");
    }

    public async Task<HttpResponseMessage> Identify(string? email = null, string? username = null,
        string? sessionId = null, string? userId = null, UmamiEventData? eventData = null)
    {
        var emailData = BuildEventData(email, username, userId, eventData);
        var payload = new UmamiPayload
        {
            SessionId = sessionId,
            Data = emailData
        };

        return await Identify(payload, eventData);
    }

    public async Task<HttpResponseMessage> IdentifySession(string sessionId)
    {
        return await Identify(sessionId: sessionId);
    }

    public async Task<UmamiDataResponse?> IdentifySessionAndDecode(string sessionId)
    {
        return await IdentifyAndDecode(sessionId);
    }

// Helper methods to reduce redundancy and centralize logic

    private UmamiEventData BuildEventData(string? email, string? username, string? userId, UmamiEventData? eventData)
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