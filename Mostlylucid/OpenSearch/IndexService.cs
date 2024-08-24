using Mostlylucid.Helpers;
using Mostlylucid.OpenSearch.Models;
using OpenSearch.Client;
using OpenSearch.Net;

namespace Mostlylucid.OpenSearch;

public class IndexService(OpenSearchClient client, ILogger<IndexService> logger)
{
    private string GetBlogIndexName(string language) => $"mostlylucid-blog-{language}";
    
    
    public async Task AddPostsToIndex(IEnumerable<BlogIndexModel> posts)
    {
        var langPosts = posts.GroupBy(p => p.Language);
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
            
        }
    }
    
    public async Task<bool> IndexExists(string language)
    {
        var languageName = language.ConvertCodeToLanguage();
        var indexName = GetBlogIndexName(languageName);
        
        var response = await client.Indices.ExistsAsync(indexName);
        return response.Exists;
    }
    
    public async Task AddPostToIndex(BlogIndexModel post)
    {
        var languageName = post.Language.ConvertCodeToLanguage();
        var indexName = GetBlogIndexName(languageName);
        
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
        var languageName = language.ConvertCodeToLanguage();
        var indexName = GetBlogIndexName(languageName);

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