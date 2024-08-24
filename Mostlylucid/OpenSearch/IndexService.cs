using Mostlylucid.Helpers;
using Mostlylucid.Models.Blog;
using Mostlylucid.OpenSearch.Models;
using OpenSearch.Client;
using OpenSearch.Net;

namespace Mostlylucid.OpenSearch;

public class IndexService(OpenSearchClient client, ILogger<IndexService> logger) : BaseService
{


    public record PostIndexModel(string Id, string Hash);

    private async Task<List<PostIndexModel>> GetExistingPosts()
    {
        var scrollTimeout = "2m"; // Keep the scroll context alive for 2 minutes
        var scrollSize = 1000; // Retrieve 1000 documents per scroll request
        var allPosts = new List<PostIndexModel>();

        var initialResponse = await client.SearchAsync<PostIndexModel>(s => s
                .Index("mostlylucid-blog-*")
                .Scroll(scrollTimeout)
                .Size(scrollSize)
                .Source(src => src.Includes(f => f.Fields("id", "hash"))) // Fetch only 'id' and 'hash'
        );

        if (!initialResponse.IsValid || !initialResponse.Documents.Any())
        {
            return allPosts; // No documents found, return empty list
        }

        allPosts.AddRange(initialResponse.Documents);

        var scrollId = initialResponse.ScrollId;

        while (!string.IsNullOrEmpty(scrollId))
        {
            var scrollResponse = await client.ScrollAsync<PostIndexModel>(scrollTimeout, scrollId);

            if (!scrollResponse.IsValid || !scrollResponse.Documents.Any())
            {
                break; // No more documents to scroll through
            }

            allPosts.AddRange(scrollResponse.Documents);
            scrollId = scrollResponse.ScrollId;
        }

        // Clear the scroll context to free up resources
        await client.ClearScrollAsync(new ClearScrollRequest(scrollId));

        return allPosts;
    }

    public async Task<List<BlogIndexModel>> GetPosts(string language, int page = 1, int pageSize = 10)
    {
        var posts = await client.SearchAsync<BlogIndexModel>(s => s
            .Index(GetBlogIndexName(language))
            .From((page - 1) * pageSize)
            .Size(pageSize)
            .Sort(sort => sort.Descending(p => p.Published))
        );
        return posts.Documents.ToList();
    }
    

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
    
    public async Task<bool> IndexExists(string language)
    {
        var indexName = GetBlogIndexName(language);
        
        var response = await client.Indices.ExistsAsync(indexName);
        return response.Exists;
    }
    
    public async Task AddPostToIndex(BlogIndexModel post)
    {
    
        var indexName = GetBlogIndexName(post.Language);
        
        var postId = post.Content.ContentHash();
        var response = await client.IndexAsync(post, i => i
            .Index(indexName)
            .Id(postId)
        );
        
        if (!response.IsValid)
        {
            logger.LogError("Failed to add post {PostId} to index {IndexName}: {Error}", postId, indexName, response.DebugInformation);
        }
    }
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
    
    
}