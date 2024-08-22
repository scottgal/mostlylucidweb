# विकास डिपेंडेंसीज़ के लिए डॉकर का प्रयोग किया जा रहा है

<!--category-- Docker -->
<datetime class="hidden">2024- 0. 909टी17: 17</datetime>

# परिचय

जब सॉफ्टवेयर हाशिया पैदा होता है हम एक डेटाबेस, एक संदेश कतार, एक कैश, और शायद कुछ अन्य सेवाएँ शुरू करते हैं. यह एक दर्द का प्रबंधन करने के लिए हो सकता है, विशेष रूप से आप कई परियोजनाओं पर काम कर रहे हैं. डॉकर बनाएं एक औज़ार है जो आपको पारिभाषित करता है तथा बहु- प्रिंटरी डॉकर अनुप्रयोगों को चलाने देता है. यह अपने विकास निर्भरता का प्रबंधन करने के लिए एक महान तरीका है.

इस पोस्ट में, मैं तुम्हें दिखाता हूँ कि कैसे डॉकर का उपयोग करें अपने विकास निर्भरता को प्रबंधित करने के लिए.

[विषय

# पूर्वपाराईज़

सबसे पहले आप जो भी मंच पर उपयोग कर रहे हैं उस पर डॉकer डेस्कटॉप संस्थापित करने की आवश्यकता होगी. आप इसे डाउनलोड कर सकते हैं [यहाँ](https://www.docker.com/products/docker-desktop).

**नोट: मैंने पाया है कि विंडोज़ पर आप वास्तव में डॉक डेस्कटॉप संस्थापित करने की जरूरत है प्रबंधक के रूप में यह ठीक से संस्थापित करने के लिए ठीक से संस्थापित करता है.**

# एक डॉक लाया जा रहा है फ़ाइल

डॉकर लिखें एक YAएमएल फ़ाइल का उपयोग उन सेवाओं को पारिभाषित करने के लिए करता है जो आप चलाना चाहते हैं. यहाँ एक सरल का उदाहरण है `devdeps-docker-compose.yml` फ़ाइल जो डाटाबेस सेवा तथा ईमेल सेवा पारिभाषित करता है:

```yaml
services: 
  smtp4dev:
    image: rnwood/smtp4dev
    ports:
      - "3002:80"
      - "2525:25"
    volumes:
      # This is where smtp4dev stores the database..
      - e:/smtp4dev-data:/smtp4dev
    restart: always
  postgres:
    image: postgres:16-alpine
    container_name: postgres
    ports:
      - "5432:5432"
    env_file:
      - .env
    volumes:
      - e:/data:/var/lib/postgresql/data  # Map e:\data to the PostgreSQL data folder
    restart: always	
networks:
  mynetwork:
        driver: bridge
```

नोट यहाँ पर मैंने हर सेवा के लिए डेटा को जारी रखने के लिए खंड निर्धारित किए हैं, यहाँ पर मैंने निर्धारित किया है

```yaml
    volumes:
      # This is where smtp4dev stores the database..
      - e:/smtp4dev-data:/smtp4dev
    volumes:
        - e:/data:/var/lib/postgresql/data  # Map e:\data to the PostgreSQL data folder
```

यह सुनिश्चित करता है कि डाटा बरतनों के चलने के बीच में लगी रहता है.

मैं भी एक निर्दिष्ट `env_file` के लिए `postgres` सेवा. यह एक फ़ाइल है जिसमें एनवायरनमेंट वेरिएबल समाहित है जो संग्राहक में पास किया गया है.
आप एनवायरनमेंट वेरिएबल की सूची देख सकते हैं जो कि एसक्यूएल कंटेनर में पास किया जा सकता है [यहाँ](https://www.docker.com/blog/how-to-use-the-postgres-docker-official-image/#1-Environment-variables).
यहाँ एक उदाहरण है `.env` फ़ाइल:

```shell
POSTGRES_DB=postgres
POSTGRES_USER=postgres
POSTGRES_PASSWORD=<somepassword>
```

यह एसक्यूएल के लिए डिफ़ॉल्ट डाटाबेस, पासवर्ड तथा उपयोक्ता को कॉन्फ़िगर करता है.

यहाँ मैं भी एसएमटीपी4 डेवव सेवा चलाता हूं, यह आपके अनुप्रयोग में ई- मेल प्रकार्य परीक्षण के लिए एक महान औजार है. आप इसके बारे में अधिक जानकारी पा सकते हैं [यहाँ](https://github.com/rnwood/smtp4dev/wiki/Installation#how-to-run-smtp4dev-in-docker).

आप मेरे में देखो `appsettings.Developmet.json` फ़ाइल आप मेरे पास एसएमटीपी सर्वर के लिए निम्न विन्यास होगा:

```json
  "SmtpSettings":
{
"Server": "localhost",
"Port": 2525,
"SenderName": "Mostlylucid",
"Username": "",
"SenderEmail": "scott.galloway@gmail.com",
"Password": "",
"EnableSSL": "false",
"EmailSendTry": 3,
"EmailSendFailed": "true",
"ToMail": "scott.galloway@gmail.com",
"EmailSubject": "Mostlylucid"

}
```

यह एसएमटीपी4डीव के लिए काम करता है और यह मुझे इस कार्य को जाँचने में सक्षम करता है (मैं किसी भी पता के लिए भेज सकते हैं, और एस एस एसओवीएफएस में ईमेल देखें http://ovvon: 6002//.

एक बार जब आप यकीन कर रहे हैं कि यह सब आप GMAL की तरह एक वास्तविक एसएमटीपी सर्वर पर जांच कर सकते हैं (e.g, देखें) [यहाँ](addingasyncsendingforemails) कैसे करें के लिए

# सेवाओं को चलाना

सेवाओं को चालू करने के लिए इस सेवा को चलाने के लिए जिसे पारिभाषित किया गया है `devdeps-docker-compose.yml` फ़ाइल, आपको निम्न कमांड को एक ही डिरेक्ट्री में फ़ाइल के रूप में चलाने की जरूरत है:

```shell
docker compose -f .\devdeps-docker-compose.yml up -d
```

नोट करें आप इसे पहले से इस तरह चलाना चाहिए; यह सुनिश्चित करता है कि आप कॉन्फ़िग तत्वों को यहाँ से पारित कर सकते हैं `.env` फ़ाइल.

```shell
docker compose -f .\devdeps-docker-compose.yml config
```

अब यदि आप डॉक डेस्कटॉप में देखें तो आप इन सेवाओं को चल रहे देख सकते हैं

![डॉक डेस्कटॉपComment](dockerdesktopdev.png)