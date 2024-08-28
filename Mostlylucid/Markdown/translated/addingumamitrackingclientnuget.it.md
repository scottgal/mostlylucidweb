# Aggiunta di Umami Tracking Client Nuget Package

<!--category-- ASP.NET, Umami, Nuget -->
<datetime class="hidden">2024-08-28T02:00</datetime>

# Introduzione

No, ho il cliente Umami che devo impacchettare e renderlo disponibile come pacchetto Nuget. Questo è un processo abbastanza semplice, ma ci sono alcune cose di cui essere consapevoli.

[TOC]

# Creazione del pacchetto Nuget

## Versione

Ho deciso di copiare [KhalidCity name (optional, probably does not need a translation)](@khalidabuhakmeh@mastodon.social) e utilizzare l'eccellente pacchetto di triturazione per la versione del mio pacchetto Nuget. Questo è un semplice pacchetto che usa il tag git version per determinare il numero di versione.

Per usarlo ho semplicemente aggiunto il seguente al mio `Umami.Net.csproj` file:

```xml
    <PropertyGroup>
    <MinVerTagPrefix>v</MinVerTagPrefix>
</PropertyGroup>
```

In questo modo posso etichettare la mia versione con un `v` e il pacchetto verrà modificato correttamente.

```bash
 git tag v0.0.8       
 git push origin v0.0.8

```

Premerà questo tag, poi ho una configurazione di azione GitHub per aspettare quel tag e costruire il pacchetto Nuget.

## Costruire il pacchetto Nuget

Ho un'azione GitHub che costruisce il pacchetto Nuget e lo spinge al repository dei pacchetti GitHub. Questo è un processo semplice che utilizza il `dotnet pack` comando per costruire il pacchetto e poi il `dotnet nuget push` comando per spingerlo al repository nuget.

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

### Aggiunta di Readme e Icona

Questo è abbastanza semplice, aggiungo un `README.md` file alla radice del progetto e un `icon.png` file alla radice del progetto. La `README.md` file è usato come la descrizione del pacchetto e il `icon.png` il file viene usato come icona del pacchetto.

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

Nel mio file README.md ho un link al repository GitHub e una descrizione del pacchetto.

Riprodotto di seguito:

# Umami.NetCity name (optional, probably does not need a translation)

Questo è un client.NET Core per l'API di monitoraggio Umami.
E' basato sul client Umami Node, che puo' essere trovato. [qui](https://github.com/umami-software/node).

Potete vedere come configurare Umami come un contenitore docker [qui](https://www.mostlylucid.net/blog/usingumamiforlocalanalytics).
Puoi leggere più dettagli su di esso è la creazione sul mio blog [qui](https://www.mostlylucid.net/blog/addingumamitrackingclientfollowup).

Per utilizzare questo client è necessaria la seguente configurazione appsettings.json:

```json
{
  "Analytics":{
    "UmamiPath" : "https://umamilocal.mostlylucid.net",
    "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee"
  },
}
```

Dove `UmamiPath` è il percorso per l'istanza Umami e `WebsiteId` è l'id del sito web che si desidera tracciare.

Per utilizzare il client è necessario aggiungere quanto segue al vostro `Program.cs`:

```csharp
using Umami.Net;

services.SetupUmamiClient(builder.Configuration);
```

Questo aggiungerà il client Umami alla raccolta dei servizi.

È quindi possibile utilizzare il client in due modi:

1. Inietti `UmamiClient` nella vostra classe e chiamare il `Track` metodo:

```csharp
 // Inject UmamiClient umamiClient
 await umamiClient.Track("Search", new UmamiEventData(){{"query", encodedQuery}});
```

2. Utilizzare il `UmamiBackgroundSender` per tracciare gli eventi in background (questo usa un `IHostedService` per inviare eventi in background):

```csharp
 // Inject UmamiBackgroundSender umamiBackgroundSender
await umamiBackgroundSender.Track("Search", new UmamiEventData(){{"query", encodedQuery}});
```

Il client invierà l'evento all'API Umami e sarà memorizzato.

La `UmamiEventData` è un dizionario di coppie di valori chiave che verranno inviate all'API Umami come dati dell'evento.

Ci sono inoltre metodi di livello più basso che possono essere utilizzati per inviare eventi alle API Umami.

In entrambi i casi: `UmamiClient` e `UmamiBackgroundSender` puoi chiamare il seguente metodo.

```csharp


 Send(UmamiPayload? payload = null, UmamiEventData? eventData = null,
        string eventType = "event")
```

Se non si passa in un `UmamiPayload` oggetto, il client ne creerà uno per voi utilizzando il `WebsiteId` dall'Apsettings.json.

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

Potete vedere che questo popola il `UmamiPayload` oggetto con il `WebsiteId` da appsettings.json, il `Url`, `IpAddress`, `UserAgent`, `Referrer` e `Hostname` dal `HttpContext`.

NOTA: eventType può essere solo "evento" o "identificare" secondo l'API Umami.

# In conclusione

Così questo è ora è possibile installare Umami.Net da Nuget e utilizzarlo nella vostra applicazione ASP.NET Core. Spero che lo trovi utile. Continuerò a modificare e aggiungere test nei post futuri.