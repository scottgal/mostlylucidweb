# 用 EasyNMT 自动翻译标记下调文件

## 一. 导言 导言 导言 导言 导言 导言 一,导言 导言 导言 导言 导言 导言

EasyNMT 是一个本地可安装的服务, 它为许多机器翻译服务提供一个简单的界面。 在此教程中, 我们将使用 EasyNMT 自动将标记文件从英文翻译成多种语言 。

您可以在[GitHub 库](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/MarkdownTranslator)这个项目。

注意:这仍然相当粗糙, 我会继续完善它。

我只翻译了这个文件[关于我](/blog/aboutme)随着我对方法的改进,我需要解决翻译方面的一些问题。

[技选委

## 先决条件

需要安装 EasyNMT 才能跟随此教程。 我通常运行它作为 Docker 服务。 您可以找到安装指令 。[在这里](https://github.com/UKPLab/EasyNMT/blob/main/docker/README.md)它覆盖了如何运行它作为一个 docker 服务。

```shell
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0-cpu
```

或者,如果您有 NVIDIA GPU 可用的话:

```shell
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0.2-cuda11.3
```

MAX_ WORKERS_BACKEND 和 MAX_ WORKERS_FRUNTEND 环境变量设定了 EasyNMT 将使用的工人数量。 您可以调整这些数量以适合您的机器 。

注意: EasyNMT 并不是 SMOOTHEST 服务运行, 但这是我为此找到的最好的。 它对于它通过的输入字符串有点不切实际, 所以您可能需要先对输入文本进行一些预处理, 然后再将其传递给 EasyNMT 。

## 负载平衡的巧妙方法

在资源方面,NMT是一个容易渴的野兽,所以在我的马克唐翻译服务公司,我有一个超级简单的随机IP选择器,它只是从我拥有的机器列表中抽取一个随机IP。这有点天真,并且可以通过使用更复杂的负载平衡算法加以改进。

```csharp
    private string[] IPs = new[] { "http://192.168.0.30:24080", "http://localhost:24080", "http://192.168.0.74:24080" };

     var ip = IPs[random.Next(IPs.Length)];
     logger.LogInformation("Sendign request to {IP}", ip);
     var response = await client.PostAsJsonAsync($"{ip}/translate", postObject, cancellationToken);

```

## 正在翻译标记下翻译文件

这是我在 Markdown 翻译服务. cs 文件中的代码。 它是一个简单的服务, 使用一个标记字符串和一个目标语言, 并返回被翻译的标记字符串 。

```csharp
    public async Task<string> TranslateMarkdown(string markdown, string targetLang, CancellationToken cancellationToken)
    {
        var document = Markdig.Markdown.Parse(markdown);
        var textStrings = ExtractTextStrings(document);
        var batchSize = 50;
        var stringLength = textStrings.Count;
        List<string> translatedStrings = new();
        for (int i = 0; i < stringLength; i += batchSize)
        {
            var batch = textStrings.Skip(i).Take(batchSize).ToArray();
            translatedStrings.AddRange(await Post(batch, targetLang, cancellationToken));
        }


        ReinsertTranslatedStrings(document, translatedStrings.ToArray());
        return document.ToMarkdownString();
    }
```

正如你所看到的,它有许多步骤:

1. `  var document = Markdig.Markdown.Parse(markdown);`- 将标记字符串切入文档中。
2. `  var textStrings = ExtractTextStrings(document);`- 这从文档中提取文本字符串 。
3. `  var batchSize = 50;`这设置了翻译服务的批量大小。 EasyNMT 限制它可以一次性翻译的字符数 。
4. `csharp await Post(batch, targetLang, cancellationToken)`
   这就要求采用一种方法,然后将批量投放到 " 方便NMT " 服务上。

```csharp
    private async Task<string[]> Post(string[] elements, string targetLang, CancellationToken cancellationToken)
    {
        try
        {
            var postObject = new PostRecord(targetLang, elements);
            var response = await client.PostAsJsonAsync("/translate", postObject, cancellationToken);

            var phrase = response.ReasonPhrase;
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<PostResponse>(cancellationToken: cancellationToken);

            return result.translated;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error translating markdown: {Message} for strings {Strings}", e.Message, string.Concat( elements, Environment.NewLine));
            throw;
        }
    }
```

5. `  ReinsertTranslatedStrings(document, translatedStrings.ToArray());`- 将翻译的字符串重新插入到文档中。 使用 MarkDig 能够行走文档并替换文本字符串 。

## 东道服务处

要运行所有这一切, 我使用一个 IHostedLifetime Service, 它在程序. cs 文件中启动。 此服务以标记文件读取, 翻译成多种语言, 并将已翻译的文件写入磁盘 。

```csharp
    public async Task StartedAsync(CancellationToken cancellationToken)
    {
        var files = Directory.GetFiles("Markdown", "*.md");

        var outDir = "Markdown/translated";

        var languages = new[] { "es", "fr", "de", "it", "jap", "uk", "zh" };
        foreach (var language in languages)
        {
            foreach (var file in files)
            {
                var fileChanged = await file.IsFileChanged(outDir);
                var outName = Path.GetFileNameWithoutExtension(file);

                var outFileName = $"{outDir}/{outName}.{language}.md";
                if (File.Exists(outFileName) && !fileChanged)
                {
                    continue;
                }

                var text = await File.ReadAllTextAsync(file, cancellationToken);
                try
                {
                    logger.LogInformation("Translating {File} to {Language}", file, language);
                    var translatedMarkdown = await blogService.TranslateMarkdown(text, language, cancellationToken);
                    await File.WriteAllTextAsync(outFileName, translatedMarkdown, cancellationToken);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error translating {File} to {Language}", file, language);
                }
            }
        }

        logger.LogInformation("Background translation service started");
    }
```

正如你所看到的,它也检查了文件的散列, 看文件翻译前是否有更改。 这是为了避免翻译尚未更改的文件 。

这样做的方法是计算原始标记文件的快速散列, 然后测试该文件在试图翻译之前是否已经更改 。

```csharp
    private static async Task<string> ComputeHash(string filePath)
    {
        await using var stream = File.OpenRead(filePath);
        stream.Position = 0;
        var bytes = new byte[stream.Length];
        await stream.ReadAsync(bytes);
        stream.Position = 0;
        var hash = XxHash64.Hash(bytes);
        var hashString = Convert.ToBase64String(hash);
        hashString = InvalidCharsRegex.Replace(hashString, "_");
        return hashString;
    }
```

程序. cs的设置很简单:

```csharp

    builder.Services.AddHostedService<BackgroundTranslateService>();
services.AddHttpClient<MarkdownTranslatorService>(options =>
{
    options.Timeout = TimeSpan.FromMinutes(15);
    options.BaseAddress = new Uri("http://192.168.0.30:24080");
});
```

我设置了托管服务(地下翻译服务)和马克唐翻译服务 HttpClient。
托管服务是一种长期服务, 它在背景中运行。 它是一个将需要持续运行的服务置于背景中或只需要一段时间才能完成的服务设置的好地方。 新的 IHostedLifeTime Servicice 界面比旧的 IHostedServices 界面更灵活一些, 并且让我们比旧的 IHostedService 更容易在背景中完全运行任务 。

您可以在这里看到我将 HttpClient 的超时时间设定为 15 分钟。 这是因为 EnterNMT 反应可能有点慢( 特别是第一次使用语言模式)。 我还在设置运行 EnterNMT 服务的机器的 IP IP 地址的基址 。

一一