using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Mostlylucid.DbContext.EntityFramework;
using Mostlylucid.SemanticSearch.Services;
using Mostlylucid.Shared.Entities;
using Mostlylucid.Shared.Helpers;
using Mostlylucid.Shared.Mapper;
using Mostlylucid.Shared.Models;
using Serilog;
using SerilogTracing;

namespace Mostlylucid.Services.Blog;

public class BlogSearchService(
    MostlylucidDbContext context,
    ISemanticSearchService semanticSearchService,
    IMemoryCache memoryCache,
    SearchRanker? searchRanker = null,
    SearchQueryParser? queryParser = null) : IBlogSearchService
{
    private readonly SearchRanker _ranker = searchRanker ?? new SearchRanker();
    private readonly SearchQueryParser _queryParser = queryParser ?? new SearchQueryParser();

    // Cache for available languages (rarely changes)
    private static readonly TimeSpan LanguageCacheDuration = TimeSpan.FromHours(1);
    private static List<string>? _cachedLanguages;
    private static DateTime _languageCacheExpiry = DateTime.MinValue;
    private static readonly SemaphoreSlim _cacheLock = new(1, 1);

    public async Task<List<string>> GetAvailableLanguagesAsync()
    {
        // Check cache first without lock
        if (_cachedLanguages != null && DateTime.UtcNow < _languageCacheExpiry)
            return _cachedLanguages;

        // Acquire lock for cache refresh
        await _cacheLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (_cachedLanguages != null && DateTime.UtcNow < _languageCacheExpiry)
                return _cachedLanguages;

            _cachedLanguages = await context.Languages
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => x.Name)
                .ToListAsync();

            _languageCacheExpiry = DateTime.UtcNow.Add(LanguageCacheDuration);
            return _cachedLanguages;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    // Cache for top categories (rarely changes)
    private static readonly TimeSpan CategoryCacheDuration = TimeSpan.FromHours(1);
    private static List<CategoryWithCount>? _cachedCategories;
    private static DateTime _categoryCacheExpiry = DateTime.MinValue;
    private static readonly SemaphoreSlim _categoryCacheLock = new(1, 1);

    public async Task<List<CategoryWithCount>> GetTopCategoriesAsync(int limit = 25)
    {
        // Check cache first without lock
        if (_cachedCategories != null && DateTime.UtcNow < _categoryCacheExpiry)
            return _cachedCategories.Take(limit).ToList();

        // Acquire lock for cache refresh
        await _categoryCacheLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (_cachedCategories != null && DateTime.UtcNow < _categoryCacheExpiry)
                return _cachedCategories.Take(limit).ToList();

            var now = DateTimeOffset.UtcNow;
            _cachedCategories = await context.Categories
                .AsNoTracking()
                .Select(c => new
                {
                    c.Name,
                    PostCount = c.BlogPosts.Count(p => !p.IsHidden && p.LanguageEntity.Name == "en"
                        && (p.ScheduledPublishDate == null || p.ScheduledPublishDate <= now))
                })
                .Where(c => c.PostCount > 0)
                .OrderByDescending(c => c.PostCount)
                .Select(c => new CategoryWithCount(c.Name, c.PostCount))
                .ToListAsync();

            _categoryCacheExpiry = DateTime.UtcNow.Add(CategoryCacheDuration);
            return _cachedCategories.Take(limit).ToList();
        }
        finally
        {
            _categoryCacheLock.Release();
        }
    }

    public async Task<BasePagingModel<BlogPostDto>> GetPosts(string? query, int page = 1, int pageSize = 10)
    {
       using var activity = Log.Logger.StartActivity("GetPosts");
       activity.AddProperty("Query", query);
        activity.AddProperty("Page", page);
        activity.AddProperty("PageSize", pageSize);
        if(string.IsNullOrEmpty(query))
        {
            return new BasePagingModel<BlogPostDto>();
        }
        IQueryable<BlogPostEntity> blogPostQuery = query.Contains(" ") ? QueryForSpaces(query) : QueryForWildCard(query);
        var totalPosts = await blogPostQuery.CountAsync();
        var results = await blogPostQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        return new BasePagingModel<BlogPostDto>()
        {
            Data = results.Select(x => x.ToDto()).ToList(),
            TotalItems = totalPosts,
            Page = page,
            PageSize = pageSize
        };
        
    }

    private IQueryable<BlogPostEntity> QueryForSpaces(string processedQuery)
    {
        var now = DateTimeOffset.UtcNow;

        // Apply technical term replacement (ASP → aspnet, C# → csharp, etc.)
        var query = _queryParser.ReplaceTechnicalTerms(processedQuery);

        // Lowercase the query for PostgreSQL full-text search (case-insensitive)
        var lowerQuery = query.ToLower();

        return context.BlogPosts
            .Include(x => x.Categories)
            .Include(x => x.LanguageEntity)
            .AsNoTracking()
            .Where(x =>
                !x.IsHidden
                && (x.ScheduledPublishDate == null || x.ScheduledPublishDate <= now)
                && (x.SearchVector.Matches(EF.Functions.WebSearchToTsQuery("english", lowerQuery))
                 || x.Categories.Any(c =>
                     EF.Functions.ToTsVector("english", c.Name)
                         .Matches(EF.Functions.WebSearchToTsQuery("english", lowerQuery)))
                 // ILIKE fallback for compound words that the English stemmer mangles
                 || EF.Functions.ILike(x.Title, $"%{lowerQuery}%")
                 || EF.Functions.ILike(x.PlainTextContent, $"%{lowerQuery}%"))
                && x.LanguageEntity.Name == "en")
            // Order by relevance score * recency boost (posts from last 2 years get boost)
            .OrderByDescending(x =>
                x.SearchVector.RankCoverDensity(EF.Functions.WebSearchToTsQuery("english", lowerQuery))
                * (x.PublishedDate > now.AddYears(-2) ? 2.0 : 1.0));
    }

    private IQueryable<BlogPostEntity> QueryForWildCard(string query)
    {
        var now = DateTimeOffset.UtcNow;

        // Apply technical term replacement (ASP → aspnet, C# → csharp, etc.)
        var processedQuery = _queryParser.ReplaceTechnicalTerms(query);

        // Lowercase the query for PostgreSQL full-text search (case-insensitive)
        var lowerQuery = processedQuery.ToLower();

        return context.BlogPosts
            .Include(x => x.Categories)
            .Include(x => x.LanguageEntity)
            .AsNoTracking()
            .Where(x =>
                !x.IsHidden
                && (x.ScheduledPublishDate == null || x.ScheduledPublishDate <= now)
                && (x.SearchVector.Matches(EF.Functions.ToTsQuery("english", lowerQuery + ":*"))
                 || x.Categories.Any(c =>
                     EF.Functions.ToTsVector("english", c.Name)
                         .Matches(EF.Functions.ToTsQuery("english", lowerQuery + ":*")))
                 // ILIKE fallback for compound words that the English stemmer mangles
                 || EF.Functions.ILike(x.Title, $"%{lowerQuery}%")
                 || EF.Functions.ILike(x.PlainTextContent, $"%{lowerQuery}%"))
                && x.LanguageEntity.Name == "en")
            // Order by relevance score * recency boost (posts from last 2 years get boost)
            .OrderByDescending(x =>
                x.SearchVector.RankCoverDensity(EF.Functions.ToTsQuery("english", lowerQuery + ":*"))
                * (x.PublishedDate > now.AddYears(-2) ? 2.0 : 1.0));
    }
    
    public async Task<List<(string Title, string Slug)>> GetSearchResultForQuery(string query)
    {
        var processedQuery = query;
        var posts = await QueryForSpaces(processedQuery)
            .Select(x => new { x.Title, x.Slug, })
            .Take(5)
            .ToListAsync();
        return posts.Select(x => (x.Title, x.Slug)).ToList();
    }



    public async Task<List<(string Title, string Slug)>> GetSearchResultForComplete(string query)
    {
        var posts = await QueryForWildCard(query)
            .Select(x => new { x.Title, x.Slug, })
            .Take(5)
            .ToListAsync();
        return posts.Select(x => (x.Title, x.Slug)).ToList();
    }

    public record SearchResults(string Title, string Slug, string Url, float Score = 1.0f);

    /// <summary>
    /// Hybrid search: tries PostgreSQL first for typeahead (better for partial matches),
    /// falls back to semantic search for longer/complete queries.
    /// Results are ordered by match quality (score)
    /// </summary>
    public async Task<List<SearchResults>> HybridSearchAsync(string query, int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<SearchResults>();

        // For typeahead, PostgreSQL FTS with ILIKE is more reliable for partial terms
        // Try PostgreSQL first
        try
        {
            var pgResults = query.Contains(" ")
                ? await GetSearchResultForQueryWithLimit(query, limit)
                : await GetSearchResultForCompleteWithLimit(query, limit);

            if (pgResults.Count > 0)
            {
                // PostgreSQL results are already ranked by ts_rank
                return pgResults.Select((r, index) => new SearchResults(
                    r.Title,
                    r.Slug,
                    $"/blog/{r.Slug}",
                    1.0f - (index * 0.05f) // Approximate score based on ranking
                )).ToList();
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Warning(ex, "PostgreSQL full-text search failed for query '{Query}', trying semantic search", query);
        }

        // Fall back to semantic search for queries that PostgreSQL couldn't match
        // (e.g., natural language questions, conceptual queries)
        if (query.Length >= 5)
        {
            try
            {
                var semanticResults = await semanticSearchService.SearchAsync(query, limit);

                if (semanticResults.Count > 0)
                {
                    // Deduplicate semantic results by slug (keep highest score)
                    var uniqueResults = semanticResults
                        .GroupBy(r => r.Slug)
                        .Select(g => g.OrderByDescending(r => r.Score).First())
                        .ToList();

                    // Get slugs from semantic search, look up English titles from PostgreSQL
                    var slugs = uniqueResults.Select(r => r.Slug).ToList();
                    var posts = await context.BlogPosts
                        .Where(p => slugs.Contains(p.Slug) && p.LanguageEntity.Name == "en")
                        .Select(p => new { p.Slug, p.Title })
                        .ToListAsync();

                    // Match back with semantic scores, preserving order
                    // Only return results that have an English version in the database
                    return uniqueResults
                        .Select(r =>
                        {
                            var post = posts.FirstOrDefault(p => p.Slug == r.Slug);
                            return post != null
                                ? new SearchResults(post.Title, r.Slug, $"/blog/{r.Slug}", r.Score)
                                : null;
                        })
                        .Where(r => r != null)
                        .Cast<SearchResults>()
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                // Log but don't fail
                Log.Logger.Warning(ex, "Semantic search also failed for query '{Query}'", query);
            }
        }

        return new List<SearchResults>();
    }

    private async Task<List<(string Title, string Slug)>> GetSearchResultForQueryWithLimit(string query, int limit)
    {
        var posts = await QueryForSpaces(query)
            .Select(x => new { x.Title, x.Slug })
            .Take(limit)
            .ToListAsync();
        return posts.Select(x => (x.Title, x.Slug)).ToList();
    }

    private async Task<List<(string Title, string Slug)>> GetSearchResultForCompleteWithLimit(string query, int limit)
    {
        var posts = await QueryForWildCard(query)
            .Select(x => new { x.Title, x.Slug })
            .Take(limit)
            .ToListAsync();
        return posts.Select(x => (x.Title, x.Slug)).ToList();
    }

    /// <summary>
    /// Hybrid search with paging: tries semantic search first, falls back to PostgreSQL
    /// </summary>
    public async Task<BasePagingModel<BlogPostDto>> HybridSearchWithPagingAsync(string? query, int page = 1, int pageSize = 10)
    {
        return await HybridSearchWithPagingAsync(query, null, null, null, page, pageSize);
    }

    /// <summary>
    /// Hybrid search with paging and filters: tries semantic search first, falls back to PostgreSQL
    /// If no results found, returns recent posts as suggestions
    /// </summary>
    public async Task<BasePagingModel<BlogPostDto>> HybridSearchWithPagingAsync(
        string? query,
        string? language,
        DateTime? startDate,
        DateTime? endDate,
        int page = 1,
        int pageSize = 10,
        string order = "date_desc")
    {
        if (string.IsNullOrWhiteSpace(query))
            return new BasePagingModel<BlogPostDto>();

        using var activity = Log.Logger.StartActivity("HybridSearchWithPaging");
        activity.AddProperty("Query", query);
        activity.AddProperty("Language", language ?? "all");
        activity.AddProperty("Page", page);
        activity.AddProperty("PageSize", pageSize);
        activity.AddProperty("Order", order);

        var targetLanguage = string.IsNullOrEmpty(language) ? "en" : language;
        var parsed = _queryParser.Parse(query);

        // Try semantic search first (with fallback to PostgreSQL on any failure)
        try
        {
            var semanticResults = await semanticSearchService.SearchAsync(query, pageSize * 3); // Get more for potential filtering

            if (semanticResults.Count > 0)
            {
                // Get full blog posts for the semantic results
                var slugs = semanticResults.Select(r => r.Slug).ToList();
                var postsQuery = ApplyMetadataFilters(context.BlogPosts
                    .Include(x => x.Categories)
                    .Include(x => x.LanguageEntity)
                    .AsNoTracking()
                    .Where(x => slugs.Contains(x.Slug)), targetLanguage, startDate, endDate, parsed);

                var posts = await postsQuery.ToListAsync();

                // Verify semantic results are actually relevant by checking if PostgreSQL also finds them
                // This prevents returning unrelated articles for gibberish queries
                if (posts.Count > 0)
                {
                    // Quick check: does PostgreSQL FTS find ANY results for this query?
                    var pgVerification = await GetPostsWithFiltersInternal(query, targetLanguage, startDate, endDate, limit: 5);

                    // If PostgreSQL finds nothing, the semantic results are likely false positives
                    // Fall through to PostgreSQL-only search
                    if (pgVerification.Count == 0)
                    {
                        Log.Logger.Information(
                            "Semantic search returned {Count} results but PostgreSQL found none for '{Query}', falling back to PostgreSQL",
                            posts.Count, query);
                        // Don't use semantic results - fall through to PostgreSQL fallback below
                    }
                    else
                    {
                    // Deduplicate semantic results by slug (keep highest score)
                    var uniqueSemanticResults = semanticResults
                        .GroupBy(r => r.Slug)
                        .Select(g => g.OrderByDescending(r => r.Score).First())
                        .ToDictionary(r => r.Slug, r => r.Score);

                    IEnumerable<BlogPostEntity> orderedPosts;

                    // Use RRF fusion for relevance ordering
                    if (order == "relevance" || order == "relevance_desc" || string.IsNullOrEmpty(order))
                    {
                        // Run PostgreSQL BM25 search in parallel with semantic (which already completed)
                        try
                        {
                            var pgResults = await GetPostsWithFiltersInternal(query, targetLanguage, startDate, endDate, pageSize * 2);

                            // Prepare vector results (from semantic search) with scores
                            var vectorResults = posts
                                .Select(p => (
                                    Post: p.ToDto(),
                                    Score: uniqueSemanticResults.TryGetValue(p.Slug, out var score) ? score : 0f
                                ))
                                .ToList();

                            // Prepare BM25 results
                            var bm25Results = pgResults.Select(p => p.ToDto()).ToList();

                            // Fuse using RRF with category/freshness boosts
                            var fusedDtos = _ranker.FuseResults(bm25Results, vectorResults, query);

                            var pagedDtos = fusedDtos
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToList();

                            return new BasePagingModel<BlogPostDto>
                            {
                                Data = pagedDtos,
                                TotalItems = fusedDtos.Count,
                                Page = page,
                                PageSize = pageSize
                            };
                        }
                        catch (Exception ex)
                        {
                            // Fall back to simple semantic ordering if RRF fusion fails
                            Log.Logger.Warning(ex, "RRF fusion failed, falling back to simple semantic ranking");
                            orderedPosts = posts.OrderByDescending(p =>
                                uniqueSemanticResults.TryGetValue(p.Slug, out var score) ? score : 0f);
                        }
                    }
                    else
                    {
                        // Non-relevance orderings use simple sorting
                        orderedPosts = order switch
                        {
                            "date_asc" => posts.OrderBy(p => p.PublishedDate),
                            "date_desc" => posts.OrderByDescending(p => p.PublishedDate),
                            "title_asc" => posts.OrderBy(p => p.Title),
                            "title_desc" => posts.OrderByDescending(p => p.Title),
                            _ => posts.OrderByDescending(p =>
                                uniqueSemanticResults.TryGetValue(p.Slug, out var score) ? score : 0f)
                        };
                    }

                    var pagedPosts = orderedPosts
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();

                    return new BasePagingModel<BlogPostDto>
                    {
                        Data = pagedPosts.Select(x => x.ToDto()).ToList(),
                        TotalItems = posts.Count,
                        Page = page,
                        PageSize = pageSize
                    };
                    } // Close the else block (pgVerification.Count > 0)
                }

                // Semantic search returned results but none matched DB filters - fall through to PostgreSQL
                Log.Logger.Debug("Semantic search returned {Count} results but none matched DB filters, falling back to PostgreSQL", semanticResults.Count);
            }
        }
        catch (Exception ex)
        {
            // Log but don't fail - fall back to PostgreSQL
            Log.Logger.Warning(ex, "Semantic search failed for query '{Query}', falling back to PostgreSQL full-text search", query);
        }

        // Fall back to PostgreSQL full-text search with filters
        try
        {
            var pgResults = await GetPostsWithFilters(query, targetLanguage, startDate, endDate, page, pageSize, order);

            // If no results found, mark it so we can return recent posts as suggestions
            if (pgResults.TotalItems == 0)
            {
                Log.Logger.Information("No matches for {Query}; suggesting recent articles", query);
            }
            else
            {
                return pgResults;
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "PostgreSQL full-text search failed for query '{Query}'", query);

        }

        // Recommendations are explicitly marked and always ordered newest first.
        return await GetRecentPostsAsSuggestions(targetLanguage, startDate, endDate, page, pageSize,
            parsed);
    }

    /// <summary>
    /// Internal method to get filtered posts without paging (for RRF fusion).
    /// </summary>
    private async Task<List<BlogPostEntity>> GetPostsWithFiltersInternal(
        string query,
        string language,
        DateTime? startDate,
        DateTime? endDate,
        int limit = 50)
    {
        var orderedQuery = BuildSearchQuery(query, language, startDate, endDate, "relevance");
        return await orderedQuery.Take(limit).ToListAsync();
    }

    private async Task<BasePagingModel<BlogPostDto>> GetPostsWithFilters(
        string query,
        string language,
        DateTime? startDate,
        DateTime? endDate,
        int page,
        int pageSize,
        string order = "date_desc")
    {
        var orderedQuery = BuildSearchQuery(query, language, startDate, endDate, order);

        var totalPosts = await orderedQuery.CountAsync();
        var results = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new BasePagingModel<BlogPostDto>
        {
            Data = results.Select(x => x.ToDto()).ToList(),
            TotalItems = totalPosts,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Build the PostgreSQL full-text search query with filters and ordering.
    /// Supports Google-style search operators: quoted phrases, -excluded terms, wildcards
    /// </summary>
    private IOrderedQueryable<BlogPostEntity> BuildSearchQuery(
        string query,
        string language,
        DateTime? startDate,
        DateTime? endDate,
        string order)
    {
        var parsed = _queryParser.Parse(query);
        var baseQuery = ApplyMetadataFilters(context.BlogPosts
            .Include(x => x.Categories)
            .Include(x => x.LanguageEntity)
            .AsNoTracking(), language, startDate, endDate, parsed);

        // Build the search query based on parsed components
        IQueryable<BlogPostEntity> searchQuery = baseQuery;

        // Handle phrases (exact substring matching) - batched into single query
        if (parsed.Phrases.Count > 0)
        {
            var phrasePatterns = parsed.Phrases.Select(phrase => $"%{phrase}%").ToArray();
            searchQuery = searchQuery.Where(x =>
                phrasePatterns.All(pattern =>
                    EF.Functions.ILike(x.Title, pattern)
                    || EF.Functions.ILike(x.PlainTextContent, pattern)
                    || x.Categories.Any(c => EF.Functions.ILike(c.Name, pattern))));
        }

        // Build tsquery for include terms and wildcards
        var tsQuery = _queryParser.BuildTsQuery(parsed);

        // Build ILIKE pattern for fallback (catches compound words the English stemmer mangles)
        // For "docsummarizer", this creates "%docsummarizer%"
        var allSearchTerms = parsed.IncludeTerms
            .Concat(parsed.WildcardTerms)
            .ToList();

        var likePattern = allSearchTerms.Count > 0
            ? $"%{string.Join("%", allSearchTerms.Select(t => t.ToLowerInvariant()))}%"
            : null;

        // Apply full-text search OR ILIKE fallback
        // ILIKE handles terms like "docsummarizer" that PostgreSQL FTS might not find due to stemming
        if (!string.IsNullOrWhiteSpace(tsQuery) && likePattern != null)
        {
            searchQuery = searchQuery.Where(x =>
                // Full-text search on content
                x.SearchVector.Matches(EF.Functions.ToTsQuery("english", tsQuery))
                // Full-text search on categories
                || x.Categories.Any(c =>
                    EF.Functions.ToTsVector("english", c.Name)
                        .Matches(EF.Functions.ToTsQuery("english", tsQuery)))
                // ILIKE fallback on title and content
                || EF.Functions.ILike(x.Title, likePattern)
                || EF.Functions.ILike(x.PlainTextContent, likePattern));
        }
        else if (!string.IsNullOrWhiteSpace(tsQuery))
        {
            searchQuery = searchQuery.Where(x =>
                x.SearchVector.Matches(EF.Functions.ToTsQuery("english", tsQuery))
                || x.Categories.Any(c =>
                    EF.Functions.ToTsVector("english", c.Name)
                        .Matches(EF.Functions.ToTsQuery("english", tsQuery))));
        }
        else if (likePattern != null)
        {
            searchQuery = searchQuery.Where(x =>
                EF.Functions.ILike(x.Title, likePattern)
                || EF.Functions.ILike(x.PlainTextContent, likePattern));
        }

        // Handle excluded terms (must NOT contain these) - batched into single query
        if (parsed.ExcludeTerms.Count > 0)
        {
            var excludedPatterns = parsed.ExcludeTerms.Select(term => $"%{term}%").ToArray();
            searchQuery = searchQuery.Where(x =>
                !excludedPatterns.Any(pattern =>
                    EF.Functions.ILike(x.Title, pattern)
                    || EF.Functions.ILike(x.PlainTextContent, pattern)
                    || x.Categories.Any(c => EF.Functions.ILike(c.Name, pattern))));
        }

        // Apply ordering - relevance uses ts_rank_cd (cover density), others use column values
        IOrderedQueryable<BlogPostEntity> orderedQuery;
        if (order == "relevance" || order == "relevance_desc")
        {
            // Order by full-text search rank using cover density (considers term proximity)
            // Only rank if we have a tsquery, otherwise use date
            if (!string.IsNullOrWhiteSpace(tsQuery))
            {
                orderedQuery = searchQuery.OrderByDescending(x =>
                    x.SearchVector.RankCoverDensity(EF.Functions.ToTsQuery("english", tsQuery)));
            }
            else
            {
                // No ts_rank_cd available (only phrases/excluded terms), sort by date
                orderedQuery = searchQuery.OrderByDescending(x => x.PublishedDate);
            }
        }
        else
        {
            orderedQuery = order switch
            {
                "date_asc" => searchQuery.OrderBy(x => x.PublishedDate),
                "date_desc" => searchQuery.OrderByDescending(x => x.PublishedDate),
                "title_asc" => searchQuery.OrderBy(x => x.Title),
                "title_desc" => searchQuery.OrderByDescending(x => x.Title),
                _ => !string.IsNullOrWhiteSpace(tsQuery)
                    ? searchQuery.OrderByDescending(x => x.SearchVector.RankCoverDensity(EF.Functions.ToTsQuery("english", tsQuery)))
                    : searchQuery.OrderByDescending(x => x.PublishedDate)
            };
        }

        return orderedQuery;
    }

    /// <summary>
    /// Get recent posts as suggestions when search returns no results.
    /// Returns posts ordered by date descending (most recent first).
    /// </summary>
    private async Task<BasePagingModel<BlogPostDto>> GetRecentPostsAsSuggestions(
        string language,
        DateTime? startDate,
        DateTime? endDate,
        int page,
        int pageSize,
        SearchQueryParser.ParsedQuery parsed)
    {
        var query = ApplyMetadataFilters(context.BlogPosts
            .Include(x => x.Categories)
            .Include(x => x.LanguageEntity)
            .AsNoTracking(), language, startDate, endDate, parsed);

        var totalPosts = await query.CountAsync();
        var results = await query
            .OrderByDescending(x => x.PublishedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new BasePagingModel<BlogPostDto>
        {
            Data = results.Select(x => x.ToDto()).ToList(),
            TotalItems = totalPosts,
            Page = page,
            PageSize = pageSize,
            NoMatchFound = true
        };
    }

    private static IQueryable<BlogPostEntity> ApplyMetadataFilters(IQueryable<BlogPostEntity> query,
        string language, DateTime? startDate, DateTime? endDate, SearchQueryParser.ParsedQuery parsed)
    {
        var now = DateTimeOffset.UtcNow;
        query = query.Where(post => !post.IsHidden
            && (post.ScheduledPublishDate == null || post.ScheduledPublishDate <= now)
            && post.LanguageEntity.Name == language);
        // Both UI dates and query operators constrain every candidate source, including suggestions.
        if (DateFilterHelper.InclusiveStart(startDate) is { } inclusiveStart)
            query = query.Where(post => post.PublishedDate >= inclusiveStart);
        if (DateFilterHelper.InclusiveStart(parsed.AfterDate) is { } afterInclusive)
            query = query.Where(post => post.PublishedDate >= afterInclusive);
        if (DateFilterHelper.ExclusiveEnd(endDate) is { } exclusiveEnd)
            query = query.Where(post => post.PublishedDate < exclusiveEnd);
        if (DateFilterHelper.ExclusiveEnd(parsed.BeforeDate) is { } beforeExclusive)
            query = query.Where(post => post.PublishedDate < beforeExclusive);
        if (parsed.Categories.Count > 0)
        {
            var categoryPatterns = parsed.Categories.Select(term => $"%{term}%").ToArray();
            query = query.Where(post => post.Categories.Any(category => categoryPatterns.Any(pattern =>
                EF.Functions.ILike(category.Name, pattern))));
        }
        return query;
    }

    /// <summary>
    /// Get search term suggestions based on actual blog content (categories, title words, etc.)
    /// Returns terms that start with the given prefix
    /// Uses memory cache with sliding expiration (acts like LFU - frequently accessed items stay cached)
    /// </summary>
    public async Task<List<string>> GetSearchTermSuggestions(string prefix, int limit = 10)
    {
        var cacheKey = $"term_suggest_{prefix.ToLower()}_{limit}";

        // Try to get from cache first
        if (memoryCache.TryGetValue(cacheKey, out List<string>? cachedSuggestions))
        {
            return cachedSuggestions!;
        }

        // Not in cache - compute suggestions
        var lowerPrefix = prefix.ToLower();
        var suggestions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1. Get matching categories (these are usually good search terms)
        var categories = await context.Categories
            .AsNoTracking()
            .Where(c => EF.Functions.ILike(c.Name, $"{lowerPrefix}%"))
            .Select(c => c.Name)
            .Take(limit)
            .ToListAsync();

        foreach (var cat in categories)
        {
            suggestions.Add(cat);
        }

        // 2. Extract common words from blog post titles that start with prefix
        var titleWords = await context.BlogPosts
            .AsNoTracking()
            .Where(p => !p.IsHidden && p.LanguageEntity.Name == "en")
            .Select(p => p.Title)
            .ToListAsync();

        // Split titles into words and find matches
        var words = titleWords
            .SelectMany(title => title.Split(new[] { ' ', '-', '.', ':', '/', '(', ')', '[', ']' },
                StringSplitOptions.RemoveEmptyEntries))
            .Where(word => word.Length >= 3) // Ignore very short words
            .Where(word => word.StartsWith(lowerPrefix, StringComparison.OrdinalIgnoreCase))
            .GroupBy(word => word.ToLower())
            .OrderByDescending(g => g.Count()) // Most frequent first
            .Select(g => g.First()) // Keep original casing of first occurrence
            .Take(limit);

        foreach (var word in words)
        {
            suggestions.Add(word);
        }

        // Return sorted by length (shorter terms first, they're usually more general)
        var result = suggestions
            .OrderBy(s => s.Length)
            .ThenBy(s => s)
            .Take(limit)
            .ToList();

        // Cache with sliding expiration (LFU-like: frequently accessed items stay cached longer)
        var cacheOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(30), // Refreshes on each access
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2), // Max 2 hours
            Priority = CacheItemPriority.Normal,
            Size = 1 // For LFU eviction when cache is full
        };

        memoryCache.Set(cacheKey, result, cacheOptions);

        return result;
    }
}
