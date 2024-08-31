# Seq pour ASP.NET Logging - Traçage avec sérilogTraçage

<datetime class="hidden">2024-08-31T11:20</datetime>

<!--category-- ASP.NET, Seq, Serilog -->
# Présentation

Dans la partie précédente, je vous ai montré comment mettre en place [auto-hébergement pour Seq en utilisant ASP.NET Core ](/blog/selfhostingseq)C'est ce que j'ai dit. Maintenant que nous l'avons configuré il est temps d'utiliser plus de ses fonctionnalités pour permettre une log & tracing plus complète en utilisant notre nouvelle instance Seq.

[TOC]

# Recherche

Traçage est comme log++ il vous donne une couche supplémentaire d'informations sur ce qui se passe dans votre application. Il est particulièrement utile lorsque vous avez un système distribué et que vous devez tracer une demande à travers plusieurs services.
Dans ce site, je l'utilise pour traquer les problèmes rapidement; juste parce que c'est un site de loisirs ne signifie pas que j'abandonne mes normes professionnelles.

## Configuration de Serilog

Configurer le traçage avec Serilog est vraiment assez simple en utilisant le [Traçage sérilogique](https://github.com/serilog-tracing/serilog-tracing) Un paquet. Vous devez d'abord installer les paquets:

Ici nous ajoutons aussi l'évier Console et l'évier Seq

```bash
dotnet add package SerilogTracing
dotnet add package SerilogTracing.Expressions
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.Seq
```

Console est toujours utile pour le débogage et Seq est ce que nous sommes ici pour. Seq dispose également d'un tas d'enrichisseurs qui peuvent ajouter des informations supplémentaires à vos journaux.

```bash
  "Serilog": {
    "Enrich": ["FromLogContext", "WithThreadId", "WithThreadName", "WithProcessId", "WithProcessName", "FromLogContext"],
    "MinimumLevel": "Warning",
    "WriteTo": [
        {
          "Name": "Seq",
          "Args":
          {
            "serverUrl": "http://seq:5341",
            "apiKey": ""
          }
        },
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/applog-.txt",
          "rollingInterval": "Day"
        }
      }

    ],
    "Properties": {
      "ApplicationName": "mostlylucid"
    }
  }
```

Pour utiliser ces enrichisseurs, vous devez les ajouter à votre `Serilog` configuration dans votre `appsettings.json` fichier. Vous devez également installer tous les enrichisseurs séparés en utilisant Nuget.

C'est l'une des bonnes et mauvaises choses à propos de Serilog, vous finissez par installer un BUNCH de paquets; mais cela signifie que vous n'ajoutez que ce dont vous avez besoin et pas seulement un paquet monolithique.
Voici la mienne.

![Enrichisseurs sérilogiques](serilogenrichers.png)

Avec toutes ces bombes, j'obtiens une assez bonne sortie de log à Seq.

![Erreur Serilog Seq](serilogerror.png)

Ici vous voyez le message d'erreur, la trace de la pile, l'id thread, l'id process et le nom process. C'est une information utile lorsque vous essayez de trouver un problème.

Une chose à noter, c'est que j'ai mis le `  "MinimumLevel": "Warning",` dans mon `appsettings.json` fichier. Cela signifie que seuls les avertissements et au-dessus seront enregistrés à Seq. Ceci est utile pour garder le bruit bas dans vos journaux.

Cependant, dans Seq, vous pouvez également spécifier cela par Api Key; ainsi vous pouvez avoir `Information` (ou si vous êtes vraiment enthousiaste `Debug`) logage défini ici et limite ce que Seq capture réellement par la clé API.

![Clé Seq Api](apikey.png)

Remarque : vous avez toujours des frais d'application, vous pouvez également rendre cela plus dynamique afin que vous puissiez ajuster le niveau à la volée). Voir [Évier Seq ](https://github.com/datalust/serilog-sinks-seq)pour plus de détails.

```json
{
    "Serilog":
    {
        "LevelSwitches": { "$controlSwitch": "Information" },
        "MinimumLevel": { "ControlledBy": "$controlSwitch" },
        "WriteTo":
        [{
            "Name": "Seq",
            "Args":
            {
                "serverUrl": "http://localhost:5341",
                "apiKey": "yeEZyL3SMcxEKUijBjN",
                "controlLevelSwitch": "$controlSwitch"
            }
        }]
    }
}
```

## Recherche

Maintenant nous ajoutons Tracing, à nouveau en utilisant SerilogTracing c'est assez simple. Nous avons la même configuration qu'avant, mais nous ajoutons un nouvel évier pour le traçage.

```bash
dotnet add package SerilogTracing
dontet add package SerilogTracing.Expressions
dotnet add SerilogTracing.Instrumentation.AspNetCore
```

Nous ajoutons également un paquet supplémentaire pour enregistrer des informations de base plus détaillées sur aspnet.

### Mise en place `Program.cs`

Maintenant, nous pouvons commencer à utiliser le traçage. D'abord, nous devons ajouter le traçage à notre `Program.cs` fichier.

```csharp
    using var listener = new ActivityListenerConfiguration()
        .Instrument.HttpClientRequests().Instrument
        .AspNetCoreRequests()
        .TraceToSharedLogger();
```

Le traçage utilise le concept d'"activités" qui représente une unité de travail. Vous pouvez commencer une activité, faire un peu de travail et ensuite l'arrêter. Ceci est utile pour le suivi d'une demande par l'intermédiaire de plusieurs services.

Dans ce cas, nous ajoutons un traçage supplémentaire pour les requêtes HttpClient et AspNetCore. Nous ajoutons également : `TraceToSharedLogger` qui enregistrera l'activité dans le même enregistreur que le reste de notre application.

## Utilisation de la recherche dans un service

Maintenant, nous avons mis en place le traçage, nous pouvons commencer à l'utiliser dans notre application. Voici un exemple de service qui utilise le traçage.

```csharp
    public async Task<PostListViewModel> GetPostsByCategory(string category, int page = 1, int pageSize = 10,
        string language = MarkdownBaseService.EnglishLanguage)
    {
        using var activity = Log.Logger.StartActivity("GetPostsByCategory {Category}, {Page}, {PageSize}, {Language}",
            new { category, page, pageSize, language });
        try
        {
            var count = await NoTrackingQuery()
                .Where(x => x.Categories.Any(c => c.Name == category) && x.LanguageEntity.Name == language)
                .CountAsync();
            var posts = await PostsQuery()
                .Where(x => x.Categories.Any(c => c.Name == category) && x.LanguageEntity.Name == language)
                .OrderByDescending(x => x.PublishedDate.DateTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var languages = await GetLanguagesForSlugs(posts.Select(x => x.Slug).ToList());
            var postListViewModel = new PostListViewModel
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = count,
                Posts = posts.Select(x => x.ToListModel(
                    languages.FirstOrDefault(entry => entry.Key == x.Slug).Value.ToArray())).ToList()
            };
            activity.Complete();
            return postListViewModel;
        }
        catch (Exception e)
        {
            activity.Complete(LogEventLevel.Error, e);
        }

        return new PostListViewModel();
    }
```

Les lignes importantes sont les suivantes :

```csharp
  using var activity = Log.Logger.StartActivity("GetPostsByCategory {Category}, {Page}, {PageSize}, {Language}",
            new { category, page, pageSize, language });
```

Cela commence par une nouvelle 'activité' qui est une unité de travail. Il est utile pour le suivi d'une demande à travers plusieurs services.
Comme nous l'avons enveloppé dans une déclaration d'utilisation, cela complétera et éliminera à la fin de notre méthode, mais c'est une bonne pratique de la compléter explicitement.

```csharp
            activity.Complete();
```

Dans notre capture de manipulation d'exception, nous complétons également l'activité, mais avec un niveau d'erreur et l'exception. Ceci est utile pour le suivi des problèmes dans votre application.

## Utilisation de traces

Maintenant nous avons toute cette configuration nous pouvons commencer à l'utiliser. Voici un exemple de trace dans ma demande.

![Trace Http](httptrace.png)

Cela vous montre la traduction d'un seul billet balisé. Vous pouvez voir les étapes multiples pour un seul message et toutes les requêtes et les timings HttpClient.

Note J'utilise Postgres pour ma base de données, contrairement au serveur SQL, le pilote npgsql a une prise en charge native pour le traçage afin que vous puissiez obtenir des données très utiles à partir de vos requêtes de base de données comme le SQL exécuté, les timings etc. Ceux-ci sont enregistrés en tant que'spans' à Seq et semblent liek les suivants:

```json
  "@t": "2024-08-31T15:23:31.0872838Z",
"@mt": "mostlylucid",
"@m": "mostlylucid",
"@i": "3c386a9a",
"@tr": "8f9be07e41f7121cbf2866c6cd886a90",
"@sp": "8d716c5f01ad07a0",
"@st": "2024-08-31T15:23:31.0706848Z",
"@ps": "622f1c86a8b33304",
"@sk": "Client",
"ActionId": "91f5105d-93fa-4e7f-9708-b1692e046a8a",
"ActionName": "Mostlylucid.Controllers.HomeController.Index (Mostlylucid)",
"ApplicationName": "mostlylucid",
"ConnectionId": "0HN69PVEQ9S7C",
"ProcessId": 30496,
"ProcessName": "Mostlylucid",
"RequestId": "0HN69PVEQ9S7C:00000015",
"RequestPath": "/",
"SourceContext": "Npgsql",
"ThreadId": 47,
"ThreadName": ".NET TP Worker",
"db.connection_id": 1565,
"db.connection_string": "Host=localhost;Database=mostlylucid;Port=5432;Username=postgres;Application Name=mostlylucid",
"db.name": "mostlylucid",
"db.statement": "SELECT t.\"Id\", t.\"ContentHash\", t.\"HtmlContent\", t.\"LanguageId\", t.\"Markdown\", t.\"PlainTextContent\", t.\"PublishedDate\", t.\"SearchVector\", t.\"Slug\", t.\"Title\", t.\"UpdatedDate\", t.\"WordCount\", t.\"Id0\", t0.\"BlogPostId\", t0.\"CategoryId\", t0.\"Id\", t0.\"Name\", t.\"Name\"\r\nFROM (\r\n    SELECT b.\"Id\", b.\"ContentHash\", b.\"HtmlContent\", b.\"LanguageId\", b.\"Markdown\", b.\"PlainTextContent\", b.\"PublishedDate\", b.\"SearchVector\", b.\"Slug\", b.\"Title\", b.\"UpdatedDate\", b.\"WordCount\", l.\"Id\" AS \"Id0\", l.\"Name\", b.\"PublishedDate\" AT TIME ZONE 'UTC' AS c\r\n    FROM mostlylucid.\"BlogPosts\" AS b\r\n    INNER JOIN mostlylucid.\"Languages\" AS l ON b.\"LanguageId\" = l.\"Id\"\r\n    WHERE l.\"Name\" = @__language_0\r\n    ORDER BY b.\"PublishedDate\" AT TIME ZONE 'UTC' DESC\r\n    LIMIT @__p_2 OFFSET @__p_1\r\n) AS t\r\nLEFT JOIN (\r\n    SELECT b0.\"BlogPostId\", b0.\"CategoryId\", c.\"Id\", c.\"Name\"\r\n    FROM mostlylucid.blogpostcategory AS b0\r\n    INNER JOIN mostlylucid.\"Categories\" AS c ON b0.\"CategoryId\" = c.\"Id\"\r\n) AS t0 ON t.\"Id\" = t0.\"BlogPostId\"\r\nORDER BY t.c DESC, t.\"Id\", t.\"Id0\", t0.\"BlogPostId\", t0.\"CategoryId\"",
"db.system": "postgresql",
"db.user": "postgres",
"net.peer.ip": "::1",
"net.peer.name": "localhost",
"net.transport": "ip_tcp",
"otel.status_code": "OK"
```

Vous pouvez voir que cela inclut à peu près tout ce que vous devez savoir sur la requête, le SQL exécuté, la chaîne de connexion, etc. C'est une information utile lorsque vous essayez de trouver un problème. Dans une application plus petite comme celle-ci, c'est juste intéressant, dans une application distribuée, c'est de l'information en or massif pour suivre les problèmes.

# En conclusion

J'ai seulement griffé la surface de Tracing ici, c'est un peu une zone avec des défenseurs passionnés. J'espère avoir montré à quel point il est simple d'aller avec le traçage simple en utilisant Seq & Serilog pour les applications ASP.NET Core. De cette façon, je peux obtenir une grande partie de l'avantage d'outils plus puissants comme Application Insights sans le coût d'Azure (ces choses peuvent être dépensées lorsque les grumes sont grandes).