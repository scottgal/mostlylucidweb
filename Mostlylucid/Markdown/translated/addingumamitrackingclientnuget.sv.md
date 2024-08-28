# Lägga till Umami Tracking Client Nuget- paket

<!--category-- ASP.NET, Umami, Nuget -->
<datetime class="hidden">2024-08-28T02:00</datetime>

# Inledning

Nu har jag Umami klienten; Jag måste packa upp den och göra den tillgänglig som ett Nuget paket. Detta är en ganska enkel process, men det finns några saker att vara medveten om.

[TOC]

# Skapa nuget-paketet

## Version

Jag bestämde mig för att kopiera [Khalid Ordförande](@khalidabuhakmeh@mastodon.social) och använd den utmärkta minver paketet för att version min Nuget paket. Detta är ett enkelt paket som använder git versionstaggen för att bestämma versionsnumret.

För att använda den lade jag helt enkelt till följande till min `Umami.Net.csproj` fil:

```xml
    <PropertyGroup>
    <MinVerTagPrefix>v</MinVerTagPrefix>
</PropertyGroup>
```

På så sätt kan jag märka min version med en `v` och paketet kommer att versionseras korrekt.

```bash
 git tag v0.0.8       
 git push origin v0.0.8

```

Kommer att trycka på den här taggen, då har jag en GitHub Action setup för att vänta på den taggen och bygga Nuget-paketet.

## Bygga Nuget-paketet

Jag har en GitHub Action som bygger Nuget-paketet och kör det till GitHub-paketarkivet. Detta är en enkel process som använder `dotnet pack` kommando för att bygga paketet och sedan `dotnet nuget push` Kommandot för att trycka den till nugetarkivet.

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

### Lägga till readme och ikon

Detta är ganska enkelt, jag lägger till en `README.md` fil till roten av projektet och en `icon.png` fil till roten av projektet. I detta sammanhang är det viktigt att se till att `README.md` filen används som beskrivning av paketet och `icon.png` filen används som ikon för paketet.

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

I min README.md-fil har jag en länk till GitHub-arkivet och en beskrivning av paketet.

Återvunnet nedan:

# Ummami.Net

Detta är en.NET Core klient för Umami spårning API.
Det är baserat på Umami Node klienten, som kan hittas [här](https://github.com/umami-software/node).

Du kan se hur man ställer in Umami som en docka behållare [här](https://www.mostlylucid.net/blog/usingumamiforlocalanalytics).
Du kan läsa mer i detalj om det är skapelse på min blogg [här](https://www.mostlylucid.net/blog/addingumamitrackingclientfollowup).

För att använda denna klient behöver du följande appsettings.json konfiguration:

```json
{
  "Analytics":{
    "UmamiPath" : "https://umamilocal.mostlylucid.net",
    "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee"
  },
}
```

där `UmamiPath` är vägen till Umami instans och `WebsiteId` är id av webbplatsen du vill spåra.

För att använda klienten måste du lägga till följande till din `Program.cs`:

```csharp
using Umami.Net;

services.SetupUmamiClient(builder.Configuration);
```

Detta kommer att lägga till Umami-klienten till tjänsteinsamlingen.

Du kan sedan använda klienten på två sätt:

1. Injicera `UmamiClient` till din klass och ring `Track` Metod:

```csharp
 // Inject UmamiClient umamiClient
 await umamiClient.Track("Search", new UmamiEventData(){{"query", encodedQuery}});
```

2. Använd `UmamiBackgroundSender` för att spåra händelser i bakgrunden (detta använder en `IHostedService` för att skicka evenemang i bakgrunden:

```csharp
 // Inject UmamiBackgroundSender umamiBackgroundSender
await umamiBackgroundSender.Track("Search", new UmamiEventData(){{"query", encodedQuery}});
```

Kunden kommer att skicka händelsen till Umami API och den kommer att lagras.

I detta sammanhang är det viktigt att se till att `UmamiEventData` är ett lexikon med nyckelvärden par som kommer att skickas till Umami API som händelsedata.

Det finns dessutom mer låg nivå metoder som kan användas för att skicka händelser till Umami API.

På båda `UmamiClient` och `UmamiBackgroundSender` Du kan kalla följande metod.

```csharp


 Send(UmamiPayload? payload = null, UmamiEventData? eventData = null,
        string eventType = "event")
```

Om du inte passerar in en `UmamiPayload` objekt, kunden kommer att skapa en för dig med hjälp av `WebsiteId` från appsettings.json.

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

Du kan se att detta befolkar `UmamiPayload` objekt med `WebsiteId` från appsettings.json, `Url`, `IpAddress`, `UserAgent`, `Referrer` och `Hostname` från `HttpContext`.

OBS: EventType kan endast vara "event" eller "identifiera" enligt Umami API.

# Slutsatser

Så det är det du nu kan installera Umami.Net från Nuget och använda det i din ASP.NET Core-applikation. Jag hoppas att du tycker att det är användbart. Jag fortsätter att justera och lägga till tester i kommande inlägg.