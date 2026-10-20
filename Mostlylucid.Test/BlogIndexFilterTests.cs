using Moq;
using Mostlylucid.Blog.ViewServices;
using Mostlylucid.Models.Blog;
using Mostlylucid.Services.Blog;
using Mostlylucid.Shared.Models;

namespace Mostlylucid.Test;

public class BlogIndexFilterTests
{
    [Fact]
    public async Task GetIndexDataAsync_CombinesCategoryDatesLanguageAndSortBeforePaging()
    {
        var start = new DateTime(2026, 1, 1);
        var end = new DateTime(2026, 10, 19);
        var service = new Mock<IBlogService>();
        PostListQueryModel? captured = null;
        service.Setup(s => s.Get(It.IsAny<PostListQueryModel>()))
            .Callback<PostListQueryModel>(query => captured = query)
            .ReturnsAsync(new BasePagingModel<BlogPostDto> { Data = new(), Page = 2, PageSize = 10 });
        service.Setup(s => s.GetCategoriesWithCount("es")).ReturnsAsync(new List<CategoryWithCount>());
        var viewService = new BlogPostViewService(service.Object);

        await viewService.GetIndexDataAsync(new BlogIndexRequest(
            Page: 2, PageSize: 10, StartDate: start, EndDate: end,
            Language: "es", Order: "title_asc", Category: "AI"));

        Assert.NotNull(captured);
        Assert.Equal(new[] { "AI" }, captured.Categories);
        Assert.Equal(start, captured.StartDate);
        Assert.Equal(end, captured.EndDate);
        Assert.Equal("es", captured.Language);
        Assert.Equal("title", captured.orderBy);
        Assert.Equal("asc", captured.orderDir);
        Assert.Equal(2, captured.Page);
        Assert.Equal(10, captured.PageSize);
    }

    [Fact]
    public async Task GetIndexDataAsync_SingleCategoryResultStaysInFilterableList()
    {
        var service = new Mock<IBlogService>();
        service.Setup(s => s.Get(It.IsAny<PostListQueryModel>()))
            .ReturnsAsync(new BasePagingModel<BlogPostDto>
            {
                Data = new() { new BlogPostDto { Title = "One article", Slug = "one", Categories = new[] { "AI" }, Languages = new[] { "en" }, Language = "en", PlainTextContent = "An article" } },
                TotalItems = 1, Page = 1, PageSize = 10
            });
        service.Setup(s => s.GetCategoriesWithCount("en")).ReturnsAsync(new List<CategoryWithCount>());
        var result = await new BlogPostViewService(service.Object)
            .GetIndexDataAsync(new BlogIndexRequest(Category: "AI"));

        Assert.Single(result.Posts.Data);
        Assert.False(result.ShouldRedirectToSinglePost);
    }
}
