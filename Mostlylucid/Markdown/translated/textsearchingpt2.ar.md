# البحث عن نص كامل (Pt 2 - مقدمة إلى OpSearch)

<!--category-- OpenSearch -->
<datetime class="hidden">2024-2024-08-24- الساعة 15:00</datetime>

## أولاً

وفي الأجزاء السابقة من هذه السلسلة أدخلنا مفهوم البحث الكامل عن النص وكيف يمكن استخدامه للبحث عن النص داخل قاعدة بيانات. في هذا الجزء سوف نقدم OpenSearch، محرك بحث قوي يمكن استخدامه للبحث عن نص.

في هذا الجزء سنقوم بتغطية كيفية الحصول على OpenSearch تشغيل وتشغيله ، في الجزء التالي سوف أغطي كيفية الحصول على البيانات في البحث المفتوح والاستعلام عنه.

[رابعاً -

## ما هو OpenSearch؟

[](https://opensearch.org/) هو محرك بحث مصمم ليكون سريعاً، قابلاً للتعديل وسهل الاستخدام. وهو من خارج Elasticsearch، وهو محرك بحث شعبي تستخدمه العديد من الشركات لتشغيل وظائف البحث الخاصة بها. وقد صُمِّم نظام OpenSearch بحيث يسهل استخدامه ويمكن استخدامه للبحث عن نص بطرق مختلفة متنوعة.

ومع ذلك فإنه ليس من السهل الحصول على الوحش للذهاب والاستخدام لذلك سوف نكون تغطية الأساسيات في هذه المادة.

![](opensearch.webp?width=900&quality=25)

## جاري تثالجل جاريكSSearSSSSSSSSSS

أولاً، هذا ليس تشكيلاً يجب أن تستخدمه في الإنتاج؛ فهو ينشئ عدداً من المستخدمين العرضيين وكلمات السر التي ليست آمنة. ينبغي أن تقرأ ما يلي: [](https://opensearch.org/docs/) لإنشاء مجموعة آمنة.

أولاً، نحن نستخدم التثبيت الافتراضي "التطوري" للتثبيت من مناظرات OpenSearch & OpenSearch (فكّر، الـ UI لإدارة المجموعة).

انظر هنا لكل التفاصيل عن [أُعِدّ إنشاء](https://opensearch.org/docs/latest/install-and-configure/install-opensearch/docker/).

سوف تحتاج إلى إجراء تعديل صغير إلى إما wsl/ linux المضيف لجعله يعمل بسلاسة:

لبيئة لينكس، نفّذ الأوامر التالية:

(ب) أداء التنبيه إلى الذاكرة المعطلة ومبادلتها على المضيف لتحسين الأداء.

```bash
sudo swapoff -a
```

زيادة عدد خرائط الذاكرة المتاحة إلى OpenSearch.

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

هذا
لـ لـ ويندوز لـ استخدام WSL أداء Doker file تنفيذ متابعة بوصة a شاشة طرفية إلى set vm.max_map_count:

```bash
wsl -d docker-desktop
sysctl -w vm.max_map_count=262144
```

ثم يمكنك أن تخلق `.env` في الملف نفسه في دليل `docker-compose.yml` ملفّ مع المحتوى التالي:
`bash OPENSEARCH_INITIAL_ADMIN_PASSWORD=<somepasswordwithlowercaseuppercaseandspecialchars> `

الآن أنت استخدام التالي Dokker- cumse ملفّ إلى setet المجموعة:

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

هذا ملفّ set a 2 عقدة مجموعة من OpenSearch و a واحد عقدة من OpenSearch Dasboards.

تعتمد على ملفك لضبط `OPENSEARCH_INITIAL_ADMIN_PASSWORD` متغيرات متغيرة.

ثمّ فقط يُدرّبُ الذي فوق.

```bash
docker compose -f opensearch-docker.yml up -d
```

في المستقبل سوف نتعمق أكثر حول ما يفعله هذا وكيفية ضبطه للإنتاج.

### الولوج مفتوحSSearch

بمجرد أن تكون المجموعة جاهزة و تعمل، يمكنك الوصول إلى OpenSearch Dashboards UI عن طريق الملاحة إلى `http://localhost:5601` في متصفح الإنترنت الخاص بك.  `admin` وكلمة السر التي وضعتها في `.env` ملف ملفّيّاً.

![فتحات](opensearchdashboards.png?width=600&format=webp&quality=25)

هذا هو واجهة admin الرئيسية (يمكنك ملئها ببعض بيانات العينة واللعب بها).

![فتحات](dashboard.png?width=600&format=webp&quality=25)

## في الإستنتاج

في الجزء التالي سأغطي كيفية الحصول على البيانات في مجال البحث المفتوح واستعلامها.