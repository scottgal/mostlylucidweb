# Ajouter Umami Tracking Client Nuget Package

<!--category-- ASP.NET, Umami, Nuget -->
<datetime class="hidden">2024-08-28T02:00</datetime>

# Présentation

Non, j'ai le client Umami que je dois emballer et le rendre disponible en tant que paquet Nuget. Il s'agit d'un processus assez simple, mais il y a quelques choses à savoir.

[TOC]

# Création du paquet Nuget

## Mise en forme

J'ai décidé de copier [Khalid](@khalidabuhakmeh@mastodon.social) et utilisez l'excellent paquet Minver pour la version de mon paquet Nuget. Il s'agit d'un paquet simple qui utilise la balise de version git pour déterminer le numéro de version.

Pour l'utiliser, j'ai simplement ajouté ce qui suit à mon `Umami.Net.csproj` fichier & #160;:

```xml
    <PropertyGroup>
    <MinVerTagPrefix>v</MinVerTagPrefix>
</PropertyGroup>
```

De cette façon, je peux tagger ma version avec un `v` et le paquet sera mis en version correctement.

```bash
 git tag v0.0.8       
 git push origin v0.0.8

```

Poussera cette balise, puis j'ai une configuration GitHub Action pour attendre cette balise et construire le paquet Nuget.

## Construire le paquet Nuget

J'ai une action GitHub qui construit le paquet Nuget et le pousse vers le dépôt de paquets GitHub. Il s'agit d'un processus simple qui utilise le `dotnet pack` commande de construire le paquet et puis le `dotnet nuget push` commande de le pousser vers le dépôt nuget.

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

### Ajout de Readme et d'Icône

C'est assez simple, j'ajoute un `README.md` fichier à la racine du projet et un `icon.png` fichier à la racine du projet. Les `README.md` fichier est utilisé comme la description du paquet et le `icon.png` fichier est utilisé comme l'icône pour le paquet.

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

Dans mon fichier README.md, j'ai un lien vers le dépôt GitHub et une description du paquet.

Reproduit ci-dessous:

# Umami.Net

C'est un client.NET Core pour l'API de suivi Umami.
Il est basé sur le client Umami Node, qui peut être trouvé [Ici.](https://github.com/umami-software/node).

Vous pouvez voir comment configurer Umami comme un conteneur Docker [Ici.](https://www.mostlylucid.net/blog/usingumamiforlocalanalytics).
Vous pouvez lire plus de détails sur sa création sur mon blog [Ici.](https://www.mostlylucid.net/blog/addingumamitrackingclientfollowup).

Pour utiliser ce client, vous avez besoin de la configuration appsettings.json suivante :

```json
{
  "Analytics":{
    "UmamiPath" : "https://umamilocal.mostlylucid.net",
    "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee"
  },
}
```

où `UmamiPath` est le chemin vers votre instance Umami et `WebsiteId` est l'identité du site que vous voulez suivre.

Pour utiliser le client, vous devez ajouter ce qui suit à votre `Program.cs`:

```csharp
using Umami.Net;

services.SetupUmamiClient(builder.Configuration);
```

Cela ajoutera le client Umami à la collection de services.

Vous pouvez ensuite utiliser le client de deux façons :

1. Injecter `UmamiClient` dans votre classe et appelez le `Track` méthode:

```csharp
 // Inject UmamiClient umamiClient
 await umamiClient.Track("Search", new UmamiEventData(){{"query", encodedQuery}});
```

2. Utilisez la `UmamiBackgroundSender` pour suivre les événements en arrière-plan (ceci utilise un `IHostedService` pour envoyer les événements en arrière-plan):

```csharp
 // Inject UmamiBackgroundSender umamiBackgroundSender
await umamiBackgroundSender.Track("Search", new UmamiEventData(){{"query", encodedQuery}});
```

Le client enverra l'événement à l'API Umami et il sera stocké.

Les `UmamiEventData` est un dictionnaire de paires de valeurs clés qui sera envoyé à l'API Umami comme données de l'événement.

Il existe en outre des méthodes plus basses qui peuvent être utilisées pour envoyer des événements à l'API Umami.

Sur les deux `UmamiClient` et `UmamiBackgroundSender` vous pouvez appeler la méthode suivante.

```csharp


 Send(UmamiPayload? payload = null, UmamiEventData? eventData = null,
        string eventType = "event")
```

Si vous ne passez pas dans un `UmamiPayload` objet, le client en créera un pour vous en utilisant le `WebsiteId` de l'appsettings.json.

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

Vous pouvez voir que cela peuple le `UmamiPayload` objet avec `WebsiteId` de l'appsettings.json, le `Url`, `IpAddress`, `UserAgent`, `Referrer` et `Hostname` de l'Organisation des Nations Unies pour l'élimination de toutes les formes de discrimination à l'égard des femmes `HttpContext`.

REMARQUE: eventType ne peut être que "événement" ou "identifier" selon l'API Umami.

# En conclusion

C'est donc cela que vous pouvez maintenant installer Umami.Net à partir de Nuget et l'utiliser dans votre application ASP.NET Core. J'espère que vous le trouverez utile. Je vais continuer à modifier et à ajouter des tests dans les futurs postes.