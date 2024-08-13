# अपने एक किक बनाना.NECT वेबसाइट एक PWA

<!--category-- ASP.NET -->
<datetime class="hidden">2024- 0. 3101T11:36</datetime>

इस लेख में, मैं आपको दिखाएगा कि आपका अप्रचलित कैसे हो सकता है.

## पूर्वपाराईज़

यह वास्तव में बहुत सरल देखने के लिए है http://shatt.com/thsssshtsss. Aptntac.mer/ bashys.

## कनेक्शन (n)

नुरू पैकेज संस्थापित करें

```bash
dotnet add package WebEssentials.AspNetCore.PWA
```

अपने कार्यक्रम में.cs जोड़ें:

```csharp
builder.Services.AddProgressiveWebApp();
```

फिर कुछ favix बनाएँ जो नीचे के आकार से मेल खाते हैं [यहाँ](https://realfavicongenerator.net/) एक औज़ार है जिसे आप उन्हें बनाने के लिए इस्तेमाल कर सकते हैं. ये वास्तव में किसी भी प्रतीक हो सकते हैं (मैंएकमोजी का इस्तेमाल किया)

Save these in your wwrroot folder as android-chrome-192x192.png and android-chrome-512x512.png (in the example below)

तो फिर आप एक स्पष्ट.jon की जरूरत है

```json
{
  "name": "mostlylucid",
  "short_name": "mostlylucid",
  "description": "The web site for mostlylucid limited",
  "icons": [
    {
      "src": "/android-chrome-192x192.png",
      "sizes": "192x192"
    },
    {
      "src": "/android-chrome-512x512.png",
      "sizes": "512x512"
    }
  ],
  "display": "standalone",
  "start_url": "/"
}
```