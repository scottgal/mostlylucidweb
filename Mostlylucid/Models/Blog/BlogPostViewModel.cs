
using Mostlylucid.Models.Comments;
using Mostlylucid.Shared;
using Mostlylucid.Shared.Models;

namespace Mostlylucid.Models.Blog;

public class BlogPostViewModel : BaseViewModel
{
    public PostListModel ToPostListModel()
    {
        return new PostListModel
        {
            Id = Id,
            Categories = Categories,
            Title = Title,
            Language = Language,
            UpdatedDate = UpdatedDate.DateTime,
            Languages = Languages,
            Slug = Slug,
            WordCount = WordCount,
            PublishedDate = PublishedDate
        };
    }
    public BlogPostDto ToBlogPostDto()
    {
        return new BlogPostDto
        {
            Id = Id,
            Categories = Categories,
            Title = Title,
            Language = Language,
            Markdown = Markdown,
            UpdatedDate = UpdatedDate,
            Languages = Languages,
            HtmlContent = HtmlContent,
            PlainTextContent = PlainTextContent,
            Slug = Slug,
            WordCount = WordCount,
            PublishedDate = PublishedDate
        };
    }
    public string Id { get; set; }
    public string[] Categories { get; set; } = Array.Empty<string>();
    
    public CommentViewList Comments { get; set; } = new();
    public string Title { get; set; }= string.Empty;
    
    public string Language { get; set; }= string.Empty;
    
    public string Markdown { get; set; }= string.Empty;
    
    public DateTimeOffset UpdatedDate { get; set; }
    
    public string[] Languages { get; set; } = Array.Empty<string>();
    
    public string HtmlContent { get; set; }= string.Empty;
    
    public string PlainTextContent { get; set; }= string.Empty;
    
    public string Slug { get; set; }= string.Empty;
    
    public string ReadingTime
    {
        get
        {
            var readCount = (float)WordCount / 200;
            return readCount <1  ? "Less than a minute" : $"{Math.Round(readCount)} minute read";
        }
    }
    
    public int WordCount { get; set; }
    
    public DateTime PublishedDate { get; set; }
    
    

}

public class TranslationModel 
{

    
    public string TranslatedMarkdown { get; set; }= string.Empty;

}