# 开发依赖使用嵌入式混音符号

<!--category-- Docker -->
<datetime class="hidden">2024-08-009T17:17</datetime>

# 一. 导言 导言 导言 导言 导言 导言 一,导言 导言 导言 导言 导言 导言

当开发软件时, 我们通常会旋转一个数据库, 信息队列, 缓存, 以及一些其他的服务。 这可能会成为管理上的一个麻烦, 特别是如果您正在从事多个工程的话。 Docker Compose 是一个工具, 使您能够定义和运行多容器 Docker 应用程序。 这是管理您的开发依赖性的一个很好的方法 。

我将教你们如何使用多克·康普斯 来管理你们的发展依赖关系。

[技选委

# 先决条件

首先您需要将 docker 桌面安装在您使用的任何平台上。 您可以从[在这里](https://www.docker.com/products/docker-desktop).

**注意: 我发现在 Windows 上您真的需要运行 Docker 桌面安装器作为管理器, 以确保它安装正确 。**

# 创建 docker 撰写文件

Docker Compose 使用 YAML 文件来定义您想要运行的服务 。 这是简单的示例`devdeps-docker-compose.yml`定义数据库服务的文件 :

```yaml
services: 
  smtp4dev:
    image: rnwood/smtp4dev
    ports:
      - "3002:80"
      - "2525:25"
    volumes:
      # This is where smtp4dev stores the database..
      - e:/smtp4dev-data:/smtp4dev
    restart: always
  postgres:
    image: postgres:16-alpine
    container_name: postgres
    ports:
      - "5432:5432"
    env_file:
      - .env
    volumes:
      - e:/data:/var/lib/postgresql/data  # Map e:\data to the PostgreSQL data folder
    restart: always	
networks:
  mynetwork:
        driver: bridge
```

注意,我在这里指定了 坚持每个服务的数据的量, 这里我指定了

```yaml
    volumes:
      # This is where smtp4dev stores the database..
      - e:/smtp4dev-data:/smtp4dev
    volumes:
        - e:/data:/var/lib/postgresql/data  # Map e:\data to the PostgreSQL data folder
```

这确保了在集装箱运行之间数据始终存在。

我还具体说明`env_file`排和排`postgres`服务。此文件包含传送到容器上的环境变量。
您可以看到可以传递到 PostgreSQL 容器的环境变量列表[在这里](https://www.docker.com/blog/how-to-use-the-postgres-docker-official-image/#1-Environment-variables).
举个例子`.env`文件 :

```shell
POSTGRES_DB=postgres
POSTGRES_USER=postgres
POSTGRES_PASSWORD=<somepassword>
```

此配置一个默认的 PostgreSQL 数据库、 密码和用户 。

在此我还运行 SMTP4Dev 服务, 这是在您的应用程序中测试电子邮件功能的极好工具。 您可以找到更多关于它的信息 。[在这里](https://github.com/rnwood/smtp4dev/wiki/Installation#how-to-run-smtp4dev-in-docker).

如果你看看我的`appsettings.Developmet.json`SMTP服务器的配置如下:

```json
  "SmtpSettings":
{
"Server": "smtp.gmail.com",
"Port": 587,
"SenderName": "Mostlylucid",
"Username": "",
"SenderEmail": "scott.galloway@gmail.com",
"Password": "",
"EnableSSL": "true",
"EmailSendTry": 3,
"EmailSendFailed": "true",
"ToMail": "scott.galloway@gmail.com",
"EmailSubject": "Mostlylucid"

}
```

这对 SMTP4Dev 有效, 它使我能够测试这个功能( 我可以发送到任何地址, 并在 http://localhost3002/) SMTP4Dev 界面上看到电子邮件 。

一旦你确定一切正常, 你可以测试一个真正的 SMTP 服务器, 比如 GMAAIL (例如, 见[在这里](addingasyncsendingforemails)如何做到这一点))

# 管理服务

运行此系统定义的服务`devdeps-docker-compose.yml`文件, 您需要与文件在同一目录中运行以下命令 :

```shell
docker compose -f .\devdeps-docker-compose.yml up -d
```

注意 注意 您应该先像这样运行它; 这样可以确保您可以看到从 NAME OF TRANSLATORS 中传递的配置元素 。`.env`文件。

```shell
docker compose -f .\devdeps-docker-compose.yml config
```

如果您现在在 Docker 桌面中查看, 您可以看到这些服务正在运行

![嵌入桌面桌面](dockerdesktopdev.png)