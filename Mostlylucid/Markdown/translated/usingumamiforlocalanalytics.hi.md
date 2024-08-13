# स्थानीय विश्लेषणों के लिए उममी इस्तेमाल किया जा रहा है

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024- 0. 0812: 5</datetime>

## परिचय

एक चीज जो मुझे अपने वर्तमान सेटअप के बारे में परेशान कर रही थी, वह थी गूगल अनिशिक डेटा प्राप्त करने के लिए उपयोग करना (यह क्या कम है?) तो मैं कुछ पाना चाहता था मैं स्व-host कि गूगल या किसी भी अन्य तीसरी पार्टी के लिए डेटा पारित नहीं किया गया था। मुझे पता चला [उममी](https://umami.is/) जो एक सरल, स्व- हस्ताक्षरित वेब समाधान है। यह गूगल विश्लेषण के लिए एक महान विकल्प है और यह आसान है ऊपर सेट करने के लिए।

[विषय

## संस्थापन

संस्थापन सरल है लेकिन वास्तव में जा रही करने के लिए एक उचित बिट ले लिया...

### डॉकर बनाएं

के रूप में मैं अपने मौजूदा डॉकeer-eeepe सेटअप में उममी जोड़ना चाहता था...... मैं मेरे लिए एक नई सेवा जोड़ने के लिए की जरूरत है `docker-compose.yml` फ़ाइल. मैंने निम्न में फ़ाइल के तल में जोड़ा:

```yaml
  umami:
    image: ghcr.io/umami-software/umami:postgresql-latest
    env_file: .env
    environment:
      DATABASE_URL: ${DATABASE_URL}
      DATABASE_TYPE: ${DATABASE_TYPE}
      HASH_SALT: ${HASH_SALT}
      APP_SECRET: ${APP_SECRET}
      TRACKER_SCRIPT_NAME: getinfo
      API_COLLECT_ENDPOINT: all
    ports:
      - "3000:3000"
    depends_on:
      - db
    networks:
      - app_network
    restart: always
  db:
    image: postgres:16-alpine
    env_file:
      - .env
    networks:
      - app_network
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER}"]
      interval: 5s
      timeout: 5s
      retries: 5
    volumes:
      - /mnt/umami/postgres:/var/lib/postgresql/data
    restart: always
  cloudflaredumami:
    image: cloudflare/cloudflared:latest
    command: tunnel --no-autoupdate run --token ${CLOUDFLARED_UMAMI_TOKEN}
    env_file:
      - .env
    restart: always
    networks:
      - app_network


```

इस डॉकer- क़िस्म.yml फ़ाइल में निम्न सेटअप है:

1. एक नयी सेवा जिसे कहा जाता है `umami` जो उपयोग में आता है `ghcr.io/umami-software/umami:postgresql-latest` छवि. इस सेवा का इस्तेमाल उमा एक अतिवादी सेवा चलाने के लिए किया जाता है।
2. एक नयी सेवा जिसे कहा जाता है `db` जो उपयोग में आता है `postgres:16-alpine` छवि. यह सेवा पोस्टग्रेस डाटाबेस को चलाने के लिए प्रयोग में लिया जाता है जो कि उममी डाटा को स्टोर करने के लिए प्रयोग करता है.
   इस सेवा के लिए नोट करें कि मैं इसे अपने सर्वर पर डिरेक्ट्री के लिए मैप कर रहा हूँ ताकि डेटा फिर से प्रारंभ करने के बीच बंद हो गया है.

```yaml
    volumes:
      - /mnt/umami/postgres:/var/lib/postgresql/data
```

आप इस निदेशक को अस्तित्व में होने की आवश्यकता होगी और आपके सर्वर पर डाकर उपयोक्ता द्वारा लिखने की जरूरत होगी (फिर से नहीं) तो 777 यहाँ है!___

```shell
chmod 777 /mnt/umami/postgres
```

3. एक नयी सेवा जिसे कहा जाता है `cloudflaredumami` जो उपयोग में आता है `cloudflare/cloudflared:latest` छवि. यह सेवा बादल के माध्यम से उमर सेवा को चलाने के लिए इस्तेमाल की जाती है ताकि इसे इंटरनेट से पहुँचा जा सके.

### लि. फ़ाइल

इस समर्थन के लिए मैं भी अपने अद्यतन किया `.env` निम्न में शामिल करने के लिए फ़ाइल:

```shell
CLOUDFLARED_UMAMI_TOKEN=<cloudflaretoken>
DATABASE_TYPE=postgresql
HASH_SALT=<salt>

POSTGRES_DB=postgres
POSTGRES_USER=<postgresuser>
POSTGRES_PASSWORD=<postgrespassword>
UMAMI_SECRET=<umamisecret>

APP_SECRET=${UMAMI_SECRET}
UMAMI_USER=${POSTGRES_USER}
UMAMI_PASS=${POSTGRES_PASSWORD}
DATABASE_URL=postgresql://${UMAMI_USER}:${UMAMI_PASS}@db:5432/${POSTGRES_DB}
```

यह विन्यास को वर्तमान विन्यास के लिए सेट करता है. `<>` हैलीम को स्पष्ट रूप से आपके स्वयं के मूल्यों से बदलने की जरूरत है. वह `cloudflaredumami` सेवा का इस्तेमाल बादल के माध्यम से उममी सेवा को चलाने के लिए किया जाता है ताकि इसे इंटरनेट से पहुँचा जा सके. यह PUSIBELTEEATH का उपयोग करने के लिए PULELELELELER है लेकिन उममी के लिए यह मुश्किल से बेस पथ बदलने के लिए एक पुनः की जरूरत है इसलिए मैं इसे अब के लिए रूट पथ के रूप में छोड़ दिया है.

### बादल शल्बनhaiti. kgm

इस के लिए बादलफुलर सुरंग सेट करने के लिए (जो कि जेsses - प्राप्तs. org के लिए इस्तेमाल किया गया है) मैं वेबसाइट उपयोग किया:

![बादल शल्बनhaiti. kgm](umamisetup.png)

यह सुरंग उममी सेवा को सेट करता है और इसे इंटरनेट से एक्सेस करने की अनुमति देता है. ध्यान दीजिए, मैं इस पर ध्यान देता हूँ `umami` डाकer- क़िस्म की फ़ाइल में सेवा (जैसा कि यह एक ही नेटवर्क पर है जो बादलोला सुरंग के रूप में एक वैध नाम है).

### पृष्ठ पर उममी सेटअप

स्क्रिप्ट के लिए पथ सक्षम करने के लिए (नाम में बुलाया गया) `getinfo` ऊपर मेरे सेटअप में, मैं मेरे एप्पल के लिए एक कॉन्फ़िग प्रविष्टि जोड़ा है

```json
 "Analytics":{
   "UmamiPath" : "https://umamilocal.mostlylucid.net/getinfo",
   "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee"
 },
```

आप इन्हें अपने.env फ़ाइल में जोड़ सकते हैं तथा वातावरण चर को डाक- क़िस्म की फ़ाइल में भेज सकते हैं.

```shell
ANALYTICS__UMAMIPATH="https://umamilocal.mostlylucid.net/getinfo"
ANALYTICS_WEBSITEID="32c2aa31-b1ac-44c0-b8f3-ff1f50403bee"
```

```yaml
  mostlylucid:
    image: scottgal/mostlylucid:latest
    ports:
      - 8080:8080
    restart: always
    environment:
    ...
      - Analytics__UmamiPath=${ANALYTICS_UMAMIPATH}
      - Analytics__WebsiteId=${ANALYTICS_WEBSITEID}
```

आप इस साइट को सेट जब उममी डाबोर्ड में वेबसाइट Id सेट किया. उममी सेवा के लिए डिफ़ॉल्ट उपयोक्ता उपयोक्ता नाम तथा पासवर्ड नहीं है `admin` और `umami`, आप सेटअप के बाद इन बदलने की जरूरत है.
![उममी डाजबोर्ड](umamiaddwebsite.png)

संबद्ध विन्यास सी फ़ाइल के साथ:

```csharp
public class AnalyticsSettings : IConfigSection
{
    public static string Section => "Analytics";
    public string? UmamiPath { get; set; }
}
```

यह फिर से मेरा POCO कॉन्फ़िग सामान उपयोग करता है (एओएओएओ कॉन्फ़िगरेशन सामान)[यहाँ](/blog/addingidentityfreegoogleauth#configuring-google-auth-with-poco)विन्यास सेट करने के लिए ()
इसे मेरे कार्यक्रम में सेट करें.cs:

```csharp
builder.Configure<AnalyticsSettings>();
```

और अंत में मेरे अंदर `BaseController.cs` `OnGet` विधि मैं निम्न को एक अजीब स्क्रिप्ट के लिए पथ सेट करने के लिए जोड़ा है:

```csharp
   public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        if (!Request.IsHtmx())
        {
            ViewBag.UmamiPath = _analyticsSettings.UmamiPath;
            ViewBag.UmamiWebsiteId = _analyticsSettings.WebsiteId;
        }
        base.OnActionExecuting(filterContext);
    }
    
```

यह खाका फ़ाइल में प्रयोग किए जाने वाले स्क्रिप्ट के लिए पथ सेट करता है.

### खाका फ़ाइल

अंत में, मैं अपने खाका फ़ाइल में शामिल करने के लिए निम्नलिखित जोड़ा है:

```html
<script defer src="@ViewBag.UmamiPath" data-website-id="@ViewBag.UmamiWebsiteId"></script>
```

इसमें पृष्ठ में स्क्रिप्ट सम्मिलित है तथा वेब साइट आईडी को मध्य सेवा के लिए सेट करता है.

## अपने आप को जादू - टोने से प्रेरित करता है

अपने आप से मुलाकातों को एक पार करने के लिए आप अपने ब्राउज़र में निम्न भंडारण जोड़ सकते हैं:

क्रोमोसीसी उपकरण (Ctrl+STI I) आप इन्हें कंसोल में जोड़ सकते हैं:

```javascript
localStorage.setItem("umami.disabled", 1)
```

## कंटेनमेंट

यह सेट करने के लिए एक faffff का एक सा था लेकिन मैं परिणाम के साथ खुश हूँ. अब मैं एक आत्म-प्रयोगीय सेवा है जो गूगल या किसी भी अन्य तीसरी पार्टी में डेटा पारित नहीं करता है. यह सेट करने के लिए एक दर्द का एक सा है लेकिन एक बार यह यह यह इस्तेमाल करने के लिए बहुत आसान है. मैं परिणाम के साथ खुश हूँ और यह किसी के लिए सुझाव होगा किसी को भी किसी भी व्यक्ति के लिए एक आत्म-प्रयोगी समाधान की तलाश.