using Mostlylucid.Services.Umami;

namespace Mostlylucid.Service.Test.Umami;

public class PopularPostsService_SlugTests
{
    [Theory]
    [InlineData("/blog/my-post", "my-post")]
    [InlineData("/blog/my-post/", "my-post")]
    [InlineData("/blog/de/my-post", "my-post")]
    [InlineData("/blog/language/my-post/fr", "my-post")]
    [InlineData("/blog/my-post.fr", "my-post")]
    [InlineData("/blog/my-post?utm_source=x", "my-post")]
    [InlineData("/blog/383", "383")]
    public void Maps_Post_Paths_To_Slug(string path, string expected)
    {
        Assert.Equal(expected, PopularPostsService.GetPostSlug(path));
    }

    [Theory]
    [InlineData("/blog")]
    [InlineData("/blog/")]
    [InlineData("/blog/category/ASP.NET")]
    [InlineData("/blog/drafts/some-draft")]
    [InlineData("/blog/categories")]
    [InlineData("/")]
    [InlineData("/software")]
    [InlineData("")]
    public void Ignores_Non_Post_Paths(string path)
    {
        Assert.Null(PopularPostsService.GetPostSlug(path));
    }
}
