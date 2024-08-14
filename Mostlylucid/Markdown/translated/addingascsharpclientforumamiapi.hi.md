# C# क्लाएंट को उममी एपीआई के लिए जोड़ा जा रहा है

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024- 14टी01: 27</datetime>

## प्रमाणपत्र उपयोग

इस पोस्ट में, मैं तुम्हें कैसे एक C# ग्राहक बनाने के लिए पता चल जाएगा उममी समाचार पत्र के लिए। यह एक सरल उदाहरण है जो दिखाता है कि कैसे एपीआई के साथ सत्यापन और इससे डेटा को प्राप्त करें.

आप इस के लिए सभी स्रोत कोड पा सकते हैं [मेरे Gihko पर](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Umami).

[विषय

## पूर्वपाराईज़

उममी संस्थापित करें. आपको संस्थापन निर्देश मिल सकते हैं [यहाँ](https://www.mostlylucid.net/blog/usingumamiforlocalanalytics) इस विवरण को मैं कैसे लागू करता हूं और उमा का उपयोग इस साइट के लिए एक समरामी प्रदान करने के लिए करते हैं.

एक बार फिर, यह उममी वेबसाइट के कुछ भागों में से एक सरल कार्यान्वयन है... / मैं आप पूरा एपीआई दस्तावेज पा सकते हैं [यहाँ](https://umami.is/docs/api/website-stats).

इस में मैंने निम्न अंतिम बिन्दुओं को लागू करने का चुनाव किया है:

- `GET /api/websites/:websiteId/pageviews` - जैसा कि नाम सुझाता है, यह अंत बिन्दु पृष्ठ दृश्य बताता है तथा एक समय पर दिए गए वेबसाइट के लिए 'Dads' बताता है.

```json
{
  "pageviews": [
    { "x": "2020-04-20 01:00:00", "y": 3 },
    { "x": "2020-04-20 02:00:00", "y": 7 }
  ],
  "sessions": [
    { "x": "2020-04-20 01:00:00", "y": 2 },
    { "x": "2020-04-20 02:00:00", "y": 4 }
  ]
}
```

- `GET /api/websites/:websiteId/stats` - यह दिए गए वेबसाइट के आधार पर मूल आंकड़े बताता है.

```json
{
  "pageviews": { "value": 5, "change": 5 },
  "visitors": { "value": 1, "change": 1 },
  "visits": { "value": 3, "change": 2 },
  "bounces": { "value": 0, "change": 0 },
  "totaltime": { "value": 4, "change": 4 }
}
```

- `GET /api/websites/:websiteId/metrics` - यह दिए गए वेबसाइट uu URL इत्यादि के लिए मुझे भेजा गया वायरस को लौटाता है...

```json
[
  { "x": "/", "y": 46 },
  { "x": "/docs", "y": 17 },
  { "x": "/download", "y": 14 }
]
```

जैसा कि आप डॉट्स से देख सकते हैं, ये सभी पैरामीटरों की संख्या स्वीकार करते हैं (और मैंने इन्हें नीचे दिए गए कोड में क्वैरी पैरामीटर्स के रूप में प्रतिनिधित्व किया है).

## सवार httpswidient में जाँच की जा रही है

मैं हमेशा Enrol-in HTTP ग्राहक में बनाया गया एपीआई परीक्षण के द्वारा शुरू होता है. यह मुझे एपीआई की जाँच करने और प्रतिक्रिया देखने की अनुमति देता है ।

```http
### Login Request and Store Token
POST https://{{umamiurl}}/api/auth/login
Content-Type: application/json

{
  "username": "{{username}}",

  "password": "{{password}}"
}
> {% client.global.set("auth_token", response.body.token);
    client.global.set("endAt", Math.round(new Date().getTime()).toString() );
    client.global.set("startAt", Math.round(new Date().getTime() - 7 * 24 * 60 * 60 * 1000).toString());
%}


### Use Token in Subsequent Request
GET https://{{umamiurl}}/api/websites/{{websiteid}}/stats?endAt={{endAt}}&startAt={{startAt}}
Authorization: Bearer {{auth_token}}

### Use Token in Subsequent Request
GET https://{{umamiurl}}/api/websites/{{websiteid}}/pageviews?endAt={{endAt}}&startAt={{startAt}}&unit=day
Authorization: Bearer {{auth_token}}


###
GET https://{{umamiurl}}}}/api/websites/{{websiteid}}/metrics?endAt={{endAt}}&startAt={{startAt}}&type=url
Authorization: Bearer {{auth_token}}
```

यह यहाँ वेरिएबल नामों को रखने के लिए अच्छा अभ्यास है `{{}}` एक प्रविष्टि.jann फ़ाइल जिसे आप निम्न की तरह उल्लेख कर सकते हैं.

```json
{
  "local": {
    "umamiurl":"umamilocal.mostlylucid.net",
    "username": "admin",
    "password": "<password{>",
    "websiteid" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee"
  }
}
```

## सेटअप

सबसे पहले हम बाहर विन्यस्त करने की आवश्यकता है और सेवाओं हम अनुरोध करने के लिए उपयोग करेंगे।

```csharp

public static class UmamiSetup
{
    public static void SetupUmamiServices(this IServiceCollection services, IConfiguration config)
    {
        var umamiSettings = services.ConfigurePOCO<UmamiSettings>(config.GetSection(UmamiSettings.Section));
        services.AddHttpClient<AuthService>(options =>
        {
            options.BaseAddress = new Uri(umamiSettings.BaseUrl);
            
        }) .SetHandlerLifetime(TimeSpan.FromMinutes(5))  //Set lifetime to five minutes
        .AddPolicyHandler(GetRetryPolicy());;
        services.AddScoped<UmamiService>();
        services.AddScoped<AuthService>();

    }
    static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg =>  msg.StatusCode == HttpStatusCode.ServiceUnavailable)
            .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

}
```

यहाँ पर हम विन्यास क्लास को कॉन्फ़िगर करें `UmamiSettings` और जोड़ें `AuthService` और `UmamiService` सेवाओं संग्रह के लिए. हम अस्थायी त्रुटिओं को नियंत्रण करने के लिए एक फिर से प्रयास नीति भी जोड़ते हैं.

अगले हम तैयार करने की जरूरत है `UmamiService` और `AuthService` क्लास ।

वह `AuthService` एपीआई से जे. सी.

```csharp
public class AuthService(HttpClient httpClient, UmamiSettings umamiSettings, ILogger<AuthService> logger)
{
    private string _token;
    public HttpClient HttpClient => httpClient;

    public async Task<bool> LoginAsync()
    {
        var loginData = new
        {
            username = umamiSettings.Username,
            password = umamiSettings.Password
        };

        var content = new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync("/api/auth/login", content);

        if (response.IsSuccessStatusCode)
        {
            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();


            _token = authResponse.Token;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            logger.LogInformation("Login successful");
            return true;
        }

        logger.LogError("Login failed");
        return false;
    }
}
```

यहाँ हम एक आसान तरीका है `LoginAsync` वह एक अंतिम निवेदन भेजता है `/api/auth/login` उपयोक्ता तथा पासवर्ड के साथ अंत बिन्दु. अगर निवेदन सफल है, हम में जेआईटी टोकन भंडारित करें `_token` क्षेत्र और सेट करें `Authorization` Vilililent पर हेडर.

वह `UmamiService` एपीआई में अनुरोध करने के लिए जिम्मेदार है.
प्रत्येक मुख्य तरीकों के लिए मैं परिभाषित निवेदन वस्तुओं के लिए है जो प्रत्येक अंतिम बिन्दु के लिए सभी पैरामीटर्स को स्वीकार करते हैं. यह कोड को जाँचने और बनाए रखने में आसान बनाता है.

वे सब एक अजीब पैटर्न का पालन करते हैं, तो मैं सिर्फ यहाँ उनमें से एक दिखा देंगे.

```csharp
public async Task<UmamiResult<StatsResponseModels>> GetStatsAsync(StatsRequest statsRequest)
{
    // Start building the query string
    var queryParams = new List<string>
    {
        $"start={statsRequest.StartAt}",
        $"end={statsRequest.EndAt}"
    };

    // Add optional parameters if they are not null
    if (!string.IsNullOrEmpty(statsRequest.Url)) queryParams.Add($"url={statsRequest.Url}");
    if (!string.IsNullOrEmpty(statsRequest.Referrer)) queryParams.Add($"referrer={statsRequest.Referrer}");
    if (!string.IsNullOrEmpty(statsRequest.Title)) queryParams.Add($"title={statsRequest.Title}");
    if (!string.IsNullOrEmpty(statsRequest.Query)) queryParams.Add($"query={statsRequest.Query}");
    if (!string.IsNullOrEmpty(statsRequest.Event)) queryParams.Add($"event={statsRequest.Event}");
    if (!string.IsNullOrEmpty(statsRequest.Host)) queryParams.Add($"host={statsRequest.Host}");
    if (!string.IsNullOrEmpty(statsRequest.Os)) queryParams.Add($"os={statsRequest.Os}");
    if (!string.IsNullOrEmpty(statsRequest.Browser)) queryParams.Add($"browser={statsRequest.Browser}");
    if (!string.IsNullOrEmpty(statsRequest.Device)) queryParams.Add($"device={statsRequest.Device}");
    if (!string.IsNullOrEmpty(statsRequest.Country)) queryParams.Add($"country={statsRequest.Country}");
    if (!string.IsNullOrEmpty(statsRequest.Region)) queryParams.Add($"region={statsRequest.Region}");
    if (!string.IsNullOrEmpty(statsRequest.City)) queryParams.Add($"city={statsRequest.City}");

    // Combine the query parameters into a query string
    var queryString = string.Join("&", queryParams);

    // Make the HTTP request
    var response = await authService.HttpClient.GetAsync($"/api/websites/{WebsiteId}/stats?{queryString}");

    if (response.IsSuccessStatusCode)
    {
        var content = await response.Content.ReadFromJsonAsync<StatsResponseModels>();
        return new UmamiResult<StatsResponseModels>(response.StatusCode, response.ReasonPhrase ?? "Success", content ?? new StatsResponseModels());
    }

    if (response.StatusCode == HttpStatusCode.Unauthorized)
    {
        await authService.LoginAsync();
        return await GetStatsAsync(statsRequest);
    }

    logger.LogError("Failed to get stats");
    return new UmamiResult<StatsResponseModels>(response.StatusCode, response.ReasonPhrase ?? "Failed to get stats", null);
}

```

यहाँ पर आप देख सकते हैं कि मैं अनुरोध वस्तु ले

```csharp
public class BaseRequest
{
    public long StartAt => StartAtDate.ToMilliseconds(); // Timestamp (in ms) of starting date
    public long EndAt => EndAtDate.ToMilliseconds(); // Timestamp (in ms) of end date
    public DateTime StartAtDate { get; set; }
    public DateTime EndAtDate { get; set; }
}
public class StatsRequest : BaseRequest
{
    // Optional properties
    public string? Url { get; set; } // Name of URL
    public string? Referrer { get; set; } // Name of referrer
    public string? Title { get; set; } // Name of page title
    public string? Query { get; set; } // Name of query
    public string? Event { get; set; } // Name of event
    public string? Host { get; set; } // Name of hostname
    public string? Os { get; set; } // Name of operating system
    public string? Browser { get; set; } // Name of browser
    public string? Device { get; set; } // Name of device (e.g., Mobile)
    public string? Country { get; set; } // Name of country
    public string? Region { get; set; } // Name of region/state/province
    public string? City { get; set; } // Name of city
}
```

और पैरामीटरों से क्वैरी वाक्यांश निर्माण करें. अगर निवेदन सफल हो गया है, तो हम विषयवस्तु को एक के रूप में लौटा देते हैं `UmamiResult` वस्तु. यदि निवेदन 401 स्थिति कोड के साथ असफल हो गया, हम कॉल करें `LoginAsync` विधि और निवेदन फिर कोशिश करें. यह सुनिश्चित करता है कि हम 'सेलीली' संकेत तुलनात्मक संभालता है.

## कंटेनमेंट

यह उममी एपीआई के लिए C# ग्राहक कैसे बनाने का एक सरल उदाहरण है. आप इसे उपयोग कर सकते हैं एक प्रारंभ बिंदु के रूप में और अधिक जटिल ग्राहक बनाने के लिए या अपने अनुप्रयोगों में एपीआई को एकीकृत करने के लिए।