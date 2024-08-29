# Adding Umami Tracking Client Nuget Package
<!--category-- ASP.NET, Umami, Nuget -->
<datetime class="hidden">2024-08-28T02:00</datetime>

# Introduction
Now I have the Umami client; I need to package it up and make it available as a Nuget package. This is a pretty simple process but there are a few things to be aware of.

[TOC]

# Creating the Nuget Package

## Versioning
I decided to copy [Khalid](https://khalidabuhakmeh.com/) and use the excellent minver package to version my Nuget package. This is a simple package that uses the git version tag to determine the version number. 

To use it I simply added the following to my `Umami.Net.csproj` file:

```xml
    <PropertyGroup>
    <MinVerTagPrefix>v</MinVerTagPrefix>
</PropertyGroup>
```
That way I can tag my version with a `v` and the package will be versioned correctly.

```bash
 git tag v0.0.8       
 git push origin v0.0.8

```
Will push this tag, then I have a GitHub Action setup to wait for that tag and build the Nuget package.

## Building the Nuget Package

I have a GitHub Action that builds the Nuget package and pushes it to the GitHub package repository. This is a simple process that uses the `dotnet pack` command to build the package and then the `dotnet nuget push` command to push it to the nuget repository.

```yaml
name: Publish Umami.NET
on:
  push:
    tags:
      - 'v*.*.*'  # This triggers the action for any tag that matches the pattern v1.0.0, v2.1.3, etc.

jobs:
  publish:
    runs-on: ubuntu-latest

    steps:
    - name: Checkout code
      uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.x' # Specify the .NET version you need

    - name: Restore dependencies
      run: dotnet restore ./Umami.Net/Umami.Net.csproj

    - name: Build project
      run: dotnet build --configuration Release ./Umami.Net/Umami.Net.csproj --no-restore

    - name: Pack project
      run: dotnet pack --configuration Release ./Umami.Net/Umami.Net.csproj --no-build --output ./nupkg

    - name: Publish to NuGet
      run: dotnet nuget push ./nupkg/*.nupkg --source https://api.nuget.org/v3/index.json --api-key ${{ secrets.UMAMI_NUGET_API_KEY }}
      env:
        NUGET_API_KEY: ${{ secrets.UMAMI_NUGET_API_KEY }}
```

### Adding Readme and Icon
This is pretty simple, I add a `README.md` file to the root of the project and a `icon.png` file to the root of the project. The `README.md` file is used as the description of the package and the `icon.png` file is used as the icon for the package.

```xml
    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
        <LangVersion>12</LangVersion>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>

        <IsPackable>true</IsPackable>
        <PackageId>Umami.Net</PackageId>
        <Authors>Scott Galloway</Authors>
        <PackageIcon>icon.png</PackageIcon>
        <RepositoryUrl>https://github.com/scottgal/mostlylucidweb/tree/main/Umami.Net</RepositoryUrl>
        <PackageLicenseExpression>MIT</PackageLicenseExpression>
        <PackageTags>web</PackageTags>
        <PackageReadmeFile>README.md</PackageReadmeFile>
        <Description>
           Adds a simple Umami endpoint to your ASP.NET Core application.
        </Description>
    </PropertyGroup>
```

In my README.md file I have a link to the GitHub repository and a description of the package. 

Reproduced below:
# Umami.Net

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

# In Conclusion
So that's it you can now install Umami.Net from Nuget and use it in your ASP.NET Core application. I hope you find it useful. I'll continue tweaking and adding tests in future posts. 