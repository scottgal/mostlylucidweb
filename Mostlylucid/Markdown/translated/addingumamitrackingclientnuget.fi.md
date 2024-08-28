# Umami-seuranta asiakas Nuget-paketin lisääminen

<!--category-- ASP.NET, Umami, Nuget -->
<datetime class="hidden">2024-08-28T02:00</datetime>

# Johdanto

Ei, minulla on Umami-asiakas, joka on pakattava ja asetettava saataville Nuget-pakettina. Tämä on aika yksinkertainen prosessi, mutta muutama asia on tiedostettava.

[TÄYTÄNTÖÖNPANO

# Nuget-paketin luominen

## Muuntaminen

Päätin kopioida [Khalid](@khalidabuhakmeh@mastodon.social) ja käytä erinomaista minver-pakettia Nuget-pakettini versioon. Tämä on yksinkertainen paketti, joka käyttää git-versiolappua numeron määrittämiseen.

Voit käyttää sitä yksinkertaisesti lisäsin seuraavat minun `Umami.Net.csproj` tiedosto:

```xml
    <PropertyGroup>
    <MinVerTagPrefix>v</MinVerTagPrefix>
</PropertyGroup>
```

Siten voin merkitä versioni `v` ja paketti versioidaan oikein.

```bash
 git tag v0.0.8       
 git push origin v0.0.8

```

Painan tätä tunnistetta, ja sitten minulla on GitHub-toimintoasetus, joka odottaa tagia ja rakentaa Nuget-paketin.

## Nuget-paketin rakentaminen

Minulla on GitHub-toiminto, joka rakentaa Nuget-paketin ja vie sen GitHub-paketin arkistoon. Tämä on yksinkertainen prosessi, jossa käytetään `dotnet pack` Komento rakentaa paketin ja sen jälkeen `dotnet nuget push` Komento työntää sen nuget- arkistoon.

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

### Lisää Readme ja kuvake

Tämä on aika yksinkertaista, lisään, `README.md` tiedosto projektin ytimeen ja a `icon.png` tiedosto projektin ytimeen. Erytropoietiini `README.md` Tiedostoa käytetään paketin kuvauksena ja `icon.png` Tiedostoa käytetään paketin kuvakkeena.

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

README.Md-tiedostossani minulla on linkki GitHub-arkistoon ja kuvaus paketista.

Tuotettu uudelleen alla:

# Umami.net

Tämä on.NET Core -asiakas Umamin jäljitysrajapinnalle.
Se perustuu Umami Node -asiakkaaseen, joka löytyy [täällä](https://github.com/umami-software/node).

Nähtäväksi näkee, miten Umami asetetaan kontiksi. [täällä](https://www.mostlylucid.net/blog/usingumamiforlocalanalytics).
Voit lukea lisää yksityiskohtia sen luomisesta blogistani [täällä](https://www.mostlylucid.net/blog/addingumamitrackingclientfollowup).

Tämän asiakkaan käyttämiseen tarvitaan seuraavat asetukset.json-asetukset:

```json
{
  "Analytics":{
    "UmamiPath" : "https://umamilocal.mostlylucid.net",
    "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee"
  },
}
```

Jossa `UmamiPath` on polku Umami-instanssin ja `WebsiteId` on sivuston tunniste, jota haluat seurata.

Jotta voit käyttää asiakasta, sinun täytyy lisätä seuraavat: `Program.cs`:

```csharp
using Umami.Net;

services.SetupUmamiClient(builder.Configuration);
```

Tämä lisää Umamin asiakkaan palvelukokoelmaan.

Asiakasta voi käyttää kahdella tavalla:

1. Ruiskuta `UmamiClient` Omalle luokalle ja soittaa `Track` menetelmä:

```csharp
 // Inject UmamiClient umamiClient
 await umamiClient.Track("Search", new UmamiEventData(){{"query", encodedQuery}});
```

2. Käytä `UmamiBackgroundSender` Taustalla olevien tapahtumien seuraaminen (tässä käytetään `IHostedService` lähettää tapahtumia taustalla:

```csharp
 // Inject UmamiBackgroundSender umamiBackgroundSender
await umamiBackgroundSender.Track("Search", new UmamiEventData(){{"query", encodedQuery}});
```

Asiakas lähettää tapahtuman Umamin sovellusrajapinnalle ja tallentaa sen.

Erytropoietiini `UmamiEventData` on avainarvoparien sanakirja, joka lähetetään tapahtumatietona Umamin rajapintaan.

Lisäksi on olemassa matalan tason menetelmiä, joita voidaan käyttää tapahtumien lähettämiseen Umamin sovellusrajapinnalle.

Molemmilla `UmamiClient` sekä `UmamiBackgroundSender` seuraavaksi metodiksi voi kutsua.

```csharp


 Send(UmamiPayload? payload = null, UmamiEventData? eventData = null,
        string eventType = "event")
```

Jos et läpäise `UmamiPayload` Object, asiakas luo sellaisen sinulle käyttämällä `WebsiteId` "Apsetations.jsonista."

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

Huomaat, että tämä väestö on `UmamiPayload` Project with the `WebsiteId` upsetations.json, `Url`, `IpAddress`, `UserAgent`, `Referrer` sekä `Hostname` Euroopan unionin toiminnasta tehdyn sopimuksen 107 artiklan 3 kohdan c alakohdan nojalla `HttpContext`.

HUOMAUTUS: EventType voi olla vain "tapahtuma" tai "tunnistautua" kuten Umamin API.

# Johtopäätöksenä

Näin voit nyt asentaa Umamin.Net from Nuget -verkon ja käyttää sitä ASP.NET Core -sovelluksessasi. Toivottavasti pidät sitä hyödyllisenä. Jatkan tulevien virkojen muokkaamista ja testien lisäämistä.