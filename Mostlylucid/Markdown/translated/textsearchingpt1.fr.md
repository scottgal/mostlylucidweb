# Recherche de texte complet (Pt 1)

<!--category-- Postgres, Entity Framework -->
<datetime class="hidden">2024-08-20T12:40</datetime>

# Présentation

La recherche de contenu est une partie critique de tout contenu site web lourd. Il améliore la découverte et l'expérience utilisateur. Dans ce post je vais couvrir comment j'ai ajouté le texte complet à la recherche de ce site

Les prochaines parties de cette série:

- [Boîte de recherche avec Postgres](/blog/textsearchingpt11)
- [Introduction à OpenSearch](/blog/textsearchingpt2)
- [Ouvrir la recherche avec C#](/blog/textsearchingpt3)

[TOC]

# Approches

Il y a un certain nombre de façons de faire la recherche de texte complet, y compris

1. Il suffit de rechercher une structure de données en mémoire (comme une liste), c'est relativement simple à implémenter, mais n'a pas de bonne échelle. En outre, il ne supporte pas les requêtes complexes sans beaucoup de travail.
2. Utilisation d'une base de données comme SQL Server ou Postgres. Bien que cela fonctionne et a le soutien de presque tous les types de base de données, ce n'est pas toujours la meilleure solution pour des structures de données plus complexes ou des requêtes complexes; cependant, c'est ce que cet article couvrira.
3. Utilisation d'une technologie de recherche légère comme [Lucene](https://lucenenet.apache.org/) ou SQLite FTS. Il s'agit d'un terrain d'entente entre les deux solutions ci-dessus. C'est plus complexe qu'une simple recherche de liste, mais moins complexe qu'une solution de base de données complète. Cependant, il est encore assez complexe à implémenter (surtout pour l'ingestion de données) et n'a pas d'échelle aussi bien qu'une solution de recherche complète. En vérité, beaucoup d'autres technologies de recherche [utiliser Lucene sous le capot pour ](https://www.elastic.co/search-labs/blog/elasticsearch-lucene-vector-database-gains) C'est des capacités de recherche vectorielle incroyables.
4. Utilisation d'un moteur de recherche comme ElasticSearch, OpenSearch ou Azure Search. C'est la solution la plus complexe et la plus intensive en ressources, mais aussi la plus puissante. C'est aussi le plus évolutif et peut gérer les requêtes complexes avec facilité. Je vais aller dans la profondeur excruciante dans la semaine prochaine ou ainsi sur la façon de s'auto-héberger, configurer et utiliser OpenSearch à partir de C#.

# Base de données Recherche texte complet avec Postgres

Dans ce blog, j'ai récemment déménagé à l'utilisation de Postgres pour ma base de données. Postgres a une fonction de recherche de texte complet qui est très puissant et (quelque peu) facile à utiliser. Il est également très rapide et peut gérer des requêtes complexes avec facilité.

Quand vous construisez `DbContext` vous pouvez spécifier quels champs ont une fonctionnalité de recherche texte complète activée.

Postgres utilise le concept de vecteurs de recherche pour réaliser une recherche texte complet rapide et efficace. Un vecteur de recherche est une structure de données qui contient les mots dans un document et leurs positions. Essentiellement, précalculer le vecteur de recherche pour chaque ligne de la base de données permet à Postgres de rechercher très rapidement des mots dans le document.
Il utilise deux types de données spécifiques pour atteindre cet objectif:

- TSVector: Un type de données spécial PostgreSQL qui stocke une liste de lexemes (pensez-en comme vecteur de mots). Il s'agit de la version indexée du document utilisé pour la recherche rapide.
- TSQuery: Un autre type de données spécial qui stocke la requête de recherche, qui inclut les termes de recherche et les opérateurs logiques (comme ET, OU, NON).

En outre, il offre une fonction de classement qui vous permet de classer les résultats en fonction de la manière dont ils correspondent à la requête de recherche. C'est très puissant et vous permet de commander les résultats par pertinence.
PostgreSQLQ attribue un classement aux résultats en fonction de la pertinence. La pertinence est calculée en tenant compte de facteurs tels que la proximité des termes de recherche les uns avec les autres et la fréquence à laquelle ils apparaissent dans le document.
Les fonctions ts_rank ou ts_rank_cd sont utilisées pour calculer ce classement.

Vous pouvez en savoir plus sur les fonctionnalités de recherche de texte complet de Postgres [Ici.](https://www.postgresql.org/docs/current/textsearch.html)

## Cadre des entités

Le paquet «Cadre d'entités Postgres» [Ici.](https://www.npgsql.org/efcore/mapping/full-text-search.html?tabs=pg12%2Cv5) fournit un support puissant pour la recherche de texte complet. Il vous permet de spécifier quels champs sont indexés en texte complet et comment les interroger.

Pour ce faire, nous ajoutons des types d'index spécifiques à nos Entités telles que définies dans `DbContext`:

```csharp
   modelBuilder.Entity<BlogPostEntity>(entity =>
        {
            entity.HasIndex(x => new { x.Slug, x.LanguageId });
            entity.HasIndex(x => x.ContentHash).IsUnique();
            entity.HasIndex(x => x.PublishedDate);

                entity.HasIndex(b => new { b.Title, b.PlainTextContent})
                .HasMethod("GIN")
                .IsTsVectorExpressionIndex("english");
  ...
```

Ici, nous ajoutons un index texte complet à la `Title` et `PlainTextContent` les champs de notre `BlogPostEntity`C'est ce que j'ai dit. Nous spécifions également que l'index devrait utiliser le `GIN` le type d'indice et le `english` langue. Ceci est important car il indique à Postgres comment indexer les données et quelle langue utiliser pour bloquer et arrêter les mots.

C'est évidemment un problème pour notre blog car nous avons plusieurs langues. Malheureusement pour l'instant, j'utilise juste le `english` langue pour tous les postes. C'est quelque chose que je vais devoir aborder à l'avenir, mais pour l'instant cela fonctionne assez bien.

Nous ajoutons également un index à notre `Category` entité:

```csharp
     modelBuilder.Entity<CategoryEntity>(entity =>
        {
            entity.HasIndex(b => b.Name).HasMethod("GIN").IsTsVectorExpressionIndex("english");;
...

```

En faisant cela, Postgres génère un vecteur de recherche pour chaque ligne de la base de données. Ce vecteur contient les mots `Title` et `PlainTextContent` les champs. Nous pouvons ensuite utiliser ce vecteur pour rechercher des mots dans le document.

Cela se traduit par une fonction to_tsvector dans SQL qui génère le vecteur de recherche pour la ligne. Nous pouvons ensuite utiliser la fonction ts_rank pour classer les résultats en fonction de la pertinence.

```postgresql
SELECT to_tsvector('english', 'a fat  cat sat on a mat - it ate a fat rats');
to_tsvector
-----------------------------------------------------
'ate':9 'cat':3 'fat':2,11 'mat':7 'rat':12 'sat':4
```

Appliquez ceci comme une migration à notre base de données et nous sommes prêts à commencer la recherche.

# Recherche

## Indice TsVector

Pour la recherche que nous utilisons utilisera le `EF.Functions.ToTsVector` et `EF.Functions.WebSearchToTsQuery` fonctions pour créer un vecteur de recherche et de requête. Nous pouvons alors utiliser le `Matches` fonction pour rechercher la requête dans le vecteur de recherche.

```csharp
  var posts = await context.BlogPosts
            .Include(x => x.Categories)
            .Include(x => x.LanguageEntity)
            .Where(x =>
                EF.Functions.ToTsVector("english", x.Title + " " + x.PlainTextContent)
                    .Matches(EF.Functions.WebSearchToTsQuery("english", query)) // Search in title and content
                && x.Categories.Any(c =>
                    EF.Functions.ToTsVector("english", c.Name)
                        .Matches(EF.Functions.WebSearchToTsQuery("english", query))) // Search in categories
                && x.LanguageEntity.Name == "en") // Filter by language
            .OrderByDescending(x =>
                EF.Functions.ToTsVector("english", x.Title + " " + x.PlainTextContent)
                    .Rank(EF.Functions.WebSearchToTsQuery("english", query))) // Rank by relevance
            .Select(x => new { x.Title, x.Slug })
            .ToListAsync();
       
```

La fonction EF.Functions.WebSearchToTsQuery génère la requête pour la ligne basée sur la syntaxe commune du moteur de recherche Web.

```postgresql
SELECT websearch_to_tsquery('english', '"sad cat" or "fat rat"');
       websearch_to_tsquery
-----------------------------------
 'sad' <-> 'cat' | 'fat' <-> 'rat'
```

Dans cet exemple, vous pouvez voir que cela génère une requête qui recherche les mots "sad cat" ou "fat rat" dans le document. C'est une fonctionnalité puissante qui nous permet de rechercher facilement des requêtes complexes.

Comme indiqué befpre ces méthodes génèrent à la fois le vecteur de recherche et la requête pour la ligne. Nous utilisons ensuite les `Matches` fonction pour rechercher la requête dans le vecteur de recherche. Nous pouvons également utiliser le `Rank` fonction de classer les résultats par pertinence.

Comme vous pouvez le voir, ce n'est pas une simple requête, mais c'est très puissant et nous permet de rechercher des mots dans le `Title`, `PlainTextContent` et `Category` les champs de notre `BlogPostEntity` et les classer par pertinence.

## WebAPI

Pour les utiliser (à l'avenir), nous pouvons créer un simple paramètre WebAPI qui prend une requête et renvoie les résultats. Il s'agit d'un contrôleur simple qui prend une requête et renvoie les résultats :

```csharp
[ApiController]
[Route("api/[controller]")]
public class SearchApi(IMostlylucidDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<JsonHttpResult<List<SearchResults>>> Search(string query)
    {;

        var posts = await context.BlogPosts
            .Include(x => x.Categories)
            .Include(x => x.LanguageEntity)
            .Where(x =>
                EF.Functions.ToTsVector("english", x.Title + " " + x.PlainTextContent)
                    .Matches(EF.Functions.WebSearchToTsQuery("english", query)) // Search in title and content
                && x.Categories.Any(c =>
                    EF.Functions.ToTsVector("english", c.Name)
                        .Matches(EF.Functions.WebSearchToTsQuery("english", query))) // Search in categories
                && x.LanguageEntity.Name == "en") // Filter by language
            .OrderByDescending(x =>
                EF.Functions.ToTsVector("english", x.Title + " " + x.PlainTextContent)
                    .Rank(EF.Functions.WebSearchToTsQuery("english", query))) // Rank by relevance
            .Select(x => new { x.Title, x.Slug })
            .ToListAsync();
        
        var output = posts.Select(x => new SearchResults(x.Title.Trim(), x.Slug)).ToList();
        
        return TypedResults.Json(output);
    }

```

## Colonne produite et typeAhead

Une autre approche pour utiliser ces indices TsVector'simple' est d'utiliser une colonne générée pour stocker le Vecteur de recherche et ensuite utiliser ceci pour rechercher. Il s'agit d'une approche plus complexe, mais qui permet une meilleure performance.
Ici, nous modifions notre `BlogPostEntity` pour ajouter un type spécial de colonne:

```csharp
   [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public NpgsqlTsVector SearchVector { get; set; }
```

Il s'agit d'une colonne calculée qui génère le vecteur de recherche pour la ligne. Nous pouvons ensuite utiliser cette colonne pour rechercher des mots dans le document.

Nous avons ensuite configuré cet index à l'intérieur de notre définition d'entité (encore pour confirmer mais cela peut également nous permettre d'avoir plusieurs langues en spécifiant une colonne de langue pour chaque message).

```csharp
   entity.Property(b => b.SearchVector)
                .HasComputedColumnSql("to_tsvector('english', coalesce(\"Title\", '') || ' ' || coalesce(\"PlainTextContent\", ''))", stored: true);
```

Vous verrez ici que nous utilisons `HasComputedColumnSql` pour spécifier explicitement la fonction PostGreSQLTM pour générer le vecteur de recherche. Nous précisons également que la colonne est stockée dans la base de données. Ceci est important car il demande à Postgres de stocker le vecteur de recherche dans la base de données. Cela nous permet de rechercher des mots dans le document en utilisant le vecteur de recherche.

Dans la base de données, ceci a été généré pour chaque ligne, qui sont les 'lexèmes' dans le document et leurs positions:

```csharp
"'1992':464 '1996':468 '20':480 '200':115 '2007':426 '2009':428 '2012':88 '2015':397 '2018':370 '2020':372 '2021':288,327,329,399 '2022':196,243,245,290 '2024':156,158,198 '25':21,477,486,522 '3d':346 '6':203,256 '8':179,485 '90':120,566 'ab':282 'access':221 'accomplish':14 'achiev':118 'across':60 'adapt':579 'advanc':134 'applic':168,316,526 'apr':155,197 'architect':83,97,159 'architectur':307,337 ...
```

### RechercheAPI

Nous pouvons ensuite utiliser cette colonne pour rechercher des mots dans le document. Nous pouvons utiliser le `Matches` fonction pour rechercher la requête dans le vecteur de recherche. Nous pouvons également utiliser le `Rank` fonction de classer les résultats par pertinence.

```csharp
       var posts = await context.BlogPosts
            .Include(x => x.Categories)
            .Include(x => x.LanguageEntity)
            .Where(x =>
                // Search using the precomputed SearchVector
                x.SearchVector.Matches(EF.Functions.ToTsQuery("english", query + ":*")) // Use precomputed SearchVector for title and content
                && x.Categories.Any(c =>
                    EF.Functions.ToTsVector("english", c.Name)
                        .Matches(EF.Functions.ToTsQuery("english", query + ":*"))) // Search in categories
                && x.LanguageEntity.Name == "en") // Filter by language
            .OrderByDescending(x =>
                // Rank based on the precomputed SearchVector
                x.SearchVector.Rank(EF.Functions.ToTsQuery("english", query + ":*"))) // Use precomputed SearchVector for ranking
            .Select(x => new { x.Title, x.Slug })
            .ToListAsync();
```

Vous voyez ici que nous utilisons également un constructeur de requêtes différent `EF.Functions.ToTsQuery("english", query + ":*")`  qui nous permet d'offrir une fonctionnalité de type TypeAhead (où nous pouvons taper par exemple. 'chat' et obtenir 'chat', 'chats', 'caterpillar' etc).

En outre, il nous permet de simplifier la requête principale de blog post pour juste rechercher la requête dans le `SearchVector` colonne. Il s'agit d'une caractéristique puissante qui nous permet de rechercher des mots dans le `Title`, `PlainTextContent`C'est ce que j'ai dit. Nous utilisons toujours l'indice que nous avons montré ci-dessus pour `CategoryEntity`.

```csharp
x.Categories.Any(c =>
                    EF.Functions.ToTsVector("english", c.Name)
                        .Matches(EF.Functions.ToTsQuery("english", query + ":*"))) 
```

Nous utilisons ensuite les `Rank` fonction de classer les résultats par pertinence en fonction de la requête.

```csharp
 x.SearchVector.Rank(EF.Functions.ToTsQuery("english", query + ":*")))
```

Cela nous permet d'utiliser le paramètre comme suit, où nous pouvons passer dans les premières lettres d'un mot et récupérer tous les messages qui correspondent à ce mot:

Vous pouvez voir la [API en action ici](https://www.mostlylucid.net/swagger/index.html) chercher les `/api/SearchApi`C'est ce que j'ai dit. (Note; J'ai activé Swagger pour ce site afin que vous puissiez voir l'API en action, mais la plupart du temps cela devrait être réservé pour `IsDevelopment()).

![API](searchapi.png?width=900&format=webp&quality=50)

À l'avenir, j'ajouterai une fonctionnalité TypeAhead à la boîte de recherche sur le site qui utilise cette fonctionnalité.

# En conclusion

Vous pouvez voir qu'il est possible d'obtenir une fonctionnalité de recherche puissante en utilisant Postgres et Entity Framework. Cependant, il a des complexités et des limites dont nous devons tenir compte (comme le truc de la langue). Dans la partie suivante, je traiterai de la façon dont nous le faisons en utilisant OpenSearch - qui a une tonne plus de configuration, mais qui est plus puissant et évolutive.