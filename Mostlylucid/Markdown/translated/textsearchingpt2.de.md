# Volltextsuche (Pt 2 - Einführung in OpenSearch)

<!--category-- OpenSearch -->
<datetime class="hidden">2024-08-24T03:00</datetime>

## Einleitung

In den vorherigen Teilen dieser Reihe haben wir das Konzept der Volltextsuche eingeführt und wie es verwendet werden kann, um Text innerhalb einer Datenbank zu suchen. In diesem Teil werden wir OpenSearch vorstellen, eine leistungsstarke Suchmaschine, die verwendet werden kann, um nach Text zu suchen.

In diesem Teil werden wir abdecken, wie man OpenSearch up und running; im nächsten Teil werde ich abdecken, wie man Daten in opensearch zu bekommen und abzufragen.

Frühere Teile dieser Serie:

- [Volltextsuche mit Postgres](/blog/textsearchingpt1)
- [Suchfeld mit Postgres](/blog/textsearchingpt11)

Nächste Teile dieser Serie:

- [Offene Suche mit C#](/blog/textsearchingpt3)

[TOC]

## Was ist OpenSearch?

[OpenSearch](https://opensearch.org/) ist eine Suchmaschine, die schnell, skalierbar und einfach zu bedienen ist. Es ist ein Ableger von Elasticsearch, eine beliebte Suchmaschine, die von vielen Unternehmen verwendet wird, um ihre Suchfunktionalität zu betreiben. OpenSearch ist einfach zu bedienen und kann verwendet werden, um Text auf verschiedene Arten zu suchen.

Allerdings ist es nicht ein einfaches Tier, um los zu bekommen und zu verwenden, so werden wir die Grundlagen in diesem Artikel zu decken.

![Offene Suche](opensearch.webp?width=900&quality=25)

## OpenSearch installieren

Zuerst ist dies keine Konfiguration, die Sie in der Produktion verwenden sollten; es setzt eine Reihe von Demo-Benutzer und Passwörter, die nicht sicher sind. Sie sollten lesen Sie die [amtliche Dokumentation](https://opensearch.org/docs/) um einen sicheren Cluster einzurichten.

Zuerst verwenden wir die Standard-Docker-Installation 'Development' von Opensearch & Opensearch Dashboards (denk an die Benutzeroberfläche, um den Cluster zu verwalten).

Sehen Sie hier für alle Details auf der [docker komponieren setup](https://opensearch.org/docs/latest/install-and-configure/install-opensearch/docker/).

Sie müssen ein kleines Tweak entweder wsl / Ihr Linux-Host machen, damit es reibungslos funktioniert:
Linux-Einstellungen
Führen Sie für eine Linux-Umgebung die folgenden Befehle aus:

Deaktivieren Sie Speicher-Paging und Swap-Performance auf dem Host, um die Leistung zu verbessern.

```bash
sudo swapoff -a
```

Erhöhen Sie die Anzahl der Speicherkarten, die OpenSearch zur Verfügung stehen.

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

Windows-Einstellungen
Führen Sie für Windows-Workloads mit WSL über Docker Desktop die folgenden Befehle in einem Terminal aus, um den vm.max_map_count festzulegen:

```bash
wsl -d docker-desktop
sysctl -w vm.max_map_count=262144
```

Dann können Sie eine `.env` Datei im gleichen Verzeichnis wie Ihre `docker-compose.yml` Datei mit folgendem Inhalt:
`bash OPENSEARCH_INITIAL_ADMIN_PASSWORD=<somepasswordwithlowercaseuppercaseandspecialchars> `

Nun verwenden Sie die folgende docker-compose-Datei, um den Cluster einzurichten:

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

Diese docker-compose-Datei wird einen 2 Knoten-Cluster von Opensearch und einen einzigen Knoten von Opensearch Dashboards einrichten.

Es stützt sich auf Ihre.env-Datei, um die `OPENSEARCH_INITIAL_ADMIN_PASSWORD` variabel.

Dann drehen Sie das einfach durch.

```bash
docker compose -f opensearch-docker.yml up -d
```

In Zukunft werden wir in die Tiefe gehen, was dies tut und wie man es für die Produktion konfiguriert.

### Zugriff auf OpenSearch

Sobald Sie den Cluster gestartet haben, können Sie auf die OpenSearch Dashboards Benutzeroberfläche zugreifen, indem Sie auf `http://localhost:5601` in Ihrem Webbrowser. Sie können sich mit dem Benutzernamen anmelden `admin` und das Passwort, das Sie in der `.env` ..............................................................................................................................

![Opensearch Dashboards](opensearchdashboards.png?width=600&format=webp&quality=25)

Dies ist Ihr Hauptadmin-Interface (Sie können es mit einigen Beispieldaten bevölkern und damit herumspielen).

![Opensearch Dashboards](dashboard.png?width=600&format=webp&quality=25)

## Schlussfolgerung

Im nächsten Teil werde ich behandeln, wie man Daten in opensearch bekommt und abfragt.