# Umami Tracking Client Nuget-pakket toevoegen

<!--category-- ASP.NET, Umami, Nuget -->
<datetime class="hidden">2024-08-28T02:00</datetime>

# Inleiding

Nu heb ik de Umami-cliënt; ik moet het inpakken en beschikbaar maken als een Nuget-pakket. Dit is een vrij eenvoudig proces, maar er zijn een paar dingen om je van bewust te zijn.

[TOC]

# Het Nuget-pakket aanmaken

## Versie

Ik besloot te kopiëren [Khalid](https://khalidabuhakmeh.com/) en gebruik het uitstekende Minver pakket om mijn Nuget pakket te versieren. Dit is een eenvoudig pakket dat de git versie tag gebruikt om het versienummer te bepalen.

Om het te gebruiken heb ik gewoon het volgende toegevoegd aan mijn `Umami.Net.csproj` bestand:

```xml
    <PropertyGroup>
    <MinVerTagPrefix>v</MinVerTagPrefix>
</PropertyGroup>
```

Op die manier kan ik mijn versie tag met een `v` en het pakket zal correct worden geversied.

```bash
 git tag v0.0.8       
 git push origin v0.0.8

```

Will push deze tag, dan heb ik een GitHub Action setup om te wachten op die tag en het Nuget pakket te bouwen.

## Bouwen van het Nuget-pakket

Ik heb een GitHub Actie die het Nuget pakket bouwt en het naar de GitHub pakket repository pusht. Dit is een eenvoudig proces dat gebruik maakt van de `dotnet pack` commando om het pakket te bouwen en dan de `dotnet nuget push` commando om het naar de nuget repository te pushen.

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

### Readme en pictogram toevoegen

Dit is vrij eenvoudig, ik voeg een `README.md` bestand naar de root van het project en a `icon.png` bestand naar de root van het project. De `README.md` bestand wordt gebruikt als de beschrijving van het pakket en de `icon.png` bestand wordt gebruikt als pictogram voor het pakket.

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

In mijn README.md bestand heb ik een link naar de GitHub repository en een beschrijving van het pakket.

Onderstaand opnieuw geproduceerd:

# Umami.Net

Dit is een.NET Core client voor de Umami tracking API.
Het is gebaseerd op de Umami Node client, die kan worden gevonden [Hier.](https://github.com/umami-software/node).

U kunt zien hoe u Umami als docker container kunt instellen [Hier.](https://www.mostlylucid.net/blog/usingumamiforlocalanalytics).
Lees meer details over het is creatie op mijn blog [Hier.](https://www.mostlylucid.net/blog/addingumamitrackingclientfollowup).

Om deze client te gebruiken heb je de volgende appsettings.json configuratie nodig:

```json
{
  "Analytics":{
    "UmamiPath" : "https://umamilocal.mostlylucid.net",
    "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee"
  },
}
```

waarbij `UmamiPath` is het pad naar uw Umami instantie en `WebsiteId` is het id van de website die u wilt volgen.

Om de client te gebruiken moet u het volgende toevoegen aan uw `Program.cs`:

```csharp
using Umami.Net;

services.SetupUmamiClient(builder.Configuration);
```

Dit zal toevoegen de Umami client aan de diensten collectie.

Vervolgens kunt u de client op twee manieren gebruiken:

1. Injecteren van de `UmamiClient` in uw klas en bel de `Track` methode:

```csharp
 // Inject UmamiClient umamiClient
 await umamiClient.Track("Search", new UmamiEventData(){{"query", encodedQuery}});
```

2. Gebruik de `UmamiBackgroundSender` om gebeurtenissen op de achtergrond te volgen (dit maakt gebruik van een `IHostedService` om gebeurtenissen op de achtergrond te verzenden:

```csharp
 // Inject UmamiBackgroundSender umamiBackgroundSender
await umamiBackgroundSender.Track("Search", new UmamiEventData(){{"query", encodedQuery}});
```

De klant zal het evenement naar de Umami API sturen en het zal worden opgeslagen.

De `UmamiEventData` is een woordenboek van sleutelwaarde paren die zal worden verzonden naar de Umami API als de gebeurtenis gegevens.

Er zijn bovendien meer laag niveau methoden die kunnen worden gebruikt om gebeurtenissen te sturen naar de Umami API.

Op zowel de `UmamiClient` en `UmamiBackgroundSender` U kunt de volgende methode bellen.

```csharp


 Send(UmamiPayload? payload = null, UmamiEventData? eventData = null,
        string eventType = "event")
```

Als u niet slaagt in een `UmamiPayload` object, de client zal een voor u maken met behulp van de `WebsiteId` van de apps.json.

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

U kunt zien dat dit bevolkt de `UmamiPayload` object met de `WebsiteId` van de apps.json, de `Url`, `IpAddress`, `UserAgent`, `Referrer` en `Hostname` van de `HttpContext`.

OPMERKING: eventType kan alleen "event" of "identificeren" zijn volgens de Umami API.

# Conclusie

Dus dat is het u kunt nu installeren Umami.Net van Nuget en gebruik het in uw ASP.NET Core applicatie. Ik hoop dat je het nuttig vindt. Ik ga verder met het aanpassen en toevoegen van tests in toekomstige berichten.