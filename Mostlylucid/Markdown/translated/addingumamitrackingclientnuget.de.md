# Hinzufügen von Umami Tracking Client Nuget Paket

<!--category-- ASP.NET, Umami, Nuget -->
<datetime class="hidden">2024-08-28T02:00</datetime>

# Einleitung

Jetzt habe ich den Umami-Client; ich muss ihn verpacken und als Nuget-Paket zur Verfügung stellen. Dies ist ein ziemlich einfacher Prozess, aber es gibt ein paar Dinge zu beachten.

[TOC]

# Erstellen des Nuget-Pakets

## Versionierung

Ich beschloss zu kopieren [Khalid](https://khalidabuhakmeh.com/) und verwenden Sie das ausgezeichnete Minver-Paket, um mein Nuget-Paket zu versionieren. Dies ist ein einfaches Paket, das das git-Versions-Tag verwendet, um die Versionsnummer zu bestimmen.

Um es zu benutzen, habe ich einfach die folgenden zu meinem `Umami.Net.csproj` Datei:

```xml
    <PropertyGroup>
    <MinVerTagPrefix>v</MinVerTagPrefix>
</PropertyGroup>
```

Auf diese Weise kann ich meine Version mit einem `v` und das Paket wird korrekt versioniert.

```bash
 git tag v0.0.8       
 git push origin v0.0.8

```

Wird dieses Tag schieben, dann habe ich ein GitHub Action-Setup, um auf dieses Tag zu warten und das Nuget-Paket zu bauen.

## Aufbau des Nuget-Pakets

Ich habe eine GitHub-Aktion, die das Nuget-Paket baut und es in das GitHub-Paket-Repository schiebt. Dies ist ein einfacher Prozess, der die `dotnet pack` Befehl, um das Paket zu bauen und dann die `dotnet nuget push` Befehl, um es in das Nuget-Repository zu schieben.

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

### Readme und Icon hinzufügen

Das ist ziemlich einfach, ich füge eine `README.md` Datei an die Wurzel des Projekts und ein `icon.png` Datei an der Wurzel des Projekts. Das `README.md` Datei wird als Beschreibung des Pakets und der `icon.png` Datei wird als Symbol für das Paket verwendet.

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

In meiner README.md-Datei habe ich einen Link zum GitHub-Repository und eine Beschreibung des Pakets.

Nachstehend neu hergestellt:

# Umami.Net

Dies ist ein.NET Core Client für die Umami Tracking API.
Es basiert auf dem Umami Node Client, der gefunden werden kann [Hierher](https://github.com/umami-software/node).

Sie können sehen, wie man Umami als Docker-Container einrichtet [Hierher](https://www.mostlylucid.net/blog/usingumamiforlocalanalytics).
Sie können mehr Details über seine Kreation auf meinem Blog lesen [Hierher](https://www.mostlylucid.net/blog/addingumamitrackingclientfollowup).

Um diesen Client zu verwenden, benötigen Sie die folgende Konfiguration appsettings.json:

```json
{
  "Analytics":{
    "UmamiPath" : "https://umamilocal.mostlylucid.net",
    "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee"
  },
}
```

Dabei ist `UmamiPath` ist der Weg zu Ihrer Umami-Instanz und `WebsiteId` ist die ID der Website, die Sie verfolgen möchten.

Um den Client nutzen zu können, müssen Sie Folgendes hinzufügen: `Program.cs`:

```csharp
using Umami.Net;

services.SetupUmamiClient(builder.Configuration);
```

Damit wird der Umami-Client zur Dienstleistungssammlung hinzugefügt.

Sie können den Client dann auf zwei Arten verwenden:

1. Injizieren Sie die `UmamiClient` in Ihre Klasse und rufen Sie die `Track` Methode:

```csharp
 // Inject UmamiClient umamiClient
 await umamiClient.Track("Search", new UmamiEventData(){{"query", encodedQuery}});
```

2. Verwenden Sie die `UmamiBackgroundSender` um Ereignisse im Hintergrund zu verfolgen (damit wird ein `IHostedService` um Ereignisse im Hintergrund zu senden:

```csharp
 // Inject UmamiBackgroundSender umamiBackgroundSender
await umamiBackgroundSender.Track("Search", new UmamiEventData(){{"query", encodedQuery}});
```

Der Client wird das Event an die Umami API senden und es wird gespeichert.

Das `UmamiEventData` ist ein Wörterbuch von Schlüsselwertpaaren, das als Ereignisdaten an die Umami API gesendet wird.

Es gibt zusätzlich mehr Low-Level-Methoden, die verwendet werden können, um Ereignisse an die Umami API zu senden.

Auf den beiden `UmamiClient` und `UmamiBackgroundSender` Sie können die folgende Methode aufrufen.

```csharp


 Send(UmamiPayload? payload = null, UmamiEventData? eventData = null,
        string eventType = "event")
```

Wenn Sie nicht in einem `UmamiPayload` Objekt, wird der Client erstellt ein für Sie mit dem `WebsiteId` von den appsettings.json.

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

Sie können sehen, dass dies bevölkert die `UmamiPayload` Objekt mit der `WebsiteId` von den appsettings.json, die `Url`, `IpAddress`, `UserAgent`, `Referrer` und `Hostname` von der `HttpContext`.

HINWEIS: eventType kann nur "Ereignis" oder "Identifizieren" gemäß der Umami API sein.

# Schlussfolgerung

So können Sie nun Umami.Net von Nuget installieren und in Ihrer ASP.NET Core Anwendung verwenden. Ich hoffe, Sie finden es nützlich. Ich werde die Tests in zukünftigen Posts weiter optimieren und hinzufügen.