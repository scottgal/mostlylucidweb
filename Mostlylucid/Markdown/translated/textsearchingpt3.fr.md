# Recherche texte complet (Pt 3 - OpenSearch avec ASP.NET Core)

<!--category-- OpenSearch, ASP.NET -->
<datetime class="hidden">2024-08-24T06:40</datetime>

## Présentation

Dans les parties précédentes de cette série, nous avons introduit le concept de recherche de texte complet et comment il peut être utilisé pour rechercher du texte dans une base de données. Dans cette partie, nous introduirons comment utiliser OpenSearch avec ASP.NET Core.

Pièces précédentes:

- [Recherche de texte complet avec Postgres](/blog/textsearchingpt1)
- [Boîte de recherche avec Postgres](/blog/textsearchingpt11)
- [Introduction à OpenSearch](/blog/textsearchingpt3)

Dans cette partie, nous aborderons comment commencer à utiliser votre nouvelle instance OpenSearch brillante avec ASP.NET Core.

[TOC]

## Configuration

Une fois que nous avons l'instance OpenSearch en cours d'exécution, nous pouvons commencer à interagir avec elle. Nous utiliserons les [Ouvrir un client de recherche](https://opensearch.org/docs/latest/clients/OSC-dot-net/) pour.NET.
Nous avons d'abord installé le client dans notre extension Setup

```csharp
    var openSearchConfig = services.ConfigurePOCO<OpenSearchConfig>(configuration.GetSection(OpenSearchConfig.Section));
        var config = new ConnectionSettings(new Uri(openSearchConfig.Endpoint))
            .EnableHttpCompression()
            .EnableDebugMode()
            .ServerCertificateValidationCallback((sender, certificate, chain, errors) => true)
            .BasicAuthentication(openSearchConfig.Username, openSearchConfig.Password);
        services.AddSingleton<OpenSearchClient>(c => new OpenSearchClient(config));
```

Cela permet de configurer le client avec le paramètre et les identifiants. Nous activons également le mode de débogage pour que nous puissions voir ce qui se passe. De plus, comme nous n'utilisons pas les certificats SSL REAL, nous désactivons la validation du certificat (ne le faites pas en production).

## Indexation des données

Le concept de base dans OpenSearch est l'index. Pensez à un index comme une table de base de données; c'est là que toutes vos données sont stockées.

Pour ce faire, nous utiliserons les [Ouvrir un client de recherche](https://opensearch.org/docs/latest/clients/OSC-dot-net/) pour.NET. Vous pouvez installer ceci via NuGet:

Vous remarquerez qu'il y en a deux - Opensearch.Net et Opensearch.Client. Le premier est les choses de bas niveau comme la gestion de connexion, le second est les choses de haut niveau comme l'indexation et la recherche.

Maintenant que nous l'avons installé, nous pouvons commencer à examiner les données d'indexation.

La création d'un index est semi-dressée vers l'avant. Vous définissez simplement à quoi votre index devrait ressembler et puis créez-le.
Dans le code ci-dessous vous pouvez voir que nous'map' notre modèle d'index (une version simplifiée du modèle de base de données du blog).
Pour chaque champ de ce modèle, nous définissons ensuite le type (texte, date, mot-clé, etc.) et le type d'analyseur à utiliser.

Le type est important car il définit comment les données sont stockées et comment elles peuvent être recherchées. Par exemple, un champ 'texte' est analysé et tokenisé, un champ'mot-clé' ne l'est pas. Donc vous vous attendez à rechercher un champ de mots clés exactement comme il est stocké, mais un champ de texte vous pouvez rechercher des parties du texte.

Aussi ici Catégories est en fait une chaîne[] mais le type de mot-clé comprend comment les gérer correctement.

```csharp
   public async Task CreateIndex(string language)
    {
        var languageName = language.ConvertCodeToLanguageName();
        var indexName = GetBlogIndexName(language);

      var response =  await client.Indices.CreateAsync(indexName, c => c
            .Settings(s => s
                .NumberOfShards(1)
                .NumberOfReplicas(1)
            )
            .Map<BlogIndexModel>(m => m
                .Properties(p => p
                    .Text(t => t
                        .Name(n => n.Title)
                        .Analyzer(languageName)
                    )
                    .Text(t => t
                        .Name(n => n.Content)
                        .Analyzer(languageName)
                    )
                    .Text(t => t
                        .Name(n => n.Language)
                    )
                    .Date(t => t
                        .Name(n => n.LastUpdated)
                    )
                    .Date(t => t
                        .Name(n => n.Published)
                    )
                    .Date(t => t
                        .Name(n => n.LastUpdated)
                    )
                    .Keyword(t => t
                        .Name(n => n.Id)
                    )
                    .Keyword(t=>t
                        .Name(n=>n.Slug)
                    )
                    .Keyword(t=>t
                        .Name(n=>n.Hash)
                    )
                    .Keyword(t => t
                        .Name(n => n.Categories)
                    )
                )
            )
        );
        
        if (!response.IsValid)
        {
           logger.LogError("Failed to create index {IndexName}: {Error}", indexName, response.DebugInformation);
        }
    }
```

## Ajout d'éléments à l'index

Une fois que nous avons mis en place notre index pour y ajouter des éléments, nous devons ajouter des éléments à cet index. Ici, en ajoutant un BUNCH, nous utilisons une méthode d'insertion en vrac.

Vous pouvez voir que nous appelons d'abord dans une méthode appelée`GetExistingPosts` qui retourne tous les messages qui sont déjà dans l'index. On regroupe ensuite les messages par langue et on filtre la langue «uk» (comme nous ne voulons pas indexer cela puisqu'il a besoin d'un plugin supplémentaire que nous n'avons pas encore). Nous filtrant ensuite tous les messages qui sont déjà dans l'index.
Nous utilisons le hachage et l'id pour identifier si un message est déjà dans l'index.

```csharp
    public async Task AddPostsToIndex(IEnumerable<BlogIndexModel> posts)
    {
        var existingPosts = await GetExistingPosts();
        var langPosts = posts.GroupBy(p => p.Language);
        langPosts=langPosts.Where(p => p.Key!="uk");
        langPosts = langPosts.Where(p =>
            p.Any(post => !existingPosts.Any(existing => existing.Id == post.Id && existing.Hash == post.Hash)));
        
        foreach (var blogIndexModels in langPosts)
        {
            
            var language = blogIndexModels.Key;
            var indexName = GetBlogIndexName(language);
            if(!await IndexExists(language))
            {
                await CreateIndex(language);
            }
            
            var bulkRequest = new BulkRequest(indexName)
            {
                Operations = new BulkOperationsCollection<IBulkOperation>(blogIndexModels.ToList()
                    .Select(p => new BulkIndexOperation<BlogIndexModel>(p))
                    .ToList()),
                Refresh = Refresh.True,
                ErrorTrace = true,
                RequestConfiguration = new RequestConfiguration
                {
                    MaxRetries = 3
                }
            };

            var bulkResponse = await client.BulkAsync(bulkRequest);
            if (!bulkResponse.IsValid)
            {
                logger.LogError("Failed to add posts to index {IndexName}: {Error}", indexName, bulkResponse.DebugInformation);
            }
            
        }
    }
```

Une fois que nous avons filtré les messages existants et notre analyseur manquant, nous créons un nouvel index (basé sur le nom, dans mon cas "la plupart des lylucide-blog-<language>") et puis créer une requête en vrac. Cette requête en vrac est une collection d'opérations à effectuer sur l'index.
Ceci est plus efficace que d'ajouter chaque article un par un.

Vous verrez ça dans le `BulkRequest` nous avons mis le `Refresh` propriété à `true`C'est ce que j'ai dit. Cela signifie qu'une fois l'insert en vrac terminé, l'index est rafraîchi. Ce n'est pas vraiment nécessaire, mais c'est utile pour le débogage.

## Recherche dans l'index

Une bonne façon de tester pour voir ce qui a été réellement créé ici est d'aller dans les outils Dev sur les tableaux de bord OpenSearch et d'exécuter une requête de recherche.

```json
GET /mostlylucid-blog-*
{}
```

Cette requête nous retournera tous les index correspondant au modèle `mostlylucid-blog-*`C'est ce que j'ai dit. (ainsi tous nos indices jusqu'à présent).

```json
{
  "mostlylucid-blog-ar": {
    "aliases": {},
    "mappings": {
      "properties": {
        "categories": {
          "type": "keyword"
        },
        "content": {
          "type": "text",
          "analyzer": "arabic"
        },
        "hash": {
          "type": "keyword"
        },
        "id": {
          "type": "keyword"
        },
        "language": {
          "type": "text"
        },
        "lastUpdated": {
          "type": "date"
        },
        "published": {
          "type": "date"
        },
        "slug": {
          "type": "keyword"
        },
        "title": {
          "type": "text",
          "analyzer": "arabic"
        }
      }
    },
    "settings": {
      "index": {
        "replication": {
          "type": "DOCUMENT"
..MANY MORE
```

Dev Tools dans OpenSearch Dashboards est un excellent moyen de tester vos requêtes avant de les mettre dans votre code.

![Outils Dev](devtools.png?width=900&quality=25)

## Recherche dans l'index

Maintenant nous pouvons commencer à chercher l'index. Nous pouvons utiliser le `Search` méthode sur le client pour le faire.
C'est là qu'intervient le vrai pouvoir d'OpenSearch. Il a littéralement [Des dizaines de différents types de requête](https://opensearch.org/docs/latest/query-dsl/) vous pouvez utiliser pour rechercher vos données. Tout, d'une simple recherche par mot-clé à une recherche "neurale" complexe.

```csharp
    public async Task<List<BlogIndexModel>> GetSearchResults(string language, string query, int page = 1, int pageSize = 10)
    {
        var indexName = GetBlogIndexName(language);
        var searchResponse = await client.SearchAsync<BlogIndexModel>(s => s
                .Index(indexName)  // Match index pattern
                .Query(q => q
                    .Bool(b => b
                        .Must(m => m
                            .MultiMatch(mm => mm
                                .Query(query)
                                .Fields(f => f
                                    .Field(p => p.Title, boost: 2.0) 
                                    .Field(p => p.Categories, boost: 1.5) 
                                    .Field(p => p.Content)
                                )
                                .Type(TextQueryType.BestFields)
                                .Fuzziness(Fuzziness.Auto)
                            )
                        )
                    )
                )
                .Skip((page -1) * pageSize)  // Skip the first n results (adjust as needed)
                .Size(pageSize)  // Limit the number of results (adjust as needed)
        );

        if(!searchResponse.IsValid)
        {
            logger.LogError("Failed to search index {IndexName}: {Error}", indexName, searchResponse.DebugInformation);
            return new List<BlogIndexModel>();
        }
        return searchResponse.Documents.ToList();
    }

```

### Description des requêtes

Cette méthode, `GetSearchResults`, est conçu pour interroger un index d'OpenSearch spécifique pour récupérer les messages de blog. Il faut trois paramètres : `language`, `query`, et paramètres de pagination `page` et `pageSize`C'est ce que j'ai dit. Voici ce qu'il fait :

1. **Sélection de l'index**:
   
   - Il récupère le nom de l'index en utilisant le `GetBlogIndexName` méthode basée sur la langue fournie. L'index est sélectionné dynamiquement selon la langue.

2. **Recherche d'interrogations**:
   
   - La requête utilise un `Bool` requête avec une `Must` une clause garantissant que les résultats correspondent à certains critères.
   - À l'intérieur du `Must` la clause a) de l'Accord sur les tarifs douaniers et le commerce (Accord sur les tarifs douaniers et le commerce (Accord sur les tarifs douaniers et le commerce) et la clause a) de l'Accord sur les tarifs douaniers et le commerce (Accord sur les tarifs douaniers et le commerce) de l'Accord sur les tarifs douaniers et le commerce (Accord sur les tarifs douaniers et le commerce (Accord sur les tarifs douaniers et le commerce) de l'Accord sur les tarifs douaniers et le commerce (Accord sur les tarifs douaniers et le commerce (Accord sur les tarifs douaniers et le commerce) et de l'Accord sur les tarifs douaniers et le commerce (Accord sur les tarifs douaniers et le commerce) de l'Accord sur les tarifs douaniers et le commerce (Accord sur les tarifs douaniers et le commerce (Accord sur les tarifs douaniers et le commerce) de l'Accord sur les tarifs douaniers et le commerce (Accord sur les `MultiMatch` requête est utilisé pour rechercher à travers plusieurs champs (`Title`, `Categories`, et `Content`).
     - **Accélération**: Les `Title` champ est donné un coup de pouce de `2.0`, le rendant plus important dans la recherche, et `Categories` a un coup de pouce de `1.5`C'est ce que j'ai dit. Cela signifie que les documents où la recherche apparaît dans le titre ou les catégories seront classés plus haut.
     - **Type de requête**: Il utilise `BestFields`, qui tente de trouver le champ le mieux adapté à la requête.
     - **Fuzzosité**: Les `Fuzziness.Auto` le paramètre permet des correspondances approximatives (p. ex. manipulation de typos mineurs).

3. **Pagination**:
   
   - Les `Skip` méthode saute la première `n` résultats en fonction du nombre de pages, calculés comme suit: `(page - 1) * pageSize`C'est ce que j'ai dit. Cela aide à naviguer à travers les résultats paginés.
   - Les `Size` méthode limite le nombre de documents retournés à la `pageSize`.

4. **Gestion des erreurs**:
   
   - Si la requête échoue, une erreur est enregistrée et une liste vide est retournée.

5. **Résultat**:
   
   - La méthode renvoie une liste de `BlogIndexModel` les documents correspondant aux critères de recherche.

Ainsi, vous pouvez voir que nous pouvons être super flexibles sur la façon dont nous recherchons nos données. Nous pouvons rechercher des champs spécifiques, nous pouvons booster certains champs, nous pouvons même rechercher à travers plusieurs index.

Un avantage BIG est la facilité qith que nous pouvons prendre en charge plusieurs langues. Nous avons un index différent pour chaque langue et permettons la recherche à l'intérieur de cet index. Cela signifie que nous pouvons utiliser le bon analyseur pour chaque langue et obtenir les meilleurs résultats.

## La nouvelle API de recherche

Contrairement à l'API de recherche que nous avons vu dans les parties précédentes de cette série, nous pouvons grandement simplifier le processus de recherche en utilisant OpenSearch. Nous pouvons simplement jeter du texte à cette requête et obtenir de bons résultats en retour.

```csharp
   [HttpGet]
    [Route("osearch/{query}")]
   [ValidateAntiForgeryToken]
    public async Task<JsonHttpResult<List<SearchResults>>> OpenSearch(string query, string language = MarkdownBaseService.EnglishLanguage)
    {
        var results = await indexService.GetSearchResults(language, query);
        
        var host = Request.Host.Value;
        var output = results.Select(x => new SearchResults(x.Title.Trim(), x.Slug, @Url.ActionLink("Show", "Blog", new{ x.Slug}, protocol:"https", host:host) )).ToList();
        return TypedResults.Json(output);
    }
```

Comme vous pouvez le voir, nous avons toutes les données dont nous avons besoin dans l'index pour retourner les résultats. Nous pouvons ensuite l'utiliser pour générer une URL vers le blog. Cela enlève la charge de notre base de données et rend le processus de recherche beaucoup plus rapide.

## En conclusion

Dans ce post, nous avons vu comment écrire un client C# pour interagir avec notre instance OpenSearch.