﻿using Microsoft.EntityFrameworkCore;
using Mostlylucid.DbContext.EntityFramework;
using Mostlylucid.Shared.Entities;
using Mostlylucid.Shared.Interfaces;
using Mostlylucid.Shared.Mapper;
using Mostlylucid.Shared.Models;
using Serilog;
using SerilogTracing;

namespace Mostlylucid.Services.Blog;

public class BlogSearchService(MostlylucidDbContext context)
{

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
        return context.BlogPosts
            .Include(x => x.Categories)
            .Include(x => x.LanguageEntity)
            .AsNoTrackingWithIdentityResolution()
            //.AsSplitQuery()
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
                    processedQuery)));
    }

    private IQueryable<BlogPostEntity> QueryForWildCard(string query)
    {
        return context.BlogPosts
            .Include(x => x.Categories)
            .Include(x => x.LanguageEntity)
            .AsNoTrackingWithIdentityResolution()
            //.AsSplitQuery()
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
                    query + ":*"))); // Use precomputed SearchVector for ranking
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

    public record SearchResults(string Title, string Slug, string Url);
}