# 使用 Dockcker 图像擦亮

<datetime class="hidden">2024-08-001T01:00</datetime>

<!--category-- Docker, ImageSharp -->
图像 Sharp 是使用. NET 中图像的伟大图书馆。 它快速、 容易使用, 并且有许多功能。 在此文章中, 我将展示您如何使用图像 Sharp 与 Docker 一起创建简单的图像处理服务 。

## 影像Sharp是什么?

图像Sharp 使我能够无缝地使用.NET 中的图像。 它是一个跨平台库, 支持各种图像格式, 并为图像处理提供一个简单的 API 。 它快速、高效且易于使用 。

然而在我的设置中, 使用 docker 和 imageSharp 存在一个问题。 当我试图从文件装入图像时, 我发现错误如下 :
"拒绝进入路径/wwroot/cache/等等..."
这是由 Docker ASP. NET 安装造成, 不允许写入缓存目录图像Sharp 用于存储临时文件 。

## 解决方案

解决方案是在 docker 容器中挂载一个显示主机目录的音量。 这样, 图像沙尔普 库就可以在不出现任何问题的情况下写入缓存目录 。

以下是如何做到这一点:

```dockerfile
mostlylucid:
image: scottgal/mostlylucid:latest
volumes:
- /mnt/imagecache:/app/wwwroot/cache
```

您在这里看到我将 /app/ wwwroot/cache 文件映射到我主机的本地目录。 这样, 图像Sharp 可以毫无问题地写入缓存目录 。

在我Ubuntu的Ubuntu机器上,我创建了一个目录/Mnt/imagecache,然后运行了启动命令,使之可以写(对任何人来说,我知道这不安全,但我不是Linux 大师:)

```shell
chmod  777 -p /mnt/imagecache
```

我的程序里有一条线是:

```csharp
builder.Services.AddImageSharp().Configure<PhysicalFileSystemCacheOptions>(options => options.CacheFolder = "cache");
```

当缓存根默认为 wwwroot 时, 此选项将写入主机上的 / mnt/ imagecache 目录 。