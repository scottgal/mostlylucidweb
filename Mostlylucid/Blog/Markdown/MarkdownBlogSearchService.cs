using Mostlylucid.Services.Blog;
using Mostlylucid.Services.Markdown;
using Mostlylucid.Shared;
using Mostlylucid.Shared.Models;

namespace Mostlylucid.Blog.Markdown;

/// <summary>Searches the populated Markdown cache when no database is configured.</summary>
public class MarkdownBlogSearchService : IBlogSearchService
{
    private readonly SearchQueryParser _parser = new();

    private static IEnumerable<BlogPostDto> VisiblePosts => PageCacheHelper.GetPageCache().Values
        .Where(post => !post.IsHidden && (post.ScheduledPublishDate == null || post.ScheduledPublishDate <= DateTimeOffset.UtcNow));

    public Task<List<string>> GetAvailableLanguagesAsync() => Task.FromResult(
        VisiblePosts.Select(post => post.Language).Distinct().OrderBy(language => language).ToList());

    public Task<List<CategoryWithCount>> GetTopCategoriesAsync(int limit = 25) => Task.FromResult(
        VisiblePosts.SelectMany(post => post.Categories).GroupBy(category => category)
            .OrderByDescending(group => group.Count()).ThenBy(group => group.Key)
            .Take(limit).Select(group => new CategoryWithCount(group.Key, group.Count())).ToList());

    public Task<BasePagingModel<BlogPostDto>> HybridSearchWithPagingAsync(string? query, string? language,
        DateTime? startDate, DateTime? endDate, int page = 1, int pageSize = 10, string order = "date_desc")
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var parsed = _parser.Parse(query ?? "");
        var terms = parsed.IncludeTerms.Concat(parsed.Phrases).Concat(parsed.WildcardTerms.Select(term => term.TrimEnd('*'))).ToArray();
        var eligible = VisiblePosts.Where(post =>
            (string.IsNullOrEmpty(language) || post.Language.Equals(language, StringComparison.OrdinalIgnoreCase))
            && (!startDate.HasValue || post.PublishedDate.Date >= startDate.Value.Date)
            && (!parsed.AfterDate.HasValue || post.PublishedDate.Date >= parsed.AfterDate.Value.Date)
            && (!endDate.HasValue || post.PublishedDate.Date <= endDate.Value.Date)
            && (!parsed.BeforeDate.HasValue || post.PublishedDate.Date <= parsed.BeforeDate.Value.Date)
            && parsed.Categories.All(category => post.Categories.Contains(category, StringComparer.OrdinalIgnoreCase))).ToList();
        var matches = eligible.Where(post =>
        {
            var text = _parser.ReplaceTechnicalTerms($"{post.Title} {post.PlainTextContent} {string.Join(' ', post.Categories)}");
            return terms.All(term => text.Contains(term, StringComparison.OrdinalIgnoreCase))
                && parsed.ExcludeTerms.All(term => !text.Contains(term, StringComparison.OrdinalIgnoreCase));
        });
        matches = order switch
        {
            "date_asc" => matches.OrderBy(post => post.PublishedDate),
            "title_asc" => matches.OrderBy(post => post.Title, StringComparer.OrdinalIgnoreCase),
            "title_desc" => matches.OrderByDescending(post => post.Title, StringComparer.OrdinalIgnoreCase),
            "relevance" => matches.OrderByDescending(post => terms.Count(term => _parser.ReplaceTechnicalTerms(post.Title).Contains(term, StringComparison.OrdinalIgnoreCase)))
                .ThenByDescending(post => post.PublishedDate),
            _ => matches.OrderByDescending(post => post.PublishedDate)
        };
        var results = string.IsNullOrWhiteSpace(query) ? new List<BlogPostDto>() : matches.ToList();
        var noMatch = !string.IsNullOrWhiteSpace(query) && results.Count == 0;
        if (noMatch) results = eligible.OrderByDescending(post => post.PublishedDate).ToList();
        return Task.FromResult(new BasePagingModel<BlogPostDto>
        {
            NoMatchFound = noMatch,
            Data = results.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            Page = page, PageSize = pageSize, TotalItems = results.Count
        });
    }

    public async Task<List<BlogSearchService.SearchResults>> HybridSearchAsync(string query, int limit = 10)
    {
        var results = await HybridSearchWithPagingAsync(query, "en", null, null, pageSize: limit, order: "relevance");
        if (results.NoMatchFound) return new();
        return results.Data.Select(post => new BlogSearchService.SearchResults(post.Title, post.Slug, $"/blog/{Uri.EscapeDataString(post.Slug)}")).ToList();
    }

    public async Task<List<(string Title, string Slug)>> GetSearchResultForQuery(string query) =>
        (await HybridSearchAsync(query)).Select(post => (post.Title, post.Slug)).ToList();

    public Task<List<(string Title, string Slug)>> GetSearchResultForComplete(string query) => GetSearchResultForQuery(query);

    public Task<List<string>> GetSearchTermSuggestions(string prefix, int limit = 10) => Task.FromResult(
        VisiblePosts.SelectMany(post => post.Categories.Concat(post.Title.Split(' ', StringSplitOptions.RemoveEmptyEntries)))
            .Where(term => term.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(term => term).Take(limit).ToList());
}
