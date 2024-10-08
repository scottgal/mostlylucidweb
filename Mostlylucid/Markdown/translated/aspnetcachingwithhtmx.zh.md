# ASP.NET 与HTMX的核心缓冲

<!--category-- ASP.NET, HTMX -->
<datetime class="hidden">2024-008-12T00:50</datetime>

## 一. 导言 导言 导言 导言 导言 导言 一,导言 导言 导言 导言 导言 导言

缓存是一种重要技术,既可以通过更快地装载内容来改进用户经验,又可以减少服务器上的负担。 在本篇文章中,我将教你如何使用 ASP.NET Core 和 HTMX 的内置缓存功能 来隐藏客户端的内容。

[技选委

## 设置设置设置设置设置设置设置

在ASP.NET核心部分,提供两类Caching

- Reponse Cache - 这是在客户端或中间 procy 服务器(或两者兼备)上隐藏的数据, 用于缓存对请求的全部回复 。
- 输出缓存 - 这是在服务器上缓存并用于缓存控制器动作输出的数据 。

要在 ASP.NET Core 中设置这些功能, 您需要在您的 ASP. NET Core 中添加一些服务 `Program.cs` 文件文件

### 反应缓存

```csharp
builder.Services.AddResponseCaching();
app.UseResponseCaching();
```

### 输出缓存

```csharp
services.AddOutputCache();
app.UseOutputCache();
```

## 反应缓存

同时也有可能在您的 `Program.cs` 通常有点不灵活(尤其是我发现使用HTMX要求时)。 您可以在您的控制器动作中,通过使用 `ResponseCache` 属性。

```csharp
        [ResponseCache(Duration = 300, VaryByHeader = "hx-request", VaryByQueryKeys = new[] {"page", "pageSize"}, Location = ResponseCacheLocation.Any)]
```

这将缓存响应300秒, 并更改缓存 。 `hx-request` 页眉和页眉 `page` 和 `pageSize` 查询参数。 我们还在设置 `Location` 至 `Any` 这意味着反应可以隐藏在客户、中间代理服务器或两者上。

在这里 `hx-request` 标题是 HTMX 随每项请求发送的页眉 。 这很重要, 因为它允许您根据 HTMX 请求或普通请求 以不同的方式隐藏响应 。

这是我们的现在 `Index` 动作方法。 Youcan 查看我们是否接受页面和页面 `ResponseCache` 属性。 这意味着这些密钥将回应“索引”并储存基于这些密钥的不同内容。

我们也在《行动行动》中 `if(Request.IsHtmx())` 这是基于 [HTMX.Net 软件包](https://github.com/khalidabuhakmeh/Htmx.Net)  并进行基本核查,以核查 `hx-request` 我们用来更改缓存的页头 。 在此,如果HTMX提出要求,我们只提出部分意见。

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

## 输出缓存

输出缓存是服务器的侧面, 相当于响应缓存 。 它会缓存控制器动作的输出 。 从本质上说,网络服务器储存请求的结果,并供日后请求使用。

```csharp
       [OutputCache(Duration = 3600,VaryByHeaderNames = new[] {"hx-request"},VaryByQueryKeys = new[] {"page", "pageSize"})]
```

在这里,我们正在缓存控制器动作的输出 3600秒 并更改缓存 `hx-request` 页眉和页眉 `page` 和 `pageSize` 查询参数。
当我们将数据服务器的侧面储存相当长的时间( 日志仅以 docker 推来更新) 时, 时间定得比回应缓存要长; 对我们的情况来说, 它实际上可能是无限的, 但是 3600 秒是一个很好的折中方案 。

和"反应缓存"一样 我们用的是 `hx-request` 标题根据请求是否来自 HTMX 来更改缓存 。

## 静静文件

ASP.NET核心还内置支持缓存静态文件。 这样做的方式是确定 `Cache-Control` 回复页头 。 你可以把这个设置在你的 `Program.cs` 文件。
请注意,这里的顺序非常重要。 如果您的静态文件需要授权支持, 您应该移动 `UseAuthorization` 之前的中继器件 `UseStaticFiles` 中间软件。 TH 使用HttpsRedirection 中间软件也应该在使用静态文件的中间软件之前, 如果您依赖此特性 。

```csharp
app.UseHttpsRedirection();
var cacheMaxAgeOneWeek = (60 * 60 * 24 * 7).ToString();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append(
            "Cache-Control", $"public, max-age={cacheMaxAgeOneWeek}");
    }
});
app.UseRouting();
app.UseCors("AllowMostlylucid");
app.UseAuthentication();
app.UseAuthorization();
```

## 结论 结论 结论 结论 结论

Caching是提高您应用程序性能的有力工具。 使用 ASP.NET Core 的内置缓存功能, 您可以很容易地在客户端或服务器端缓存内容 。 通过使用 HTMX, 您可以在客户端隐藏内容, 并提供部分视图来改善用户体验 。