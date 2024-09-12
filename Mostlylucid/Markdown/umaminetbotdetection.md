# Umami.Net and Bot Detection

# Introduction
So I've [posted a LOT](/blog/category/Umami) in the past on using Umami for analytics in a self-hosted environment and even published the [Umami.Net Nuget pacakge](https://www.nuget.org/packages/Umami.Net/). However I was having an issue where I wanted to track users of my RSS feed; this post goes into why and how I solved it.

[TOC]

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-09-12T14:50</datetime>
# The Problem
The problem is that RSS feed readers try to pass *useful* User Agents when requesting the feed. This allows **compliant** providers to track the number of users and the type of users that are consuming the feed. However, this also means that Umami will identify these requests as *bot* requests. This is an issue for my use as it results in the request being ignored and not tracked.

The Feedbin user agent looks like this:

```plaintext
Feedbin feed-id:1234 - 21 subscribers
```
So pretty useful right, it passes some useful details about what your feed id is, the number of users and the user agent. However, this is also a problem as it means that Umami will ignore the request; in fact it'll return a 200 status BUT the content contains `{"beep": "boop"}` meaning that this is identified as a bot request. This is annoying as I can't handle this through normal error handling (it's a 200, not say a 403 etc).

# The Solution
So what's the solution to this? I can't manually parse all these requests and detect if Umami will detect them as a bot; it uses IsBot (https://www.npmjs.com/package/isbot) to detect if a request is a bot or not. There's no C# equivalent and it's a changing list so I can't even use that list (in future I MAY get clever and use the list to detect if a request is a bot or not).
So I need to intercept the request before it gets to Umami and change the User Agent to something that Umami will accept for specific requests. 

So now I added some additional parameters to my tracking methods in Umami.Net. These allow you to specify the new 'Default User Agent' will be sent to Umami instead of the original User Agent. This allows me to specify that the User Agent should be changed to a specific value for specific requests. 

## The Methods
On my `UmamiBackgroundSender` I added the following:
```csharp
   public async Task TrackPageView(string url, string title, UmamiPayload? payload = null,
        UmamiEventData? eventData = null, bool useDefaultUserAgent = false)
```
This exists on all the tracking methods there and just sets a parameter on the `UmamiPayload` object.

On `UmamiClient` these can be set as follows:

```csharp
    [Fact]
    public async Task TrackPageView_WithUrl()
    {
        var handler = JwtResponseHandler.Create();
        var umamiClient = GetServices(handler);
        var response = await umamiClient.TrackPageViewAndDecode("https://example.com", "Example Page",
            new UmamiPayload { UseDefaultUserAgent = true });
        Assert.NotNull(response);
        Assert.Equal(UmamiDataResponse.ResponseStatus.Success, response.Status);
    }
```

In this test I use the new `TrackPageViewAndDecode` method which returns a `UmamiDataResponse` object. This object contains decoded JWT token (which is invalid if it's a bot so this is useful to check) and the status of the request.

## `PayloadService`
This is all handled in the `Payload` Service which is responsible for populating the payload object. This is where the `UseDefaultUserAgent` is set.

By default I populate the payload from the `HttpContext` so you usually get this set correctly; I'll show later where this is pulled back in from Umami. 

```csharp
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
```

THEN I have a piece of code called `PopulateFromPayload` which is where the request object gets it's data set up:

```csharp
    public static string DefaultUserAgent =>
        $"Mozilla/5.0 (Windows 11)  Umami.Net/{Assembly.GetAssembly(typeof(UmamiClient))!.GetName().Version}";

    public UmamiPayload PopulateFromPayload(UmamiPayload? payload, UmamiEventData? data)
    {
        var newPayload = GetPayload(data: data);
        ...
        
        newPayload.UserAgent = payload.UserAgent ?? DefaultUserAgent;

        if (payload.UseDefaultUserAgent)
        {
            var userData = newPayload.Data ?? new UmamiEventData();
            userData.TryAdd("OriginalUserAgent", newPayload.UserAgent ?? "");
            newPayload.UserAgent = DefaultUserAgent;
            newPayload.Data = userData;
        }


        logger.LogInformation("Using UserAgent: {UserAgent}", newPayload.UserAgent);
     }        
        
```
You'll see that this defines a new Useragent at the top of the file (which I've confirmed isn't *currently* detected as a bot). Then in the method it detects whether either the UserAgent is null (which shouldn't happen unless it's called from code without an HttpContext) or if the `UseDefaultUserAgent` is set. If it is then it sets the UserAgent to the default and adds the original UserAgent to the data object.

This is then logged so you can see what UserAgent is being used.

## Decoding the Response.

In Umami.Net 0.3.0 I added a number of new 'AndDecode' methods which return a `UmamiDataResponse` object. This object contains the decoded JWT token.

```csharp
    public async Task<UmamiDataResponse?> TrackPageViewAndDecode(
        string? url = "",
        string? title = "",
        UmamiPayload? payload = null,
        UmamiEventData? eventData = null)
    {
        var response = await TrackPageView(url, title, payload, eventData);
        return await DecodeResponse(response);
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
```
You can see that this calls into the normal `TrackPageView` method then calls a method called `DecodeResponse` which checks the response for the `beep` and `boop` strings (for bot detection). If it finds them then it logs a warning and returns a `BotDetected` status. If it doesn't find them then it decodes the JWT token and returns the payload.

The JWT token itself is just a Base64 encoded string which contains the data that Umami has stored. This is decoded and returned as a `UmamiDataResponse` object.

The complete source for this is below:

<details>
<summary>Response Decoder</summary>

```csharp
using System.IdentityModel.Tokens.Jwt;

namespace Umami.Net.Models;

public class UmamiDataResponse
{
    public enum ResponseStatus
    {
        Failed,
        BotDetected,
        Success
    }

    public UmamiDataResponse(ResponseStatus status)
    {
        Status = status;
    }

    public ResponseStatus Status { get; set; }

    public Guid Id { get; set; }
    public Guid WebsiteId { get; set; }
    public string? Hostname { get; set; }
    public string? Browser { get; set; }
    public string? Os { get; set; }
    public string? Device { get; set; }
    public string? Screen { get; set; }
    public string? Language { get; set; }
    public string? Country { get; set; }
    public string? Subdivision1 { get; set; }
    public string? Subdivision2 { get; set; }
    public string? City { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid VisitId { get; set; }
    public long Iat { get; set; }

    public static UmamiDataResponse Decode(JwtPayload? payload)
    {
        if (payload == null) return new UmamiDataResponse(ResponseStatus.Failed);
        payload.TryGetValue("visitId", out var visitIdObj);
        payload.TryGetValue("iat", out var iatObj);
        //This should only happen then the payload is dummy.
        if (payload.Count == 2)
        {
            var visitId = visitIdObj != null ? Guid.Parse(visitIdObj.ToString()!) : Guid.Empty;
            var iat = iatObj != null ? long.Parse(iatObj.ToString()!) : 0;

            return new UmamiDataResponse(ResponseStatus.Success)
            {
                VisitId = visitId,
                Iat = iat
            };
        }

        payload.TryGetValue("id", out var idObj);
        payload.TryGetValue("websiteId", out var websiteIdObj);
        payload.TryGetValue("hostname", out var hostnameObj);
        payload.TryGetValue("browser", out var browserObj);
        payload.TryGetValue("os", out var osObj);
        payload.TryGetValue("device", out var deviceObj);
        payload.TryGetValue("screen", out var screenObj);
        payload.TryGetValue("language", out var languageObj);
        payload.TryGetValue("country", out var countryObj);
        payload.TryGetValue("subdivision1", out var subdivision1Obj);
        payload.TryGetValue("subdivision2", out var subdivision2Obj);
        payload.TryGetValue("city", out var cityObj);
        payload.TryGetValue("createdAt", out var createdAtObj);

        return new UmamiDataResponse(ResponseStatus.Success)
        {
            Id = idObj != null ? Guid.Parse(idObj.ToString()!) : Guid.Empty,
            WebsiteId = websiteIdObj != null ? Guid.Parse(websiteIdObj.ToString()!) : Guid.Empty,
            Hostname = hostnameObj?.ToString(),
            Browser = browserObj?.ToString(),
            Os = osObj?.ToString(),
            Device = deviceObj?.ToString(),
            Screen = screenObj?.ToString(),
            Language = languageObj?.ToString(),
            Country = countryObj?.ToString(),
            Subdivision1 = subdivision1Obj?.ToString(),
            Subdivision2 = subdivision2Obj?.ToString(),
            City = cityObj?.ToString(),
            CreatedAt = createdAtObj != null ? DateTime.Parse(createdAtObj.ToString()!) : DateTime.MinValue,
            VisitId = visitIdObj != null ? Guid.Parse(visitIdObj.ToString()!) : Guid.Empty,
            Iat = iatObj != null ? long.Parse(iatObj.ToString()!) : 0
        };
    }
}
```

</details>

You can see that this contains a bunch of useful information about the request that Umami has stored. If you wanted for example to show different content based on locale, language,  browser etc this lets you do it.

```csharp
    public Guid Id { get; set; }
    public Guid WebsiteId { get; set; }
    public string? Hostname { get; set; }
    public string? Browser { get; set; }
    public string? Os { get; set; }
    public string? Device { get; set; }
    public string? Screen { get; set; }
    public string? Language { get; set; }
    public string? Country { get; set; }
    public string? Subdivision1 { get; set; }
    public string? Subdivision2 { get; set; }
    public string? City { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid VisitId { get; set; }
    public long Iat { get; set; }
```

# In Conclusion
So just a short post covering some new functionality in Umami.Net 0.4.0 which allows you to specify a default User Agent for specific requests. This is useful for tracking requests that Umami would otherwise ignore.