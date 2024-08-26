# Πλήρης αναζήτηση κειμένου (Pt 2 - Εισαγωγή στο OpenSearch)

<!--category-- OpenSearch -->
<datetime class="hidden">2024-08-24T03:00</datetime>

## Εισαγωγή

Στα προηγούμενα μέρη αυτής της σειράς εισάγαμε την έννοια της πλήρους αναζήτησης κειμένου και πώς μπορεί να χρησιμοποιηθεί για την αναζήτηση κειμένου μέσα σε μια βάση δεδομένων. Σε αυτό το μέρος θα εισαγάγει OpenSearch, μια ισχυρή μηχανή αναζήτησης που μπορεί να χρησιμοποιηθεί για την αναζήτηση κειμένου.

Σε αυτό το μέρος θα καλύψουμε το πώς να κάνουμε το OpenSearch να λειτουργεί.Στο επόμενο μέρος θα καλύψω το πώς να βάλω τα δεδομένα στο opensearch και να τα εξετάσω.

Προηγούμενα μέρη σε αυτή τη σειρά:

- [Πλήρης αναζήτηση κειμένου με Postgres](/blog/textsearchingpt1)
- [Κουτί αναζήτησης με Postgres](/blog/textsearchingpt11)

Επόμενα μέρη σε αυτή τη σειρά:

- [Opensearch με C#](/blog/textsearchingpt3)

[TOC]

## Τι είναι το OpenSearch;

[OpenSearch](https://opensearch.org/) είναι μια μηχανή αναζήτησης που έχει σχεδιαστεί για να είναι γρήγορη, κλιμακωτή και εύκολη στη χρήση. Είναι ένα offshoot της Elasticsearch, μια δημοφιλής μηχανή αναζήτησης που χρησιμοποιείται από πολλές εταιρείες για να ενεργοποιήσει τη λειτουργία αναζήτησης τους. Το OpenSearch έχει σχεδιαστεί για να είναι εύκολο στη χρήση και μπορεί να χρησιμοποιηθεί για την αναζήτηση κειμένου με διάφορους τρόπους.

Ωστόσο, δεν είναι ένα απλό θηρίο να πηγαίνει και να χρησιμοποιεί έτσι θα πρέπει να καλύπτει τα βασικά σε αυτό το άρθρο.

![Opensearch](opensearch.webp?width=900&quality=25)

## Εγκατάσταση OpenSearch

Πρώτον, αυτό δεν είναι μια διαμόρφωση που θα πρέπει να χρησιμοποιήσετε στην παραγωγή, δημιουργεί έναν αριθμό χρηστών demo και κωδικών πρόσβασης που δεν είναι ασφαλείς. Θα πρέπει να διαβάσετε το [επίσημη τεκμηρίωση](https://opensearch.org/docs/) να δημιουργήσει ένα ασφαλές σύμπλεγμα.

Πρώτον, χρησιμοποιούμε την προεπιλεγμένη εγκατάσταση Docker 'ανάπτυξη' Opensearch & Opensearch Dashboards (σκέψου, το UI για τη διαχείριση της συστάδας).

Δείτε εδώ για όλες τις λεπτομέρειες σχετικά με το [Docker συνθέτουν τη ρύθμιση](https://opensearch.org/docs/latest/install-and-configure/install-opensearch/docker/).

Θα πρέπει να κάνετε ένα μικρό tweak είτε wsl / ξενιστή σας linux για να το κάνετε να λειτουργήσει ομαλά:
Ρυθμίσεις Linux
Για ένα περιβάλλον Linux, εκτελέστε τις ακόλουθες εντολές:

Απενεργοποιήστε την επιγραφή μνήμης και την ανταλλαγή επιδόσεων στον υπολογιστή για τη βελτίωση της απόδοσης.

```bash
sudo swapoff -a
```

Αύξηση του αριθμού των χαρτών μνήμης που είναι διαθέσιμοι στο OpenSearch.

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

Ρυθμίσεις των Windows
Για το φόρτο εργασίας των Windows χρησιμοποιώντας WSL μέσω Desktop Docker, εκτελέσετε τις ακόλουθες εντολές σε ένα τερματικό για να ρυθμίσετε το vm.max_map_count:

```bash
wsl -d docker-desktop
sysctl -w vm.max_map_count=262144
```

Τότε μπορείς να δημιουργήσεις ένα... `.env` αρχείο στον ίδιο κατάλογο με σας `docker-compose.yml` αρχείο με το ακόλουθο περιεχόμενο:
`bash OPENSEARCH_INITIAL_ADMIN_PASSWORD=<somepasswordwithlowercaseuppercaseandspecialchars> `

Τώρα χρησιμοποιείτε το ακόλουθο αρχείο Docker-compose για να ρυθμίσετε τη συστάδα:

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

Αυτό το docker-compose αρχείο θα δημιουργήσει ένα σύμπλεγμα 2 κόμβου Opensearch και ένα ενιαίο κόμβο Opensearch Dashboards.

Βασίζεται στο αρχείο.env σας για να ρυθμίσετε το `OPENSEARCH_INITIAL_ADMIN_PASSWORD` Μεταβλητή.

Τότε γύρισέ το.

```bash
docker compose -f opensearch-docker.yml up -d
```

Στο μέλλον θα μπούμε σε μεγαλύτερο βάθος σχετικά με το τι κάνει αυτό και πώς να το ρυθμίσετε για την παραγωγή.

### Πρόσβαση στο OpenSearch

Μόλις έχετε το σύμπλεγμα μέχρι και λειτουργία, μπορείτε να έχετε πρόσβαση στο OpenSearch Dashboards UI με την πλοήγηση σε `http://localhost:5601` στο πρόγραμμα περιήγησης ιστού σας. Μπορείτε να συνδεθείτε χρησιμοποιώντας το όνομα χρήστη `admin` και τον κωδικό πρόσβασης που βάλατε στο `.env` Φάκελος.

![Opensearch Dashboards](opensearchdashboards.png?width=600&format=webp&quality=25)

Αυτή είναι η κύρια διεπαφή admin σας (μπορείτε να το κατοικήσετε με κάποια στοιχεία δείγμα και να παίξετε γύρω με αυτό).

![Opensearch Dashboards](dashboard.png?width=600&format=webp&quality=25)

## Συμπέρασμα

Στο επόμενο μέρος θα καλύψω το πώς να βάλω δεδομένα σε ανοιχτή αναζήτηση και να τα εξετάσω.