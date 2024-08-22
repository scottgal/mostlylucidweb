# Búsqueda de texto completo (Pt 1)

<!--category-- Postgres, Entity Framework -->
<datetime class="hidden">2024-08-20T12:40</datetime>

# Introducción

La búsqueda de contenido es una parte crítica de cualquier sitio web de contenido pesado. Mejora la capacidad de descubrir y la experiencia del usuario. En este post voy a cubrir cómo he añadido texto completo en busca de este sitio

[TOC]

# Enfoques

Hay una serie de maneras de hacer búsqueda de texto completo incluyendo

1. Sólo la búsqueda de una estructura de datos de memoria (como una lista), esto es relativamente simple de implementar, pero no escala bien. Además, no admite consultas complejas sin mucho trabajo.
2. Usando una base de datos como SQL Server o Postgres. Aunque esto funciona y tiene soporte de casi todos los tipos de bases de datos, no siempre es la mejor solución para estructuras de datos más complejas o consultas complejas; sin embargo, es lo que cubrirá este artículo.
3. Usando una tecnología de búsqueda ligera como [Luceno](https://lucenenet.apache.org/) o SQLite FTS. Este es un punto medio entre las dos soluciones anteriores. Es más complejo que buscar una lista pero menos complejo que una solución de base de datos completa. Sin embargo, sigue siendo bastante complejo de implementar (especialmente para ingerir datos) y no escala tanto como una solución de búsqueda completa. En verdad muchas otras tecnologías de búsqueda [usar Lucene bajo el capó para ](https://www.elastic.co/search-labs/blog/elasticsearch-lucene-vector-database-gains) Es increíble capacidad de búsqueda de vectores.
4. Usando un motor de búsqueda como ElasticSearch, OpenSearch o Azure Search. Esta es la solución más compleja y intensiva en recursos, pero también la más poderosa. También es la más escalable y puede manejar consultas complejas con facilidad. Voy a entrar en una profundidad insoportable en la próxima semana o así sobre cómo auto-hosting, configurar y utilizar OpenSearch desde C#.

# Búsqueda de texto completo de base de datos con Postgres

En este blog me he mudado recientemente a usar Postgres para mi base de datos. Postgres tiene una función de búsqueda de texto completo que es muy potente y (algo) fácil de usar. También es muy rápido y puede manejar consultas complejas con facilidad.

Al construir yout `DbContext` puede especificar qué campos tienen activada la funcionalidad de búsqueda de texto completo.

Postgres utiliza el concepto de vectores de búsqueda para lograr una búsqueda de texto completa rápida y eficiente. Un vector de búsqueda es una estructura de datos que contiene las palabras en un documento y sus posiciones. Esencialmente precomputar el vector de búsqueda para cada fila en la base de datos permite a Postgres buscar palabras en el documento muy rápidamente.
Utiliza dos tipos de datos especiales para lograr esto:

- TSVector: Un tipo especial de datos PostgreSQL que almacena una lista de lexemes (pensarlo como un vector de palabras). Es la versión indexada del documento utilizado para la búsqueda rápida.
- TSQuery: Otro tipo de datos especiales que almacena la consulta de búsqueda, que incluye los términos de búsqueda y los operadores lógicos (como AND, OR, NOT).

Además, ofrece una función de clasificación que le permite clasificar los resultados en función de lo bien que coincidan con la consulta de búsqueda. Esto es muy potente y le permite ordenar los resultados por relevancia.
PostgreSQL asigna un ranking a los resultados basado en la relevancia. La relevancia se calcula considerando factores como la proximidad de los términos de búsqueda entre sí y la frecuencia con que aparecen en el documento.
Las funciones ts_rank o ts_rank_cd se utilizan para calcular este ranking.

Puede leer más sobre las características de búsqueda de texto completo de Postgres [aquí](https://www.postgresql.org/docs/current/textsearch.html)

## Marco de las entidades

El paquete marco de la entidad Postgres [aquí](https://www.npgsql.org/efcore/mapping/full-text-search.html?tabs=pg12%2Cv5) proporciona un potente soporte para la búsqueda de texto completo. Le permite especificar qué campos están indexados en texto completo y cómo consultarlos.

Para ello añadimos tipos de índice específicos a nuestras Entidades tal como se definen en `DbContext`:

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

Aquí estamos añadiendo un índice de texto completo a la `Title` y `PlainTextContent` campos de nuestra `BlogPostEntity`. También estamos especificando que el índice debe utilizar el `GIN` tipo de índice y el `english` lenguaje. Esto es importante ya que le dice a Postgres cómo indexar los datos y qué lenguaje usar para detener y detener las palabras.

Esto es obviamente un problema para nuestro blog ya que tenemos varios idiomas. Desafortunadamente por ahora sólo estoy usando el `english` lenguaje para todos los puestos. Esto es algo que tendré que abordar en el futuro, pero por ahora funciona lo suficientemente bien.

También añadimos un índice a nuestro `Category` entidad:

```csharp
     modelBuilder.Entity<CategoryEntity>(entity =>
        {
            entity.HasIndex(b => b.Name).HasMethod("GIN").IsTsVectorExpressionIndex("english");;
...

```

Al hacer esto Postgres genera un vector de búsqueda para cada fila en la base de datos. Este vector contiene las palabras en el `Title` y `PlainTextContent` campos. Entonces podemos utilizar este vector para buscar palabras en el documento.

Esto se traduce a una función to_tsvector en SQL que genera el vector de búsqueda para la fila. Entonces podemos utilizar la función ts_rank para clasificar los resultados basados en la relevancia.

```postgresql
SELECT to_tsvector('english', 'a fat  cat sat on a mat - it ate a fat rats');
to_tsvector
-----------------------------------------------------
'ate':9 'cat':3 'fat':2,11 'mat':7 'rat':12 'sat':4
```

Aplique esto como migración a nuestra base de datos y estamos listos para empezar a buscar.

# Búsqueda

## Índice TsVector

Para la búsqueda que utilizamos usaremos el `EF.Functions.ToTsVector` y `EF.Functions.WebSearchToTsQuery` funciones para crear un vector de búsqueda y consulta. A continuación, podemos utilizar el `Matches` función para buscar la consulta en el vector de búsqueda.

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

La función EF.Functions.WebSearchToTsQuery genera la consulta para la fila basada en la sintaxis común del motor de búsqueda web.

```postgresql
SELECT websearch_to_tsquery('english', '"sad cat" or "fat rat"');
       websearch_to_tsquery
-----------------------------------
 'sad' <-> 'cat' | 'fat' <-> 'rat'
```

En este ejemplo se puede ver que esto genera una consulta que busca las palabras "sad cat" o "fat rat" en el documento. Esta es una característica poderosa que nos permite buscar consultas complejas con facilidad.

Como se indica befpre estos métodos generan el vector de búsqueda y la consulta para la fila. A continuación, utilizar el `Matches` función para buscar la consulta en el vector de búsqueda. También podemos utilizar el `Rank` función para clasificar los resultados por relevancia.

Como se puede ver esto no es una simple consulta, pero es muy potente y nos permite buscar palabras en el `Title`, `PlainTextContent` y `Category` campos de nuestra `BlogPostEntity` y clasificarlos por relevancia.

## WebAPI

Para utilizar estos (en el futuro) podemos crear un endpoint WebAPI simple que toma una consulta y devuelve los resultados. Este es un controlador simple que toma una consulta y devuelve los resultados:

```csharp
[ApiController]
[Route("api/[controller]")]
public class SearchApi(MostlylucidDbContext context) : ControllerBase
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

## Columna generada y tipoAhead

Un enfoque alternativo para usar estos Índices TsVector'simple' es usar una columna generada para almacenar el vector de búsqueda y luego usar esto para buscar. Este es un enfoque más complejo, pero permite un mejor rendimiento.
Aquí modificamos nuestro `BlogPostEntity` para añadir un tipo especial de columna:

```csharp
   [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public NpgsqlTsVector SearchVector { get; set; }
```

Esta es una columna computada que genera el vector de búsqueda para la fila. A continuación, podemos utilizar esta columna para buscar palabras en el documento.

A continuación, configuramos este índice dentro de nuestra definición de entidad (todavía para confirmar, pero esto también puede permitirnos tener varios idiomas especificando una columna de idioma para cada mensaje).

```csharp
   entity.Property(b => b.SearchVector)
                .HasComputedColumnSql("to_tsvector('english', coalesce(\"Title\", '') || ' ' || coalesce(\"PlainTextContent\", ''))", stored: true);
```

Usted verá aquí que usamos `HasComputedColumnSql` especificar explícitamente la función PostGreSQL para generar el vector de búsqueda. También especificamos que la columna se almacena en la base de datos. Esto es importante ya que le dice a Postgres que almacene el vector de búsqueda en la base de datos. Esto nos permite buscar palabras en el documento usando el vector de búsqueda.

En la base de datos esto generó esto para cada fila, que son los 'lexemes' en el documento y sus posiciones:

```csharp
"'1992':464 '1996':468 '20':480 '200':115 '2007':426 '2009':428 '2012':88 '2015':397 '2018':370 '2020':372 '2021':288,327,329,399 '2022':196,243,245,290 '2024':156,158,198 '25':21,477,486,522 '3d':346 '6':203,256 '8':179,485 '90':120,566 'ab':282 'access':221 'accomplish':14 'achiev':118 'across':60 'adapt':579 'advanc':134 'applic':168,316,526 'apr':155,197 'architect':83,97,159 'architectur':307,337 ...
```

### SearchAPI

A continuación, podemos utilizar esta columna para buscar palabras en el documento. Podemos usar el `Matches` función para buscar la consulta en el vector de búsqueda. También podemos utilizar el `Rank` función para clasificar los resultados por relevancia.

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

Veo aquí que también usamos un constructor de consulta diferente. `EF.Functions.ToTsQuery("english", query + ":*")`  que nos permite ofrecer una funcionalidad de tipo TypeAhead (donde podemos escribir e.g. 'gato' y obtener 'gato', 'gatos', 'caterpillar' etc).

Además, nos permite simplificar la consulta principal post blog sólo para buscar la consulta en el `SearchVector` columna. Esta es una característica poderosa que nos permite buscar palabras en el `Title`, `PlainTextContent`. Todavía usamos el índice que mostramos arriba para el `CategoryEntity`.

```csharp
x.Categories.Any(c =>
                    EF.Functions.ToTsVector("english", c.Name)
                        .Matches(EF.Functions.ToTsQuery("english", query + ":*"))) 
```

A continuación, utilizar el `Rank` función para clasificar los resultados por relevancia en función de la consulta.

```csharp
 x.SearchVector.Rank(EF.Functions.ToTsQuery("english", query + ":*")))
```

Esto nos permite usar el punto final como sigue, donde podemos pasar en las primeras letras de una palabra y recuperar todos los mensajes que coincidan con esa palabra:

Usted puede ver el [API en acción aquí](https://www.mostlylucid.net/swagger/index.html) buscar el `/api/SearchApi`. (Nota; He activado Swagger para este sitio para que pueda ver la API en acción, pero la mayoría de las veces esto debe reservarse para `IsDevelopment()).

![API](searchapi.png?width=900&format=webp&quality=50)

En el futuro añadiré una función TypeAhead al cuadro de búsqueda en el sitio que utiliza esta funcionalidad.

# Conclusión

Puedes ver que es posible obtener potente funcionalidad de búsqueda usando Postgres y Entity Framework. Sin embargo, tiene complejidades y limitaciones que necesitamos tener en cuenta (como la cosa del lenguaje). En la siguiente parte voy a cubrir cómo haríamos esto usando OpenSearch - que es tiene un montón más de configuración, pero es más potente y escalable.