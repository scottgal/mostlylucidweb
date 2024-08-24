# Htmx 与 Asp.Net 核心

<datetime class="hidden">2024-08-001T03:42</datetime>

<!--category-- ASP.NET, HTMX -->
## 一. 导言 导言 导言 导言 导言 导言 一,导言 导言 导言 导言 导言 导言

使用带有 ASP. NET Core 的 HTMX 是建立动态网络应用程序的绝佳方法, HTMX 允许您更新页面的部分内容, 无需重新加载整页, 使您的应用程序感觉更敏感和互动。

我用它来称为“杂交”的网页设计, 通过服务器端代码使页面完全更新, 然后用 HTMX 来动态更新页面的部分 。

在文章中,我教你如何在 ASP.NET核心应用中 开始使用HTMX

[技选委

## 先决条件

HTMX - Htmx 是一个 JavaScript 软件包, 将它包含在您的项目中的方式是使用 CDN 。 (见 [在这里](https://htmx.org/docs/#installing) )

```html
<script src="https://unpkg.com/htmx.org@2.0.1" integrity="sha384-QWGpdj554B4ETpJJC9z+ZHJcA/i59TyjxEPXiiUgN2WmTyV5OEZWCD6gQhgkdpB/" crossorigin="anonymous"></script>
```

您当然也可以下载副本, 并包含“ 手动” (或使用 Libman 或 npm ) 。

## ASP.NET比特项目

我建议安装 Htmx 标签辅助器 [在这里](https://github.com/khalidabuhakmeh/Htmx.Net)

这些都是来自美妙的 [哈立德·阿布哈克梅
](https://mastodon.social/@khalidabuhakmeh@mastodon.social)

```shell
dotnet add package Htmx.TagHelpers
```

Htmx Giget 软件包来自 [在这里](https://www.nuget.org/packages/Htmx/)

```shell
 dotnet add package Htmx
```

标签助手允许您这样做 :

```razor
    <a hx-controller="Blog" hx-action="Show" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-slug="@Model.Slug"
        class="block font-body text-lg font-semibold text-primary transition-colors hover:text-green dark:text-white dark:hover:text-secondary">@Model.Title</a>
```

### 替代办法。

**注:这一方法有一个重大缺点;它不会产生员额链接的提示。 这对平等就业机会和无障碍环境是一个问题。 这也意味着如果HTMX因某种原因不装载, 这些链接就会失效( CDNs DO会下降 ) 。**

另一种做法是使用 ` hx-boost="true"` 和普通的 asp. net 核心标记助手 。 见见  [在这里](https://htmx.org/docs/#hx-boost) 更多关于hx-bust的信息(虽然医生人数少了一点) 。
这将输出一个正常的 href, 但将被 HTMX 拦截, 并动态装入内容 。

兹修改如下:

```razor
 <a asp-controller="Blog" asp-action="Show" hx-boost="true"  hx-target="#contentcontainer" asp-route-slug="@Model.Slug"
       class="block font-body text-lg font-semibold text-primary transition-colors hover:text-green dark:text-white dark:hover:text-secondary">@Model.Title</a>
```

### 部分部分部分

HTMX以部分观点运作良好。 您可以使用 HTMX 将部分视图装入页面上的容器。 这是伟大的, 用于在不重新装入整页页面的情况下, 动态地装入您页面的部分内容 。

在此应用程序中, 我们在布局. cshtml 文件中有一个容器, 我们要装入部分视图 。

```razor
    <div class="container mx-auto" id="contentcontainer">
   @RenderBody()

    </div>
```

通常它能提供服务器的侧侧内容, 但使用 HTMX 标签助手来帮助您, 可以看到我们的目标 `hx-target="#contentcontainer"` 将部分视图装入容器。

BlogView的片面观点是, 我们想把货箱装进货箱。

![img.png](project.png)

然后在博客管理员里,我们有了

```csharp
    [Route("{slug}")]
    [OutputCache(Duration = 3600)]
    public IActionResult Show(string slug)
    {
       var post =  blogService.GetPost(slug);
       if(Request.IsHtmx())
       {
              return PartialView("_PostPartial", post);
       }
       return View("Post", post);
    }
```

您可以在这里看到我们有 HTMX 请求 。 IsHtmx () 方法, 如果请求是 HTMX 请求, 这将返回为真 。 如果我们回过头来是部分观点,如果不是,我们回过头来是全部观点。

利用这一点,我们就能确保我们也支持直接询问,但很少作出实际努力。

在此情况下,我们充分的看法是部分提及:

```razor
@model Mostlylucid.Models.Blog.BlogPostViewModel

@{
ViewBag.Title = "title";
Layout = "_Layout";
}
<partial name="_PostPartial" model="Model"/>
```

所以我们现在有一个超级简单的方法 用HTMX 将部分视图装入我们的页面。