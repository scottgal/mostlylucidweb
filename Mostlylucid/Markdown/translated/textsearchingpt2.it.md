# Ricerca completa del testo (Pt 2 - Introduzione alla ricerca aperta)

<!--category-- OpenSearch -->
<datetime class="hidden">2024-08-24T03:00</datetime>

## Introduzione

Nelle parti precedenti di questa serie abbiamo introdotto il concetto di ricerca completa del testo e come può essere utilizzato per cercare il testo all'interno di un database. In questa parte presenteremo OpenSearch, un potente motore di ricerca che può essere utilizzato per la ricerca di testo.

In questa parte copriremo come ottenere OpenSearch in esecuzione; nella parte successiva coprirò come ottenere i dati in opensearch e interrogarlo.

[TOC]

## Che cos'è OpenSearch?

[OpenSearch](https://opensearch.org/) è un motore di ricerca che è progettato per essere veloce, scalabile e facile da usare. Si tratta di un offshoot di Elasticsearch, un motore di ricerca popolare che viene utilizzato da molte aziende per alimentare la loro funzionalità di ricerca. OpenSearch è progettato per essere facile da usare e può essere utilizzato per la ricerca di testo in una varietà di modi diversi.

Tuttavia non è una bestia semplice da ottenere andare e utilizzare quindi ci occuperemo delle basi in questo articolo.

![Ricerca aperta](opensearch.webp?width=900&quality=25)

## Installazione di OpenSearch

In primo luogo, questa non è una configurazione che si dovrebbe usare in produzione; imposta un certo numero di utenti demo e password che non sono sicuri. Dovresti leggere il [documentazione ufficiale](https://opensearch.org/docs/) per creare un cluster sicuro.

In primo luogo, stiamo usando l'installazione docker "Sviluppo" predefinita di Opensearch & Opensearch Dashboards (pensate, l'interfaccia utente per gestire il cluster).

Vedi qui per tutti i dettagli sulla [docker comporre configurazione](https://opensearch.org/docs/latest/install-and-configure/install-opensearch/docker/).

Dovrete fare una piccola modifica a wsl / il vostro host linux per farlo funzionare senza intoppi:
Impostazioni Linux
Per un ambiente Linux, eseguire i seguenti comandi:

Disabilita il memory paging e lo scambio di prestazioni sull'host per migliorare le prestazioni.

```bash
sudo swapoff -a
```

Aumenta il numero di mappe di memoria disponibili per OpenSearch.

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

Impostazioni di Windows
Per i carichi di lavoro Windows che utilizzano WSL tramite Docker Desktop, eseguire i seguenti comandi in un terminale per impostare vm.max_map_count:

```bash
wsl -d docker-desktop
sysctl -w vm.max_map_count=262144
```

Poi si può creare un `.env` file nella stessa directory del tuo `docker-compose.yml` file con il seguente contenuto:
`bash OPENSEARCH_INITIAL_ADMIN_PASSWORD=<somepasswordwithlowercaseuppercaseandspecialchars> `

Ora si utilizza il seguente file docker-compose per impostare il cluster:

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

Questo file docker-compose imposta un cluster di 2 nodi di Opensearch e un singolo nodo di Opensearch Dashboards.

Si basa sul vostro file.env per impostare il `OPENSEARCH_INITIAL_ADMIN_PASSWORD` variabile.

Allora fallo saltare in aria.

```bash
docker compose -f opensearch-docker.yml up -d
```

In futuro andremo più a fondo su ciò che questo sta facendo e su come configurarlo per la produzione.

### Accesso alla ricerca aperta

Una volta che hai il cluster attivo e in esecuzione, è possibile accedere all'interfaccia utente di OpenSearch Dashboards navigando su `http://localhost:5601` nel tuo browser web. È possibile effettuare il login utilizzando il nome utente `admin` e la password che hai impostato nel `.env` Archivio.

![Cruscotti opensearch](opensearchdashboards.png?width=600&format=webp&quality=25)

Questa è la tua interfaccia principale di amministrazione (è possibile popolarlo con alcuni dati di esempio e giocare con esso).

![Cruscotti opensearch](dashboard.png?width=600&format=webp&quality=25)

## In conclusione

Nella prossima parte coprirò come ottenere i dati in opensearch e interrogarlo.