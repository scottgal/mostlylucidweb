using Mostlylucid.Shared.Models;

namespace Mostlylucid.Services.Interfaces;

public interface IMarkdownBlogService
{
    Task<BlogPostDto> GetPage(string filePath);
    Task<List<BlogPostDto>> GetPages();
    Dictionary<string, List<string>> LanguageList();
}