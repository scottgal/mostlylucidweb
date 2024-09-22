using Mostlylucid.Shared.Helpers;
using Mostlylucid.Shared.Interfaces;
using Mostlylucid.Shared.Models;

namespace Mostlylucid.Models.Blog;

public class PostListViewModel : BaseViewModel, IPagingModel<PostListModel>
{
    public string LinkUrl { get; set; }
    public int Page { get; set; }
    
    public int TotalItems { get; set; }
    
    public int PageSize { get; set; }
    

    public List<PostListModel> Data { get; set; }
    
    public static PostListModel GetListModel(BlogPostViewModel model, int truncateAt = 200)
    {
        return new PostListModel
        {
            Title = model.Title,
            PublishedDate = model.PublishedDate,
            Slug = model.Slug,
            WordCount = model.WordCount,
            Language = model.Language,
            Categories = model.Categories,
            Summary = model.PlainTextContent.TruncateAtWord(truncateAt) + "...",
            Languages = model.Languages
        };
    }
    
    public static PostListModel GetListModel(BlogPostDto model, int truncateAt = 200)
    {
        return new PostListModel
        {
            Title = model.Title,
            PublishedDate = model.PublishedDate,
            Slug = model.Slug,
            WordCount = model.WordCount,
            Language = model.Language,
            Categories = model.Categories,
            Summary = model.PlainTextContent.TruncateAtWord(truncateAt) + "...",
            Languages = model.Languages
        };
    }
}