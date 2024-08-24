# पूर्ण पाठ खोज (तंकंक को खोलने के लिए 2 - परिचय)

<!--category-- OpenSearch -->
<datetime class="hidden">2024- 023: 00</datetime>

## परिचय

इस श्रृंखला के पिछले भाग में हमने पूरा पाठ खोज की धारणा शुरू की और यह कैसे एक डाटाबेस के भीतर पाठ को खोजने के लिए प्रयोग किया जा सकता है. इस भाग में हम एक शक्‍तिशाली खोज इंजन का परिचय करेंगे, जो पाठ को खोजने के लिए प्रयोग किया जा सकता है ।

इस भाग में हम खोलें खोजने के लिए और चलाने के लिए कैसे कवर करेंगे, अगले भाग में मैं डेटा को खोलने के लिए कैसे बंद कर देंगे और इसे क्वेरी करने के लिए.

इस क्रम में पिछला हिस्सा:

- [पोस्ट- धर्म के साथ पूरा पाठ खोज रहा है](/blog/textsearchingpt1)
- [पोस्ट- वाक्यांशों के साथ बॉक्स खोजें](/blog/textsearchingpt11)

इस क्रम में अगले भाग में:

- [सी# के साथ खोज खोलें](/blog/textsearchingpt3)

[विषय

## कौन - सी खोज खोलें?

[ढूंढें](https://opensearch.org/) एक खोज इंजन है जो तेजी से, कैल्डेबल और प्रयोग करने के लिए आसान बनाया गया है । यह एक लोकप्रिय खोज इंजन है, जो अनेक कंपनियों द्वारा अपने खोज कार्य को शक्‍ति देने के लिए प्रयोग किया जाता है । खोज की शुरूआत अलग - अलग तरीकों से की जा सकती है ।

लेकिन यह जाने और उपयोग करने के लिए एक साधारण जानवर नहीं है तो हम इस लेख में मूलओं को ढक दिया जाएगा.

![ढूंढें](opensearch.webp?width=900&quality=25)

## ओपन सर्च संस्थापित किया जा रहा है

सबसे पहले यह विन्यास आपको उत्पादन में उपयोग नहीं करना चाहिए, यह कई डेमो उपयोक्ताओं तथा कूटशब्द को सेट करता है जो सुरक्षित नहीं हैं. आपको पढ़ना चाहिए [रिमोट दस्तावेज़](https://opensearch.org/docs/) एक सुरक्षित गुच्छ सेट करने के लिए.

पहले, हम उपयोग कर रहे हैं डिफ़ॉल्ट 'डिप्शन' डॉकर का उपयोग कर रहे हैं सर्च डीशबोर्ड का उपयोग कर रहे हैं (एक्सएक्स प्रबंधित करने के लिए).

यहाँ पर सभी विवरणों के लिए देखें [बंद करें और इसे सेटअप करें](https://opensearch.org/docs/latest/install-and-configure/install-opensearch/docker/).

आप या तो wlL / अपनी जाँच मेजबान को आसानी से काम करने के लिए एक छोटा sltrol बनाने के लिए की आवश्यकता होगी:
लिनक्स विन्यास
लिनक्स वातावरण के लिए, निम्न कमांड चलाएँ:

प्रदर्शन को बेहतर बनाने के लिए होस्ट परकरणिंग तथा कड़ियों को निष्क्रिय करें.

```bash
sudo swapoff -a
```

खोलने के लिए स्मृति नक्शे की संख्या बढ़ाने के लिए उपलब्ध है.

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

विंडोज़ सेटिंग
विंडोज़ कार्य लोड करने के लिए डॉकer डेस्कटॉप के माध्यम से विंडो खोलें, वीएम को सेट करने के लिए नीचे के कमांड को चलाएँ.वीएम_M_M_count:

```bash
wsl -d docker-desktop
sysctl -w vm.max_map_count=262144
```

तो फिर आप एक निर्मित कर सकते हैं `.env` फ़ाइल में आपके जैसे निर्देशिका `docker-compose.yml` निम्न अंतर्वस्तु के साथ फ़ाइल:
`bash OPENSEARCH_INITIAL_ADMIN_PASSWORD=<somepasswordwithlowercaseuppercaseandspecialchars> `

अब आप निम्न डॉक- न्यूक्लास फ़ाइल का उपयोग गुच्छ सेट करने के लिए करें:

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

यह डॉकer- क़िस्म- क़िस्म की फ़ाइल ओपन सर्च के 2 नोड तथा सर्च डी-बोर्ड के एकल नोड को सेट करेगा.

यह सेट करने के लिए आपके.env फ़ाइल पर निर्भर करता है `OPENSEARCH_INITIAL_ADMIN_PASSWORD` चर.

तो बस कि ऊपर सफर.

```bash
docker compose -f opensearch-docker.yml up -d
```

भविष्य में हम यह क्या कर रहा है के बारे में और अधिक गहराई में जाना होगा और उत्पादन के लिए इसे कॉन्फ़िगर करने के लिए कैसे.

### मेलबाक्स फिर से प्राप्त किया जा रहा है

एक बार जब आप केर ऊपर है और चल रहा है, आप ओपन सर्च डीशबोर्ड यूआई को पहुँच सकते हैं `http://localhost:5601` अपने वेब ब्राउज़र में. आप उपयोक्ता नाम का उपयोग कर सकते हैं `admin` और आपने जो कूटशब्द सेट किया है `.env` फ़ाइल.

![सर्च डी-बोर्ड खोलें](opensearchdashboards.png?width=600&format=webp&quality=25)

यह आपका मुख्य प्रशासक इंटरफेस है (आप इसे कुछ नमूना डाटा से भर सकते हैं और इसके साथ चारों ओर खेल सकते हैं).

![सर्च डी-बोर्ड खोलें](dashboard.png?width=600&format=webp&quality=25)

## ऑन्टियम

अगले भाग में मैं डेटा को खोलने और इसे क्वेरी करने के लिए कैसे कवर करेंगे.