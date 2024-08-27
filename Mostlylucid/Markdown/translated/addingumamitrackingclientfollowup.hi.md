# उममी खोज क्लाइंट का अनुसरण कर रहा है

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024- 0. 272: 00</datetime>

# परिचय

एक में [पहले पोस्ट](/blog/addingumamitrackingclient.md) मैं C# में उममी के लिए एक ट्रैकिंग ग्राहक कैसे चित्रित किया.
खैर मैं अंत में यह व्यापक परीक्षण करने के लिए एक मौका था और इसे सुधार (हाँएएक और अधिक है) `IHostedService`).

[विषय

# उममी एपीआई के रजाई

उममी ट्रैकिंग एपीआई दोनों बहुत अलग और बहुत विचार है. तो मुझे निम्नलिखित कोड को संभालने के लिए अद्यतन करना पड़ा:

1. एपीआई 'सही' उपयोक्ता एजेंट स्ट्रिंग की आशा करता है. तो मैं एक वास्तविक उपयोक्ता एजेंट स्ट्रिंग का उपयोग करने के लिए ग्राहकों को अद्यतन करना पड़ा (या मैं एक ब्राउज़र से एक वास्तविक उपयोक्ता एजेंट स्ट्रिंग को पकड़ लिया और उस उपयोग में लिया.
2. एपीआई आशा करता है कि यह एक बहुत विशिष्ट प्रारूप में JSON इनपुट है; खाली स्ट्रिंग स्वीकार्य नहीं हैं. तो मैं इस संभाल करने के लिए ग्राहक अद्यतन किया था.
3. वह [API क्लाइंट](https://github.com/umami-software/node) एक अजीब सतह क्षेत्र का एक सा है. यह तुरंत स्पष्ट नहीं है कि एपीआई क्या उम्मीद करता है. इसलिए मुझे कुछ परीक्षण करना पड़ा...... और इसे काम करने के लिए त्रुटि.

## नोड एपीआई क्लाएंट

पूरी तरह से नोड एपीआई ग्राहक नीचे है, यह सुपर फेरबदल है लेकिन वास्तव में अच्छी तरह से रिकॉर्ड नहीं है.

```javascript
export interface UmamiOptions {
  hostUrl?: string;
  websiteId?: string;
  sessionId?: string;
  userAgent?: string;
}

export interface UmamiPayload {
  website: string;
  session?: string;
  hostname?: string;
  language?: string;
  referrer?: string;
  screen?: string;
  title?: string;
  url?: string;
  name?: string;
  data?: {
    [key: string]: string | number | Date;
  };
}

export interface UmamiEventData {
  [key: string]: string | number | Date;
}

export class Umami {
  options: UmamiOptions;
  properties: object;

  constructor(options: UmamiOptions = {}) {
    this.options = options;
    this.properties = {};
  }

  init(options: UmamiOptions) {
    this.options = { ...this.options, ...options };
  }

  send(payload: UmamiPayload, type: 'event' | 'identify' = 'event') {
    const { hostUrl, userAgent } = this.options;

    return fetch(`${hostUrl}/api/send`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'User-Agent': userAgent || `Mozilla/5.0 Umami/${process.version}`,
      },
      body: JSON.stringify({ type, payload }),
    });
  }

  track(event: object | string, eventData?: UmamiEventData) {
    const type = typeof event;
    const { websiteId } = this.options;

    switch (type) {
      case 'string':
        return this.send({
          website: websiteId,
          name: event as string,
          data: eventData,
        });
      case 'object':
        return this.send({ website: websiteId, ...(event as UmamiPayload) });
    }

    return Promise.reject('Invalid payload.');
  }

  identify(properties: object = {}) {
    this.properties = { ...this.properties, ...properties };
    const { websiteId, sessionId } = this.options;

    return this.send(
      { website: websiteId, session: sessionId, data: { ...this.properties } },
      'identify',
    );
  }

  reset() {
    this.properties = {};
  }
}

const umami = new Umami();

export default umami;
```

जैसा कि आप देखते हैं कि यह निम्नलिखित तरीक़ों का परदाफ़ाश करता है:

1. `init` - विकल्प सेट करने के लिए.
2. `send` - भुगतान भेजने के लिए.
3. `track` - एक घटना को ट्रैक करने के लिए.
4. `identify` - उपयोगकर्ता की पहचान करने के लिए.
5. `reset` - गुण रीसेट करने के लिए.

इस के शीर्ष है `send` विधि जो बिल में लोड होता है

```javascript
  send(payload: UmamiPayload, type: 'event' | 'identify' = 'event') {
    const { hostUrl, userAgent } = this.options;

    return fetch(`${hostUrl}/api/send`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'User-Agent': userAgent || `Mozilla/5.0 Umami/${process.version}`,
      },
      body: JSON.stringify({ type, payload }),
    });
  }
```

# C# क्लाएंट

मैं के साथ शुरू करने के लिए बहुत अधिक नोड एपीआई ग्राहक की नक़ल की `UmamiOptions` और `UmamiPayload` कक्षा (मैं उन्हें फिर से नहीं छोड़ा होगा वे बड़े हैं).

तो अब मेरा `Send` विधि इस तरह दिखती है:

```csharp
     public async Task<HttpResponseMessage> Send(UmamiPayload? payload=null, UmamiEventData? eventData =null,  string type = "event")
        {
            var websiteId = settings.WebsiteId;
             payload = PopulateFromPayload(websiteId, payload, eventData);
            
            var jsonPayload = new { type, payload };
            logger.LogInformation("Sending data to Umami: {Payload}", JsonSerializer.Serialize(jsonPayload, options));

            var response = await client.PostAsJsonAsync("api/send", jsonPayload, options);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Failed to send data to Umami: {StatusCode}, {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                logger.LogInformation("Successfully sent data to Umami: {StatusCode}, {ReasonPhrase}, {Content}", response.StatusCode, response.ReasonPhrase, content);
            }

            return response;
        }

```

दो महत्वपूर्ण भागों यहाँ है:

1. वह `PopulateFromPayload` विधि जो भुगतान वेबसाइट Id तथा घटना डाटा के साथ भरता है.
2. रेंडर का JSON सीरियलीकरण, इसे नल मूल्यों को अलग करने की जरूरत है.

## वह `PopulateFromPayload` विधि

```csharp
        public static UmamiPayload PopulateFromPayload(string webSite, UmamiPayload? payload, UmamiEventData? data)
        {
            var newPayload = GetPayload(webSite, data: data);
            if(payload==null) return newPayload;
            if(payload.Hostname != null)
                newPayload.Hostname = payload.Hostname;
            if(payload.Language != null)
                newPayload.Language = payload.Language;
            if(payload.Referrer != null)
                newPayload.Referrer = payload.Referrer;
            if(payload.Screen != null)
                newPayload.Screen = payload.Screen;
            if(payload.Title != null)
                newPayload.Title = payload.Title;
            if(payload.Url != null)
                newPayload.Url = payload.Url;
            if(payload.Name != null)
                newPayload.Name = payload.Name;
            if(payload.Data != null)
                newPayload.Data = payload.Data;
            return newPayload;          
        }
        
        private static UmamiPayload GetPayload(string websiteId, string? url = null, UmamiEventData? data = null)
        {
            var payload = new UmamiPayload
            {
            Website = websiteId,
                Data = data,
                Url = url ?? string.Empty
            };
            

            return payload;
        }

```

आप देख सकते हैं कि हम हमेशा सुनिश्चित कर सकते हैं `websiteId` सेट है और हम सिर्फ अन्य मान सेट करें यदि वे नल नहीं हैं. यह हमें क्रियाोसी के एक बिट के ख़र्च पर हल्का - सा बल देता है ।

## giociolient सेटअप

जैसा कि पहले उल्लेख किया गया है हमें कुछ असली उपयोक्ता एजेंट वाक्यांश एपीआई में देने की जरूरत है. यह किया जाता है `HttpClient` सेटअप.

```csharp
              services.AddHttpClient<UmamiClient>((serviceProvider, client) =>
            {
                 umamiSettings = serviceProvider.GetRequiredService<UmamiClientSettings>();
            client.DefaultRequestHeaders.Add("User-Agent", $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/127.0.0.0 Safari/537.36");
            client.BaseAddress = new Uri(umamiSettings.UmamiPath);
        }).SetHandlerLifetime(TimeSpan.FromMinutes(5))  //Set lifetime to five minutes
        .AddPolicyHandler(GetRetryPolicy())
       #if DEBUG 
        .AddLogger<HttpLogger>();
        #else
        ;
        #endif

```

## पृष्ठभूमि सेवा

यह एक और है `IHostedService`, इन ऊपर सेट करने के लिए कैसे के लेखों का एक गुच्छा है तो मैं यहाँ में नहीं जाना होगा (खोज पट्टी)!___

केवल दर्द का मतलब था विडगेट का प्रयोग करना `HttpClient` में `UmamiClient` वर्ग. क्लाएंट & मैं एक इस्तेमाल की सेवा के scenting के बारे में `IServiceScopeFactory` सेना के निर्माणकर्ता में शामिल होने के बाद प्रत्येक भेजें निवेदन के लिए उसे पकड़ लें.

```csharp
    

    private async Task SendRequest(CancellationToken token)
    {
        logger.LogInformation("Umami background delivery started");

        while (await _channel.Reader.WaitToReadAsync(token))
        {
            while (_channel.Reader.TryRead(out var payload))
            {
                try
                {
                   using  var scope = scopeFactory.CreateScope();
                    var client = scope.ServiceProvider.GetRequiredService<UmamiClient>();
                    // Send the event via the client
                    await client.Send(payload.Payload);

                    logger.LogInformation("Umami background event sent: {EventType}", payload.EventType);
                }
                catch (OperationCanceledException)
                {
                    logger.LogWarning("Umami background delivery canceled.");
                    return; // Exit the loop on cancellation
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error sending Umami background event.");
                }
            }
        }
    }
   
```

### होस्ट्ड सेवा इस्तेमाल किया जा रहा है

अब जबकि हमारे पास यह सेवा है, हम पृष्ठभूमि में घटनाओं को भेजने के द्वारा नाटकीय रूप से सुधार कर सकते हैं ।

मैं इसे अपने में, अलग अलग जगहों में इस्तेमाल किया है `Program.cs` मैंने कहा कि ERS फ़ीड निवेदन के प्रयोग से RSS फ़ीड को ट्रैक करने का प्रयास करने का निर्णय किया, यह सिर्फ 'RSS' में समाप्त किसी भी पथ का पता चलता है और एक पृष्ठभूमि घटना भेजता है.

```csharp
app.Use( async (context, next) =>
{
var path = context.Request.Path.Value;
if (path.EndsWith("RSS", StringComparison.OrdinalIgnoreCase))
{
var rss = context.RequestServices.GetRequiredService<UmamiBackgroundSender>();
// Send the event in the background
await rss.SendBackground(new UmamiPayload(){Url  = path, Name = "RSS Feed"});
}
await next();
});
```

मैं भी अपने से अधिक डेटा पारित कर दिया है `TranslateAPI` अंत बिन्दु.
जो मुझे यह देखने की अनुमति देता है कि कितने समय से अनुवाद ले रहे हैं; ध्यान दीजिए कि इनमें से कोई भी मुख्य थ्रेड या व्यक्‍तिगत व्यक्‍तियों का पालन करने के लिए ब्लॉक नहीं कर रहे हैं ।

```csharp
    
       await  umamiClient.SendBackground(new UmamiPayload(){  Name = "Get Translation"}, new UmamiEventData(){{"timetaken", translationTask.TotalMilliseconds}, {"language",translationTask.Language}});
        var result = new TranslateResultTask(translationTask, true);
```

# ऑन्टियम

उममी एपीआई एक छोटा सा सा है...... लेकिन यह एक आत्म-hosted तरीके से घटनाओं को ट्रैक करने के लिए एक महान तरीका है. उम्मीद है कि मैं इसे और अधिक साफ करने के लिए एक मौका मिल जाएगा और वहाँ एक उममी nuget पैकेज बाहर मिलता है.
द्वारा (F) [पहले लेख](/blog/addingascsharpclientforumamiapi)  मैं उममी के बाहर से डेटा बाहर खींच करना चाहते हैं लोगों को लोकप्रिय छंटाई के रूप में प्रदान करने के लिए।