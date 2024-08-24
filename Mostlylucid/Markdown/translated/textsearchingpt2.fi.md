# Tekstihaku kokonaisuudessaan (Pt 2 - Johdatus avoimeen hakuun)

<!--category-- OpenSearch -->
<datetime class="hidden">2024-08-24T03:00</datetime>

## Johdanto

Sarjan edellisissä osissa esitimme täydellisen tekstinhaun konseptin ja sen, miten sitä voidaan käyttää tekstin hakemiseen tietokannasta. Tässä osassa esittelemme OpenSearchin, joka on tehokas hakukone, jota voidaan käyttää tekstin hakuun.

Tässä osassa kerromme, miten OpenSearch saadaan käyntiin, seuraavassa osassa selvitän, miten data saadaan avoimeen hakuun ja tiedustelen sitä.

Aikaisemmat osat tässä sarjassa:

- [Täydellinen tekstihaku postinjakajilla](/blog/textsearchingpt1)
- [Hakulaatikko, jossa postgres](/blog/textsearchingpt11)

Seuraavat osat tässä sarjassa:

- [Avaa haku C#:llä](/blog/textsearchingpt3)


[TOC]

## Mikä on OpenSearch?

[Avoin haku](https://opensearch.org/) on hakukone, joka on suunniteltu nopeaksi, skaalautuvaksi ja helppokäyttöiseksi. Se on Elastisen hakukone, joka on suosittu hakukone, jota monet yritykset käyttävät voimanlähteenä hakutoiminnoissaan. OpenSearch on suunniteltu helppokäyttöiseksi ja sitä voidaan käyttää tekstin etsimiseen eri tavoin.

Se ei kuitenkaan ole yksinkertainen peto, joka lähtee liikkeelle ja käyttää sitä, joten käsittelemme tämän artikkelin perusasiat.

![Avoin haku](opensearch.webp?width=900&quality=25)

## Avoimen haun asentaminen

Ensinnäkin, tämä ei ole kokoonpano, jota sinun pitäisi käyttää tuotannossa, vaan se asettaa useita demokäyttäjiä ja salasanoja, jotka eivät ole turvallisia. Sinun pitäisi lukea [viralliset asiakirjat](https://opensearch.org/docs/) Perustetaan turvallinen ryhmä.

Ensinnäkin käytämme Opensearch & Opensearch Dashboards -levyjen oletusasennetta "Kehitys" (ajattele, UI hallinnoi ryhmää).

Katso tästä kaikki tiedot [dockerin sävellys](https://opensearch.org/docs/latest/install-and-configure/install-opensearch/docker/).

Sinun täytyy tehdä pieni muutos joko wsl / linux isäntä, jotta se toimii sujuvasti:
Linux-asetukset
Linux-ympäristöä varten suorita seuraavat komennot:

Poista muistin haku ja vaihtaminen isännän suorituksen parantamiseksi.

```bash
sudo swapoff -a
```

Lisää OpenSearchin muistikarttojen määrää.

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

Windows-asetukset
Jos haluat käyttää WSL:n työmäärää Docker-työpöydän kautta, suorita seuraavat komennot päätteessä vm.max_map_count:

```bash
wsl -d docker-desktop
sysctl -w vm.max_map_count=262144
```

Sitten voit luoda `.env` tiedosto samassa hakemistossa kuin `docker-compose.yml` tiedosto, jossa on seuraava sisältö:
`bash OPENSEARCH_INITIAL_ADMIN_PASSWORD=<somepasswordwithlowercaseuppercaseandspecialchars> `

Nyt käytät seuraavaa docker-kokonaisuutta ryhmän asettamiseen:

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

Tämä docker-kokonaisuus perustaa kahden solmun ryhmän Opensearchia ja yhden solmun Opensearch Dashboardsia.

Se luottaa.env-tiedoston asettaa `OPENSEARCH_INITIAL_ADMIN_PASSWORD` Vaihteleva.

Käännä se sitten ylös.

```bash
docker compose -f opensearch-docker.yml up -d
```

Tulevaisuudessa pohdimme tarkemmin, mitä tämä tekee ja miten se voidaan konfiguroida tuotantoa varten.

### Avoimen haun käyttö

Kun ryhmä on toiminnassa, voit käyttää OpenSearch Dashboards UI:ta navigoimalla `http://localhost:5601` Nettiselaimessasi. Voit kirjautua sisään käyttäjänimellä `admin` ja salasana, jonka laitoit sisään `.env` Kansio.

![Avaa hakukojetaulut](opensearchdashboards.png?width=600&format=webp&quality=25)

Tämä on tärkein admin-käyttöliittymäsi (voit kansoittaa sen jollain näytedatalla ja pelata sen kanssa).

![Avaa hakukojetaulut](dashboard.png?width=600&format=webp&quality=25)

## Johtopäätöksenä

Seuraavassa osassa käsittelen, miten data saadaan avoimeen hakuun ja tiedustelen sitä.