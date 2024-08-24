# Повний пошук тексту (Pt 2 - Вступ до OpenSearch)

<!--category-- OpenSearch -->
<datetime class="hidden">2024- 08- 24T03: 00</datetime>

## Вступ

У попередніх частинах цієї серії ми ввели концепцію повноформатного пошуку тексту і того, як ним можна скористатися для пошуку тексту у базі даних. У цій частині ми познайомимо OpenSearch, потужний пошуковий рушій, який можна використовувати для пошуку тексту.

У цій частині ми поговоримо про те, як запустити OpenSearch; у наступній частині я розповім про те, як отримати дані у відкритий пошук і запитати їх.

[TOC]

## Що таке OpenSearch?

[OpenSearch](https://opensearch.org/) є пошуковим рушієм, розробленим для швидкого, масштабованого і легкого використання. Це галузка еластичних пошуків, популярної пошукової системи, якою користуються багато компаній, щоб оприлюднити їхню функцію пошуку. OpenSearch розроблено для того, щоб його було легко використовувати, і ним можна скористатися для пошуку тексту різними способами.

Проте, це не просто тварина, щоб користуватися ним, тому ми будемо розглядати основи цієї статті.

![Opensearch](opensearch.webp?width=900&quality=25)

## Встановлення OpenSearch

По-перше, це не конфігурація, яку ви повинні використовувати у виробництві; вона встановлює кількість демонстраційних користувачів і паролів, які не є безпечними. Тобі слід прочитати [офіційна документація](https://opensearch.org/docs/) щоб створити безпечне скупчення.

По-перше, ми використовуємо типові дошки Opensearch і Opensearch (подумайте, інтерфейс інтерфейсу користувача для керування кластером).

Див. тут за всіма подробицями [Налаштування набору docker](https://opensearch.org/docs/latest/install-and-configure/install-opensearch/docker/).

Для того, щоб сервер linux працював плавно, вам слід створити невеличку конструкцію для wsl / вашого вузла linux:
Параметри Linux
Для середовища Linux виконайте такі команди:

Вимкнути швидкодію пересування пам' яті та перемикання даних на вузлі, щоб покращити швидкодію.

```bash
sudo swapoff -a
```

Збільшує кількість карт пам' яті для OpenSearch.

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

Параметри Windows
Для завантаження Windows за допомогою WSL за допомогою стільниці Docker виконайте такі команди у терміналі, щоб встановити vm. max_ map_ count:

```bash
wsl -d docker-desktop
sysctl -w vm.max_map_count=262144
```

Тоді можна створити `.env` файл у тому ж каталозі, що і ваш `docker-compose.yml` файл з таким вмістом:
`bash OPENSEARCH_INITIAL_ADMIN_PASSWORD=<somepasswordwithlowercaseuppercaseandspecialchars> `

Тепер ви можете скористатися таким файлом набору docker для налаштування списку:

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

За допомогою цього пункту можна встановити набір вузлів у 2 вузлах Opensearch і єдиний вузол на панель приладів Opensearch.

Він покладається на ваш файл. env для встановлення `OPENSEARCH_INITIAL_ADMIN_PASSWORD` змінна.

Тоді просто розкручуй це.

```bash
docker compose -f opensearch-docker.yml up -d
```

У майбутньому ми детальніше поговоримо про те, що це робить і як налаштувати виробництво.

### Доступ до OpenSearch

Після того, як ви запустите кластер, ви зможете отримати доступ до інтерфейсу OpenSearch Dispboards. `http://localhost:5601` у вашому браузері. Ви можете увійти до системи за допомогою імені користувача `admin` і пароль, який ви вказали `.env` файл.

![Панель приладів Opensearch](opensearchdashboards.png?width=600&format=webp&quality=25)

Це ваш головний адміністративний інтерфейс (ви можете заповнити його прикладними даними і погратися з ними).

![Панель приладів Opensearch](dashboard.png?width=600&format=webp&quality=25)

## Включення

У наступній частині я розповім, як отримати дані у відкритий пошук і запитати їх.