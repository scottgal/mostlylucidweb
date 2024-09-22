using Mostlylucid.OpenSearch.Models;
using Mostlylucid.Services.Blog;
using Mostlylucid.Shared.Models;

namespace Mostlylucid.OpenSearch;

public class PostIndexer(IBlogService blogService, IndexService indexService, ILogger<PostIndexer> logger)
{
    public async Task AddAllPostsToIndex()
    {
        var posts = await blogService.Get(new PostListQueryModel());
        if (posts == null) throw new ArgumentNullException(nameof(posts));
        var blogIndexModels = posts.Data.Select(p => new BlogIndexModel(p)).ToList();
        await indexService.AddPostsToIndex(blogIndexModels);
    }
}