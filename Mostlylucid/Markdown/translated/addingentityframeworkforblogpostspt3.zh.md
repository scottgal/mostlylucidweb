# 添加博客文章实体框架(第3部分)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-16T18:00</datetime>

您可以在博客文章中找到所有源代码 [吉特胡布](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Blog)

**关于将实体框架添加到.NET核心项目的系列第1和第2部分。**

第一部分可以找到 [在这里](/blog/addingentityframeworkforblogpostspt1).

第2部分可以找到 [在这里](/blog/addingentityframeworkforblogpostspt2).

## 一. 导言 导言 导言 导言 导言 导言 一,导言 导言 导言 导言 导言 导言

在前几部分中,我们建立了数据库和博客文章的背景,并增加了与数据库互动的服务。 在这个职位上,我们将详细说明这些服务目前如何与现有的控制者和观点合作。

[技选委

## 主计长

博客的外部控制器其实很简单;

### ASP.NET MVC中的胖子主计长模式

监查会框架是一个良好的做法,就是在控制器方法方面尽量少做一些工作。 这是因为控制器负责处理请求并回复回复。 它不应对申请的商业逻辑负责。 这是模型的责任。

控制器做太多事的地方是“Fat Concorder”的抗模式。 这可能导致若干问题,包括:

1. 多个动作中代码的重复 :
   一项行动应当是一个单一的工作单位,只需充实模型和回溯观点即可。 如果您发现自己在多个动作中重复代码, 这是一个信号, 您应该将该代码重构成一个单独的方法 。
2. 难以检验的编码:
   有了"脂肪控制器" 你可能很难测试密码 测试应努力遵循守则中的所有可能途径,如果守则结构不完善,侧重于单一责任,则可能难以做到。
3. 难以维持的法典:
   在开发应用软件时,可维持性是一个关键问题。 拥有“厨房下水道”的操作方法很容易导致您和其他开发商使用代码进行改变,从而打破应用程序的其他部分。
4. 难以理解的守则:
   这是开发商关注的一个关键问题。 如果您在一个拥有大代码库的项目中工作, 很难理解控制器操作如果做太多, 将会发生什么 。

### 博客控制器

博客控制器相对简单。 它有4个主要行动(以及旧博客链接的“兼容行动”)。 它们是:

```csharp
Task<IActionResult> Index(int page = 1, int pageSize = 5)

Task<IActionResult> Show(string slug, string language = BaseService.EnglishLanguage)

Task<IActionResult> Category(string category, int page = 1, int pageSize = 5)

Task<IActionResult> Language(string slug, string language)

IActionResult Compat(string slug, string language)
```

反过来,这些行动又称为: `IBlogService` 以获得他们需要的数据。 缩略 `IBlogService` 详细内容见 [上一个职位](/blog/addingentityframeworkforblogpostspt2).

这些行动如下:

- 索引:这是博客文章列表(对英语的默认值;我们稍后可以扩大,以便允许多种语言)。 你会看到它需要 `page` 和 `pageSize` 作为参数。 这是用来排练的 结果。
- 显示:这是个人博客文章。 它需要 `slug` 员额、职位和职位 `language` 作为参数。 THS是您目前在阅读此博客文章时使用的方法。
- 类别: 这是特定类别的博客文章列表 。 它需要 `category`, `page` 和 `pageSize` 作为参数。
- 语言: 这里显示特定语言的博客文章 。 它需要 `slug` 和 `language` 作为参数。
- compat:这是对旧博客链接的折中行动。 它需要 `slug` 和 `language` 作为参数。

### 缓缓

(a) 如在(a)和(b)项中提及 [上一个职位](https://www.mostlylucid.net/blog/aspnetcachingwithhtmx) 我们执行 `OutputCache` 和 `ResponseCahce` 以隐藏博客文章的结果。 这样可以改善用户体验,减少服务器的负载。

使用适当的行动装饰器来实施这些措施,这些装饰器说明行动所使用的参数(以及 `hx-request` 申请HTMX)。 用于考试 `Index` 我们具体规定如下:

```csharp
    [ResponseCache(Duration = 300, VaryByHeader  = "hx-request", VaryByQueryKeys = new[] {nameof(page), nameof(pageSize)}, Location = ResponseCacheLocation.Any)]
    [OutputCache(Duration = 3600, VaryByHeaderNames = new[] {"hx-request"} ,VaryByQueryKeys = new[] { nameof(page), nameof(pageSize)})]
```

## 意见意见

博客的观点比较简单。 他们大多只是博客文章的列表, 意见载于《意见汇编》和《意见汇编》中。 `Views/Blog` 文件夹。 主要观点是:

### `_PostPartial.cshtml`

这是对单一博客文章的部分看法。 它在我们的 `Post.cshtml` 视图。

```razor
@model Mostlylucid.Models.Blog.BlogPostViewModel

@{
    Layout = "_Layout";
}
<partial name="_PostPartial" model="Model"/>
```

### `_BlogSummaryList.cshtml`

这是对博客文章列表的部分看法。 它在我们的 `Index.cshtml` 视图和主页。

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

此处使用 `_ListPost` 部分显示个人博客文章及 [呼叫标签助手](/blog/addpagingwithhtmx) 这让我们可以上传博客文章。

### `_ListPost.cshtml`

缩略 _使用列表部分视图来显示列表中的个别博客文章。 用于 `_BlogSummaryList` 视图。

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

与个人博客文章、该文章的分类、该文章的语文、该文章的摘要、出版日期及阅读时间等有链接。

我们还为这些类别和语言设置了HTMX链接标签,使我们能够显示某一类别的地方化职位和职位。

我们这里有两种使用HTMX的方法,一种是提供完整的 URL,另一种是“HTML”(即“HTML”) 没有 URL) 。 因为我们想使用完整的 URL 用于分类和语言, 但我们不需要完整的 URL 用于个人博客文章 。

```razor
 <a asp-controller="Blog" asp-action="Show" hx-boost="true"  hx-swap="show:window:top"  hx-target="#contentcontainer" asp-route-slug="@Model.Slug"
```

此方法为单个博客日志和使用提供一个完整的 URL 。 `hx-boost` 启动“ 启动” 请求使用 HTMX (此设置 `hx-request` 标题到 `true`).

```razor
  <a hx-controller="Blog" hx-action="Category" class="rounded-full bg-blue-dark font-body text-sm text-white px-2 py-1 outline outline-1 outline-white" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-category="@category" href>@category
    </a>
```

使用HTMX标签来获得博客文章的分类。 此处使用 `hx-controller`, `hx-action`, `hx-push-url`, `hx-get`, `hx-target` 和 `hx-route-category` 标签,以获得博客文章的分类 `hx-push-url` 设置为 `true` 将 URL 推到浏览器历史中。

也用于我们 `Index` HTMX要求的行动方法。

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

当它能让我们对HTMX的要求回馈全景或只是部分观点时,

## 主网页首页主页

在 `HomeController` 我们亦提及这些博客服务, 以获得主页的最新博客文章。 这是在 `Index` 动作方法。

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

正如你们在这里看到的,我们使用 `IBlogService` 以获得主页的最新博客文章。 我们也使用 `GetUserInfo` 获取主页用户信息的方法。

HTMX再次要求回复部分博客文章的观点, 以便我们可以在主页上张贴博客文章。

## 在结论结论中

在接下来的下一部分 我们将细细细地讲述我们如何使用 `IMarkdownBlogService` 以从标记文件中以博客文章填充数据库。 这是应用程序的一个关键部分, 因为它允许我们使用标记文件, 用博客文章填充数据库 。