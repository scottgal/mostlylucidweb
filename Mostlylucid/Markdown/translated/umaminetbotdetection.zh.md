# Umami.Net和Bot探测

# 一. 导言 导言 导言 导言 导言 导言 一,导言 导言 导言 导言 导言 导言

所以我已经 [张贴的LOT](/blog/category/Umami) 过去曾使用Umami在自托管的环境中进行分析, [Umami. Net Nuget Pakakge 网](https://www.nuget.org/packages/Umami.Net/).. 然而,我有一个问题,我想追踪我的简易新闻聚合(RSS)的用户;这篇文章谈到我为什么以及如何解决这个问题。

[TOC]

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-009-12T14:50</datetime>

# 问题

问题在于 RSS 种子阅读器试图通过 *有用* 用户代理在请求输入时使用 。 这样可以 **遵守情况** 提供方跟踪用户数量和用户使用种子的类型。 然而,这也意味着Umami将确定这些请求为: *立方体体* 请求。 这是一个供我使用的问题,因为它导致请求被忽视,没有跟踪。

Feedbin 用户代理器看起来是这样 :

```plaintext
Feedbin feed-id:1234 - 21 subscribers
```

它传达了一些有用的细节 说明你的种子识别码是什么 用户和用户代理的数量 然而,这也是个问题, 因为它意味着Umami会无视请求; 事实上它会返回200个状态, 但内容包含 `{"beep": "boop"}` 意思是,这被确定为机器人请求。 这很烦人,因为我无法通过正常的错误处理(这是200美元,不是403美元等)来解决这个问题。

# 解决方案

那么,有什么解决办法呢? 我无法手动分析所有这些请求, 检测Umami是否检测为机器人; 它使用IsBot(https://www.npmjs.com/package/isbot)检测请求是否为机器人。 我甚至不能使用这份名单(将来我可能会变得聪明, 用这份名单来检测请求是否为机器人)。
所以,我需要拦截请求 之前,它到达乌马米 并改变用户代理 将U马米会接受的东西 对于具体的请求。

现在,我在Umami.Net的追踪方法中增加了一些额外的参数。 这些允许您指定新的“  Default 用户代理” 将发送到 Umami, 而不是原用户代理 。 这使我能够具体说明,用户代理应修改为具体请求的具体价值。

## 方法和方法

在我的上 `UmamiBackgroundSender` 我补充了以下内容:

```csharp
   public async Task TrackPageView(string url, string title, UmamiPayload? payload = null,
        UmamiEventData? eventData = null, bool useDefaultUserAgent = false)
```

它存在于所有的跟踪方法上 仅设置一个参数 `UmamiPayload` 对象。

日期 于 `UmamiClient` 这些标准可规定如下:

```csharp
    [Fact]
    public async Task TrackPageView_WithUrl()
    {
        var handler = JwtResponseHandler.Create();
        var umamiClient = GetServices(handler);
        var response = await umamiClient.TrackPageViewAndDecode("https://example.com", "Example Page",
            new UmamiPayload { UseDefaultUserAgent = true });
        Assert.NotNull(response);
        Assert.Equal(UmamiDataResponse.ResponseStatus.Success, response.Status);
    }
```

在这个测试中,我用的是新的 `TrackPageViewAndDecode` 返回 `UmamiDataResponse` 对象。 此对象包含已解码的 JWT 质象( 如果是机器人, 无效) 和请求状态 。

## `PayloadService`

这一切都是处理在 `Payload` 负责装载有效载荷物体的服务 这里就是 `UseDefaultUserAgent` 设置了。

默认情况下I 将有效载荷从 `HttpContext` 所以通常你会得到正确的设置; 我稍后会显示这是从Umami拿回来的。

```csharp
    private UmamiPayload GetPayload(string? url = null, UmamiEventData? data = null)
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
            Hostname = request?.Host.Host
        };

        return payload;
    }
```

然后,我有一个代码 叫做 `PopulateFromPayload` 这是请求对象获得数据的地方:

```csharp
    public static string DefaultUserAgent =>
        $"Mozilla/5.0 (Windows 11)  Umami.Net/{Assembly.GetAssembly(typeof(UmamiClient))!.GetName().Version}";

    public UmamiPayload PopulateFromPayload(UmamiPayload? payload, UmamiEventData? data)
    {
        var newPayload = GetPayload(data: data);
        ...
        
        newPayload.UserAgent = payload.UserAgent ?? DefaultUserAgent;

        if (payload.UseDefaultUserAgent)
        {
            var userData = newPayload.Data ?? new UmamiEventData();
            userData.TryAdd("OriginalUserAgent", newPayload.UserAgent ?? "");
            newPayload.UserAgent = DefaultUserAgent;
            newPayload.Data = userData;
        }


        logger.LogInformation("Using UserAgent: {UserAgent}", newPayload.UserAgent);
     }        
        
```

你会看到,这定义了一个新的用户代理器 在文件的顶部(我证实这不是 *目前* 被检测为机器人)。 然后在方法中,它检测用户代理器是否无效(除非没有 HttpContext 的代码被调用,否则不应发生),或者如果 `UseDefaultUserAgent` 设置了。 如果是,则将用户代理设定为默认值,并将原始用户代理添加到数据对象中。

然后将此记录下来, 这样您就可以看到用户代理正在使用什么 。

## 解码反应。

在Umami.Net 0.3.0中,我添加了一些新的“十二月”方法,这些方法返回了 `UmamiDataResponse` 对象。 此对象包含已解码的 JWT 符号 。

```csharp
    public async Task<UmamiDataResponse?> TrackPageViewAndDecode(
        string? url = "",
        string? title = "",
        UmamiPayload? payload = null,
        UmamiEventData? eventData = null)
    {
        var response = await TrackPageView(url, title, payload, eventData);
        return await DecodeResponse(response);
    }
    
        private async Task<UmamiDataResponse?> DecodeResponse(HttpResponseMessage responseMessage)
    {
        var responseString = await responseMessage.Content.ReadAsStringAsync();

        switch (responseMessage.IsSuccessStatusCode)
        {
            case false:
                return new UmamiDataResponse(UmamiDataResponse.ResponseStatus.Failed);
            case true when responseString.Contains("beep") && responseString.Contains("boop"):
                logger.LogWarning("Bot detected data not stored in Umami");
                return new UmamiDataResponse(UmamiDataResponse.ResponseStatus.BotDetected);

            case true:
                var decoded = await jwtDecoder.DecodeResponse(responseString);
                if (decoded == null)
                {
                    logger.LogError("Failed to decode response from Umami");
                    return null;
                }

                var payload = UmamiDataResponse.Decode(decoded);

                return payload;
        }
    }
```

你可以看到,这要求 进入正常 `TrackPageView` 方法,然后调用一种叫做方法的方法 `DecodeResponse` 用于检查应答的 `beep` 和 `boop` 字符串( 用于检测机器人) 。 如果发现它们,它会记录警告并返回 `BotDetected` 状态。 如果找不到它们,它会解码JWT标志 并返回有效载荷。

JWT标志本身只是一个Base64编码的字符串,其中载有Umami储存的数据。 编码解码, 以 a 格式返回 `UmamiDataResponse` 对象。

完整的资料来源如下:

<details>
<summary>Response Decoder</summary>

```csharp
using System.IdentityModel.Tokens.Jwt;

namespace Umami.Net.Models;

public class UmamiDataResponse
{
    public enum ResponseStatus
    {
        Failed,
        BotDetected,
        Success
    }

    public UmamiDataResponse(ResponseStatus status)
    {
        Status = status;
    }

    public ResponseStatus Status { get; set; }

    public Guid Id { get; set; }
    public Guid WebsiteId { get; set; }
    public string? Hostname { get; set; }
    public string? Browser { get; set; }
    public string? Os { get; set; }
    public string? Device { get; set; }
    public string? Screen { get; set; }
    public string? Language { get; set; }
    public string? Country { get; set; }
    public string? Subdivision1 { get; set; }
    public string? Subdivision2 { get; set; }
    public string? City { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid VisitId { get; set; }
    public long Iat { get; set; }

    public static UmamiDataResponse Decode(JwtPayload? payload)
    {
        if (payload == null) return new UmamiDataResponse(ResponseStatus.Failed);
        payload.TryGetValue("visitId", out var visitIdObj);
        payload.TryGetValue("iat", out var iatObj);
        //This should only happen then the payload is dummy.
        if (payload.Count == 2)
        {
            var visitId = visitIdObj != null ? Guid.Parse(visitIdObj.ToString()!) : Guid.Empty;
            var iat = iatObj != null ? long.Parse(iatObj.ToString()!) : 0;

            return new UmamiDataResponse(ResponseStatus.Success)
            {
                VisitId = visitId,
                Iat = iat
            };
        }

        payload.TryGetValue("id", out var idObj);
        payload.TryGetValue("websiteId", out var websiteIdObj);
        payload.TryGetValue("hostname", out var hostnameObj);
        payload.TryGetValue("browser", out var browserObj);
        payload.TryGetValue("os", out var osObj);
        payload.TryGetValue("device", out var deviceObj);
        payload.TryGetValue("screen", out var screenObj);
        payload.TryGetValue("language", out var languageObj);
        payload.TryGetValue("country", out var countryObj);
        payload.TryGetValue("subdivision1", out var subdivision1Obj);
        payload.TryGetValue("subdivision2", out var subdivision2Obj);
        payload.TryGetValue("city", out var cityObj);
        payload.TryGetValue("createdAt", out var createdAtObj);

        return new UmamiDataResponse(ResponseStatus.Success)
        {
            Id = idObj != null ? Guid.Parse(idObj.ToString()!) : Guid.Empty,
            WebsiteId = websiteIdObj != null ? Guid.Parse(websiteIdObj.ToString()!) : Guid.Empty,
            Hostname = hostnameObj?.ToString(),
            Browser = browserObj?.ToString(),
            Os = osObj?.ToString(),
            Device = deviceObj?.ToString(),
            Screen = screenObj?.ToString(),
            Language = languageObj?.ToString(),
            Country = countryObj?.ToString(),
            Subdivision1 = subdivision1Obj?.ToString(),
            Subdivision2 = subdivision2Obj?.ToString(),
            City = cityObj?.ToString(),
            CreatedAt = createdAtObj != null ? DateTime.Parse(createdAtObj.ToString()!) : DateTime.MinValue,
            VisitId = visitIdObj != null ? Guid.Parse(visitIdObj.ToString()!) : Guid.Empty,
            Iat = iatObj != null ? long.Parse(iatObj.ToString()!) : 0
        };
    }
}
```

</details>
你可以看到,这里面有一堆有用的信息 关于Umami存储的请求。 如果您想要显示基于语区、语言、浏览器等的不同内容, 您可以这样做 。

```csharp
    public Guid Id { get; set; }
    public Guid WebsiteId { get; set; }
    public string? Hostname { get; set; }
    public string? Browser { get; set; }
    public string? Os { get; set; }
    public string? Device { get; set; }
    public string? Screen { get; set; }
    public string? Language { get; set; }
    public string? Country { get; set; }
    public string? Subdivision1 { get; set; }
    public string? Subdivision2 { get; set; }
    public string? City { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid VisitId { get; set; }
    public long Iat { get; set; }
```

# 在结论结论中

所以只要一个短位, 覆盖 Umami. Net 0. 0. 0. 0. 0. 0 中的一些新功能, 这样您就可以指定一个默认用户代理, 用于特定请求 。 这有助于追踪Umami否则会忽略的请求。