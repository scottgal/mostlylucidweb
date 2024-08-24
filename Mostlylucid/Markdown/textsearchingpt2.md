# Full Text Searching (Pt 2 - Introduction to OpenSearch)
<!--category-- OpenSearch -->
<datetime class="hidden">2024-08-24T03:00</datetime>

## Introduction
In the previous parts of this series we introduced the concept of full text searching and how it can be used to search for text within a database. In this part we will introduce OpenSearch, a powerful search engine that can be used to search for text.

In this part we'll cover how to get OpenSearch up and running; in the next part I'll cover how to get data into opensearch and query it.

[TOC]

## What is OpenSearch?
[OpenSearch](https://opensearch.org/) is a search engine that is designed to be fast, scalable and easy to use. It is an offshoot of Elasticsearch, a popular search engine that is used by many companies to power their search functionality. OpenSearch is designed to be easy to use and can be used to search for text in a variety of different ways.

However it's not a simple beast to get going and use so we will be covering the basics in this article.

![Opensearch](opensearch.webp?width=900&quality=25)

## Installing OpenSearch
First, this isn't a configuration you should use in production; it sets up a number of demo users and passwords that are not secure. You should read the [official documentation](https://opensearch.org/docs/) to set up a secure cluster.

First, we are using the default 'Development' docker install of Opensearch & Opensearch Dashboards (think, the UI to manage the cluster). 

See here for all the details on the [docker compose setup](https://opensearch.org/docs/latest/install-and-configure/install-opensearch/docker/).

You'll need to make a small tweak to either wsl / your linux host to make it work smoothly:
Linux settings
For a Linux environment, run the following commands:

Disable memory paging and swapping performance on the host to improve performance.
```bash
sudo swapoff -a
```

Increase the number of memory maps available to OpenSearch.
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

Windows settings
For Windows workloads using WSL through Docker Desktop, run the following commands in a terminal to set the vm.max_map_count:
```bash
wsl -d docker-desktop
sysctl -w vm.max_map_count=262144
```
Then you can create a `.env` file in the same directory as your `docker-compose.yml` file with the following content:
    ```bash
    OPENSEARCH_INITIAL_ADMIN_PASSWORD=<somepasswordwithlowercaseuppercaseandspecialchars>
    ```

Now you use the following docker-compose file to set up the cluster:

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

This docker-compose file will set up a 2 node cluster of Opensearch and a single node of Opensearch Dashboards.

It relies on your .env file to set the `OPENSEARCH_INITIAL_ADMIN_PASSWORD` variable. 

Then just spin that up.
    
```bash
docker compose -f opensearch-docker.yml up -d
```
In future we'll go into more depth about what this is doing and how to configure it for production.

### Accessing OpenSearch
Once you have the cluster up and running, you can access the OpenSearch Dashboards UI by navigating to `http://localhost:5601` in your web browser. You can log in using the username `admin` and the password you set in the `.env` file.

![Opensearch Dashboards](opensearchdashboards.png?width=600&format=webp&quality=25)

This is your main admin interface (you can populate it with some sample data and play around with it).

![Opensearch Dashboards](dashboard.png?width=600&format=webp&quality=25)

## In Conclusion
In the next part I'll cover how to get data into opensearch and query it.