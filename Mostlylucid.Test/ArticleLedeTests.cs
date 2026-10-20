using Mostlylucid.Mapper;
using Mostlylucid.Shared.Models;

namespace Mostlylucid.Test;

public class ArticleLedeTests
{
    [Fact]
    public void BothListMappers_PreserveTheOpeningProse()
    {
        var post = new BlogPostDto { PlainTextContent = "Introduction\r\n\r\nThis is the opening paragraph.\nThe second sentence explains why it matters." };
        var fileResult = post.ToPostListModel();
        var databaseResult = post.ToListModel();
        Assert.Equal("This is the opening paragraph. The second sentence explains why it matters.", fileResult.Summary);
        Assert.Equal(fileResult.Summary, databaseResult.Summary);
    }

    [Fact]
    public void LongLede_IsTruncatedAtAWordBoundary()
    {
        var post = new BlogPostDto { PlainTextContent = string.Join(" ", Enumerable.Repeat("engineering", 60)) };
        var lede = post.ToPostListModel().Summary;
        Assert.EndsWith("engineering…", lede);
        Assert.True(lede.Length <= 261);
    }

    [Fact]
    public void MissingProse_DoesNotInventAnEllipsis()
    {
        Assert.Empty(new BlogPostDto().ToPostListModel().Summary);
        Assert.Empty(new BlogPostDto().ToListModel().Summary);
    }
}
