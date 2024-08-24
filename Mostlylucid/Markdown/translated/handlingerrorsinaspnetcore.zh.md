# ASP.NET核心的处理(未处理)错误

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-17-002:00</datetime>

## 一. 导言 导言 导言 导言 导言 导言 一,导言 导言 导言 导言 导言 导言

在任何网络应用程序中,必须优雅地处理错误。 在生产环境中尤其如此,在生产环境中,你想提供良好的用户经验,而不要披露任何敏感信息。 我们将研究如何处理ASP.NET核心应用程序中的错误。

[技选委

## 问题

当 ASP. NET 核心应用程序出现未处理的例外时,默认行为是返回通用错误页面, 状态代码为 500 。 这一点并不理想,原因如下:

1. 这是丑陋的,没有提供 一个良好的用户经验。
2. 它不为用户提供任何有用的信息。
3. 通常很难调试这个问题,因为错误信息太通用。
4. 这是丑陋的; 通用浏览器错误页面只是一个带文字的灰色屏幕 。

## 解决方案

在ASP.NET核心,有一个整齐的功能结构 使我们能够处理这些错误。

```csharp
app.UseStatusCodePagesWithReExecute("/error/{0}");
```

我们把这个放进我们的 `Program.cs` 在管道中的早期文件 。 任何不是200的状态代码 转到 `/error` 以状态代码为参数的路径。

我们的错误控制器会看起来是这样的:

```csharp
    [Route("/error/{statusCode}")]
    public IActionResult HandleError(int statusCode)
    {
        // Retrieve the original request information
        var statusCodeReExecuteFeature = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
        
        if (statusCodeReExecuteFeature != null)
        {
            // Access the original path and query string that caused the error
            var originalPath = statusCodeReExecuteFeature.OriginalPath;
            var originalQueryString = statusCodeReExecuteFeature.OriginalQueryString;

            
            // Optionally log the original URL or pass it to the view
            ViewData["OriginalUrl"] = $"{originalPath}{originalQueryString}";
        }

        // Handle specific status codes and return corresponding views
        switch (statusCode)
        {
            case 404:
            return View("NotFound");
            case 500:
            return View("ServerError");
            default:
            return View("Error");
        }
    }
```

此控制器将处理错误并返回基于状态代码的自定义视图 。 我们还可以登录造成错误的原始 URL, 并将其传送到视图中 。
如果我们有一个中央记录/分析服务, 我们可以将错误登录到该服务。

我们的意见如下:

```razor
<h1>404 - Page not found</h1>

<p>Sorry that Url doesn't look valid</p>
@section Scripts {
    <script>
            document.addEventListener('DOMContentLoaded', function () {
                if (!window.hasTracked) {
                    umami.track('404', { page:'@ViewData["OriginalUrl"]'});
                    window.hasTracked = true;
                }
            });

    </script>
}
```

很简单吧? 我们还可以登录错误到一个记录服务, 如应用程序 Insights 或 Serilog 。 这样我们就可以追踪错误 并在错误成为问题之前纠正错误
以我们为例,我们将此记录为Umami分析服务的一个事件。 这样我们就可以追踪我们有多少404个错误 以及它们来自何方。

这也使您的页面保持与您所选择的布局和设计相一致。

![404页 404页](new404.png)

## 在结论结论中

这是处理 ASP. NET 核心应用程序错误的简单方法 。 它提供了良好的用户经验,使我们能够追踪错误。 将错误登录到一个日志服务处是一个好主意, 这样你就可以在错误成为问题之前 追踪和修正错误。