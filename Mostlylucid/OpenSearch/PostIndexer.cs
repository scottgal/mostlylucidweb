using Mostlylucid.OpenSearch.Models;

namespace Mostlylucid.OpenSearch;

public class PostIndexer(IBlogService blogService, IndexService indexService, ILogger<PostIndexer> logger)
{
    public async Task AddAllPostsToIndex()
    {
        var posts = await blogService.GetAllPosts();
        var blogIndexModels = posts.Select(p => new BlogIndexModel(p)).ToList();
        await indexService.AddPostsToIndex(blogIndexModels);
    }
}