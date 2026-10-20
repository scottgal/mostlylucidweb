using Mostlylucid.Models.Blog;
using Mostlylucid.Models.Error;

namespace Mostlylucid.Helpers;

public static class MissingPageSuggestions
{
    public static List<SuggestionWithScore> Create(IEnumerable<BlogPostViewModel> posts, string path)
    {
        var parts = path.Trim('/').Split('/');
        var requested = parts.LastOrDefault()?.ToLowerInvariant() ?? "";
        requested = requested.Length > 200 ? requested[..200] : requested;
        var language = parts.Length > 2 && parts[0] == "blog" ? parts[1] : "en";
        var available = posts.Where(post => post.Language == language).ToList();
        var ranked = available.Select(post => (Post: post, Score: Similarity(requested, post.Slug.ToLowerInvariant())))
            .Where(item => item.Score >= .45).OrderByDescending(item => item.Score)
            .ThenByDescending(item => item.Post.PublishedDate).Take(5).ToList();
        if (ranked.Count == 0)
            ranked = available.OrderByDescending(post => post.PublishedDate).Take(5).Select(post => (Post: post, Score: 0d)).ToList();
        return ranked.Select(item => new SuggestionWithScore
        {
            Score = item.Score,
            Post = new Mostlylucid.Shared.Models.Blog.PostListModel
            {
                Title = item.Post.Title, Slug = item.Post.Slug, Language = item.Post.Language,
                Categories = item.Post.Categories, PublishedDate = item.Post.PublishedDate, WordCount = item.Post.WordCount
            }
        }).ToList();
    }

    private static double Similarity(string source, string target)
    {
        if (source.Length == 0 || target.Length == 0) return 0;
        var row = Enumerable.Range(0, target.Length + 1).ToArray();
        for (var i = 1; i <= source.Length; i++)
        {
            var previous = row[0];
            row[0] = i;
            for (var j = 1; j <= target.Length; j++)
            {
                var old = row[j];
                row[j] = Math.Min(Math.Min(row[j] + 1, row[j - 1] + 1), previous + (source[i - 1] == target[j - 1] ? 0 : 1));
                previous = old;
            }
        }
        return 1d - (double)row[^1] / Math.Max(source.Length, target.Length);
    }
}
