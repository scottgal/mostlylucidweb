# 用 EasyNMT 自动翻译标记下调文件

<datetime class="hidden">2024-008-003T13:30</datetime>

<!--category-- EasyNMT, Markdown -->
## 一. 导言 导言 导言 导言 导言 导言 一,导言 导言 导言 导言 导言 导言

EasyNMT是一种可在当地安装的服务,为一些机器翻译服务提供一个简单的接口。 在此教程中, 我们将使用 EasyNMT 自动将标记文件从英文翻译成多种语言 。

您可以在 [GitHub 库](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/MarkdownTranslator) 这个项目。

该输出生成了目标语言的新标记文件 BUNCH 。 这是将博客文章翻译成多种语言的超级简单方式。

[翻译后员额](/translatedposts.png)

[技选委

## 先决条件

需要安装简易NMT, 才能跟随此教程 。 我通常用杜克服务来经营 您可以找到安装指令 [在这里](https://github.com/UKPLab/EasyNMT/blob/main/docker/README.md) 它覆盖了如何运行它作为一个 docker 服务。

```shell
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0-cpu
```

或者,如果您有 NVIDIA GPU 可用的话:

```shell
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0.2-cuda11.3
```

MAX_WORKERS_BACKEND 和 MAX_WORKERS_FRUNTEND 环境变量设定了 EasyNMT 将使用的工人数量 。 你可以调整这些 适合你的机器。

注意: EasyNMT不是SMOOTHEST服务运行, 但它是我为这个目的找到的最好的。 它对于它通过的输入字符串有点粗略, 所以你可能需要先对输入文本进行一些预处理, 然后再把它传递给 EasyNMT 。

哦,它也翻译了“结论” 和一些无稽之谈 关于向欧盟提交提案... 教唆它的训练。

## 负载平衡的巧妙方法

所以在我的马克唐翻译服务公司, 我有一个超简单的随机IP选择器, 通过我用来运行 EasyNMT 的机器的IP 列表旋转。

一开始,这是在 `model_name` 在 EasyNMT 服务上的方法, 这是一个快速、简单的方法, 检查服务是否启动 。 如果是的话,它会将IP添加到工作IP的列表中。 如果不是,它不会把它添加到清单中。

```csharp
    private string[] IPs = translateServiceConfig.IPs;
    public async ValueTask<bool> IsServiceUp(CancellationToken cancellationToken)
    {
        var workingIPs = new List<string>();

        try
        {
            foreach (var ip in IPs)
            {
                logger.LogInformation("Checking service status at {IP}", ip);
                var response = await client.GetAsync($"{ip}/model_name", cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    workingIPs.Add(ip);
                }
            }

            IPs = workingIPs.ToArray();
            if (!IPs.Any()) return false;
            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error checking service status");
            return false;
        }
    }
```

然后在 `Post` 方法方法的计算方法的计算方法 `MarkdownTranslatorService` 我们通过工作IP旋转 找到下一个IP。

```csharp
          if(!IPs.Any())
            {
                logger.LogError("No IPs available for translation");
                throw new Exception("No IPs available for translation");
            }
            var ip = IPs[currentIPIndex];
            
            logger.LogInformation("Sending request to {IP}", ip);
        
            // Update the index for the next request
            currentIPIndex = (currentIPIndex + 1) % IPs.Length;
```

这是一个超简单的方式 来平衡一些机器的要求。 它不完美(这不是指超级忙碌的考卷机), 但它足以满足我的目的。

那个笨蛋 ` currentIPIndex = (currentIPIndex + 1) % IPs.Length;` 只需从 0 开始在 IP 列表中旋转, 直至列表的长度 。

## 正在翻译标记下翻译文件

这是我在Markdown翻译服务公司文件中的代码 这是一个简单的服务, 使用一个标记字符串和一个目标语言 并返回翻译的标记字符串。

```csharp
    public async Task<string> TranslateMarkdown(string markdown, string targetLang, CancellationToken cancellationToken)
    {
        var document = Markdig.Markdown.Parse(markdown);
        var textStrings = ExtractTextStrings(document);
        var batchSize = 10;
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

1. `  var document = Markdig.Markdown.Parse(markdown);` - 将标记字符串切入文档中。
2. `  var textStrings = ExtractTextStrings(document);` - 这从文档中提取文本字符串 。
   此方法使用此方法

```csharp
  private bool IsWord(string text)
    {
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".svg" };
        if (imageExtensions.Any(text.Contains)) return false;
        return text.Any(char.IsLetter);
    } 
```

此选项检查“ 字” 是否真的是一个工作; 图像名称会破坏 EasyNMT 中的句子分割功能 。

3. `  var batchSize = 10;` - 这确定了翻译服务的批量大小。 EmpleNMT对可以一次性翻译的字数有一定限制(约500行,因此这里的10行一般是好的批量大小)。
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

5. `  ReinsertTranslatedStrings(document, translatedStrings.ToArray());` - 将翻译的字符串重新插入到文档中。 使用 MarkDig 的能力行走文档并替换文本字符串 。

```csharp

    private void ReinsertTranslatedStrings(MarkdownDocument document, string[] translatedStrings)
    {
        int index = 0;

        foreach (var node in document.Descendants())
        {
            if (node is LiteralInline literalInline && index < translatedStrings.Length)
            {
                var content = literalInline.Content.ToString();
         
                if (!IsWord(content)) continue;
                literalInline.Content = new Markdig.Helpers.StringSlice(translatedStrings[index]);
                index++;
            }
        }
    }
```

## 东道服务处

要运行所有这一切,我使用一个 IHOSTED Lifetime Service, 它在程序. cs 文件中启动 。 此服务读取标记文件, 翻译为多种语言, 并将译出的文件写成磁盘 。

```csharp
  public async Task StartedAsync(CancellationToken cancellationToken)
    {
        if(!await blogService.IsServiceUp(cancellationToken))
        {
            logger.LogError("Translation service is not available");
            return;
        }
        ParallelOptions parallelOptions = new() { MaxDegreeOfParallelism = blogService.IPCount, CancellationToken = cancellationToken};
        var files = Directory.GetFiles(markdownConfig.MarkdownPath, "*.md");

        var outDir = markdownConfig.MarkdownTranslatedPath;

        var languages = translateServiceConfig.Languages;
        foreach(var language in languages)
        {
            await Parallel.ForEachAsync(files, parallelOptions, async (file,ct) =>
            {
                var fileChanged = await file.IsFileChanged(outDir);
                var outName = Path.GetFileNameWithoutExtension(file);

                var outFileName = $"{outDir}/{outName}.{language}.md";
                if (File.Exists(outFileName) && !fileChanged)
                {
                    return;
                }

                var text = await File.ReadAllTextAsync(file, cancellationToken);
                try
                {
                    logger.LogInformation("Translating {File} to {Language}", file, language);
                    var translatedMarkdown = await blogService.TranslateMarkdown(text, language, ct);
                    await File.WriteAllTextAsync(outFileName, translatedMarkdown, cancellationToken);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error translating {File} to {Language}", file, language);
                }
            });
        }
       
```

正如你所看到的,它也检查了文件的散列, 看文件翻译前是否有更改 。 这是为了避免翻译尚未更改的文件 。

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
});
```

我设置了托管服务(地下翻译服务)和马克唐翻译服务 HttpClient。
托管服务是一种长期服务,在背景中运行。 这是一个很好的地方 将服务 需要持续运行 在背景 或只是花一段时间完成。 新的IHED 终身服务界面比旧的IHED Services界面灵活一些,让我们比旧的IHED Services更容易在背景中完全执行任务。

这里你可以看到 我把HttpClient的超时时间设定为15分钟 这是因为容易NMT可能反应缓慢(特别是首次使用语言模式)。 我还在设定运行 EasyNMT 服务的机器的IP IP IP 的基址 。

## 在结论结论中

这是将标记文件翻译成多种语言的简单方法。 这不是完美的,但它是一个良好的开始。 通常我为每个新博客文章都做这个, `MarkdownBlogService` 将每个博客文章的翻译名调出来。