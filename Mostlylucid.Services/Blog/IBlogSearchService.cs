using Mostlylucid.Shared.Models;

namespace Mostlylucid.Services.Blog;

public interface IBlogSearchService
{
    Task<List<string>> GetAvailableLanguagesAsync();
    Task<List<CategoryWithCount>> GetTopCategoriesAsync(int limit = 25);
    Task<List<(string Title, string Slug)>> GetSearchResultForQuery(string query);
    Task<List<(string Title, string Slug)>> GetSearchResultForComplete(string query);
    Task<List<BlogSearchService.SearchResults>> HybridSearchAsync(string query, int limit = 10);
    Task<BasePagingModel<BlogPostDto>> HybridSearchWithPagingAsync(string? query, string? language,
        DateTime? startDate, DateTime? endDate, int page = 1, int pageSize = 10, string order = "date_desc");
    Task<List<string>> GetSearchTermSuggestions(string prefix, int limit = 10);
}
