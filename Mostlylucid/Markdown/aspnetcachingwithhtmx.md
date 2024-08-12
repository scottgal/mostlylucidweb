# ASP.NET Core Caching with HTMX

<!--category-- ASP.NET, HTMX -->
<datetime class="hidden">2024-08-12T00:50</datetime>

## Introduction
Caching is an important technique to both improve user experience by loading content faster and to reduce the load on your server. In this article I'll show you how to use the built-in caching features of ASP.NET Core with HTMX to cache content on the client side.

[TOC]
## Setup
In ASP.NET Core, there are two types of Caching offered 
- Reponse Cache - This is data which is cached on the client or in intermediary procy servers (or both) and is used to cache the entire response for a request. 
- Output Cache - This is data which is cached on the server and is used to cache the output of a controller action.

To set these up in ASP.NET Core you need to add a couple of services in your `Program.cs` file

### Response Caching
```csharp
builder.Services.AddResponseCaching();
app.UseResponseCaching();
```

### Output Caching
```csharp
services.AddOutputCache();
app.UseOutputCache();
```

## Response Caching
While it is possible to set up Response Caching in your `Program.cs` it's often a bit inflexible (especially when using HTMX requests as I discovered). You can set up Response Caching in your controller actions by using the `ResponseCache` attribute.

```csharp
        [ResponseCache(Duration = 300, VaryByHeader = "hx-request", VaryByQueryKeys = new[] {"page", "pageSize"}, Location = ResponseCacheLocation.Any)]
```

This will cache the response for 300 seconds and vary the cache by the `hx-request` header and the `page` and `pageSize` query parameters. We're also setting the `Location` to `Any` which means that the response can be cached on the client, on intermediary proxy servers, or both.

Here the `hx-request` header is the header that HTMX sends with each request. This is important as it allows you to cache the response differently based on whether it's an HTMX request or a normal request.

This is our current `Index` action method. Yo ucan see that we accept a page and pageSize parameter here and we added these as varyby query keys in the `ResponseCache` attribute. Meaning that responses are 'indexed' by these keys and store different content based on these.

In out Action we also have `if(Request.IsHtmx())` this is based on the [HTMX.Net package](https://github.com/khalidabuhakmeh/Htmx.Net)  and essentially checks for the same `hx-request` header that we're using to vary the cache. Here we return a partial view if the request is from HTMX.


```csharp
    public async Task<IActionResult> Index(int page = 1,int pageSize = 5)
    {
            var authenticateResult = GetUserInfo();
            var posts =await blogService.GetPosts(page, pageSize);
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

## Output Caching
Output Caching is the server side equivalent of Response Caching. It caches the output of a controller action. In essence the web server stores the result of a request and serves it up for subsequent requests.

```csharp
       [OutputCache(Duration = 3600,VaryByHeaderNames = new[] {"hx-request"},VaryByQueryKeys = new[] {"page", "pageSize"})]
```

Here we're caching the output of the controller action for 3600 seconds and varying the cache by the `hx-request` header and the `page` and `pageSize` query parameters.
As we're storing data server side for a significant time (the posts only updating with a docker push) this is set to longer than the Response Cache; it could actually be infinite in our case but 3600 seconds is a good compromise.

As with the Response Cache we're using the `hx-request` header to vary the cache based on whether the request is from HTMX or not.

## Conclusion
Caching is a powerful tool to improve the performance of your application. By using the built-in caching features of ASP.NET Core you can easily cache content on the client or server side. By using HTMX you can cache content on the client side and serve up partial views to improve the user experience.