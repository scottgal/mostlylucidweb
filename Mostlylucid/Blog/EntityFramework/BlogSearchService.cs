namespace Mostlylucid.Blog.EntityFramework;

public class BlogSearchService(MostlylucidDbContext context)
{
    public async Task<List<(string Title, string Slug)>> GetSearchResultForQuery(string query)
    {
        var processedQuery = query;
        var posts = await context.BlogPosts
            .Include(x => x.Categories)
            .Include(x => x.LanguageEntity)
            .Where(x =>
                // Search using the precomputed SearchVector
                (x.SearchVector.Matches(EF.Functions.WebSearchToTsQuery("english",
                     processedQuery)) // Use precomputed SearchVector for title and content
                 || x.Categories.Any(c =>
                     EF.Functions.ToTsVector("english", c.Name)
                         .Matches(EF.Functions.WebSearchToTsQuery("english", processedQuery)))) // Search in categories
                && x.LanguageEntity.Name == "en") // Filter by language
            .OrderByDescending(x =>
                // Rank based on the precomputed SearchVector
                x.SearchVector.Rank(EF.Functions.WebSearchToTsQuery("english",
                    processedQuery))) // Use precomputed SearchVector for ranking
            .Select(x => new { x.Title, x.Slug, })
            .Take(5)
            .ToListAsync();
        return posts.Select(x => (x.Title, x.Slug)).ToList();
    }

    public async Task<List<(string Title, string Slug)>> GetSearchResultForComplete(string query)
    {
        var posts = await context.BlogPosts
            .Include(x => x.Categories)
            .Include(x => x.LanguageEntity)
            .Where(x =>
                // Search using the precomputed SearchVector
                (x.SearchVector.Matches(EF.Functions.ToTsQuery("english",
                     query + ":*")) // Use precomputed SearchVector for title and content
                 || x.Categories.Any(c =>
                     EF.Functions.ToTsVector("english", c.Name)
                         .Matches(EF.Functions.ToTsQuery("english", query + ":*")))) // Search in categories
                && x.LanguageEntity.Name == "en") // Filter by language
            .OrderByDescending(x =>
                // Rank based on the precomputed SearchVector
                x.SearchVector.Rank(EF.Functions.ToTsQuery("english",
                    query + ":*"))) // Use precomputed SearchVector for ranking
            .Select(x => new { x.Title, x.Slug, })
            .Take(5)
            .ToListAsync();
        return posts.Select(x => (x.Title, x.Slug)).ToList();
    }

    public record SearchResults(string Title, string Slug, string Url);
}