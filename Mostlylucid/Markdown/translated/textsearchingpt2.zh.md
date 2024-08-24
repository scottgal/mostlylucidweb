# 全文搜索文本( Pt 2 - 打开搜索介绍)

<!--category-- OpenSearch -->
<datetime class="hidden">2024-08-24-03:00</datetime>

## 一. 导言 导言 导言 导言 导言 导言 一,导言 导言 导言 导言 导言 导言

在本系列的前面部分,我们介绍了全文搜索的概念,以及如何利用它来在数据库中搜索文本。 在这一部分,我们将介绍OpenSearch,这是一个强大的搜索引擎,可用于搜索文本。

在这一部分,我们将报道如何启动和运行 OpenSearch; 在下一部分,我将报道如何将数据输入公开搜索并查询。

本序列的先前部分 :

- [使用 Postgres 搜索完整文本](/blog/textsearchingpt1)
- [带有海报的搜索框](/blog/textsearchingpt11)

本系列的下一部分:

- [用 C # 打开搜索](/blog/textsearchingpt3)

[技选委

## 什么是OpenSearch号?

[开放搜索](https://opensearch.org/) 是一个设计迅速、可缩放和易于使用的搜索引擎。 它是Elasticsearch的分支,这是一家受欢迎的搜索引擎,许多公司都用它来为搜索功能提供动力。 OpenSearch的设计易于使用,可以不同方式用于搜索文本。

然而,这不是一个简单的野兽 得到和使用 所以我们将覆盖 基本在本文章。

![打开搜索](opensearch.webp?width=900&quality=25)

## 安装 OpenSearch 安装

首先,这不是你生产时应该使用的配置; 它设置了一些不安全的演示用户和密码。 你应该读读 [正式文件 正式文件](https://opensearch.org/docs/) 以设置一个安全组群。

首先,我们正在使用 Opensearch & Opensearch Dashboards 的默认“ 开发” 嵌入器安装( 思维, 管理集的 UI ) 。

详情请见此 [docker 编曲设置](https://opensearch.org/docs/latest/install-and-configure/install-opensearch/docker/).

你需要给两个Wsl/你的Linux主机 做一个小调子 让它顺利运作:
Linux 设置
对于 Linux 环境,运行以下命令:

使主机无法使用内存传动和互换性能来改进性能。

```bash
sudo swapoff -a
```

增加可供 OpenSearch 使用的记忆图数量。

```bash

# Edit the sysctl config file
sudo vi /etc/sysctl.conf

# Add a line to define the desired value
# or change the value if the key exists,
# and then save your changes.
vm.max_map_count=262144

# Reload the kernel parameters using sysctl
sudo sysctl -p

# Verify that the change was applied by checking the value
cat /proc/sys/vm/max_map_count

```

Windows 设置
对于通过 Docker 桌面使用 WSL 操作 WSL 的 Windows 工作量, 在终端中运行以下命令以设定 vm.max_map_count :

```bash
wsl -d docker-desktop
sysctl -w vm.max_map_count=262144
```

然后,你可以创建一个 `.env` 在与您相同的目录中的文件 `docker-compose.yml` 包含以下内容的文件 :
`bash OPENSEARCH_INITIAL_ADMIN_PASSWORD=<somepasswordwithlowercaseuppercaseandspecialchars> `

现在您使用以下 docker 组合文件来设置集 :

```yaml
version: '3'
services:
  opensearch-node1: # This is also the hostname of the container within the Docker network (i.e. https://opensearch-node1/)
    image: opensearchproject/opensearch:latest # Specifying the latest available image - modify if you want a specific version
    container_name: opensearch-node1
    environment:
      - cluster.name=opensearch-cluster # Name the cluster
      - node.name=opensearch-node1 # Name the node that will run in this container
      - discovery.seed_hosts=opensearch-node1,opensearch-node2 # Nodes to look for when discovering the cluster
      - cluster.initial_cluster_manager_nodes=opensearch-node1,opensearch-node2 # Nodes eligible to serve as cluster manager
      - bootstrap.memory_lock=true # Disable JVM heap memory swapping
      - "OPENSEARCH_JAVA_OPTS=-Xms512m -Xmx512m" # Set min and max JVM heap sizes to at least 50% of system RAM
      - OPENSEARCH_INITIAL_ADMIN_PASSWORD=${OPENSEARCH_INITIAL_ADMIN_PASSWORD}    # Sets the demo admin user password when using demo configuration, required for OpenSearch 2.12 and later
    ulimits:
      memlock:
        soft: -1 # Set memlock to unlimited (no soft or hard limit)
        hard: -1
      nofile:
        soft: 65536 # Maximum number of open files for the opensearch user - set to at least 65536
        hard: 65536
    volumes:
      - opensearch-data1:/usr/share/opensearch/data # Creates volume called opensearch-data1 and mounts it to the container
    ports:
      - 9200:9200 # REST API
      - 9600:9600 # Performance Analyzer
    networks:
      - opensearch-net # All of the containers will join the same Docker bridge network
  opensearch-node2:
    image: opensearchproject/opensearch:latest # This should be the same image used for opensearch-node1 to avoid issues
    container_name: opensearch-node2
    environment:
      - cluster.name=opensearch-cluster
      - node.name=opensearch-node2
      - discovery.seed_hosts=opensearch-node1,opensearch-node2
      - cluster.initial_cluster_manager_nodes=opensearch-node1,opensearch-node2
      - bootstrap.memory_lock=true
      - "OPENSEARCH_JAVA_OPTS=-Xms512m -Xmx512m"
      - OPENSEARCH_INITIAL_ADMIN_PASSWORD=${OPENSEARCH_INITIAL_ADMIN_PASSWORD}
    ulimits:
      memlock:
        soft: -1
        hard: -1
      nofile:
        soft: 65536
        hard: 65536
    volumes:
      - opensearch-data2:/usr/share/opensearch/data
    networks:
      - opensearch-net
  opensearch-dashboards:
    image: opensearchproject/opensearch-dashboards:latest # Make sure the version of opensearch-dashboards matches the version of opensearch installed on other nodes
    container_name: opensearch-dashboards
    ports:
      - 5601:5601 # Map host port 5601 to container port 5601
    expose:
      - "5601" # Expose port 5601 for web access to OpenSearch Dashboards
    environment:
      OPENSEARCH_HOSTS: '["https://opensearch-node1:9200","https://opensearch-node2:9200"]' # Define the OpenSearch nodes that OpenSearch Dashboards will query
    networks:
      - opensearch-net

volumes:
  opensearch-data1:
  opensearch-data2:

networks:
  opensearch-net:
```

此 docker 配置文件将设置一个 2 节点群集的 Opensearch 和 Opensearch Dashboard 的单一节点 。

它依靠您的. env 文件设置 `OPENSEARCH_INITIAL_ADMIN_PASSWORD` 变量。

然后,只是旋转起来。

```bash
docker compose -f opensearch-docker.yml up -d
```

未来我们会深入了解它正在做什么 以及如何为生产配置它

### 访问开放搜索

一旦您将集集建立并运行,您就可以通过导航访问 OpenSearch Dashboard 用户界面 `http://localhost:5601` 在您的网络浏览器中。 您可以使用用户名登录 `admin` 和您在 `.env` 文件。

![打开搜索仪表板](opensearchdashboards.png?width=600&format=webp&quality=25)

这是您的主要管理界面( 您可以用一些样本数据来填充它, 并使用它来玩 ) 。

![打开搜索仪表板](dashboard.png?width=600&format=webp&quality=25)

## 在结论结论中

下一部分我将介绍如何将数据输入公开搜索并查询。