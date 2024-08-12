# 使用 Umami 进行局部分析分析

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-008T15:53</datetime>

## 一. 导言 导言 导言 导言 导言 导言 一,导言 导言 导言 导言 导言 导言

令我对目前设置感到不快的一件事是,我不得不使用谷歌分析工具获取访问者数据(其中有多少????????? 。 )所以我想找到一些可以自我主机的东西,没有将数据传送到谷歌或其他第三方。 我找到了一些可以自我主机的东西,没有将数据传送到谷歌或其他第三方。[木美](https://umami.is/)这是一个简单、自我托管的网络分析解决方案。它是谷歌分析的伟大替代方案,并且(相对而言)很容易建立。

[技选委

## 安装安装

安装很简单,但花了 相当小的摆弄才真正开始...

### docker 合成器

我想把乌玛美 加入我目前的套套套中 我需要为我的新服务`docker-compose.yml`。 我将以下内容添加到文件底部 :

```yaml
  umami:
    image: ghcr.io/umami-software/umami:postgresql-latest
    env_file: .env
    environment:
      DATABASE_URL: ${DATABASE_URL}
      DATABASE_TYPE: ${DATABASE_TYPE}
      HASH_SALT: ${HASH_SALT}
      APP_SECRET: ${APP_SECRET}
      TRACKER_SCRIPT_NAME: getinfo
      API_COLLECT_ENDPOINT: all
    ports:
      - "3000:3000"
    depends_on:
      - db
    networks:
      - app_network
    restart: always
  db:
    image: postgres:16-alpine
    env_file:
      - .env
    networks:
      - app_network
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER}"]
      interval: 5s
      timeout: 5s
      retries: 5
    volumes:
      - /mnt/umami/postgres:/var/lib/postgresql/data
    restart: always
  cloudflaredumami:
    image: cloudflare/cloudflared:latest
    command: tunnel --no-autoupdate run --token ${CLOUDFLARED_UMAMI_TOKEN}
    env_file:
      - .env
    restart: always
    networks:
      - app_network


```

此 docker-competect. yml 文件包含以下设置 :

1. 名为新服务的新服务`umami`使用`ghcr.io/umami-software/umami:postgresql-latest`此服务用于运行 Umami 分析服务 。
2. 名为新服务的新服务`db`使用`postgres:16-alpine`此服务用于运行Umami用来存储数据的Postgres数据库。
   此服务的注释, 我将它映射到服务器上的目录上, 这样数据会在重新启动之间持续 。

```yaml
    volumes:
      - /mnt/umami/postgres:/var/lib/postgresql/data
```

您需要这位导演的存在, 并可以被服务器上的docker用户写写( 而不是Linux专家, 所以777人可能在这里超杀!) )!

```shell
chmod 777 /mnt/umami/postgres
```

3. 名为新服务的新服务`cloudflaredumami`使用`cloudflare/cloudflared:latest`图像 。 此服务用于通过 Cloudflare 通过 Cloudflare 隧道连接 Umami 服务, 以便从互联网上访问它 。

### 信封文件

为支持这一支持,我还更新了`.env`包括以下文件:

```shell
CLOUDFLARED_UMAMI_TOKEN=<cloudflaretoken>
DATABASE_TYPE=postgresql
HASH_SALT=<salt>

POSTGRES_DB=postgres
POSTGRES_USER=<postgresuser>
POSTGRES_PASSWORD=<postgrespassword>
UMAMI_SECRET=<umamisecret>

APP_SECRET=${UMAMI_SECRET}
UMAMI_USER=${POSTGRES_USER}
UMAMI_PASS=${POSTGRES_PASSWORD}
DATABASE_URL=postgresql://${UMAMI_USER}:${UMAMI_PASS}@db:5432/${POSTGRES_DB}
```

这将设置 docker 曲组的配置( the`<>`elemets 显然需要替换为您自己的值 。`cloudflaredumami`服务用于通过云雾隧道连接 Umami 服务, 以便从互联网上访问它。 使用 BASE_ PATH 是可能的, 但对于 Umami 来说, 它讨厌地需要重建来改变基础路径, 因此我离开它作为现在的根路径 。

### Cloudflare Cloudflare 隧道

为此设置云层隧道( 作为用于分析的 js 文件路径- getinfo.js) 我使用网站:

![Cloudflare Cloudflare 隧道](umamisetup.png)

设置通向Umami服务的隧道, 并允许它从互联网上接入。 注意, 我将它指向`umami`在 docker- comption 文件中的服务( 它与云点隧道的网络相同, 是一个有效名称 ) 。

### 页面中的 Umami 设置

启用脚脚脚脚脚的路径`getinfo`我在上面的设置中增加了一个配置条目。

```json
 "Analytics":{
   "UmamiPath" : "https://umamilocal.mostlylucid.net/getinfo",
   "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee"
 },
```

您也可以将这些添加到您的 env 文件中, 并将其作为环境变量传送到 docker- complect 文件中 。

```shell
ANALYTICS__UMAMIPATH="https://umamilocal.mostlylucid.net/getinfo"
ANALYTICS_WEBSITEID="32c2aa31-b1ac-44c0-b8f3-ff1f50403bee"
```

```yaml
  mostlylucid:
    image: scottgal/mostlylucid:latest
    ports:
      - 8080:8080
    restart: always
    environment:
    ...
      - Analytics__UmamiPath=${ANALYTICS_UMAMIPATH}
      - Analytics__WebsiteId=${ANALYTICS_WEBSITEID}
```

当您设置网站时, 您可以在 Umami 仪表板上设置网站标识 。 (注意 Umami 服务的默认用户名和密码是 Umami 服务默认用户名和密码 。`admin`和`umami`,你需要改变这些设置后)。
![Umami 仪表板](umamiaddwebsite.png)

相关设置 cs 文件 :

```csharp
public class AnalyticsSettings : IConfigSection
{
    public static string Section => "Analytics";
    public string? UmamiPath { get; set; }
}
```

这再次使用我的 POCO 配置( POCO 配置) 。[在这里](/blog/addingidentityfreegoogleauth#configuring-google-auth-with-poco)设置设置。
把它设置在我的程序,cs:

```csharp
builder.Configure<AnalyticsSettings>();
```

终于在我的`BaseController.cs` `OnGet`我添加了以下方法来设置分析脚本的路径 :

```csharp
   public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        if (!Request.IsHtmx())
        {
            ViewBag.UmamiPath = _analyticsSettings.UmamiPath;
            ViewBag.UmamiWebsiteId = _analyticsSettings.WebsiteId;
        }
        base.OnActionExecuting(filterContext);
    }
    
```

这将设置布局文件中使用的分析脚本的路径 。

### 布局文件

最后,我在我的布局文件中添加了以下内容, 以包括分析脚本:

```html
<script defer src="@ViewBag.UmamiPath" data-website-id="@ViewBag.UmamiWebsiteId"></script>
```

这包括页面中的脚本, 并设置分析服务的网站 ID 。

## 将自己排除在分析之外

为了将您自己的访问从分析数据中排除,您可以在浏览器中添加以下本地存储内容:

在 Chrome Dev 工具( 窗口上的 Ctrl+Shift+I) 中, 您可以在控制台中添加以下内容 :

```javascript
localStorage.setItem("umami.disabled", 1)
```

## 结论 结论 结论 结论 结论

这是一个有点虚构的设置, 但我对结果很满意。 我现在有一个自办的解析服务, 它不会将数据传送到谷歌或其他第三方。 设置起来有点麻烦, 但是一旦完成, 它就很容易使用。 我对结果很满意, 并且会推荐给任何寻找自办解析解决方案的人 。