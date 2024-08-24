# Full Text Searching (Pt 3 - OpenSearch with ASP.NET Core)
<!--category-- OpenSearch, ASP.NET -->
<datetime class="hidden">2024-08-24T06:40</datetime>

## Introduction
In the previous parts of this series we introduced the concept of full text searching and how it can be used to search for text within a database. In this part we will introduce how to use OpenSearch with ASP.NET Core.

Previous parts:
- [Full Text Searching with Postgres](/blog/textsearchingpt1)
- [Search Box with Postgres](/blog/textsearchingpt11)
- [Introduction to OpenSearch](/blog/textsearchingpt2)

In this part we'll cover how to start using you new shiny OpenSearch instance with ASP.NET Core.

[TOC]

## Setup
Once we have the OpenSearch instance up and running we can start to interact with it. We'll be using the [OpenSearch Client](https://opensearch.org/docs/latest/clients/OSC-dot-net/) for .NET. 
First we set up the client in our Setup extension

```csharp
    var openSearchConfig = services.ConfigurePOCO<OpenSearchConfig>(configuration.GetSection(OpenSearchConfig.Section));
        var config = new ConnectionSettings(new Uri(openSearchConfig.Endpoint))
            .EnableHttpCompression()
            .EnableDebugMode()
            .ServerCertificateValidationCallback((sender, certificate, chain, errors) => true)
            .BasicAuthentication(openSearchConfig.Username, openSearchConfig.Password);
        services.AddSingleton<OpenSearchClient>(c => new OpenSearchClient(config));
```

This sets up the client with the endpoint and credentials. We also enable debug mode so we can see what's going on. Further as we're not using REAL SSL certificates we disable certificate validation (don't do this in production). 

## Indexing Data
The core concept in OpenSearch is the Index. Think of an Index like a Database table; it's where all your data is stored.

To do this we'll use the [OpenSearch Client](https://opensearch.org/docs/latest/clients/OSC-dot-net/) for .NET. You can install this via NuGet:

You'll notice there's two there - Opensearch.Net and Opensearch.Client. The first is the low level stuff like connection management, the second is the high level stuff like indexing and searching.

Now that we have it installed we can start looking at indexing data.

Creating an index is semi-straightforward. You just define what your index should look like and then create it.
In the code below you can see we 'map' our Index Model (a simplified version of the blog's database model). 
For each field of this model we then define what type it is (text, date, keyword etc) and what analyser to use.

The Type is important as it defines how the data is stored and how it can be searched. For example, a 'text' field is analysed and tokenised, a 'keyword' field is not. So you'd expect to search for a keyword field exactly as it is stored, but a text field you can search for parts of the text.

Also here Categories is actually a string[] but the keyword type understands how to handle them correctly.

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
## Adding Items to the Index
Once we have our index set up to add items to it we need to add items to this index. Here as we're adding a BUNCH we use a bulk insert method.

You can see that we initially call into a method called`GetExistingPosts` which returns all the posts that are already in the index. We then group the posts by language and filter out the 'uk' language (as we don't want to index that as it needs an extra plugin we don't have yet). We then filter out any posts that are already in the index.
We use the hash and id to identify if a post is already in the index.

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
Once we've filtered out the existing posts and our missing analyzer we create a new Index (based on the name, in my case "mostlylucid-blog-<language>") and then create a bulk request. This bulk request is a collection of operations to perform on the index. 
This is more efficient than adding each item one by one.

You'll see that in the `BulkRequest` we set the `Refresh` property to `true`. This means that after the bulk insert is complete the index is refreshed. This isn't REALLY necessary but it's useful for debugging.

## Searching the Index
A good way to test to see what's actually been created here is to go into the Dev Tools on OpenSearch Dashboards and run a search query. 

```json
GET /mostlylucid-blog-*
{}
```
This query will return us all the indexes matching the pattern `mostlylucid-blog-*`. (so all our indexes so far). 

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

Dev Tools in OpenSearch Dashboards is a great way to test your queries before you put them into your code.

![Dev Tools](devtools.png?width=900&quality=25)

## Searching the Index
Now we can start searching the index. We can use the `Search` method on the client to do this. 
This is where the real power of OpenSearch comes in. It has literally [dozens of different types of query](https://opensearch.org/docs/latest/query-dsl/) you can use to search your data. Everything from a simple keyword search to a complex 'neural' search.

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

### Query Description

This method, `GetSearchResults`, is designed to query a specific OpenSearch index to retrieve blog posts. It takes three parameters: `language`, `query`, and pagination parameters `page` and `pageSize`. Here's what it does:

1. **Index Selection**:
    - It retrieves the index name using the `GetBlogIndexName` method based on the language provided. The index is dynamically selected according to the language.

2. **Search Query**:
    - The query uses a `Bool` query with a `Must` clause to ensure that results match certain criteria.
    - Inside the `Must` clause, a `MultiMatch` query is used to search across multiple fields (`Title`, `Categories`, and `Content`).
        - **Boosting**: The `Title` field is given a boost of `2.0`, making it more important in the search, and `Categories` has a boost of `1.5`. This means documents where the search query appears in the title or categories will rank higher.
        - **Query Type**: It uses `BestFields`, which attempts to find the best matching field for the query.
        - **Fuzziness**: The `Fuzziness.Auto` parameter allows for approximate matches (e.g., handling minor typos).

3. **Pagination**:
    - The `Skip` method skips the first `n` results depending on the page number, calculated as `(page - 1) * pageSize`. This helps in navigating through paginated results.
    - The `Size` method limits the number of documents returned to the specified `pageSize`.

4. **Error Handling**:
    - If the query fails, an error is logged and an empty list is returned.

5. **Result**:
    - The method returns a list of `BlogIndexModel` documents matching the search criteria.

So you can see we can be super flexible about how we search our data. We can search for specific fields, we can boost certain fields, we can even search across multiple indexes.

One BIG advantage is the ease qith which we can support multiple languages. We  have a different index for each language and enable searching within that index. This means we can use the correct analyser for each language and get the best results.

## The New Search API
In contrast with the Search API we saw in the previous parts of this series, we can vastly simplify the search process by using OpenSearch. We can just throw in text to this query and get great results back.

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
As you can see we have all the data we need  in the index to return the results. We can then use this to generate a URL to the blog post. This takes the load off our database and makes the search process much faster.

## In Conclusion
In this post we saw how to write a C# client to interact with our OpenSearch instance. 