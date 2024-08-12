# 添加 HTMX 和 ASP.NET 核心与标签求助器的呼叫

<!--category-- ASP.NET, HTMX -->
<datetime class="hidden">2024-08-09T12:50</datetime>

## 一. 导言 导言 导言 导言 导言 导言 一,导言 导言 导言 导言 导言 导言

现在,我有一堆博客文章, 主页越来越长, 所以我决定添加博客文章的传呼机制。

并增加博客文章的完整封存, 使这个网站迅速高效,

见[博客服务源](https://github.com/scottgal/mostlylucidweb/blob/main/Mostlylucid/Services/Markdown/MarkdownBlogService.cs)使用 IMemoryCache 执行此操作的方法; 使用 IMemoryCache 的操作非常简单 。

[技选委

### 标签辅助工具

我决定使用“标签帮助”来实施传呼机制。这是封装传呼逻辑并使其重新使用的绝佳方法。
此处使用[来自Darrel O'Neill的抛射标记助手](https://github.com/darrel-oneil/PaginationTagHelper)包含在项目中,作为金字塔包。

然后将此添加到_查看imports.cshtml 文件, 以便所有视图都可用 。

```razor
@addTagHelper *,PaginationTagHelper.AspNetCore
```

### 标签帮助者

在_BlogSummaryList. cshtml 部分观点我添加了以下代码,

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

以下是一些值得注意的事情:

1. `link-url`此选项允许标签助手为调用链接生成正确的 URL 。 在“ 主控器索引” 方法中, 此选项设定为此动作 。

```csharp
   var posts = blogService.GetPostsForFiles(page, pageSize);
   posts.LinkUrl= Url.Action("Index", "Home");
   if (Request.IsHtmx())
   {
      return PartialView("_BlogSummaryList", posts);
   }
```

在博客控制器中

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

此设置为 URL 。 这样可以确保 月球助手可以使用 任何顶级方法 。

### HTMX 属性

`hx-boost`, `hx-push-url`, `hx-target`, `hx-swap`这些都是 HTMX 特性, 使该呼叫能够使用 HTMX 工作 。

```razor
     hx-boost="true"
       hx-push-url="true"
       hx-target="#content"
       hx-swap="show:none"
```

我们在这里使用`hx-boost="true"`这样可以让页码标记助手通过拦截正常的 URL 生成和使用当前 URL 来不需修改 。

`hx-push-url="true"`以确保在浏览器的 URL 历史中将 URL 替换( 它允许直接链接到页面) 。

`hx-target="#content"`这是将替换为新内容的目标 div 。

`hx-swap="show:none"`这是当内容被替换时将使用的互换效果。 在这种情况下, 它会防止HTMX在互换内容时使用的正常“ 跳” 效果 。

#### CSS 安保部

我还在我的/弧目录的主. cs 中添加了样式, 允许我使用尾风 CSS 课程进行页码链接 。

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

### 主计长

`page`, `page-size`, `total-items`是页码标记助手用来生成传呼链接的属性。
这些通过控制器的部分视图。

```csharp
 public IActionResult Index(int page = 1, int pageSize = 5)
```

### 博客服务

这里的页面和页面 Size 是从 URL 中传送的, 全部项目是从博客服务中计算出来的 。

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

在这里,我们只需从缓存处获取柱子,按日期订购,然后跳过,取出页面的正确页数。

### 结论 结论 结论 结论 结论

这是对网站的简单补充,但它使网站更便于使用。 HTMX集成使网站感到反应更灵敏,同时又不给网站添加更多的JavaScript。