# Búsqueda de texto completo (Pt 3 - OpenSearch con ASP.NET Core)

<!--category-- OpenSearch, ASP.NET -->
<datetime class="hidden">2024-08-24T06:40</datetime>

## Introducción

En las partes anteriores de esta serie introdujimos el concepto de búsqueda de texto completo y cómo se puede utilizar para buscar texto dentro de una base de datos. En esta parte vamos a introducir cómo utilizar OpenSearch con ASP.NET Core.

Partes anteriores:

- [Búsqueda de texto completo con Postgres](/blog/textsearchingpt1)
- [Buzón de búsqueda con Postgres](/blog/textsearchingpt11)
- [Introducción a OpenSearch](/blog/textsearchingpt3)

En esta parte cubriremos cómo empezar a usar la nueva instancia brillante de OpenSearch con ASP.NET Core.

[TOC]

## Configuración

Una vez que tengamos la instancia de OpenSearch en funcionamiento podemos empezar a interactuar con ella. Estaremos usando el [Cliente OpenSearch](https://opensearch.org/docs/latest/clients/OSC-dot-net/) para.NET.
Primero configuramos el cliente en nuestra extensión de configuración

```csharp
    var openSearchConfig = services.ConfigurePOCO<OpenSearchConfig>(configuration.GetSection(OpenSearchConfig.Section));
        var config = new ConnectionSettings(new Uri(openSearchConfig.Endpoint))
            .EnableHttpCompression()
            .EnableDebugMode()
            .ServerCertificateValidationCallback((sender, certificate, chain, errors) => true)
            .BasicAuthentication(openSearchConfig.Username, openSearchConfig.Password);
        services.AddSingleton<OpenSearchClient>(c => new OpenSearchClient(config));
```

Esto establece el cliente con el punto final y las credenciales. También habilitamos el modo de depuración para que podamos ver lo que está pasando. Además, como no estamos usando certificados SSL REAL desactivamos la validación de certificados (no lo hagas en producción).

## Datos de indización

El concepto central en OpenSearch es el Índice. Piense en un índice como una tabla de base de datos; es donde se almacenan todos sus datos.

Para hacer esto usaremos el [Cliente OpenSearch](https://opensearch.org/docs/latest/clients/OSC-dot-net/) para.NET. Puede instalar esto a través de NuGet:

Usted notará que hay dos allí - Opensearch.Net y Opensearch.Client. La primera es la materia de bajo nivel como la gestión de la conexión, la segunda es la materia de alto nivel como la indexación y la búsqueda.

Ahora que lo tenemos instalado podemos empezar a buscar datos de indexación.

Crear un índice es semi directo hacia adelante. Sólo tienes que definir cómo debe ser tu índice y luego crearlo.
En el siguiente código se puede ver'mapear' nuestro Modelo de Índice (una versión simplificada del modelo de base de datos del blog).
Para cada campo de este modelo definimos qué tipo es (texto, fecha, palabra clave, etc) y qué analizador usar.

El Tipo es importante ya que define cómo se almacenan los datos y cómo se pueden buscar. Por ejemplo, se analiza y muestra un campo de 'texto', un campo de 'palabra clave' no lo es. Así que esperaría buscar un campo de palabras clave exactamente como se almacena, pero un campo de texto puede buscar partes del texto.

También aquí Categorías es en realidad una cadena[] pero el tipo de palabra clave entiende cómo manejarlos correctamente.

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

## Añadir elementos al índice

Una vez que hayamos configurado nuestro índice para añadir elementos a él, necesitamos añadir elementos a este índice. Aquí como estamos añadiendo un BUNCH utilizamos un método de inserción a granel.

Se puede ver que inicialmente llamamos a un método llamado`GetExistingPosts` que devuelve todos los mensajes que ya están en el índice. A continuación, agrupamos los mensajes por idioma y filtramos el lenguaje 'uk' (ya que no queremos indexar eso ya que necesita un plugin adicional que aún no tenemos). A continuación, filtramos los mensajes que ya están en el índice.
Utilizamos el hash y el id para identificar si un post ya está en el índice.

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

Una vez que hemos filtrado los posts existentes y nuestro analizador perdido creamos un nuevo Índice (basado en el nombre, en mi caso "la mayoría de lucid-blog-<language>") y luego crear una petición a granel. Esta solicitud masiva es una colección de operaciones para realizar en el índice.
Esto es más eficiente que agregar cada elemento uno por uno.

Ya lo verás en el `BulkRequest` establecemos el `Refresh` bienes a `true`. Esto significa que después de completar el inserto a granel se actualiza el índice. Esto no es REALMENTE necesario, pero es útil para la depuración.

## Buscando el índice

Una buena manera de probar para ver lo que realmente se ha creado aquí es ir a las Herramientas Dev en OpenSearch Dashboards y ejecutar una consulta de búsqueda.

```json
GET /mostlylucid-blog-*
{}
```

Esta consulta nos devolverá todos los índices que coincidan con el patrón `mostlylucid-blog-*`. (así que todos nuestros índices hasta ahora).

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

Herramientas Dev en OpenSearch Dashboards es una gran manera de probar sus consultas antes de ponerlas en su código.

![Herramientas Dev](devtools.png?width=900&quality=25)

## Buscando el índice

Ahora podemos empezar a buscar el índice. Podemos usar el `Search` método en el cliente para hacer esto.
Aquí es donde entra el verdadero poder de OpenSearch. Literalmente. [docenas de diferentes tipos de consultas](https://opensearch.org/docs/latest/query-dsl/) puede utilizar para buscar sus datos. Todo, desde una simple búsqueda de palabras clave hasta una compleja búsqueda 'neural'.

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

### Descripción de la consulta

Este método, `GetSearchResults`, está diseñado para consultar un índice específico de OpenSearch para recuperar publicaciones de blog. Se necesitan tres parámetros: `language`, `query`, y parámetros de paginación `page` y `pageSize`. Esto es lo que hace:

1. **Selección de índices**:
   
   - Se recupera el nombre del índice utilizando el `GetBlogIndexName` método basado en el idioma proporcionado. El índice se selecciona dinámicamente según el idioma.

2. **Consulta de búsqueda**:
   
   - La consulta utiliza una `Bool` consulta con una `Must` la cláusula para garantizar que los resultados se ajusten a determinados criterios.
   - Dentro de la `Must` cláusula, a `MultiMatch` consulta se utiliza para buscar en múltiples campos (`Title`, `Categories`, y `Content`).
     - **Impulsando**: El `Title` campo se le da un impulso de `2.0`, haciéndolo más importante en la búsqueda, y `Categories` tiene un impulso de `1.5`. Esto significa que los documentos donde la consulta de búsqueda aparece en el título o categorías se clasificarán más alto.
     - **Tipo de consulta**: Se utiliza `BestFields`, que intenta encontrar el mejor campo de coincidencia para la consulta.
     - **Fuzzness**: El `Fuzziness.Auto` el parámetro permite coincidencias aproximadas (por ejemplo, manejo de errores tipográficos menores).

3. **Paginación**:
   
   - Los `Skip` método omite el primero `n` resultados según el número de página, calculado como `(page - 1) * pageSize`. Esto ayuda a navegar a través de resultados paginados.
   - Los `Size` método limita el número de documentos devueltos al especificado `pageSize`.

4. **Manejo de errores**:
   
   - Si la consulta falla, se registra un error y se devuelve una lista vacía.

5. **Resultado**:
   
   - El método devuelve una lista de `BlogIndexModel` documentos que coincidan con los criterios de búsqueda.

Así que puedes ver que podemos ser súper flexibles sobre cómo buscamos nuestros datos. Podemos buscar campos específicos, podemos impulsar ciertos campos, incluso podemos buscar a través de múltiples índices.

Una gran ventaja es la facilidad qith que podemos soportar varios idiomas. Tenemos un índice diferente para cada idioma y habilitamos la búsqueda dentro de ese índice. Esto significa que podemos utilizar el analizador correcto para cada idioma y obtener los mejores resultados.

## La nueva API de búsqueda

En contraste con la API de búsqueda que vimos en las partes anteriores de esta serie, podemos simplificar enormemente el proceso de búsqueda utilizando OpenSearch. Podemos simplemente lanzar texto a esta consulta y obtener grandes resultados de vuelta.

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

Como pueden ver tenemos todos los datos que necesitamos en el índice para devolver los resultados. Entonces podemos utilizar esto para generar una URL a la entrada del blog. Esto quita la carga de nuestra base de datos y hace el proceso de búsqueda mucho más rápido.

## Conclusión

En este post vimos cómo escribir un cliente C# para interactuar con nuestra instancia de OpenSearch.