# Adding Entity Framework for Blog Posts (Part 3)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-16T18:00</datetime>

You can find all the source code for the blog posts on [GitHub](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Blog)

**Parts 1 & 2 of the series on adding Entity Framework to a .NET Core project.**

Part 1 can be found [here](/blog/addingentityframeworkforblogpostspt1).

Part 2 can be found [here](/blog/addingentityframeworkforblogpostspt2).

## Introduction
In the previous parts we set up the database and the context for our blog posts, and added the services to interact with the database. In this post, we will detail how these services now work with the existing controllers and views.

[TOC]

## Controllers
Out controllers for Blogs are really pretty simple; in line with avoiding the 'Fat Controller' antipattern (a pattern we ideintified early in the ASP.NET MVC days).

### The Fat Controller pattern in ASP.NET MVC
I MVC frameworks a good practice is to do as little as possible in your controller methods. This is because the controller is responsible for handling the request and returning a response. It should not be responsible for the business logic of the application. This is the responsibility of the model.

The 'Fat Controller' antipattern is where the controller does too much. This can lead to a number of problems, including:
1. Duplication of code in multiple Actions: 
An  action should  be a single unit of work, simply populating the model and returning the view. If you find yourself repeating code in multiple actions, it is a sign that you should refactor this code into a separate method.
2. Code that is difficult to test: 
By having 'fat controllers' you may be making it difficult to test the code. Testing should attempt to follow all the possible paths through the code, and this can be difficult if the code is not well-structured and focused on a single responsibility.
3. Code that is difficult to maintain:
Maintainability is a key concern when building applications. Having 'kitchen sink' action methods can easily lead to you as well as other developers using the code to make changes that break other parts of the application.
4. Code that is difficult to understand:
This is a key concern for developers. If you are working on a project with a large codebase, it can be difficult to understand what is happening in a controller action if it is doing too much.

### The Blog Controller
The blog controller is relatively simple. It has 4 main actions (and one 'compat action' for the old blog links). These are:

```csharp
Task<IActionResult> Index(int page = 1, int pageSize = 5)

Task<IActionResult> Show(string slug, string language = BaseService.EnglishLanguage)

Task<IActionResult> Category(string category, int page = 1, int pageSize = 5)

Task<IActionResult> Language(string slug, string language)

IActionResult Compat(string slug, string language)
```

In turn these actions call the `IBlogService` to get the data they need. The `IBlogService` is detailed in the [previous post](/blog/addingentityframeworkforblogpostspt2).

In turn these actions are as follows

- Index: This is the list of blog posts (defaults to English Language; we may extend this later to allow for multiple languages). You'll see it takes `page` and `pageSize` as parameters. This is for pagination. of  the results.
- Show: This is the individual blog post. It takes the `slug` of the post and the `language` as parameters. THis is the method you're currently using for reading this blog post.
- Category: This is the list of blog posts for a given category. It takes the `category`, `page` and `pageSize` as parameters.
- Language: This shows a blog post for a given language. It takes the `slug` and `language` as parameters.
- Compat: This is a compatibilty action for the old blog links. It takes the `slug` and `language` as parameters.

### Caching
As mentioned in an [earlier post](https://www.mostlylucid.net/blog/aspnetcachingwithhtmx) we implement `OutputCache` and `ResponseCahce` to cache the results of the blog posts. This improves the user experience and reduces the load on the server.

These are implemented using the appropriate Action decorators which specify the parameters used for the Action (as well as `hx-request` for HTMX requests). For exampel with `Index` we specify these:
```csharp
    [ResponseCache(Duration = 300, VaryByHeader  = "hx-request", VaryByQueryKeys = new[] {nameof(page), nameof(pageSize)}, Location = ResponseCacheLocation.Any)]
    [OutputCache(Duration = 3600, VaryByHeaderNames = new[] {"hx-request"} ,VaryByQueryKeys = new[] { nameof(page), nameof(pageSize)})]
```

## Views
The views for the blog are relatively simple. They are mostly just a list of blog posts, with a few details for each post. The views are in the `Views/Blog` folder. The main views are:

### `_PostPartial.cshtml`
This is the partial view for a single blog post. It is used within our `Post.cshtml` view.

```razor
@model Mostlylucid.Models.Blog.BlogPostViewModel

@{
    Layout = "_Layout";
}
<partial name="_PostPartial" model="Model"/>
```

### `_BlogSummaryList.cshtml`
This is the partial view for a list of blog posts. It is used within our `Index.cshtml` view as well as in the homepage.

```razor
@model Mostlylucid.Models.Blog.PostListViewModel
<div class="pt-2" id="content">

    @if (Model.TotalItems > Model.PageSize)
    {
        <pager
            x-ref="pager"
            link-url="@Model.LinkUrl"
               hx-boost="true"
               hx-push-url="true"
               hx-target="#content"
               hx-swap="show:none"
               page="@Model.Page"
               page-size="@Model.PageSize"
               total-items="@Model.TotalItems"
            class="w-full"></pager>
    }
    @if(ViewBag.Categories != null)
{
    <div class="pb-3">
        <h4 class="font-body text-lg text-primary dark:text-white">Categories</h4>
        <div class="flex flex-wrap gap-2 pt-2">
            @foreach (var category in ViewBag.Categories)
            {
                <a hx-controller="Blog" hx-action="Category" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-category="@category" href>
                    <span class="inline-block rounded-full dark bg-blue-dark px-2 py-1 font-body text-sm text-white outline-1 outline outline-green-dark dark:outline-white">@category</span>
                </a>
            }
        </div>
    </div>
}
@foreach (var post in Model.Posts)
{
    <partial name="_ListPost" model="post"/>
}
</div>
```
This uses the `_ListPost` partial view to display the individual blog posts along with the [paging tag helper](/blog/addpagingwithhtmx) which allows us to page the blog posts.

### `_ListPost.cshtml`
The _Listpost partial view is used to display the individual blog posts in the list. It is used within the `_BlogSummaryList` view.

```razor
@model Mostlylucid.Models.Blog.PostListModel

<div class="border-b border-grey-lighter pb-8 mb-8">
 
    <a asp-controller="Blog" asp-action="Show" hx-boost="true"  hx-swap="show:window:top"  hx-target="#contentcontainer" asp-route-slug="@Model.Slug"
       class="block font-body text-lg font-semibold transition-colors hover:text-green text-blue-dark dark:text-white  dark:hover:text-secondary">@Model.Title</a>
    <div class="flex space-x-2 items-center py-4">
    @foreach (var category in Model.Categories)
    {
    <a hx-controller="Blog" hx-action="Category" class="rounded-full bg-blue-dark font-body text-sm text-white px-2 py-1 outline outline-1 outline-white" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-category="@category" href>@category
    </a>
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
As you'll se here we have a link to the individual blog post, the categories for the post, the languages the post is available in, the summary of the post, the published date and the reading time.

We also have HTMX link tags for the categories and the languages to allow us to display the localized posts and the posts for a given category.

We have two ways of using HTMX here, one which gives the full URL and one which is 'HTML only' (i.e. no URL). This is because we want to use the full URL for the categories and the languages, but we don't need the full URL for the individual blog post.

```razor
 <a asp-controller="Blog" asp-action="Show" hx-boost="true"  hx-swap="show:window:top"  hx-target="#contentcontainer" asp-route-slug="@Model.Slug"
 ```
This approach populates a full URL for the individual blog post and uses `hx-boost` to 'boost' the request to use HTMX (this sets the `hx-request` header to `true`).

```razor
  <a hx-controller="Blog" hx-action="Category" class="rounded-full bg-blue-dark font-body text-sm text-white px-2 py-1 outline outline-1 outline-white" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-category="@category" href>@category
    </a>
```
Alternatively this approach uses the HTMX tags to get the categories for the blog posts. This uses the `hx-controller`, `hx-action`, `hx-push-url`, `hx-get`, `hx-target` and `hx-route-category` tags to get the categories for the blog posts while `hx-push-url` is set to `true` to push the URL to the browser history. 


It is also used within our `Index` Action method for the HTMX requests.

```csharp
  public async Task<IActionResult> Index(int page = 1, int pageSize = 5)
    {
        var posts =await  blogService.GetPagedPosts(page, pageSize);
        if(Request.IsHtmx())
        {
            return PartialView("_BlogSummaryList", posts);
        }
        posts.LinkUrl = Url.Action("Index", "Blog");
        return View("Index", posts);
    }
```
Where it enables us to either return the full view or just the partial view for HTMX requests, giving a 'SPA' like experience.

## Home Page
In the `HomeController` we also refer to these blog services to get the latest blog posts for the home page. This is done in the `Index` action method.

```csharp
   public async Task<IActionResult> Index(int page = 1,int pageSize = 5)
    {
            var authenticateResult = GetUserInfo();
            var posts =await blogService.GetPagedPosts(page, pageSize);
            posts.LinkUrl= Url.Action("Index", "Home");
            if (Request.IsHtmx())
            {
                return PartialView("_BlogSummaryList", posts);
            }
            var indexPageViewModel = new IndexPageViewModel
            {
                Posts = posts, Authenticated = authenticateResult.LoggedIn, Name = authenticateResult.Name,
                AvatarUrl = authenticateResult.AvatarUrl
            };
            
            return View(indexPageViewModel);
    }
```
As you'll see in here we use the `IBlogService` to get the latest blog posts for the home page. We also use the `GetUserInfo` method to get the user information for the home page.

Again this has an HTMX request to return the partial view for the blog posts to allow us to page the blog posts in the home page.

## In Conclusion
In our next part we'll go into excruciating detail of how we use the `IMarkdownBlogService` to populate the database with the blog posts from the markdown files. This is a key part of the application as it allows us to use the markdown files to populate the database with the blog posts.