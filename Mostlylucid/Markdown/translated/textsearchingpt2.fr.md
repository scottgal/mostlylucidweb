# Recherche en texte complet (Pt 2 - Introduction à OpenSearch)

<!--category-- OpenSearch -->
<datetime class="hidden">2024-08-24T03:00</datetime>

## Présentation

Dans les parties précédentes de cette série, nous avons introduit le concept de recherche de texte complet et comment il peut être utilisé pour rechercher du texte dans une base de données. Dans cette partie, nous allons introduire OpenSearch, un puissant moteur de recherche qui peut être utilisé pour rechercher du texte.

Dans cette partie, nous aborderons la façon d'obtenir OpenSearch en cours d'exécution; dans la partie suivante, je traiterai la façon d'obtenir les données dans openSearch et je les interrogerai.

Pièces précédentes de cette série:

- [Recherche de texte complet avec Postgres](/blog/textsearchingpt1)
- [Boîte de recherche avec Postgres](/blog/textsearchingpt11)

Les prochaines parties de cette série:

- [Ouvrir la recherche avec C#](/blog/textsearchingpt3)

[TOC]

## Qu'est-ce qu'OpenSearch?

[Ouvrir la recherche](https://opensearch.org/) est un moteur de recherche qui est conçu pour être rapide, évolutive et facile à utiliser. C'est une sortie d'Elasticsearch, un moteur de recherche populaire qui est utilisé par de nombreuses entreprises pour alimenter leur fonctionnalité de recherche. OpenSearch est conçu pour être facile à utiliser et peut être utilisé pour rechercher du texte de différentes façons.

Cependant, ce n'est pas une simple bête d'aller et d'utiliser ainsi nous allons couvrir les bases dans cet article.

![Ouvrir la recherche](opensearch.webp?width=900&quality=25)

## Installation d'OpenSearch

Tout d'abord, il ne s'agit pas d'une configuration que vous devriez utiliser dans la production; il met en place un certain nombre d'utilisateurs de démonstration et de mots de passe qui ne sont pas sécurisés. Vous devriez lire le [de la documentation officielle](https://opensearch.org/docs/) pour mettre en place un cluster sécurisé.

Tout d'abord, nous utilisons l'installation Docker par défaut d'Opensearch & Opensearch Dashboards (pensez, l'interface utilisateur pour gérer le cluster).

Voir ici pour tous les détails sur le [docker compose la configuration](https://opensearch.org/docs/latest/install-and-configure/install-opensearch/docker/).

Vous aurez besoin de faire un petit tweak à l'un ou l'autre wsl / votre hôte Linux pour le faire fonctionner en douceur:
Paramètres Linux
Pour un environnement Linux, exécutez les commandes suivantes :

Désactiver les performances de recherche et d'échange de mémoire sur l'hôte pour améliorer les performances.

```bash
sudo swapoff -a
```

Augmenter le nombre de cartes mémoire disponibles pour OpenSearch.

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

Paramètres de Windows
Pour les charges de travail de Windows en utilisant WSL via Docker Desktop, exécutez les commandes suivantes dans un terminal pour définir le vm.max_map_count:

```bash
wsl -d docker-desktop
sysctl -w vm.max_map_count=262144
```

Ensuite, vous pouvez créer un `.env` fichier dans le même répertoire que votre `docker-compose.yml` fichier avec le contenu suivant:
`bash OPENSEARCH_INITIAL_ADMIN_PASSWORD=<somepasswordwithlowercaseuppercaseandspecialchars> `

Maintenant, vous utilisez le fichier docker-compose suivant pour configurer le cluster :

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

Ce fichier composé de docker va configurer un cluster de 2 noeuds d'Opensearch et un seul noeud d'Opensearch Dashboards.

Il s'appuie sur votre fichier.env pour définir le `OPENSEARCH_INITIAL_ADMIN_PASSWORD` variable.

Alors fais tourner ça.

```bash
docker compose -f opensearch-docker.yml up -d
```

À l'avenir, nous nous pencherons plus en profondeur sur ce que cela fait et sur la façon de le configurer pour la production.

### Accès à OpenSearch

Une fois que vous avez le cluster en cours d'exécution, vous pouvez accéder à l'interface utilisateur OpenSearch Dashboards en naviguant vers `http://localhost:5601` dans votre navigateur web. Vous pouvez vous connecter en utilisant le nom d'utilisateur `admin` et le mot de passe que vous définissez dans le `.env` fichier.

![Tableaux de bord d'Opensearch](opensearchdashboards.png?width=600&format=webp&quality=25)

C'est votre interface principale d'administration (vous pouvez la remplir avec quelques données d'échantillon et jouer autour avec elle).

![Tableaux de bord d'Opensearch](dashboard.png?width=600&format=webp&quality=25)

## En conclusion

Dans la partie suivante, je traiterai de la façon d'obtenir des données dans opensearch et de l'interroger.