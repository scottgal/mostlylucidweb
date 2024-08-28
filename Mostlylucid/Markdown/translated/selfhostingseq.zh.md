# ASP.NET 登录自托管 Seq

<datetime class="hidden">2024-08-28T09:37</datetime>

<!--category-- ASP.NET, Seq, Serilog, Docker -->
# 一. 导言 导言 导言 导言 导言 导言 一,导言 导言 导言 导言 导言 导言

Seq 是一个允许您查看和分析日志的应用程序 。 这是一个很好的工具 调试和监测你的应用程序。 本文将报导我如何成立Seq来记录我的 ASP. NET核心应用程序。
无法拥有太多的仪表板 :)

![SeqDashboard 键盘](seqdashboard.png)

[技选委

# 建立 Seq

赛克有几口香味 您可以使用云的版本或自行主机 。 我选择自己主办它 因为我想保持我的日志隐私。

我首先访问Seq网站,发现 [嵌入器安装指令](https://docs.datalust.co/docs/getting-started-with-docker).

## 当地(当地)

要在本地运行,您首先需要获得一个散列密码 。 您可以运行以下命令来做到这一点 :

```bash
echo '<password>' | docker run --rm -i datalust/seq config hash
```

要在本地运行此命令, 您可以使用以下命令 :

```bash
docker run --name seq -d --restart unless-stopped -e ACCEPT_EULA=Y -e SEQ_FIRSTRUN_ADMINPASSWORDHASH=<hashfromabove> -v C:\seq:/data -p 5443:443 -p 45341:45341 -p 5341:5341 -p 82:80 datalust/seq

```

在我的Ubuntu本地机器上,我把它制作成一个剧本:

```shell
#!/bin/bash
PH=$(echo 'Abc1234!' | docker run --rm -i datalust/seq config hash)

mkdir -p /mnt/seq
chmod 777 /mnt/seq

docker run \
  --name seq \
  -d \
  --restart unless-stopped \
  -e ACCEPT_EULA=Y \
  -e SEQ_FIRSTRUN_ADMINPASSWORDHASH="$PH" \
  -v /mnt/seq:/data \
  -p 5443:443 \
  -p 45341:45341 \
  -p 5341:5341 \
  -p 82:80 \
  datalust/seq
```

然后

```shell
chmod +x seq.sh
./seq.sh
```

这样你就能起跑跑起来 然后去 `http://localhost:82` / `http://<machineip>:82` 查看后续安装(默认管理员密码是您输入的密码) <password> 上文。

## 在 docker 中

我在我的Docker作曲档案中添加了以下内容:

```docker
  seq:
    image: datalust/seq
    container_name: seq
    restart: unless-stopped
    environment:
      ACCEPT_EULA: "Y"
      SEQ_FIRSTRUN_ADMINPASSWORDHASH: ${SEQ_DEFAULT_HASH}
    volumes:
      - /mnt/seq:/data
    networks:
      - app_network
```

请注意,我有一个名为 `/mnt/seq` (对于窗口,请使用窗口路径) 这里将存储日志 。

我也有一个 `SEQ_DEFAULT_HASH` 环境变量,该变量是我.env 文件中管理用户的散列密码。

# 建立ASP.NET核心

随我用 [血清](https://serilog.net/) 对我来说 建立Seq其实是很容易的 它甚至有医生 如何做到这一点 [在这里](https://docs.datalust.co/docs/using-serilog).

基本上,你只要把水槽加进你的项目:

```shell
dotnet add package Serilog.Sinks.Seq
```

我更喜欢用 `appsettings.json` 我为我的配置,所以我只有 在我的"标准"设置 `Program.cs`:

```csharp
builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
    Serilog.Debugging.SelfLog.Enable(Console.Error);
    Console.WriteLine($"Serilog Minimum Level: {configuration.MinimumLevel.ToString()}");
});
```

然后在我的“appgts.json”中,我有这种配置

```json
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File", "Serilog.Sinks.Seq"],
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
    "Enrich": ["FromLogContext", "WithMachineName"],
    "Properties": {
      "ApplicationName": "mostlylucid"
    }
  }

```

你会看到我有一个 `serverUrl` 联 联 年 月 日 月 日 月 月 日 `http://seq:5341`.. 是因为我还有下几个下集 在一个叫Docker的容器里跑来跑去 `seq` 在港口 `5341`.. 如果你在本地经营 你可以使用 `http://localhost:5341`.
我还使用 API 密钥, 这样我就可以使用密钥动态指定日志级别( 您可以设置密钥只接收一定水平的日志消息 ) 。

你设置它 在你的后继实例 通过去 `http://<machine>:82` 单击右上方的设置。 然后点击 `API Keys` 选项卡,并添加新密钥。 然后您可以使用此密钥 `appsettings.json` 文件。

![Seq 单位](seqapikey.png)

# docker 合成器

现在我们有了这个设置, 我们需要配置 ASP. NET 应用程序来获取密钥 。 我用一个 `.env` 保存我的秘密文件 。

```dotenv
SEQ_DEFAULT_HASH="<adminpasswordhash>"
SEQ_API_KEY="<apikey>"
```

然后在我的 docker 拼写文件中, 我指定该值应该作为环境变量 注入到我的 ASP. NET 应用程序中 :

```docker
services:
  mostlylucid:
    image: scottgal/mostlylucid:latest
    ports:
      - 8080:8080
    restart: always
    labels:
        - "com.centurylinklabs.watchtower.enable=true"
    env_file:
      - .env
    environment:
      - Auth__GoogleClientId=${AUTH_GOOGLECLIENTID}
      - Auth__GoogleClientSecret=${AUTH_GOOGLECLIENTSECRET}
      - Auth__AdminUserGoogleId=${AUTH_ADMINUSERGOOGLEID}
      - SmtpSettings__UserName=${SMTPSETTINGS_USERNAME}
      - SmtpSettings__Password=${SMTPSETTINGS_PASSWORD}
      - Analytics__UmamiPath=${ANALYTICS_UMAMIPATH}
      - Analytics__WebsiteId=${ANALYTICS_WEBSITEID}
      - ConnectionStrings__DefaultConnection=${POSTGRES_CONNECTIONSTRING}
      - TranslateService__ServiceIPs=${EASYNMT_IPS}
      - Serilog__WriteTo__0__Args__apiKey=${SEQ_API_KEY}
    volumes:
      - /mnt/imagecache:/app/wwwroot/cache
      - /mnt/markdown/comments:/app/Markdown/comments
      - /mnt/logs:/app/logs
    networks:
      - app_network
```

请注意, `Serilog__WriteTo__0__Args__apiKey` 设置为 `SEQ_API_KEY` 调自自 `.env` 文件。 "0"是"0"的索引 `WriteTo` 数组中的 `appsettings.json` 文件。

# 卡迪( 卡迪)

Seq和我的ASP.NET应用程序的笔记 `app_network` 网络。 这是因为我用Caddy作为反向代理 和它在同一网络上。 这意味着我可以在我的 Caddy 文件中使用服务名作为 URL 。

```caddy
{
    email scott.galloway@gmail.com
}
seq.mostlylucid.net
{
   reverse_proxy seq:80
}

http://seq.mostlylucid.net
{
   redir https://{host}{uri}
}
```

因此,这能够绘制地图 `seq.mostlylucid.net` 我接下来的事例。

# 结论 结论 结论 结论 结论

Seq是记录和监测您的应用程序的伟大工具。 它很容易建立,使用 和融合好与Serilog。 我发现它在调试我的应用程序方面 很有价值 我相信你也会的