# Adding Paging with HTMX and ASP.NET Core with TagHelper

<!--category-- ASP.NET, HTMX -->
<datetime class="hidden">2024-08-09T12:50</datetime>

## Introduction
Now that I have a bunch of blog posts the home page was getting rather length so I decided to add a paging mechanism for blog posts. 

This goes along with adding full caching for blog posts to make this a quick and efficient site.

See the [Blog Service source](https://github.com/scottgal/mostlylucidweb/blob/main/Mostlylucid/Services/Markdown/MarkdownBlogService.cs) for how this is implemented; it's really pretty simple using the IMemoryCache.

[TOC]

### TagHelper
I decided to use a TagHelper to implement the paging mechanism. This is a great way to encapsulate the paging logic and make it reusable.
This uses the [pagination taghelper from Darrel O'Neill ](https://github.com/darrel-oneil/PaginationTagHelper) this is included in the project as a nuget package.

This is then added to the _ViewImports.cshtml file so it is available to all views.

```razor
@addTagHelper *,PaginationTagHelper.AspNetCore
```

### The TagHelper
In the _BlogSummaryList.cshtml partial view I added the following code to render the paging mechanism.

```razor
<pager link-url="@Model.LinkUrl"
       hx-boost="true"
       hx-push-url="true"
       hx-target="#content"
       hx-swap="show:none"
       page="@Model.Page"
       page-size="@Model.PageSize"
       total-items="@Model.TotalItems" ></pager>
```

A few notable things here:
1. `link-url` this allows the taghelper to generate the correct url for the paging links. In the HomeController Index method this is set to that action.
```csharp
   var posts = blogService.GetPostsForFiles(page, pageSize);
   posts.LinkUrl= Url.Action("Index", "Home");
   if (Request.IsHtmx())
   {
      return PartialView("_BlogSummaryList", posts);
   }
```
And in the Blog controller 
```csharp
    public IActionResult Index(int page = 1, int pageSize = 5)
    {
        var posts = blogService.GetPostsForFiles(page, pageSize);
        if(Request.IsHtmx())
        {
            return PartialView("_BlogSummaryList", posts);
        }
        posts.LinkUrl = Url.Action("Index", "Blog");
        return View("Index", posts);
    }
```

This is set to that URl. This ensures the pagination helper can work for either top level method. 

### HTMX Properties
`hx-boost`, `hx-push-url`, `hx-target`, `hx-swap` these are all HTMX properties that allow the paging to work with HTMX.
```razor
     hx-boost="true"
       hx-push-url="true"
       hx-target="#content"
       hx-swap="show:none"
```
Here we use `hx-boost="true"` this allows the pagination taghelper to not need any modifications by intercepting it's normal URL generation and using the current URL.

`hx-push-url="true"` to ensure the URL is swapped in the browser's URL history (which allows direct linking to pages).

`hx-target="#content"` this is the target div that will be replaced with the new content.

`hx-swap="show:none"` this is the swap effect that will be used when the content is replaced. In this case it prevents the normal 'jump' effect which HTMX uses on swapping content.

#### CSS
I also added styles to the main.css in my /src directory allowing me to use the Tailwind CSS classes for the pagination links.
```css
.pagination {
    @apply py-2 flex list-none p-0 m-0 justify-center items-center;
}

.page-item {
    @apply mx-1 text-black  dark:text-white rounded;
}

.page-item a {
    @apply block rounded-md transition duration-300 ease-in-out;
}

.page-item a:hover {
    @apply bg-blue-dark text-white;
}

.page-item.disabled a {
    @apply text-blue-dark pointer-events-none cursor-not-allowed;
}

```

### Controller
`page`, `page-size`, `total-items` are the properties that the pagination taghelper uses to generate the paging links.
These are passed into the partial view from the controller.
```csharp
 public IActionResult Index(int page = 1, int pageSize = 5)
```

### Blog Service
Here page and pageSize are passed in from the URL and the total items are calculated from the blog service.

```csharp
    public PostListViewModel GetPostsForFiles(int page=1, int pageSize=10)
    {
        var model = new PostListViewModel();
        var posts = GetPageCache().Values.Select(GetListModel).ToList();
        model.Posts = posts.OrderByDescending(x => x.PublishedDate).Skip((page - 1) * pageSize).Take(pageSize).ToList();
        model.TotalItems = posts.Count();
        model.PageSize = pageSize;
        model.Page = page;
        return model;
    }
```
 Here we simply get the posts from the cache, order them by date and then skip and take the correct number of posts for the page.

### Conclusion
This was a simple addition to the site but it makes it much more usable. The HTMX integration makes the site feel more responsive while not adding more JavaScript to the site.