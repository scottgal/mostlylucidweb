# Umami.Net

## UmamiClient
This is a .NET Core client for the Umami tracking API.
It's based on the Umami Node client, which can be found [here](https://github.com/umami-software/node).

You can see how to set up Umami as a docker container [here](https://www.mostlylucid.net/blog/usingumamiforlocalanalytics).
You can read more detail about it's creation on my blog [here](https://www.mostlylucid.net/blog/addingumamitrackingclientfollowup).

To use this client you need the following appsettings.json configuration:

```json
{
  "Analytics":{
    "UmamiPath" : "https://umamilocal.mostlylucid.net",
    "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee"
  },
}
```

Where `UmamiPath` is the path to your Umami instance and `WebsiteId` is the id of the website you want to track.

To use the client you need to add the following to your `Program.cs`:

```csharp
using Umami.Net;

services.SetupUmamiClient(builder.Configuration);
```

This will add the Umami client to the services collection.

You can then use the client in two ways:

## Track
1. Inject the `UmamiClient` into your class and call the `Track` method:

```csharp    
 // Inject UmamiClient umamiClient
 await umamiClient.Track("Search", new UmamiEventData(){{"query", encodedQuery}});
```

2. Use the `UmamiBackgroundSender` to track events in the background (this uses an `IHostedService` to send events in the background):

```csharp
 // Inject UmamiBackgroundSender umamiBackgroundSender
await umamiBackgroundSender.Track("Search", new UmamiEventData(){{"query", encodedQuery}});
```

The client will send the event to the Umami API and it will be stored.

The `UmamiEventData` is a dictionary of key value pairs that will be sent to the Umami API as the event data.

There are additionally more low level methods that can be used to send events to the Umami API.

## Track PageView
There's also a convenience method to track a page view. This will send an  event to the Umami API with the url set (which counts as a pageview).

```csharp
  await  umamiBackgroundSender.TrackPageView("api/search/" + encodedQuery, "searchEvent", eventData: new UmamiEventData(){{"query", encodedQuery}});
  
   await umamiClient.TrackPageView("api/search/" + encodedQuery, "searchEvent", eventData: new UmamiEventData(){{"query", encodedQuery}});
```

Here we're setting the url to "api/search/" + encodedQuery and the event type to "searchEvent". We're also passing in a dictionary of key value pairs as the event data.


## Raw 'Send' method

On both the `UmamiClient` and `UmamiBackgroundSender` you can call the following method.
```csharp


 Send(UmamiPayload? payload = null, UmamiEventData? eventData = null,
        string eventType = "event")
```
If you don't pass in a `UmamiPayload` object, the client will create one for you using the `WebsiteId` from the appsettings.json.
```csharp
    public  UmamiPayload GetPayload(string? url = null, UmamiEventData? data = null)
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
           Hostname = request?.Host.Host,
        };
        
        return payload;
    }

```
You can see that this populates the `UmamiPayload` object with the `WebsiteId` from the appsettings.json, the `Url`, `IpAddress`, `UserAgent`, `Referrer` and `Hostname` from the `HttpContext`.

NOTE: eventType can only be "event" or "identify" as per the Umami API.


## UmamiData
There's also a service that can be used to pull data from the Umami API. This is a service that allows me to pull data from my Umami instance to use in stuff like sorting posts by popularity etc...

To set it up you need to add a username and password for your umami instance to the Analytics element in your settings file:
```json
    "Analytics":{
        "UmamiPath" : "https://umami.mostlylucid.net",
        "WebsiteId" : "1e3b7657-9487-4857-a9e9-4e1920aa8c42",
        "UserName": "admin",
        "Password": ""
     
    }
```
Then in your `Program.cs` you set up the `UmamiDataService` as follows:
```csharp
    services.SetupUmamiData(config);
```

You can then inject the `UmamiDataService` into your class and use it to pull data from the Umami API.

For example. to fetch all PageViews for a given date range:
```csharp
    var data = await umamiDataService.GetPageViews("2021-01-01", "2021-01-31");
```