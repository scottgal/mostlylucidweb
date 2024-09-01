# एनईएससी के लिए सेंडसी लॉग - से- पॉपिंग से

<datetime class="hidden">2024- 0. 3131T11: 20</datetime>

<!--category-- ASP.NET, Seq, Serilog -->
# परिचय

पिछले भाग में मैं तुम्हें दिखाने के लिए कैसे ऊपर सेट करने के लिए पता चला [क्यूपी का उपयोग करने के लिए खुद का मेजबान. NENT कोर ](/blog/selfhostingseq)___ अब कि हम यह सेट किया है यह का उपयोग करने के लिए समय है इससे अधिक विस्तार के लिए हमारे नए SEEEEEEE उदाहरण का उपयोग करने की अनुमति देने के लिए।

[विषय

# ट्रैस कर रहा है

शिकार करना लॉगिंग++ की तरह है यह आपको आपके अनुप्रयोग में क्या हो रहा है के बारे में जानकारी की एक अतिरिक्त परत देता है. यह विशेष रूप से उपयोगी है जब आपके पास वितरित तंत्र है और आपको अनेक सेवाओं के माध्यम से निवेदन का पता लगाने की जरूरत है.
इस साइट में मैं इसे जल्दी से विषय पर ट्रैक करने के लिए उपयोग कर रहा हूँ, सिर्फ इसलिए कि यह एक शौकपूर्ण साइट है मैं अपने पेशेवर स्तरों को छोड़ देने का मतलब नहीं है।

## सेआईलॉग सेट किया जा रहा है

सेवरी लॉग के साथ प्राप्ति सेटअप करना वास्तव में बहुत सरल है [सेर- लिब- ट्रेकरName](https://github.com/serilog-tracing/serilog-tracing) पैकेज. पहले आपको पैकेज संस्थापित करना होगा:

यहाँ हम कंसोल सिंक और SEQ डूबने को भी जोड़ते हैं

```bash
dotnet add package SerilogTracing
dotnet add package SerilogTracing.Expressions
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.Seq
```

कंसोल हमेशा डिबगिंग और सीक्यू के लिए उपयोगी है हम के लिए यहाँ क्या कर रहे हैं. Sequin 'इनरंसर्स' का समूह भी है जो आपके लॉगों के लिए अतिरिक्त जानकारी जोड़ सकता है.

```bash
  "Serilog": {
    "Enrich": ["FromLogContext", "WithThreadId", "WithThreadName", "WithProcessId", "WithProcessName", "FromLogContext"],
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
    "Properties": {
      "ApplicationName": "mostlylucid"
    }
  }
```

इन समृद्धों का उपयोग करने के लिए आप उन्हें अपने में जोड़ने की जरूरत है `Serilog` अपने में कॉन्फ़िगरेशन `appsettings.json` फ़ाइल. आपको एनयूवा के प्रयोग से सभी रिसर्चो को भी संस्थापित करने की ज़रूरत है ।

यह सेरल के बारे में अच्छी और बुरी चीजों में से एक है, आप हवा ऊपर पैकेज के एक BUNCH का उपयोग करते हैं, लेकिन इसका मतलब है कि आप केवल एक ही चीज़ जोड़ते हैं और केवल एक एकल एकल पैकेज.
यहाँ मेरा है

![सेर लॉगरिएशन्स](serilogenrichers.png)

इन सभी बमों के साथ मैं सी. में एक बहुत अच्छा लॉग आउटपुट मिलता है.

![सेरेसिलॉग सेक त्रुटि](serilogerror.png)

यहाँ आप त्रुटि संदेश देखते हैं, स्टैक ट्रेस, थ्रेड आईडी, प्रक्रिया आईडी तथा प्रक्रिया नाम देखें. यह सब उपयोगी जानकारी है जब आप एक अंक को नीचे ट्रैक करने की कोशिश कर रहे हैं.

एक बात मैं सेट है ध्यान केंद्रित करने के लिए एक बात है `  "MinimumLevel": "Warning",` मेरे में `appsettings.json` फ़ाइल. इसका मतलब है कि सिर्फ चेतावनियाँ और ऊपर से सी. यह आपके लॉग में ध्वनि को कम रखने के लिए उपयोगी है.

हालांकि सेक्वेंट में आप इस प्रति ईपी कुंजी को भी निर्दिष्ट कर सकते हैं, इसी प्रकार आप कर सकते हैं `Information` (या अगर आप वास्तव में उत्साही हैं) `Debug`लॉगिंग यहाँ सेट और सीमा जो कि सीजे वास्तव में एपीआई कुंजी द्वारा कैप्चर करता है.

![सेमीपी कुंजी](apikey.png)

ध्यान दीजिए: आपके पास अभी - भी ऊपरी कपड़ा है, तो आप इसे और अधिक शक्‍तिशाली बना सकते हैं ताकि आप हवाई - जहाज़ के स्तर को समायोजित कर सकें । देखें [सेक्वेंस सिंक ](https://github.com/datalust/serilog-sinks-seq)अधिक विवरण के लिए.

```json
{
    "Serilog":
    {
        "LevelSwitches": { "$controlSwitch": "Information" },
        "MinimumLevel": { "ControlledBy": "$controlSwitch" },
        "WriteTo":
        [{
            "Name": "Seq",
            "Args":
            {
                "serverUrl": "http://localhost:5341",
                "apiKey": "yeEZyL3SMcxEKUijBjN",
                "controlLevelSwitch": "$controlSwitch"
            }
        }]
    }
}
```

## ट्रैस कर रहा है

अब हम बार-बार जोड़ते हैं, फिर से से से यह बहुत सरल है। हम पहले के रूप में एक ही सेटअप है लेकिन हम ज्ञानियों के लिए एक नया सिंक जोड़.

```bash
dotnet add package SerilogTracing
dontet add package SerilogTracing.Expressions
dotnet add SerilogTracing.Instrumentation.AspNetCore
```

हम एक अतिरिक्त पैकेज को भी जोड़ते हैं जो अधिक विस्तृत रूप से सूचना के रूप में लॉग कर रहा है.

### में सेटअप करें `Program.cs`

अब हम वास्तव में ज्ञानियों का उपयोग शुरू कर सकते हैं. सबसे पहले हम अपने लिए बढ़ई जोड़ने की जरूरत है `Program.cs` फ़ाइल.

```csharp
    using var listener = new ActivityListenerConfiguration()
        .Instrument.HttpClientRequests().Instrument
        .AspNetCoreRequests()
        .TraceToSharedLogger();
```

वार्तालाप 'काम' की धारणा का उपयोग करता है जो काम की इकाई का प्रतिनिधित्व करता है. आप एक गतिविधि शुरू कर सकते हैं, कुछ काम करो और फिर इसे बंद करो. अनेक सेवाओं के माध्यम से निवेदन करने के लिए यह उपयोगी है.

इस मामले में हम Vacralilient अनुरोधों और Aptetanna अनुरोधों के लिए अतिरिक्त ज्ञान जोड़ते हैं. हम भी एक जोड़ `TraceToSharedLogger` जो हमारे शेष अनुप्रयोग के रूप में एक ही लॉगर का उपयोग करेगा.

## सेवा में रुकावटों का इस्तेमाल किया जा रहा है

अब हम अपने अनुप्रयोग में इसका उपयोग शुरू कर सकते हैं ट्रेसी सेट किया है. यहाँ एक सेवा का एक उदाहरण है जो ज्ञानियों का उपयोग करती है।

```csharp
    public async Task<PostListViewModel> GetPostsByCategory(string category, int page = 1, int pageSize = 10,
        string language = MarkdownBaseService.EnglishLanguage)
    {
        using var activity = Log.Logger.StartActivity("GetPostsByCategory {Category}, {Page}, {PageSize}, {Language}",
            new { category, page, pageSize, language });
        try
        {
            var count = await NoTrackingQuery()
                .Where(x => x.Categories.Any(c => c.Name == category) && x.LanguageEntity.Name == language)
                .CountAsync();
            var posts = await PostsQuery()
                .Where(x => x.Categories.Any(c => c.Name == category) && x.LanguageEntity.Name == language)
                .OrderByDescending(x => x.PublishedDate.DateTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var languages = await GetLanguagesForSlugs(posts.Select(x => x.Slug).ToList());
            var postListViewModel = new PostListViewModel
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = count,
                Posts = posts.Select(x => x.ToListModel(
                    languages.FirstOrDefault(entry => entry.Key == x.Slug).Value.ToArray())).ToList()
            };
            activity.Complete();
            return postListViewModel;
        }
        catch (Exception e)
        {
            activity.Complete(LogEventLevel.Error, e);
        }

        return new PostListViewModel();
    }
```

महत्वपूर्ण पंक्तियाँ यहाँ हैं:

```csharp
  using var activity = Log.Logger.StartActivity("GetPostsByCategory {Category}, {Page}, {PageSize}, {Language}",
            new { category, page, pageSize, language });
```

यह एक नया 'कार्य' प्रारंभ करता है जो कि कार्य की इकाई है. यह बहुत से सेवाओं के माध्यम से एक अनुरोध के लिए उपयोगी है.
जैसा कि हमने इसे इस कथन का उपयोग करके पूरा कर लिया है...... और हमारे तरीके के अंत में...... लेकिन यह स्पष्ट रूप से पूरी तरह से इसे पूरा करने के लिए अच्छा अभ्यास है.

```csharp
            activity.Complete();
```

अगर हम ऐसा करते हैं, तो हम यहोवा के साथ एक करीबी रिश्‍ता बना सकते हैं । यह आपके अनुप्रयोग में नीचे दिए मुद्दों को चिपकाने के लिए उपयोगी है.

## ट्रेसेस का उपयोग कर रहा है

अब हम यह सब सेटअप है हम इसे उपयोग कर शुरू कर सकते हैं. यहाँ मेरे अनुप्रयोग में एक ट्रेस का एक उदाहरण है.

![दिल का पता](httptrace.png)

यह आपको एक चिह्न के बाद के अनुवाद को दिखाता है. आप एक एकल पोस्ट के लिए बहुत से कदम देख सकते हैं और सभी gicicentents और समय समय के लिए अनुरोध कर सकते हैं.

नोट करें कि मैं अपने डाटाबेस के लिए पोस्टआरआईएस इस्तेमाल करता हूँ, एसक्यूएल सर्वर से विपरीत एसक्यूएल सर्वर के लिए एनजीजीएस ड्राइवर के लिए आपका समर्थित समर्थन है, तो आप अपने डाटाबेस के डाटाबेस के अनुग्रह से बहुत उपयोगी डाटा प्राप्त कर सकते हैं जैसे कि एसक्यूएल कार्य चलाया गया, समय इत्यादि. इन्हें Sqqq के रूप में सुरक्षित रखा जाता है और निम्न को झूठ रूप में देखते हैं:

```json
  "@t": "2024-08-31T15:23:31.0872838Z",
"@mt": "mostlylucid",
"@m": "mostlylucid",
"@i": "3c386a9a",
"@tr": "8f9be07e41f7121cbf2866c6cd886a90",
"@sp": "8d716c5f01ad07a0",
"@st": "2024-08-31T15:23:31.0706848Z",
"@ps": "622f1c86a8b33304",
"@sk": "Client",
"ActionId": "91f5105d-93fa-4e7f-9708-b1692e046a8a",
"ActionName": "Mostlylucid.Controllers.HomeController.Index (Mostlylucid)",
"ApplicationName": "mostlylucid",
"ConnectionId": "0HN69PVEQ9S7C",
"ProcessId": 30496,
"ProcessName": "Mostlylucid",
"RequestId": "0HN69PVEQ9S7C:00000015",
"RequestPath": "/",
"SourceContext": "Npgsql",
"ThreadId": 47,
"ThreadName": ".NET TP Worker",
"db.connection_id": 1565,
"db.connection_string": "Host=localhost;Database=mostlylucid;Port=5432;Username=postgres;Application Name=mostlylucid",
"db.name": "mostlylucid",
"db.statement": "SELECT t.\"Id\", t.\"ContentHash\", t.\"HtmlContent\", t.\"LanguageId\", t.\"Markdown\", t.\"PlainTextContent\", t.\"PublishedDate\", t.\"SearchVector\", t.\"Slug\", t.\"Title\", t.\"UpdatedDate\", t.\"WordCount\", t.\"Id0\", t0.\"BlogPostId\", t0.\"CategoryId\", t0.\"Id\", t0.\"Name\", t.\"Name\"\r\nFROM (\r\n    SELECT b.\"Id\", b.\"ContentHash\", b.\"HtmlContent\", b.\"LanguageId\", b.\"Markdown\", b.\"PlainTextContent\", b.\"PublishedDate\", b.\"SearchVector\", b.\"Slug\", b.\"Title\", b.\"UpdatedDate\", b.\"WordCount\", l.\"Id\" AS \"Id0\", l.\"Name\", b.\"PublishedDate\" AT TIME ZONE 'UTC' AS c\r\n    FROM mostlylucid.\"BlogPosts\" AS b\r\n    INNER JOIN mostlylucid.\"Languages\" AS l ON b.\"LanguageId\" = l.\"Id\"\r\n    WHERE l.\"Name\" = @__language_0\r\n    ORDER BY b.\"PublishedDate\" AT TIME ZONE 'UTC' DESC\r\n    LIMIT @__p_2 OFFSET @__p_1\r\n) AS t\r\nLEFT JOIN (\r\n    SELECT b0.\"BlogPostId\", b0.\"CategoryId\", c.\"Id\", c.\"Name\"\r\n    FROM mostlylucid.blogpostcategory AS b0\r\n    INNER JOIN mostlylucid.\"Categories\" AS c ON b0.\"CategoryId\" = c.\"Id\"\r\n) AS t0 ON t.\"Id\" = t0.\"BlogPostId\"\r\nORDER BY t.c DESC, t.\"Id\", t.\"Id0\", t0.\"BlogPostId\", t0.\"CategoryId\"",
"db.system": "postgresql",
"db.user": "postgres",
"net.peer.ip": "::1",
"net.peer.name": "localhost",
"net.transport": "ip_tcp",
"otel.status_code": "OK"
```

आप देख सकते हैं कि इस में कितना कुछ सम्मिलित है जिसे आप प्रश्न के बारे में जानना चाहते हैं, एसक्यूएल ने दिया, कनेक्शन वाक्यांश इत्यादि. यह सब उपयोगी जानकारी है जब आप एक अंक को नीचे ट्रैक करने की कोशिश कर रहे हैं. इस तरह एक छोटे से ऐप में यह सिर्फ दिलचस्प है, एक वितरित अनुप्रयोग में यह नीचे अंकों को ट्रैक करने के लिए ठोस सोने जानकारी है।

# ऑन्टियम

मैं केवल यहाँ आक्रमण की सतह, यह जोशपूर्ण समर्थक के साथ एक सा क्षेत्र है. उम्मीद है कि मैंने देखा है कि यह कितना सरल है सीक्कर के साथ जा रहा है ACaqucilog के लिए उपयोग कर। NET कोर अनुप्रयोगों के लिए। इस तरह मैं इंसाइट ऑन द स्क्रिप्चर्स की तरह ज़्यादा शक्‍तिशाली औज़ारों से फायदा पा सकता हूँ ।