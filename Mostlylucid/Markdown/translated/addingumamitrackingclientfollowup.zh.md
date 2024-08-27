# 添加 Umami 跟踪客户端后续跟踪

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-008-27-002:00</datetime>

# 一. 导言 导言 导言 导言 导言 导言 一,导言 导言 导言 导言 导言 导言

在一个 [上一个职位](/blog/addingumamitrackingclient.md) 我勾画了C#的Umami追踪客户端如何运作。
嗯,我终于有机会 广泛测试它 并改进它的操作(是另一个) `IHostedService`).

[技选委

# Umami API 的基尔克

Umami追踪API既见解深刻,也非常偏差。 因此我不得不更新客户代码 来处理以下事项:

1. API 期望有“ 真实” 的用户代理字符串 。 因此我不得不更新客户端以使用一个真正的用户代理字符串(或者更精确地说,我从浏览器中捕捉到一个真正的用户代理字符串,然后使用这个字符串)。
2. API 期待它以非常特殊的格式输入 JSON 输入; 不允许空字符串 。 所以我不得不更新客户 来处理这件事。
3. 缩略 [节点 API 客户端](https://github.com/umami-software/node) 有一丁点奇异的表面面积。 目前还不清楚API预期会有什么结果。 所以我不得不做一些试验和错误 才能让它起作用。

## 节点 API 客户端

节点 API 客户的总数低于以下, 它超灵活,但真的没有很好的记录。

```javascript
export interface UmamiOptions {
  hostUrl?: string;
  websiteId?: string;
  sessionId?: string;
  userAgent?: string;
}

export interface UmamiPayload {
  website: string;
  session?: string;
  hostname?: string;
  language?: string;
  referrer?: string;
  screen?: string;
  title?: string;
  url?: string;
  name?: string;
  data?: {
    [key: string]: string | number | Date;
  };
}

export interface UmamiEventData {
  [key: string]: string | number | Date;
}

export class Umami {
  options: UmamiOptions;
  properties: object;

  constructor(options: UmamiOptions = {}) {
    this.options = options;
    this.properties = {};
  }

  init(options: UmamiOptions) {
    this.options = { ...this.options, ...options };
  }

  send(payload: UmamiPayload, type: 'event' | 'identify' = 'event') {
    const { hostUrl, userAgent } = this.options;

    return fetch(`${hostUrl}/api/send`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'User-Agent': userAgent || `Mozilla/5.0 Umami/${process.version}`,
      },
      body: JSON.stringify({ type, payload }),
    });
  }

  track(event: object | string, eventData?: UmamiEventData) {
    const type = typeof event;
    const { websiteId } = this.options;

    switch (type) {
      case 'string':
        return this.send({
          website: websiteId,
          name: event as string,
          data: eventData,
        });
      case 'object':
        return this.send({ website: websiteId, ...(event as UmamiPayload) });
    }

    return Promise.reject('Invalid payload.');
  }

  identify(properties: object = {}) {
    this.properties = { ...this.properties, ...properties };
    const { websiteId, sessionId } = this.options;

    return this.send(
      { website: websiteId, session: sessionId, data: { ...this.properties } },
      'identify',
    );
  }

  reset() {
    this.properties = {};
  }
}

const umami = new Umami();

export default umami;
```

正如你所看到的,它暴露了以下方法:

1. `init` - 设置选项。
2. `send` - 发送有效载荷。
3. `track` - 跟踪一个事件。
4. `identify` - 识别一个用户。
5. `reset` - 重置属性。

其核心是 `send` 将有效载荷发送到 API 的方法 。

```javascript
  send(payload: UmamiPayload, type: 'event' | 'identify' = 'event') {
    const { hostUrl, userAgent } = this.options;

    return fetch(`${hostUrl}/api/send`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'User-Agent': userAgent || `Mozilla/5.0 Umami/${process.version}`,
      },
      body: JSON.stringify({ type, payload }),
    });
  }
```

# C# 客户端

首先,我几乎抄袭了节点API客户的客户 `UmamiOptions` 和 `UmamiPayload` 班级(我不会再过它们了,它们太大了)

所以现在我 `Send` 方法看起来是这样 :

```csharp
     public async Task<HttpResponseMessage> Send(UmamiPayload? payload=null, UmamiEventData? eventData =null,  string type = "event")
        {
            var websiteId = settings.WebsiteId;
             payload = PopulateFromPayload(websiteId, payload, eventData);
            
            var jsonPayload = new { type, payload };
            logger.LogInformation("Sending data to Umami: {Payload}", JsonSerializer.Serialize(jsonPayload, options));

            var response = await client.PostAsJsonAsync("api/send", jsonPayload, options);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Failed to send data to Umami: {StatusCode}, {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                logger.LogInformation("Successfully sent data to Umami: {StatusCode}, {ReasonPhrase}, {Content}", response.StatusCode, response.ReasonPhrase, content);
            }

            return response;
        }

```

这里有两个关键部分:

1. 缩略 `PopulateFromPayload` 将有效载荷与网站Id和事件Data相匹配的方法。
2. 有效载荷的JSON序列,它需要排除无效值。

## 缩略 `PopulateFromPayload` 方法方法方法

```csharp
        public static UmamiPayload PopulateFromPayload(string webSite, UmamiPayload? payload, UmamiEventData? data)
        {
            var newPayload = GetPayload(webSite, data: data);
            if(payload==null) return newPayload;
            if(payload.Hostname != null)
                newPayload.Hostname = payload.Hostname;
            if(payload.Language != null)
                newPayload.Language = payload.Language;
            if(payload.Referrer != null)
                newPayload.Referrer = payload.Referrer;
            if(payload.Screen != null)
                newPayload.Screen = payload.Screen;
            if(payload.Title != null)
                newPayload.Title = payload.Title;
            if(payload.Url != null)
                newPayload.Url = payload.Url;
            if(payload.Name != null)
                newPayload.Name = payload.Name;
            if(payload.Data != null)
                newPayload.Data = payload.Data;
            return newPayload;          
        }
        
        private static UmamiPayload GetPayload(string websiteId, string? url = null, UmamiEventData? data = null)
        {
            var payload = new UmamiPayload
            {
            Website = websiteId,
                Data = data,
                Url = url ?? string.Empty
            };
            

            return payload;
        }

```

你可以看到,我们总是确保 `websiteId` 设置后,我们只设定其他值,如果它们不是空的。 这给了我们灵活性,而牺牲了一点动词。

## HttppClient 服务设置

如前所述,我们需要给API一个有点真实的用户代理字符串。 这是在 `HttpClient` 设置 。

```csharp
              services.AddHttpClient<UmamiClient>((serviceProvider, client) =>
            {
                 umamiSettings = serviceProvider.GetRequiredService<UmamiClientSettings>();
            client.DefaultRequestHeaders.Add("User-Agent", $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/127.0.0.0 Safari/537.36");
            client.BaseAddress = new Uri(umamiSettings.UmamiPath);
        }).SetHandlerLifetime(TimeSpan.FromMinutes(5))  //Set lifetime to five minutes
        .AddPolicyHandler(GetRetryPolicy())
       #if DEBUG 
        .AddLogger<HttpLogger>();
        #else
        ;
        #endif

```

## 背景事务处

这是又一个 `IHostedService`,有一堆文章 关于如何设置这些, 所以我不会进入它在这里(尝试搜索栏!! )) )

唯一的疼痛点是注射 `HttpClient` 和在 `UmamiClient` 类。 由于客户范围界定以及我使用的服务 `IServiceScopeFactory` 输入主机服务器的构建器中, 然后为每个发送请求抓取它 。

```csharp
    

    private async Task SendRequest(CancellationToken token)
    {
        logger.LogInformation("Umami background delivery started");

        while (await _channel.Reader.WaitToReadAsync(token))
        {
            while (_channel.Reader.TryRead(out var payload))
            {
                try
                {
                   using  var scope = scopeFactory.CreateScope();
                    var client = scope.ServiceProvider.GetRequiredService<UmamiClient>();
                    // Send the event via the client
                    await client.Send(payload.Payload);

                    logger.LogInformation("Umami background event sent: {EventType}", payload.EventType);
                }
                catch (OperationCanceledException)
                {
                    logger.LogWarning("Umami background delivery canceled.");
                    return; // Exit the loop on cancellation
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error sending Umami background event.");
                }
            }
        }
    }
   
```

### 使用托管服务

现在我们有了这种托管服务,我们可以通过将事件发送到背景中,大大改善业绩。

我在几个不同的地方用过这个 `Program.cs` 我决定尝试使用Middleware 追踪 RSS 种子请求,

```csharp
app.Use( async (context, next) =>
{
var path = context.Request.Path.Value;
if (path.EndsWith("RSS", StringComparison.OrdinalIgnoreCase))
{
var rss = context.RequestServices.GetRequiredService<UmamiBackgroundSender>();
// Send the event in the background
await rss.SendBackground(new UmamiPayload(){Url  = path, Name = "RSS Feed"});
}
await next();
});
```

我还传递了更多的数据 从我的 `TranslateAPI` 终点。
这让我可以看到翻译需要多长时间; 注意这些都没有阻碍主线 OR 跟踪个别用户。

```csharp
    
       await  umamiClient.SendBackground(new UmamiPayload(){  Name = "Get Translation"}, new UmamiEventData(){{"timetaken", translationTask.TotalMilliseconds}, {"language",translationTask.Language}});
        var result = new TranslateResultTask(translationTask, true);
```

# 在结论结论中

Umami API有点古怪,但它是一个很好的方式 以自办的方式跟踪事件。 希望我能有机会再清理一下 弄个木马核桃包出来
另加 [前一条](/blog/addingascsharpclientforumamiapi)  我想把数据从Umami调出来 提供流行化分类等功能