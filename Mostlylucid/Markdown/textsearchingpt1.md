# Full Text Searching (Pt 1)
<!--category-- Postgres, Entity Framework -->
<datetime class="hidden">2024-08-20T12:40</datetime>


# Introduction
Searching for content is a critical part of any content heavy website. It enhances discoverability and user experience. In this post I'll cover how I added full text searching for this site

[TOC]

# Approaches
There's a number of ways to do full text searching including
1. Just Searching an in memory data structure (like a list), this is relatively simple to implement but doesn't scale well. Additionally it doesn't support complex queries without a lot of work.
2. Using a database like SQL Server or Postgres. While this does work and has support from almost all database types it's not always the best solution for more complex data structures or complex queries; however it's what this article will cover.
3. Using a lightweight Search technology like Lucene or SQLite FTS. This is a middle ground between the two above solutions. It's more complex than just searching a list but less complex than a full database solution. However it's still pretty complex to implement (expecially for ingesting data) and doesn't scale as well as a full search solution
4Using a search engine like ElasticSearch, OpenSearch or Azure Search. This is the most complex solution but also the most powerful. It's also the most scalable and can handle complex queries with ease.

# Database Full Text Searching
In this blog I've recently moved to using PostgreSQL for my database. Postgres has a full text search feature that is very powerful and (somewhat) easy to use. It's also very fast and can handle complex queries with ease.

When building yout `DbContext` you can specify which fields have full text search functionality enabled. 

Postgres uses the concept of search vectors to achieve fast, efficient Full Text Searching. A search vector is a data structure that contains the words in a document and their positions. This allows Postgres to quickly search for words in a document and return the results.
It uses two special data types to achieve this:

- TSVector: A special PostgreSQL data type that stores a list of lexemes (think of it as a vector of words). It is the indexed version of the document used for fast searching.
- TSQuery: Another special data type that stores the search query, which includes the search terms and logical operators (like AND, OR, NOT).

Additionally it offers a ranking function that allows you to rank the results based on how well they match the search query. This is very powerful and allows you to order the results by relevance.
PostgreSQL assigns a ranking to the results based on relevance. Relevance is calculated by considering factors such as the proximity of the search terms to each other and how often they appear in the document.
The ts_rank or ts_rank_cd functions are used to compute this ranking.

You can read more about the full text search features of Postgres [here](https://www.postgresql.org/docs/current/textsearch.html)

## Entity Framework
The Postgres Entity Framework package [here](https://www.npgsql.org/efcore/mapping/full-text-search.html?tabs=pg12%2Cv5) provides powerful support for full text searching. It allows you to specify which fields are full text indexed and how to query them.

To do this we add specific index types to our Entities as defined in `DbContext`:

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
Here we're adding a full text index to the `Title` and `PlainTextContent` fields of our `BlogPostEntity`. We're also specifying that the index should use the `GIN` index type and the `english` language. This is important as it tells Postgres how to index the data and what language to use for stemming and stop words.

This is obviously an issue for our blog as we have multiple languages. Unfortunately for now I'm just using the `english` language for all posts. This is something I'll need to address in the future but for now it works well enough.

We also add an index to our `Category` entity:

```csharp
     modelBuilder.Entity<CategoryEntity>(entity =>
        {
            entity.HasIndex(b => b.Name).HasMethod("GIN").IsTsVectorExpressionIndex("english");;
...

```

By doing this Postgres generates a search vector for each row in the database. This vector contains the words in the `Title` and `PlainTextContent` fields. We can then use this vector to search for words in the document.

This translates to a to_tsvector function in SQL that generates the search vector for the row. We can then use the ts_rank function to rank the results based on relevance.

```postgresql
SELECT to_tsvector('english', 'a fat  cat sat on a mat - it ate a fat rats');
to_tsvector
-----------------------------------------------------
'ate':9 'cat':3 'fat':2,11 'mat':7 'rat':12 'sat':4
```

Apply this as a migration to our database and we're ready to start searching.

# Searching
To search we use will use the `EF.Functions.ToTsVector` and `EF.Functions.WebSearchToTsQuery` functions to create a search vector and query. We can then use the `Matches` function to search for the query in the search vector.

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
The EF.Functions.WebSearchToTsQuery function generates the query for the row based on common Web Search engine syntax. 
```postgresql
SELECT websearch_to_tsquery('english', '"sad cat" or "fat rat"');
       websearch_to_tsquery
-----------------------------------
 'sad' <-> 'cat' | 'fat' <-> 'rat'
```

In this example you can see that this generates a query that searches for the words "sad cat" or "fat rat" in the document. This is a powerful feature that allows us to search for complex queries with ease.

As stated befpre these methods both generate the search vector and query for the row. We then use the `Matches` function to search for the query in the search vector. We can also use the `Rank` function to rank the results by relevance.

As you can see this isn't a simple query but it's very powerful and allows us to search for words in the `Title`, `PlainTextContent` and `Category` fields of our `BlogPostEntity` and rank these by relevance.

## WebAPI
To use these (in future) we can create a simple WebAPI endpoint that takes a query and returns the results. This is a simple controller that takes a query and returns the results:

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

# In Conclusion
You can see that it's possible to get powerful search functionality using Postgres and Entity Framework. However it has complexities and limitations we need to account for (like the language thing). In the next part I'll cover how we'd do this using OpenSearch - which is has a ton more setup but is more powerful and scalable.