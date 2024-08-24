# Búsqueda de texto completo (Pt 2 - Introducción a OpenSearch)

<!--category-- OpenSearch -->
<datetime class="hidden">2024-08-24T03:00</datetime>

## Introducción

En las partes anteriores de esta serie introdujimos el concepto de búsqueda de texto completo y cómo se puede utilizar para buscar texto dentro de una base de datos. En esta parte vamos a introducir OpenSearch, un potente motor de búsqueda que se puede utilizar para buscar texto.

En esta parte cubriremos cómo obtener OpenSearch y en funcionamiento; en la siguiente parte cubriré cómo obtener datos en opensearch y consultarlo.

[TOC]

## ¿Qué es OpenSearch?

[OpenSearch](https://opensearch.org/) es un motor de búsqueda que está diseñado para ser rápido, escalable y fácil de usar. Es una rama de Elasticsearch, un popular motor de búsqueda que es utilizado por muchas empresas para impulsar su funcionalidad de búsqueda. OpenSearch está diseñado para ser fácil de usar y se puede utilizar para buscar texto de diferentes maneras.

Sin embargo, no es una simple bestia para ponerse en marcha y utilizar por lo que vamos a cubrir los conceptos básicos en este artículo.

![Búsqueda abierta](opensearch.webp?width=900&quality=25)

## Instalación de OpenSearch

En primer lugar, esta no es una configuración que debas usar en la producción; configura una serie de usuarios de demostración y contraseñas que no son seguras. Usted debe leer el [documentación oficial](https://opensearch.org/docs/) para establecer un clúster seguro.

Primero, estamos usando el docker predeterminado 'Desarrollo' de Opensearch & Opensearch Dashboards (piensa, la interfaz de usuario para administrar el clúster).

Vea aquí para todos los detalles sobre el [configuración de composición docker](https://opensearch.org/docs/latest/install-and-configure/install-opensearch/docker/).

Usted tendrá que hacer un pequeño ajuste a cualquiera wsl / su host linux para que funcione sin problemas:
Configuración de Linux
Para un entorno Linux, ejecute los siguientes comandos:

Desactivar la paginación de memoria y el rendimiento de intercambio en el host para mejorar el rendimiento.

```bash
sudo swapoff -a
```

Aumente el número de mapas de memoria disponibles para OpenSearch.

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

Configuración de Windows
Para las cargas de trabajo de Windows usando WSL a través de Docker Desktop, ejecute los siguientes comandos en un terminal para establecer el vm.max_map_count:

```bash
wsl -d docker-desktop
sysctl -w vm.max_map_count=262144
```

Entonces usted puede crear un `.env` archivo en el mismo directorio que su `docker-compose.yml` archivo con el siguiente contenido:
`bash OPENSEARCH_INITIAL_ADMIN_PASSWORD=<somepasswordwithlowercaseuppercaseandspecialchars> `

Ahora usted utiliza el siguiente archivo docker-compose para configurar el clúster:

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

Este archivo docker-compose configurará un clúster de 2 nodos de Opensearch y un solo nodo de Opensearch Dashboards.

Se basa en su archivo.env para establecer el `OPENSEARCH_INITIAL_ADMIN_PASSWORD` variable.

Entonces sólo tienes que girar hacia arriba.

```bash
docker compose -f opensearch-docker.yml up -d
```

En el futuro vamos a profundizar en lo que esto está haciendo y cómo configurarlo para la producción.

### Acceso a OpenSearch

Una vez que tengas el clúster en funcionamiento, puedes acceder a la interfaz de usuario OpenSearch Dashboards navegando a `http://localhost:5601` en su navegador web. Puede iniciar sesión usando el nombre de usuario `admin` y la contraseña que se establece en el `.env` archivo.

![Opensearch Dashboards](opensearchdashboards.png?width=600&format=webp&quality=25)

Esta es su interfaz de administración principal (puede poblarlo con algunos datos de muestra y jugar con él).

![Opensearch Dashboards](dashboard.png?width=600&format=webp&quality=25)

## Conclusión

En la siguiente parte cubriré cómo obtener datos en opensearch y consultarlo.