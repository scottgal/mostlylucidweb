# Simple Search Using HTMX & EF Core for ASP.NET Core

<!--category-- HTMX, Postgres, Entity Framework, ASP.NET -->
<datetime class="hidden">2024-09-17T17:36</datetime>

# Introduction
This is just a quick article as it builds on the others in the full text search series such as the [typeahead dropdown](/blog/textsearchingpt11) and [Postgres full text search](/blog/textsearchingpt1). 
In this post, I will show you how to implement a simple search page using HTMX and EF Core in an ASP.NET Core application.

[TOC]

## Don't I already have a search?
Well yes, in the header of the site I have a search function which provides typeahead (where as you type the results come in real time). However I hide that in mobile mode and I also wanted to be able to link the search results (like [/search/umami](/search/umami)) to a dedicated search page. This gives a better user experience as well as working on mobile devices.

# Search Service
To do this I modified how I did my searches. I created a `BlogSearchService`, this is based on my two Full Text query methods. These unfortunately need to be split into two methods because of the way the queries are structured with the Postgres Full Text Search extensions, `EF.Functions.WebSearchToTsQuery("english",
processedQuery)` and `EF.Functions.ToTsQuery("english", query + ":*")`.

The first takes proper search terms and the second takes wildcard searches.


```csharp
    private IQueryable<BlogPostEntity> QueryForSpaces(string processedQuery)
    {
        return context.BlogPosts
            .Include(x => x.Categories)
            .Include(x => x.LanguageEntity)
            .AsNoTrackingWithIdentityResolution()
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
```

Again these use my precomputed `SearchVector` column which is updated on post creation and update. This is created in my `DbContext` using the `OnModelCreating` method.
```csharp
      entity.Property(b => b.SearchVector)
                .HasComputedColumnSql("to_tsvector('english', coalesce(\"Title\", '') || ' ' || coalesce(\"PlainTextContent\", ''))", stored: true);
            
           entity.HasIndex(b => b.SearchVector)
                .HasMethod("GIN");
```
Again the drawback of this approach is that it only works for English as-is. I would need a radical rebuild of the Database to make it work for other languages (likely a table for each language).

I then use these methods in my `BlogSearchService` to return the results based on the search query.

```csharp
    public async Task<PostListViewModel> GetPosts(string? query, int page = 1, int pageSize = 10)
    {
        if(string.IsNullOrEmpty(query))
        {
            return new PostListViewModel();
        }
        IQueryable<BlogPostEntity> blogPostQuery = query.Contains(" ") ? QueryForSpaces(query) : QueryForWildCard(query);
        var totalPosts = await blogPostQuery.CountAsync();
        var results = await blogPostQuery
            .Select(x => x.ToListModel())
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        return new PostListViewModel()
        {
            Posts = results,
            TotalItems = totalPosts,
            Page = page,
            PageSize = pageSize
        };
        
    }
```
I use the simple check for whether there are spaces in the query to determine which method to call.

## Search Controller
The Search controller follows the pattern I use for most of my controllers where it detects whether the call comes from HTMX or not to enable sending either the partial or full layout page (meaning it works for direct navigation as well as HTMX requests).

```csharp
[Route("search")]
public class SearchController(
    BaseControllerService baseControllerService,
    BlogSearchService searchService,
    ILogger<SearchController> logger)
    : BaseController(baseControllerService, logger)
{
    [HttpGet]
    [Route("{query?}")]
    public async Task<IActionResult> Search([FromRoute] string? query)
    {
        var searchResults = await searchService.GetPosts(query);
        var searchModel = new SearchResultsModel
        {
            Query = query,
            SearchResults = searchResults
        };
        searchModel = await PopulateBaseModel(searchModel);
        searchModel.SearchResults.LinkUrl = Url.Action("SearchResults", "Search");
        if (Request.IsHtmx()) return PartialView("SearchResults", searchModel);
        return View("SearchResults", searchModel);
    }

    [HttpGet]
    [Route("results")]
    public async Task<IActionResult> SearchResults([Required] string query, int page = 1, int pageSize = 10)
    {
        var searchResults = await searchService.GetPosts(query, page, pageSize);
        var searchModel = new SearchResultsModel
        {
            Query = query,
            SearchResults = searchResults
        };
        searchModel = await PopulateBaseModel(searchModel);
        searchModel.SearchResults.LinkUrl = Url.Action("SearchResults", "Search");
        if (Request.IsHtmx()) return PartialView("_SearchResultsPartial", searchModel.SearchResults);
        return View("SearchResults", searchModel);
    }
}
```
This is the whole controller, you can see that I have two Actions, one which returns the page (optionally populated with results) and one which returns just the results for HTMX requests.

```csharp
 if (Request.IsHtmx()) return PartialView("_SearchResultsPartial", searchModel.SearchResults);
```
### Search Results Partial
You can see that this optionally returns the `_SearchResultsPartial` view if the request is an HTMX request for results.

This is a pretty simple Paritial View which has paging bits and the results.

```razor
@model Mostlylucid.Models.Blog.PostListViewModel
<div class="pt-2" id="content">
    @if (Model.Posts?.Any() is true)
    {
        <div class="inline-flex w-full items-center justify-center print:!hidden">
            @if (Model.TotalItems > Model.PageSize)
            {
                <pager
                    x-ref="pager"
                    link-url="@Model.LinkUrl"
                    hx-boost="true"
                    hx-target="#content"
                    hx-swap="show:none"
                    page="@Model.Page"
                    page-size="@Model.PageSize"
                    total-items="@Model.TotalItems"
                    hx-headers='{"pagerequest": "true"}'>
                </pager>
            }
            <partial name="_Pager" model="Model"/>

        </div>
        @foreach (var post in Model.Posts)
        {
            <partial name="_ListPost" model="post"/>
        }
    }
</div>
```

### List Post Partial View
I use the same `_ListPost` partial view wherever I need to list posts.

```razor
@model Mostlylucid.Models.Blog.PostListModel

<div class="border-b border-grey-lighter pb-8 mb-8">
 
    <a asp-controller="Blog" asp-action="Show" hx-boost="true"  hx-swap="show:window:top"  hx-target="#contentcontainer" asp-route-slug="@Model.Slug"
       class="block font-body text-lg font-semibold transition-colors hover:text-green text-blue-dark dark:text-white  dark:hover:text-secondary">@Model.Title</a>  
    <div class="flex flex-wrap space-x-2 items-center py-4 print:!hidden">
    @foreach (var category in Model.Categories)
    {
        <partial name="_Category" model="category"/>
    }
    @{ var languageModel = (Model.Slug, Model.Languages, Model.Language); }
        <partial name="_LanguageList" model="languageModel"/>
    </div>
    <div class="block font-body text-black dark:text-white">@Model.Summary</div>
    <div class="flex items-center pt-4">
        <p class="pr-2 font-body font-light text-primary light:text-black dark:text-white">
            @Model.PublishedDate.ToString("f")
        </p>
        <span class="font-body text-grey dark:text-white">//</span>
        <p class="pl-2 font-body font-light text-primary light:text-black dark:text-white">
            @Model.ReadingTime
        </p>
    </div>
</div>
```

## HTMX and The Search Page
Again my use of HTMX here is pretty simple. I simply hook into the button (this time I decided to NOT change the input on keyup / change the URL) and send the request when the button is clicked.
I Include the query in the request using `hx-include` and target the `#content` div to replace the results.

```razor
<div class="flex items-center gap-2 bg-neutral-500 bg-opacity-10 p-2 rounded-lg">
    <button
        hx-get="@Url.Action("SearchResults", "Search")"
        hx-target="#content"
        hx-include="[name='query']"
        hx-swap="outerHTML"
        class="btn btn-outline btn-sm flex items-center gap-2 text-black dark:text-white">
        Search
        <i class="bx bx-search text-lg"></i>
    </button>
    <input
        type="text"
        placeholder="Search..."
        value="@Model.Query"
        name="query"
        class="input input-sm border-0 grow text-black dark:text-white bg-transparent focus:outline-none"/>

</div>
```

# Update
So following some comments from [Khalid](https://khalidabuhakmeh.com/)  I decided to enhance this functionality to enable the search to be:
1. Triggered on Enter key press
2. Triggered on entering more than two characters (so becomes a loose typeahead).

In future I need to add the page size functionality back in; it's a biit of a hack and needs to support further requirements.

## Updated Search Form

To do this I first wrapped the input in a form and used Alpine.js to submit the form when a user is typing.
You can see that I use `x-data` to create a reactive variable for the query and then I check the length of the query to determine whether to submit the form.

```razor
<form
    x-data="{ query: '@Model.Query', checkSubmit() { if (this.query.length > 2) { $refs.searchButton.click(); } } }"
    class="flex items-center gap-2 bg-neutral-500 bg-opacity-10 p-2 rounded-lg"
    action="@Url.Action("Search", "Search")"
    hx-push-url="true"
    hx-boost="true"
    hx-target="#content"
    hx-swap="outerHTML show:window:top"
    hx-headers='{"pagerequest": "true"}'>
    <button
        type="submit"
        x-ref="searchButton"
        class="btn btn-outline btn-sm flex items-center gap-2 text-black dark:text-white">
        Search
        <i class="bx bx-search text-lg"></i>
    </button>
    <input
        type="text"
        placeholder="Search..."
        name="query"
        value="@Model.Query"
        x-model="query"
        x-on:input.debounce.200ms="checkSubmit"
        x-on:keydown.enter.prevent="$refs.searchButton.click()"
        class="input input-sm border-0 grow text-black dark:text-white bg-transparent focus:outline-none"
    />
</form>
```

In order to reuse the same controller action I also set the `pagerequest` header to indicate that this is a paginated request.

I also use the Alpine.js `x-on:keydown.enter.prevent` to trigger the button click when the Enter key is pressed and a debounce on the input to prevent too many requests.

## Controller Update
In the controller I removed the SearchResults action and instead added more 'intelligence' to the main `Search` action to handle both the initial search and the paginated requests.

Here you can see I add an extra parameter called `pagerequest` to determine whether this is a paginated request or not and indicate that this should be populated from the header collection.

```csharp

    [HttpGet]
    [Route("")]
    public async Task<IActionResult> Search(string? query, int page = 1, int pageSize = 10,[FromHeader] bool pagerequest=false)
    {
        var searchResults = await searchService.GetPosts(query, page, pageSize);
        var searchModel = new SearchResultsModel
        {
            Query = query,
            SearchResults = searchResults
        };
        searchModel = await PopulateBaseModel(searchModel);
        var linkUrl = Url.Action("Search", "Search");
        searchModel.SearchResults.LinkUrl = linkUrl;
        if(pagerequest && Request.IsHtmx()) return PartialView("_SearchResultsPartial", searchModel.SearchResults);
        
        if (Request.IsHtmx()) return PartialView("SearchResults", searchModel);
        return View("SearchResults", searchModel);
    }
```

I then get the results and detect this header to determine which view / partialview to return.

I further added a seperate retirect action to handle the likes of `/search/umami` to redirect to the main search page with the query.

```csharp
   [HttpGet]
    [Route("{query}")]
    public  IActionResult InitialSearch([FromRoute] string query)
    {
        return RedirectToAction("Search", new { query });
    }
```


# In Conclusion
So, pretty simple right? This is a straightforward implementation of a search page using HTMX and EF Core in an ASP.NET Core application. You can easily extend this to include more features like filtering, sorting, or even integrating with other search services. The key takeaway is how to leverage HTMX for a smooth user experience while keeping the backend logic clean and efficient. 