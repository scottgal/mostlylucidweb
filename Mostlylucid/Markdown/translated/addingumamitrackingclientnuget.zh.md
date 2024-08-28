# 添加 Umami 跟踪客户端 Giget 软件包

<!--category-- ASP.NET, Umami, Nuget -->
<datetime class="hidden">2024-008-28-002:00</datetime>

# 一. 导言 导言 导言 导言 导言 导言 一,导言 导言 导言 导言 导言 导言

现在我有了Umami客户端;我需要把它包装起来,并把它作为Nuget软件包提供。 这是一个相当简单的过程,但有一些事情需要注意。

[技选委

# 创建“ 发明” 软件包

## 版本

我决定复印 [哈立德( Khalid)](@khalidabuhakmeh@mastodon.social) 并使用优异的薄荷包来版本我的 Nuget 软件包 。 这是一个简单的软件包, 使用 git 版本标签来确定版本编号 。

为了使用它,我简单地补充了以下内容: `Umami.Net.csproj` 文件 :

```xml
    <PropertyGroup>
    <MinVerTagPrefix>v</MinVerTagPrefix>
</PropertyGroup>
```

这样我就可以用 `v` 软件包的版本将正确无误。

```bash
 git tag v0.0.8       
 git push origin v0.0.8

```

将按下此标签, 然后我将设置一个 GitHub Action 设置, 等待标签, 并构建 Niget 软件包 。

## 构建 Giget 软件包

我有一个GitHub Action, 建造了Nuget软件包, 并把它推到 GitHub 软件包仓库。 这是一个简单的程序, 使用 `dotnet pack` 命令,然后构建软件包,然后 `dotnet nuget push` 命令将它推到 nuget 仓库。

```yaml
name: Publish Umami.NET
on:
  push:
    tags:
      - 'v*.*.*'  # This triggers the action for any tag that matches the pattern v1.0.0, v2.1.3, etc.

jobs:
  publish:
    runs-on: ubuntu-latest

    steps:
    - name: Checkout code
      uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.x' # Specify the .NET version you need

    - name: Restore dependencies
      run: dotnet restore ./Umami.Net/Umami.Net.csproj

    - name: Build project
      run: dotnet build --configuration Release ./Umami.Net/Umami.Net.csproj --no-restore

    - name: Pack project
      run: dotnet pack --configuration Release ./Umami.Net/Umami.Net.csproj --no-build --output ./nupkg

    - name: Publish to NuGet
      run: dotnet nuget push ./nupkg/*.nupkg --source https://api.nuget.org/v3/index.json --api-key ${{ secrets.UMAMI_NUGET_API_KEY }}
      env:
        NUGET_API_KEY: ${{ secrets.UMAMI_NUGET_API_KEY }}
```

### 正在添加 readme 和图标

这很简单,我加一个 `README.md` 到工程根和工程根的文件 `icon.png` 到工程根的文件。 缩略 `README.md` 用于描述软件包和 `icon.png` 软件包使用文件作为图标。

```xml
    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
        <LangVersion>12</LangVersion>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>

        <IsPackable>true</IsPackable>
        <PackageId>Umami.Net</PackageId>
        <Authors>Scott Galloway</Authors>
        <PackageIcon>icon.png</PackageIcon>
        <RepositoryUrl>https://github.com/scottgal/mostlylucidweb/tree/main/Umami.Net</RepositoryUrl>
        <PackageLicenseExpression>MIT</PackageLicenseExpression>
        <PackageTags>web</PackageTags>
        <PackageReadmeFile>README.md</PackageReadmeFile>
        <Description>
           Adds a simple Umami endpoint to your ASP.NET Core application.
        </Description>
    </PropertyGroup>
```

在我的README. md 文件中,我有一个链接 与GitHub 仓库 和对软件包的描述。

转录如下:

# Umami. Net

这是Umami追踪API的.NET核心客户端。
以Umami节点客户为基础 可以找到 [在这里](https://github.com/umami-software/node).

你可以看到如何将乌玛美设成一个船舱集装箱 [在这里](https://www.mostlylucid.net/blog/usingumamiforlocalanalytics).
你可以在我的博客上阅读更多关于它创建的细节 [在这里](https://www.mostlylucid.net/blog/addingumamitrackingclientfollowup).

要使用此客户端, 您需要以下 apings.json 配置 :

```json
{
  "Analytics":{
    "UmamiPath" : "https://umamilocal.mostlylucid.net",
    "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee"
  },
}
```

何处处 `UmamiPath` 您的 Umami 实例和路径 `WebsiteId` 是您想要跟踪的网站的 ID 。

要使用客户端, 您需要在客户端中添加以下内容 `Program.cs`:

```csharp
using Umami.Net;

services.SetupUmamiClient(builder.Configuration);
```

这将在服务收藏中增加Umami客户端。

然后,您可以用两种方式使用客户端:

1. 注射 `UmamiClient` 进入班级,然后拨打 `Track` 方法 :

```csharp
 // Inject UmamiClient umamiClient
 await umamiClient.Track("Search", new UmamiEventData(){{"query", encodedQuery}});
```

2. 使用 `UmamiBackgroundSender` 来跟踪背景中的事件(此使用 `IHostedService` 发送背景中的事件:

```csharp
 // Inject UmamiBackgroundSender umamiBackgroundSender
await umamiBackgroundSender.Track("Search", new UmamiEventData(){{"query", encodedQuery}});
```

客户将把事件发送至Umami API, 并存储该事件 。

缩略 `UmamiEventData` 是一个关键值对的字典,作为事件数据发送到 Umami API。

此外,还可以使用更低水平的方法将事件发送至Umami API。

两者 `UmamiClient` 和 `UmamiBackgroundSender` 您可以调用以下方法。

```csharp


 Send(UmamiPayload? payload = null, UmamiEventData? eventData = null,
        string eventType = "event")
```

如果你不通过 `UmamiPayload` 对象,客户端会为使用 `WebsiteId` 照片来源:json.

```csharp
    public  UmamiPayload GetPayload(string? url = null, UmamiEventData? data = null)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var request = httpContext?.Request;

        var payload = new UmamiPayload
        {
            Website = settings.WebsiteId,
            Data = data,
            Url = url ?? httpContext?.Request?.Path.Value,
            IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString(),
            UserAgent = request?.Headers["User-Agent"].FirstOrDefault(),
            Referrer = request?.Headers["Referer"].FirstOrDefault(),
           Hostname = request?.Host.Host,
        };
        
        return payload;
    }

```

你可以看到,这个弹出 `UmamiPayload` 对象 `WebsiteId` ......................... `Url`, `IpAddress`, `UserAgent`, `Referrer` 和 `Hostname` 调自自 `HttpContext`.

注意: 事件类型Type 只能按照 Umami API 的“ 活动” 或“ 身份识别 ” 进行 。

# 在结论结论中

所以,这就是你现在可以安装 Ummi. Net 从Nuget。 并将其用于 ASP. NET 核心应用程序。 我希望你觉得这很有用 我将继续调整和添加测试 在未来的岗位上。