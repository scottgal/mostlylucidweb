# Volledige tekst zoeken (Pt 2 - Inleiding tot OpenZoeken)

<!--category-- OpenSearch -->
<datetime class="hidden">2024-08-24T03:00</datetime>

## Inleiding

In de vorige delen van deze serie introduceerden we het concept van full text searching en hoe het gebruikt kan worden om tekst binnen een database te zoeken. In dit deel introduceren we OpenSearch, een krachtige zoekmachine die gebruikt kan worden om naar tekst te zoeken.

In dit deel zullen we behandelen hoe OpenSearch aan de gang te krijgen; in het volgende deel zal ik behandelen hoe om gegevens te krijgen in opensearch en query it.

[TOC]

## Wat is OpenSearch?

[OpenZoeken](https://opensearch.org/) is een zoekmachine die is ontworpen om snel, schaalbaar en gemakkelijk te gebruiken. Het is een offshoot van Elasticsearch, een populaire zoekmachine die wordt gebruikt door veel bedrijven om hun zoekfunctionaliteit stroom. OpenSearch is ontworpen om eenvoudig te gebruiken en kan worden gebruikt om op verschillende manieren naar tekst te zoeken.

Maar het is niet een eenvoudig beest om te gaan en te gebruiken dus we zullen de basis in dit artikel te dekken.

![Openzoeken](opensearch.webp?width=900&quality=25)

## OpenSearch installeren

Ten eerste is dit geen configuratie die je in productie moet gebruiken; het stelt een aantal demo-gebruikers en wachtwoorden op die niet veilig zijn. U moet het lezen van de [officiële documentatie](https://opensearch.org/docs/) om een veilige cluster op te zetten.

Ten eerste gebruiken we de standaard 'Development' docker installatie van Opensearch & Opensearch Dashboards (denk, de UI om het cluster te beheren).

Zie hier voor alle details over de [docker componeert setup](https://opensearch.org/docs/latest/install-and-configure/install-opensearch/docker/).

Je moet een kleine tweak maken naar wsl / je linux host om het soepel te laten werken:
Linux-instellingen
Voer voor een Linux-omgeving de volgende commando's uit:

Schakel geheugenoproep en het wisselen van prestaties op de host om de prestaties te verbeteren.

```bash
sudo swapoff -a
```

Verhoog het aantal geheugenkaarten dat beschikbaar is voor OpenSearch.

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

Venstersinstellingen
Voor Windows workloads met behulp van WSL via Docker Desktop, voer de volgende commando's in een terminal in om de vm.max_map_count in te stellen:

```bash
wsl -d docker-desktop
sysctl -w vm.max_map_count=262144
```

Dan kunt u een `.env` bestand in dezelfde map als uw `docker-compose.yml` bestand met de volgende inhoud:
`bash OPENSEARCH_INITIAL_ADMIN_PASSWORD=<somepasswordwithlowercaseuppercaseandspecialchars> `

Nu gebruik je het volgende docker-compose bestand om het cluster op te zetten:

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

Dit docker-compose bestand zal een 2 knooppunt cluster van Opensearch en een enkele knooppunt van Opensearch Dashboards.

Het is gebaseerd op uw.env bestand om de `OPENSEARCH_INITIAL_ADMIN_PASSWORD` variabel.

Draai dat dan om.

```bash
docker compose -f opensearch-docker.yml up -d
```

In de toekomst gaan we dieper in op wat dit doet en hoe we het kunnen configureren voor productie.

### Toegang tot OpenSearch

Zodra u de cluster up and running, kunt u toegang krijgen tot de OpenSearch Dashboards UI door navigeren naar `http://localhost:5601` in uw webbrowser. U kunt inloggen met de gebruikersnaam `admin` en het wachtwoord dat u in de `.env` bestand.

![Opensearch Dashboards](opensearchdashboards.png?width=600&format=webp&quality=25)

Dit is uw belangrijkste admin interface (u kunt het bevolken met een aantal sample gegevens en spelen rond met het).

![Opensearch Dashboards](dashboard.png?width=600&format=webp&quality=25)

## Conclusie

In het volgende deel zal ik behandelen hoe om gegevens te krijgen in opensearch en query it.