# 使用有 ASP.NET 核心的图像Sharp.web

<datetime class="hidden">2024-08-13T14:16</datetime>

<!--category-- ASP.NET, ImageSharp -->
## 一. 导言 导言 导言 导言 导言 导言 一,导言 导言 导言 导言 导言 导言

[图像共享](https://docs.sixlabors.com/index.html) 是一个强大的图像处理库, 允许您以各种方式操作图像 。 图像Sharp.Web是图像Sharp的延伸,为使用ASP.NET核心应用程序中的图像提供了额外的功能。 在此教程中, 我们将探索如何使用 imageSharp. Web 来调整此应用程序中的图像大小、 裁剪和格式 。

[技选委

## 图像Sharrp.web安装

要以 imageSharp.Web 启动, 您需要安装以下 Nuget 软件包 :

```bash
dotnet add package SixLabors.ImageSharp
dotnet add package SixLabors.ImageSharp.Web
```

## 图像Sharp. Web 配置

在我们的程序. cs 文件中,我们然后设置了图像Sharp. Web。 以我们为例, 我们用一个名为“图像”的文件夹来描述和储存我们的图像, 然后我们设置了图像Sharp. Web 中间软件, 将此文件夹用作我们图像的来源 。

图像Sharp. Web 还使用“ 缓存” 文件夹存储已处理的文件( 这样可以防止它每次调阅文件 ) 。

```csharp
services.AddImageSharp().Configure<PhysicalFileSystemCacheOptions>(options => options.CacheFolder = "cache");
```

这些文件夹与 wwwroot 有关, 所以我们有以下结构 :

![文件夹结构](/cachefolder.png)

图像 Sharp. Web 有多个选项, 用于存储您的文件和缓存( 详情请参见这里 : [https://docs. sixlabors.com/articles/imagesharp.web/imageprovideers.html?tabs=tabid-1%2Ctabid-1a 。](https://docs.sixlabors.com/articles/imagesharp.web/imageproviders.html?tabs=tabid-1%2Ctabid-1a))

例如,要将您的图像存储在 Azure blob 容器( 缩放方便) 中, 您可以使用 Azure BlobCache 选项的 Azure 提供方 :

```bash
dotnet add SixLabors.ImageSharp.Web.Providers.Azure
```

```csharp
// Configure and register the containers.  
// Alteratively use `appsettings.json` to represent the class and bind those settings.
.Configure<AzureBlobStorageImageProviderOptions>(options =>
{
    // The "BlobContainers" collection allows registration of multiple containers.
    options.BlobContainers.Add(new AzureBlobContainerClientOptions
    {
        ConnectionString = {AZURE_CONNECTION_STRING},
        ContainerName = {AZURE_CONTAINER_NAME}
    });
})
.AddProvider<AzureBlobStorageImageProvider>()
```

## 图像Sharp.Web使用率

现在我们有了这个设置 申请中使用它就很简单了 例如,如果我们想要服务于一个变缩图像, 我们可以同时使用 [标签帮助者](https://sixlabors.com/posts/announcing-imagesharp-web-300/#imagetaghelper) 或直接指定 URL。

标签求助器 :

```razor
<img
    src="sixlabors.imagesharp.web.png"
    imagesharp-width="300"
    imagesharp-height="200"
    imagesharp-rmode="ResizeMode.Pad"
    imagesharp-rcolor="Color.LimeGreen" />

```

在此请注意, 我们正在调整图像的大小, 设置宽度和高度, 并设置调整模式和重新显示图像的颜色 。

在此应用程序中,我们用更简单的方式使用查询参数。 对于标记,我们使用一个扩展名,让我们可以指定图像大小和格式。

```csharp
    public void ChangeImgPath(MarkdownDocument document)
    {
        foreach (var link in document.Descendants<LinkInline>())
            if (link.IsImage)
            {
                if(link.Url.StartsWith("http")) continue;
                
                if (!link.Url.Contains("?"))
                {
                   link.Url += "?format=webp&quality=50";
                }

                link.Url = "/articleimages/" + link.Url;
            }
               
    }
```

这样一来,我们就可以在诸如

```markdown
![image](/image.jpg?format=webp&quality=50)
```

此图像将来自何方 `wwwroot/articleimages/image.jpg` 并调整大小,使其质量达到50%,并采用Webp格式。

或者我们只要按原样使用图像, 就可以按照查询字符串中指定的方式调整图像大小和格式 。

## 嵌嵌入器

注注: `cache` 上面我用的焊接器 需要被申请书写下来 如果你在利用多克 你就得确保情况如此
见见 [我先前的岗位](/blog/imagesharpwithdocker) 用于我如何使用绘制的音量来管理它 。

## 结论 结论 结论 结论 结论

正如你所看到的图像Sharp.Web 给了我们一个巨大的能力 来调整我们的 ASP.NET 核心应用程序中的图像大小和格式。 它很容易设置和使用, 并提供了许多灵活性 如何在应用程序中操作图像。