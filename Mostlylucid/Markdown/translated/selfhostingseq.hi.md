# एनएसईसी के लिए सेकणिंग स्व.

<datetime class="hidden">2024- 0. 2828टी09:37</datetime>

<!--category-- ASP.NET, Seq, Serilog, Docker -->
# परिचय

SEQ एक अनुप्रयोग है जो आपको देखने और लॉग देखने देता है. यह डिबगिंग के लिए एक महान औजार है तथा अपने अनुप्रयोग की निगरानी कर रहा है. इस पोस्ट में मैं कवर होगा कैसे मैं अपने Aack लॉग करने के लिए Sequ सेट.E कोर अनुप्रयोग.
बहुत सारे तूफानों में कभी नहीं हो सकता है:)

![सेमी- बोर्ड](seqdashboard.png)

[विषय

# सेक्वेंस सेट किया जा रहा है

सी. सी. आप या तो बादल संस्करण या स्वयं होस्ट का उपयोग कर सकते हैं. मैं अपने लॉग निजी रखने के लिए चाहता था के रूप में मैं यह खुद मेजबान करने के लिए चुना.

पहले मैं सीपीआई वेबसाइट का दौरा करके और प्राप्त करने के द्वारा शुरू [डॉकएण्ड निर्देश संस्थापित करें](https://docs.datalust.co/docs/getting-started-with-docker).

## स्थानीय

स्थानीय स्तर पर चलाने के लिए आपको पहले Heped कूटशब्द पाने की जरूरत है. आप निम्न कमांड चलाने के द्वारा यह कर सकते हैं:

```bash
echo '<password>' | docker run --rm -i datalust/seq config hash
```

इसे स्थानीय रूप से चलाने के लिए आप निम्न कमांड का उपयोग कर सकते हैं:

```bash
docker run --name seq -d --restart unless-stopped -e ACCEPT_EULA=Y -e SEQ_FIRSTRUN_ADMINPASSWORDHASH=<hashfromabove> -v C:\seq:/data -p 5443:443 -p 45341:45341 -p 5341:5341 -p 82:80 datalust/seq

```

मेरे उबुन्टू स्थानीय मशीन पर मैं इसे एक चादर स्क्रिप्ट में बनाया:

```shell
#!/bin/bash
PH=$(echo 'Abc1234!' | docker run --rm -i datalust/seq config hash)

mkdir -p /mnt/seq
chmod 777 /mnt/seq

docker run \
  --name seq \
  -d \
  --restart unless-stopped \
  -e ACCEPT_EULA=Y \
  -e SEQ_FIRSTRUN_ADMINPASSWORDHASH="$PH" \
  -v /mnt/seq:/data \
  -p 5443:443 \
  -p 45341:45341 \
  -p 5341:5341 \
  -p 82:80 \
  datalust/seq
```

तब

```shell
chmod +x seq.sh
./seq.sh
```

यह तुम उठो और फिर चलाने के लिए जाना होगा `http://localhost:82` / `http://<machineip>:82` अपने पूर्वप संस्थापित को देखने के लिए (डिफ़ॉल्ट प्रशासक पासवर्ड है जो आपने के लिए भरा है) <password> ऊपर.

## डॉकर में

मैंने अपने डॉकनेर फ़ाइल को जोड़ने के लिए आगे बढ़ाया:

```docker
  seq:
    image: datalust/seq
    container_name: seq
    restart: unless-stopped
    environment:
      ACCEPT_EULA: "Y"
      SEQ_FIRSTRUN_ADMINPASSWORDHASH: ${SEQ_DEFAULT_HASH}
    volumes:
      - /mnt/seq:/data
    networks:
      - app_network
```

नोट करें कि मेरे पास एक डिरेक्ट्री है जिसे बुलाया गया है `/mnt/seq` ( विंडो के लिए, एक विंडो पथ का प्रयोग करें) यह है कि लॉग जमा किया जाएगा जहां है.

मैं भी एक है `SEQ_DEFAULT_HASH` एनवायरनमेंट वेरिएबल जो मेरे ई.env फ़ाइल में प्रशासक के लिए हैश्ड पासवर्ड है.

# एनईएसईआई को सेट किया जा रहा है.

जैसा कि मैं उपयोग करता हूँ [सेरेयूलॉग](https://serilog.net/) मेरे लॉगिंग के लिए यह वास्तव में Sequ सेट करने के लिए बहुत आसान है. यह भी ऐसा करने पर निर्भर करता है [यहाँ](https://docs.datalust.co/docs/using-serilog).

मूलतः आप बस अपनी परियोजना में सिंक जोड़ते हैं:

```shell
dotnet add package Serilog.Sinks.Seq
```

मुझे उपयोग करना पसंद है `appsettings.json` मेरे कॉन्फ़िग के लिए तो मैं सिर्फ 'स्ट' सेट है मेरे में `Program.cs`:

```csharp
builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
    Serilog.Debugging.SelfLog.Enable(Console.Error);
    Console.WriteLine($"Serilog Minimum Level: {configuration.MinimumLevel.ToString()}");
});
```

फिर मेरे आईसेफाइट्स में मैं इस कॉन्फ़िगरेशन है

```json
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File", "Serilog.Sinks.Seq"],
    "MinimumLevel": "Warning",
    "WriteTo": [
        {
          "Name": "Seq",
          "Args":
          {
            "serverUrl": "http://seq:5341",
            "apiKey": ""
          }
        },
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/applog-.txt",
          "rollingInterval": "Day"
        }
      }

    ],
    "Enrich": ["FromLogContext", "WithMachineName"],
    "Properties": {
      "ApplicationName": "mostlylucid"
    }
  }

```

आप मैं एक है कि देखेंगे `serverUrl` का `http://seq:5341`___ यह इसलिए है क्योंकि मैं एक डाकर पात्र में चल रहा है `seq` और यह पोर्ट पर है `5341`___ आप इसे स्थानीय रूप से चल रहे हैं तो आप इस्तेमाल कर सकते हैं `http://localhost:5341`.
मैं भी एपीआई कुंजी का उपयोग कर सकते हैं ताकि लॉग स्तर गतिशील रूप से निर्दिष्ट कर सकूँ (आप केवल लॉग संदेशों के किसी खास स्तर को स्वीकार करने के लिए एक कुंजी निर्धारित कर सकते हैं).

आप इसे अपने sque उदाहरण में सेट करने के लिए जा रहा है `http://<machine>:82` ऊपरी दाएँ की तरफ के विन्यास में क्लिक कर या क्लिक करें. तब उस पर क्लिक करें `API Keys` टैब तथा एक नई कुंजी जोड़ें. आप तब इस कुंजी का उपयोग अपने में कर सकते हैं `appsettings.json` फ़ाइल.

![सेक.](seqapikey.png)

# डॉकर बनाएं

अब हम यह सेट किया है हम हमारे गंभीरता को कॉन्फ़िगर करने की जरूरत है. किसी कुंजी को लेने के लिए इस्तेमाल किया गया अनुप्रयोग. मैं एक उपयोग `.env` मेरे रहस्य भंडारित करने के लिए फ़ाइल.

```dotenv
SEQ_DEFAULT_HASH="<adminpasswordhash>"
SEQ_API_KEY="<apikey>"
```

तो मेरे डॉकर रचना फ़ाइल में मैं निर्धारित करता हूँ कि मूल्य को एक एनवायरनमेंट वेरिएबल के रूप में मेरे सीमेंट के रूप में हल किया जाना चाहिए.

```docker
services:
  mostlylucid:
    image: scottgal/mostlylucid:latest
    ports:
      - 8080:8080
    restart: always
    labels:
        - "com.centurylinklabs.watchtower.enable=true"
    env_file:
      - .env
    environment:
      - Auth__GoogleClientId=${AUTH_GOOGLECLIENTID}
      - Auth__GoogleClientSecret=${AUTH_GOOGLECLIENTSECRET}
      - Auth__AdminUserGoogleId=${AUTH_ADMINUSERGOOGLEID}
      - SmtpSettings__UserName=${SMTPSETTINGS_USERNAME}
      - SmtpSettings__Password=${SMTPSETTINGS_PASSWORD}
      - Analytics__UmamiPath=${ANALYTICS_UMAMIPATH}
      - Analytics__WebsiteId=${ANALYTICS_WEBSITEID}
      - ConnectionStrings__DefaultConnection=${POSTGRES_CONNECTIONSTRING}
      - TranslateService__ServiceIPs=${EASYNMT_IPS}
      - Serilog__WriteTo__0__Args__apiKey=${SEQ_API_KEY}
    volumes:
      - /mnt/imagecache:/app/wwwroot/cache
      - /mnt/markdown/comments:/app/Markdown/comments
      - /mnt/logs:/app/logs
    networks:
      - app_network
```

ध्यान दीजिए कि `Serilog__WriteTo__0__Args__apiKey` का मान सेट है `SEQ_API_KEY` से `.env` फ़ाइल. '0' वह निर्देशिका है `WriteTo` क्रम से प्रेषित करें (_p) `appsettings.json` फ़ाइल.

# चाउ

Seeq और मेरे अतिरिक्त दोनों के लिए ध्यान दें। मैं निर्दिष्ट किया है कि वे दोनों मेरे हैं `app_network` नेटवर्क. यह है क्योंकि मैं cudy प्रॉक्सी को उलट कर के रूप में उपयोग करते हैं और यह एक ही नेटवर्क पर है. इसका अर्थ है कि मैं सेवा नाम का उपयोग अपने Cudy फ़ाइल में यूआरएल के रूप में कर सकते हैं.

```caddy
{
    email scott.galloway@gmail.com
}
seq.mostlylucid.net
{
   reverse_proxy seq:80
}

http://seq.mostlylucid.net
{
   redir https://{host}{uri}
}
```

तो यह नक्शे को सक्षम कर सकता है `seq.mostlylucid.net` मेरे उदाहरण के लिए.

# कंटेनमेंट

केऑप्ट एक महान औज़ार है लॉगिंग के लिए तथा अपने अनुप्रयोग की गणना करने के लिए. यह ऊपर सेट करने के लिए आसान है और अधिक सेरिट के साथ अच्छी तरह से उपयोग और निर्माण. मुझे लगता है कि मेरे अनुप्रयोगों में यह मूल्यवान पाया है और मुझे यकीन है कि आप भी होगा।