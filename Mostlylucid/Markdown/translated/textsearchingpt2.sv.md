# Fulltextsökning (Pt 2 - Introduktion till OpenSearch)

<!--category-- OpenSearch -->
<datetime class="hidden">2024-08-24T03:00</datetime>

## Inledning

I de tidigare delarna av denna serie introducerade vi begreppet fulltextsökning och hur den kan användas för att söka efter text i en databas. I den här delen introducerar vi OpenSearch, en kraftfull sökmotor som kan användas för att söka efter text.

I denna del kommer vi att täcka hur du får OpenSearch igång; i nästa del kommer jag att täcka hur du får data i opensearch och fråga det.

[TOC]

## Vad är OpenSearch?

[OpenSearch](https://opensearch.org/) är en sökmotor som är utformad för att vara snabb, skalbar och lätt att använda. Det är en förskjutning av Elasticsearch, en populär sökmotor som används av många företag för att driva sin sökfunktion. OpenSearch är utformad för att vara lätt att använda och kan användas för att söka efter text på en mängd olika sätt.

Men det är inte en enkel best att komma igång och använda så vi kommer att täcka grunderna i denna artikel.

![Öppen sökning](opensearch.webp?width=900&quality=25)

## Installera OpenSearch

För det första är detta inte en konfiguration som du bör använda i produktionen; det ställer upp ett antal demoanvändare och lösenord som inte är säkra. Du bör läsa [officiell dokumentation](https://opensearch.org/docs/) Att inrätta ett säkert kluster.

Först använder vi standard 'Utveckling' docker installera av Opensearch & Opensearch Dashboards (tänk, UI för att hantera kluster).

Se här för alla detaljer om [Docker komponera inställning](https://opensearch.org/docs/latest/install-and-configure/install-opensearch/docker/).

Du måste göra en liten tweak till antingen wsl / din linux värd för att få det att fungera smidigt:
Linuxinställningar
För en Linux-miljö, kör följande kommandon:

Inaktivera minnessökning och byta prestanda på värden för att förbättra prestanda.

```bash
sudo swapoff -a
```

Öka antalet minneskartor tillgängliga för OpenSearch.

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

Fönsterinställningar
För Windows arbetsbelastningar med WSL genom Docker Desktop, kör följande kommandon i en terminal för att ställa in vm.max_map_count:

```bash
wsl -d docker-desktop
sysctl -w vm.max_map_count=262144
```

Då kan du skapa en `.env` fil i samma katalog som din `docker-compose.yml` fil med följande innehåll:
`bash OPENSEARCH_INITIAL_ADMIN_PASSWORD=<somepasswordwithlowercaseuppercaseandspecialchars> `

Nu använder du följande Docker-compose-fil för att ställa in kluster:

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

Denna Docker-compose fil kommer att ställa upp en 2 nod kluster av Opensearch och en enda nod av Opensearch Dashboards.

Den förlitar sig på din.env-fil för att ställa in `OPENSEARCH_INITIAL_ADMIN_PASSWORD` variabel.

Snurra upp det där.

```bash
docker compose -f opensearch-docker.yml up -d
```

I framtiden kommer vi att gå djupare in på vad detta gör och hur man konfigurerar det för produktion.

### Åtkomst till OpenSearch

När du har klungan igång kan du komma åt OpenSearch Dashboards UI genom att navigera till `http://localhost:5601` i din webbläsare. Du kan logga in med användarnamnet `admin` och lösenordet du anger i `.env` En akt.

![Opensearch- tavlorName](opensearchdashboards.png?width=600&format=webp&quality=25)

Detta är ditt huvudsakliga admin-gränssnitt (du kan fylla det med några provdata och spela runt med det).

![Opensearch- tavlorName](dashboard.png?width=600&format=webp&quality=25)

## Slutsatser

I nästa del ska jag täcka hur man får in data i opensearch och fråga det.