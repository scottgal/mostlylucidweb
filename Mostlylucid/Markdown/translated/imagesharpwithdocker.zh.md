# 使用 Dockcker 图像擦亮

<datetime class="hidden">2024-08-001T01:00</datetime>

<!--category-- Docker, ImageSharp -->
图像Sharp是使用.NET中图像的伟大图书馆。 它很快,易于使用, 并且有很多特征。 我将展示如何使用图像Sharp与Docker建立简单的图像处理服务。

## 影像Sharp是什么?

图像Sharp让我能够无缝地使用.NET中的图像。 这是一个跨平台的图书馆, 支持各种图像格式, 为图像处理提供一个简单的 API 。 速度快,效率高,容易使用

不过我用Docker和图像Sharp设计时 有个问题。 当我试图从文件装入图像时, 我得到以下错误 :
"拒绝进入路径/wwroot/cache/等等..."
这是由 Docker ASP. NET 安装造成, 不允许写入缓存目录图像Sharp 用于存储临时文件 。

## 解决方案

解决办法是在嵌入器容器中挂载一个音量,该音量指向主机的目录。 此方式, 图像分享库可以毫无问题地写入缓存目录 。

以下是如何做到这一点:

```dockerfile
mostlylucid:
image: scottgal/mostlylucid:latest
volumes:
- /mnt/imagecache:/app/wwwroot/cache
```

您在这里看到我将 /app/ wwwroot/cache 文件映射到我主机的本地目录 。 此方式, 图像Sharp 可以写入缓存目录, 而不出现任何问题 。

在我Ubuntu的Ubuntu机器上,我创建了一个目录/Mnt/imagecache,然后运行了启动命令,使之可以写(对任何人来说,我知道这不安全,但我不是Linux 大师:)

```shell
chmod  777 -p /mnt/imagecache
```

我的程序里有一条线是:

```csharp
builder.Services.AddImageSharp().Configure<PhysicalFileSystemCacheOptions>(options => options.CacheFolder = "cache");
```

当缓存根默认为 wwwroot 时, 此选项将写入主机上的 / mnt/ imagecache 目录 。