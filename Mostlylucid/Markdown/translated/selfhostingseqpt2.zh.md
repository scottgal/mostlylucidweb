# Seq for ASP.NET测网-与SerilogTtract一起进行追踪

<datetime class="hidden">2024-08-331T11:20</datetime>

<!--category-- ASP.NET, Seq, Serilog -->
# 一. 导言 导言 导言 导言 导言 导言 一,导言 导言 导言 导言 导言 导言

在前一部分,我教你如何设置 [使用 ASP.NET 核心的 Seq 自定义主机 ](/blog/selfhostingseq).. 利用我们新的Seq实例进行更完整的采伐和追踪。

[技选委

# 追踪追踪

追踪就像记录++ 一样, 它会给你额外的一层信息 来了解您应用程序中发生的事情。 它特别有用 当你有一个分布式系统 你需要通过多种服务 追踪一个请求
在这个网站里,我用它来快速追踪问题; 仅仅因为这是一个业余爱好网站, 并不意味着我放弃专业标准。

## 建立Serilog

使用 Serilog 设置跟踪功能非常简单 [血清追踪](https://github.com/serilog-tracing/serilog-tracing) 软件包。 首先,您需要安装软件包 :

在这里,我们还要加上控制台水槽和Seq水槽

```bash
dotnet add package SerilogTracing
dotnet add package SerilogTracing.Expressions
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.Seq
```

控制台对调试总是有用的 而Seq是我们来这里的目的 Seq 也有一系列“ 富人” 功能, 可以在日志中添加额外信息 。

```bash
  "Serilog": {
    "Enrich": ["FromLogContext", "WithThreadId", "WithThreadName", "WithProcessId", "WithProcessName", "FromLogContext"],
    "MinimumLevel": "Warning",
    "WriteTo": [
        {
          "Name": "Seq",
          "Args":
          {
            "serverUrl": "http://seq:5341",
            "apiKey": ""
          }
        },
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/applog-.txt",
          "rollingInterval": "Day"
        }
      }

    ],
    "Properties": {
      "ApplicationName": "mostlylucid"
    }
  }
```

要使用这些浓缩器, 您需要将这些添加到您的 `Serilog` 在您的配置中 `appsettings.json` 文件。 您也需要安装所有使用 Nuget 的分离浓缩器 。

这是关于Serilog的好坏事物之一, 你最终会安装一个BUNCH包件; 但这确实意味着你只添加你需要的东西, 而不仅仅是一个单一的包件。
这是我的

![精密喷入器](serilogenrichers.png)

随着所有这些炸弹袭击 我得到了一个相当不错的日志输出 在Seq。

![Serilog Seq 错误](serilogerror.png)

您可以在此看到错误消息、 堆栈跟踪、 线索 ID 、 进程 ID 和进程名称 。 这是所有有用的信息 当你试图找到一个问题。

有一点需要注意的是 我已经设定了 `  "MinimumLevel": "Warning",` 在我的 `appsettings.json` 文件。 这意味着只有警告及以上将被登录到Seq。 这是有用的 保持噪音 在你的日志。

然而,在 Seq 中,您也可以指定此 pApi 键;这样您就可以有 `Information` (或者如果你真的很热心) `Debug`)在此设定记录并限制 Seq 以 API 键实际捕捉到的内容 。

![Seq Api 键](apikey.png)

注意: 您仍然有应用程序管理费, 您也可以使这个程序更具动态性, 这样您就可以调整苍蝇上的水平 。 见 [Seq 汇汇 ](https://github.com/datalust/serilog-sinks-seq)以获取更多细节。

```json
{
    "Serilog":
    {
        "LevelSwitches": { "$controlSwitch": "Information" },
        "MinimumLevel": { "ControlledBy": "$controlSwitch" },
        "WriteTo":
        [{
            "Name": "Seq",
            "Args":
            {
                "serverUrl": "http://localhost:5341",
                "apiKey": "yeEZyL3SMcxEKUijBjN",
                "controlLevelSwitch": "$controlSwitch"
            }
        }]
    }
}
```

## 追踪追踪

现在,我们加上追踪,再次使用SerilogTtrace,这很简单。 我们有与过去相同的安排,但我们增加了一个新的追踪汇。

```bash
dotnet add package SerilogTracing
dontet add package SerilogTracing.Expressions
dotnet add SerilogTracing.Instrumentation.AspNetCore
```

我们还增加一个额外的软件包,以记录更详细的aspnet核心信息。

### 设置于 `Program.cs`

现在我们可以开始 实际使用追踪。 首先,我们需要在我们的 `Program.cs` 文件。

```csharp
    using var listener = new ActivityListenerConfiguration()
        .Instrument.HttpClientRequests().Instrument
        .AspNetCoreRequests()
        .TraceToSharedLogger();
```

追踪使用代表工作单位的“活动”概念。 你可以开始一项活动,做一些工作,然后停止它。 这有助于通过多种服务跟踪请求。

在此情况下,我们增加了对HttpClient请求和AspNetCore请求的额外追踪。 我们还增加: `TraceToSharedLogger` 将活动记录到与我们申请的其余部分相同的登录器上。

## 服务处使用追查

现在我们有了追踪装置 我们可以开始在申请中使用它 这是使用追踪的一个服务的例子。

```csharp
    public async Task<PostListViewModel> GetPostsByCategory(string category, int page = 1, int pageSize = 10,
        string language = MarkdownBaseService.EnglishLanguage)
    {
        using var activity = Log.Logger.StartActivity("GetPostsByCategory {Category}, {Page}, {PageSize}, {Language}",
            new { category, page, pageSize, language });
        try
        {
            var count = await NoTrackingQuery()
                .Where(x => x.Categories.Any(c => c.Name == category) && x.LanguageEntity.Name == language)
                .CountAsync();
            var posts = await PostsQuery()
                .Where(x => x.Categories.Any(c => c.Name == category) && x.LanguageEntity.Name == language)
                .OrderByDescending(x => x.PublishedDate.DateTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var languages = await GetLanguagesForSlugs(posts.Select(x => x.Slug).ToList());
            var postListViewModel = new PostListViewModel
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = count,
                Posts = posts.Select(x => x.ToListModel(
                    languages.FirstOrDefault(entry => entry.Key == x.Slug).Value.ToArray())).ToList()
            };
            activity.Complete();
            return postListViewModel;
        }
        catch (Exception e)
        {
            activity.Complete(LogEventLevel.Error, e);
        }

        return new PostListViewModel();
    }
```

这里的重要线条是:

```csharp
  using var activity = Log.Logger.StartActivity("GetPostsByCategory {Category}, {Page}, {PageSize}, {Language}",
            new { category, page, pageSize, language });
```

这开始了一个新的“活动”,这是一个工作单位。 它有助于通过多种服务跟踪请求。
正如我们在使用说明中包扎的一样,这将在我们的方法结束时完成和处理,但明确完成是良好做法。

```csharp
            activity.Complete();
```

在我们处理渔获的例外中,我们也完成了活动,但有误差和例外。 这有助于追踪您申请中的问题。

## 使用追踪

现在我们有了所有这些设置 我们可以开始使用它。 这是我申请表上的一个痕迹的例子

![Http 追踪](httptrace.png)

此选项显示您对单个标记点的翻译。 NAME OF TRANSLATORS 您可以看到单个员额的多个步骤以及 HttpClient 请求和时间。

注意 我使用 Postgres 来访问数据库, 与 SQL 服务器不同, npgsql 驱动程序有本地支持的追踪功能, 这样您可以从您的数据库查询中获得非常有用的数据, 如 SQL 执行、 计时等 。 它们被保存为 Seq 的“ spans ”, 并看起来有以下的谎言 :

```json
  "@t": "2024-08-31T15:23:31.0872838Z",
"@mt": "mostlylucid",
"@m": "mostlylucid",
"@i": "3c386a9a",
"@tr": "8f9be07e41f7121cbf2866c6cd886a90",
"@sp": "8d716c5f01ad07a0",
"@st": "2024-08-31T15:23:31.0706848Z",
"@ps": "622f1c86a8b33304",
"@sk": "Client",
"ActionId": "91f5105d-93fa-4e7f-9708-b1692e046a8a",
"ActionName": "Mostlylucid.Controllers.HomeController.Index (Mostlylucid)",
"ApplicationName": "mostlylucid",
"ConnectionId": "0HN69PVEQ9S7C",
"ProcessId": 30496,
"ProcessName": "Mostlylucid",
"RequestId": "0HN69PVEQ9S7C:00000015",
"RequestPath": "/",
"SourceContext": "Npgsql",
"ThreadId": 47,
"ThreadName": ".NET TP Worker",
"db.connection_id": 1565,
"db.connection_string": "Host=localhost;Database=mostlylucid;Port=5432;Username=postgres;Application Name=mostlylucid",
"db.name": "mostlylucid",
"db.statement": "SELECT t.\"Id\", t.\"ContentHash\", t.\"HtmlContent\", t.\"LanguageId\", t.\"Markdown\", t.\"PlainTextContent\", t.\"PublishedDate\", t.\"SearchVector\", t.\"Slug\", t.\"Title\", t.\"UpdatedDate\", t.\"WordCount\", t.\"Id0\", t0.\"BlogPostId\", t0.\"CategoryId\", t0.\"Id\", t0.\"Name\", t.\"Name\"\r\nFROM (\r\n    SELECT b.\"Id\", b.\"ContentHash\", b.\"HtmlContent\", b.\"LanguageId\", b.\"Markdown\", b.\"PlainTextContent\", b.\"PublishedDate\", b.\"SearchVector\", b.\"Slug\", b.\"Title\", b.\"UpdatedDate\", b.\"WordCount\", l.\"Id\" AS \"Id0\", l.\"Name\", b.\"PublishedDate\" AT TIME ZONE 'UTC' AS c\r\n    FROM mostlylucid.\"BlogPosts\" AS b\r\n    INNER JOIN mostlylucid.\"Languages\" AS l ON b.\"LanguageId\" = l.\"Id\"\r\n    WHERE l.\"Name\" = @__language_0\r\n    ORDER BY b.\"PublishedDate\" AT TIME ZONE 'UTC' DESC\r\n    LIMIT @__p_2 OFFSET @__p_1\r\n) AS t\r\nLEFT JOIN (\r\n    SELECT b0.\"BlogPostId\", b0.\"CategoryId\", c.\"Id\", c.\"Name\"\r\n    FROM mostlylucid.blogpostcategory AS b0\r\n    INNER JOIN mostlylucid.\"Categories\" AS c ON b0.\"CategoryId\" = c.\"Id\"\r\n) AS t0 ON t.\"Id\" = t0.\"BlogPostId\"\r\nORDER BY t.c DESC, t.\"Id\", t.\"Id0\", t0.\"BlogPostId\", t0.\"CategoryId\"",
"db.system": "postgresql",
"db.user": "postgres",
"net.peer.ip": "::1",
"net.peer.name": "localhost",
"net.transport": "ip_tcp",
"otel.status_code": "OK"
```

您可以看到它包括几乎所有您需要知道的关于查询、 SQL 执行、 连接字符串等的信息。 这是所有有用的信息 当你试图找到一个问题。 在像这样较小的应用程序中,这很有趣, 在分布式应用程序中, 它是一个固态的黄金信息 来追踪问题。

# 在结论结论中

我在这里只划过"追踪"的表面, 这是一个有点地方 充满热情的倡导者。 希望我已经展示了 使用Seq & Serilog 进行简单追踪有多简单 用于 ASP. NET 核心应用。 这样我就能从应用透视等更强大的工具中获益, 无需花费Azure成本(当日志大的时候,