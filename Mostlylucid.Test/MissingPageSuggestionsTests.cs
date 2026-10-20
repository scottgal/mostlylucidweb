using Mostlylucid.Helpers;
using Mostlylucid.Models.Blog;

namespace Mostlylucid.Test;

public class MissingPageSuggestionsTests
{
    private static readonly BlogPostViewModel[] Posts =
    [
        new() { Slug = "bot-detection", Title = "Bot detection", Language = "en", PublishedDate = new(2026, 9, 1) },
        new() { Slug = "latest-project", Title = "Latest project", Language = "en", PublishedDate = new(2026, 10, 1) },
        new() { Slug = "bot-detection-fr", Title = "Bots (French)", Language = "fr", PublishedDate = new(2026, 10, 2) }
    ];

    [Fact]
    public void TypoSuggestsExistingArticleInRequestedLanguage()
    {
        var result = MissingPageSuggestions.Create(Posts, "/blog/bot-detecton");
        Assert.Equal("bot-detection", result[0].Post.Slug);
        Assert.True(result[0].Score > .8);
        Assert.All(result, suggestion => Assert.Equal("en", suggestion.Post.Language));
    }

    [Fact]
    public void UnknownPathSuggestsLatestArticlesInDateOrder()
    {
        var result = MissingPageSuggestions.Create(Posts, "/qzxqzxqzxqzx");
        Assert.Equal(new[] { "latest-project", "bot-detection" }, result.Select(suggestion => suggestion.Post.Slug));
        Assert.All(result, suggestion => Assert.Equal(0, suggestion.Score));
    }
}
