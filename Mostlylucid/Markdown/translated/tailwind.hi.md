# साइंट सीएसएस वपीएस. neck कोर

<datetime class="hidden">2024- 26- 302: 30</datetime>

<!--category-- ASP.NET, Tailwind -->
महाजाल सीएसएस बड़ी तेज़ी से निर्माण योजनाओं के लिए एक यूटिलिटी- प्रथम सीएसएस फ्रेमवर्क है. यह एक बहुत बड़ा कस्टम-प्रयोगक है, कम-स्तर सीएसएस फ्रेमवर्क है जो आपको इमारत के सभी उपकरणों को बनाने की जरूरत है...... आप किसी भी शिकायती स्टाइल को मिटाने के लिए आप लड़ने के लिए की जरूरत है.

"Millossssssssspp की तरह के बड़े लाभ में से एक है कि बूटेज में एक 'परिंगिंगिंग' और निर्माण कदम शामिल है तो केवल इतना ही शामिल है कि आप अपनी परियोजना में उपयोग कर रहे हैं. इसका अर्थ है कि आप अपनी परियोजना में सम्पूर्ण साइल सीएसएस लाइब्रेरी शामिल कर सकते हैं और सीएसएस फ़ाइल के आकार की चिंता नहीं कर सकते.

## संस्थापन

बूट़ की तुलना में एक बड़े चित्र की तुलना में फ़्लिप है कि 'लांटा' फ़ाइल में 'लांटिंग' नहीं है. आपको इसे nK या समय का प्रयोग करना आवश्यक है (सामान्य भाग से है) [इस](https://tailwindcss.com/docs/installation)).

```bash
npm install -D tailwindcss
npx tailwindcss init
```

यह टेक्स हवा सीएसएस को स्थापित करेगा तथा एक तैयार करेगा [`tailwind.config.js` ](#tailwindconfigjs) आपकी परियोजना की रूट में. यह फ़ाइल फ़्लिप सीएसएस को कॉन्फ़िगर करने के लिए उपयोग में लिया जाएगा.

### पैकेज.jin

यदि आप देख रहे हैं [इस परियोजना का स्रोत](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid) तुम मुझे एक है कि देखेंगे `package.json` फ़ाइल जिसमें निम्नलिखित का स्क्रिप्ट शामिल है तथा 'डेविडिप्रस' परिभाषा शामिल है:

```json
{
  "scripts": {
    "dev": "npm-run-all --parallel dev:*",
    "dev:js": "webpack",
    "dev:tw": "npx tailwindcss -i ./src/css/main.css -o ./wwwroot/css/dist/main.css",
    "watch": "npm-run-all --parallel watch:*",
    "watch:js": "webpack --watch --env development",
    "watch:tw": "npx tailwindcss -i ./src/css/main.css -o ./wwwroot/css/dist/main.css --watch",
    "build": "npm-run-all --parallel build:*",
    "build:js": "webpack --env production",
    "build:tw": "npx tailwindcss -i ./src/css/main.css -o ./wwwroot/css/dist/main.css --minify"
  },
  "devDependencies": {
    "@tailwindcss/aspect-ratio": "^0.4.2",
    "@tailwindcss/forms": "^0.5.7",
    "@tailwindcss/typography": "^0.5.12",
    "@types/alpinejs": "^3.13.10",
    "autoprefixer": "^10.4.19",
    "cssnano": "^7.0.4",
    "daisyui": "^4.12.10",
    "npm-run-all": "^4.1.5",
    "tailwindcss": "^3.4.3",
    "ts-loader": "^9.5.1",
    "typescript": "^5.4.5",
    "webpack": "^5.91.0",
    "webpack-cli": "^5.1.4"
  }
}
```

ये दस्तावेज़ हैं कि मैं साइंट सीएसएस फ़ाइल बनाने के लिए उपयोग में आते हैं. वह `dev` स्क्रिप्ट यह है कि मैं विकास के लिए सीएसएस फ़ाइल बनाने के लिए उपयोग में आता हूँ. वह `watch` स्क्रिप्ट वह है जिसे मैं परिवर्तन के लिए सीएसएस फ़ाइल को देखने के लिए उपयोग में आता हूं. वह `build` स्क्रिप्ट एक है जिसे मैं उत्पादन के लिए सीएसएस फ़ाइल बनाने के लिए उपयोग में आता है.

Gudddsseps खंड आपके uget पैकेज के रूप में हैं. Nint परियोजनाओं के लिए. वे उन पैकेजों के हैं जो सीएसएस फ़ाइल बनाने के लिए प्रयोग में लिए गए हैं.

### परीक्षा.config.japan. kgm

ये लोग भी उनके साथ दौड़े चले जा रहे हैं `tailwind.config.js` फ़ाइल जो परियोजना के रूट में है. यह फ़ाइल फ़्लिप सीएसएस को कॉन्फ़िगर करने के लिए उपयोग में लिया जाएगा. यहाँ है `tailwind.config.js` फ़ाइल जो मैं इस्तेमाल करता हूं:

```javascript
// tailwind.config.js

const defaultTheme = require("tailwindcss/defaultTheme");

module.exports = {
    content:   [
        './Pages/**/*.{html,cshtml}',
        './Components/**/*.{html,cshtml}',
        './Views/**/*.{html,cshtml}',
    ],
    safelist: ["dark"],
    darkMode: "class",
    theme: {

        },
    },
    plugins: [
        require("@tailwindcss/typography")({
            modifiers: [],
        }),
        require("@tailwindcss/forms"),
        require("@tailwindcss/aspect-ratio"),
        require('daisyui'),
    ]
};
```

यह फ़ाइल फ़्लिप सीएसएस को कॉन्फ़िगर करने के लिए उपयोग में लिया जाएगा. वह `content` खंड का उपयोग करके सीएसएस क्लासों को बताने के लिए किया जाता है कि आप अपनी परियोजना में उपयोग कर रहे हैं. इस कोरल में आम तौर पर शामिल किया जाएगा `Pages`, `Components`, और `Views` फ़ोल्डर आप यह भी नोट करेंगे कि किस तरह केएमएल फ़ाइलें हैं.
एक 'ल हवा के लिए खींच लिया' है कि आप शामिल करने के लिए कोईoe हो सकता है ` <div class="hidden></div> ` समूह जो आप शामिल करने के लिए उपयोग में सभी आवश्यक सीस क्लासों को शामिल करें 'इस' में आप अपने मार्कअप में नहीं है (जैसे, जोड़े गए कोड का उपयोग करते हैं).

वह `safelist` खंड का उपयोग सीएसएस फ़ाइल में शामिल होने के लिए फ़ाइलों का उपयोग किया जाता है. वह `darkMode` खंड का उपयोग अँधेरे मोड क्लासों का उपयोग करने के लिए सीएसएस को बताने के लिए उपयोग में लिया जाता है. वह `theme` फ़ाइल का आकार बढ़ाने के लिए उपयोग में लिया जाता है. वह `plugins` विभाग का प्रयोग कर रहा है प्लगइन जिसे आप अपनी परियोजना में प्रयोग कर रहे हैं. तब यह फ़ाइल सीएसएस फ़ाइल को उपयोग में लेने के लिए उपयोग में लिया जाता है:

"स्टिंग: Sttw: "npssssscss - asss/ assss. scs - as. cass / sys/ rets/ asssss. Cass: s - mass - mphs - "mps"

### सीप-प

इस का अंतिम भाग ही सीएसप्रोजे फ़ाइल में है. इसमें समाप्ति से पहले एक खंड को सही तरह से शामिल करना शामिल है  `<Project> ` टैग:

```xml

    <Target Name="BuildCss" BeforeTargets="Compile">
        <Exec Command="npm run dev" Condition=" '$(Configuration)' == 'Debug' " />
        <Exec Command="npm run build" Condition=" '$(Configuration)' == 'Release' " EnvironmentVariables="NODE_ENV=production" />
    </Target>

```

जो आप देख सकते हैं कि किस तरह के स्क्रिप्ट का मतलब है प्रत्येक परियोजना पर सीएसएस को फिर से निर्माण करने के लिए.