# साइंट सीएसएस वपीएस. neck कोर

<datetime class="hidden">2024- 26- 302: 30</datetime>
Rilloppps सीएसएस एक उपयोगिता है तेज निर्माण व्यवस्था व्यवस्था व्यवस्था व्यवस्था निर्माण के लिए. यह एक बहुत ही कस्टम प्रबंधक है कि एक बहुत कम कस्टम एजेंट है, कम-प्रयोगी सीएसएस एजेंट है जो आपको इमारत के सभी उपकरणों के निर्माण के लिए आप की जरूरत है बिना किसी भी अत्याचार की राय के आप को रद्द करने के लिए की जरूरत है.

"plillssssspp की तरह के बड़े लाभ में से एक है कि बूटेज में एक 'सिंगिंगिंग' और निर्माण कदम शामिल है तो सिर्फ आप वास्तव में अपनी परियोजना में उपयोग किया जा सकता है। इसका मतलब है कि आप शामिल कर सकते हैं अपने प्रोजेक्ट में पूरी हवा सीएसएस सीएसएस निर्माण निर्माण पुस्तकालय में शामिल कर सकते हैं और सीएसएस फ़ाइल के आकार के बारे में चिंता नहीं है।

## संस्थापन

बूट्सपी की तुलना में एक बड़े चित्र की तुलना है कि Tillo' सीएसएस फ़ाइल में नहीं है. आपको इसे nm या समय का उपयोग करने की जरूरत है (प्रयोगात्मक खंड से है)[इस](https://tailwindcss.com/docs/installation)).

```bash
npm install -D tailwindcss
npx tailwindcss init
```

यह टेक्स हवा सीएसएस को स्थापित करेगा तथा एक तैयार करेगा[`tailwind.config.js` ](#tailwindconfigjs)आपकी परियोजना की रूट फ़ाइल में. यह फ़ाइल newipp सीएसएस को कॉन्फ़िगर करने के लिए उपयोग में लिया जाता है.

### पैकेज.jin

यदि आप देख रहे हैं[इस परियोजना का स्रोत](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid)तुम मुझे एक है कि देखेंगे`package.json`फ़ाइल जिसमें निम्नलिखित का स्क्रिप्ट शामिल है तथा 'डेविडिप्रस' परिभाषा शामिल है:

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

ये दस्तावेज़ हैं कि मैं साइंट सीएसएस फ़ाइल बनाने के लिए उपयोग में आते हैं.`dev`स्क्रिप्ट यह है कि मैं विकास के लिए सीएसएस फ़ाइल बनाने के लिए उपयोग में आता हूं.`watch`स्क्रिप्ट एक है जिसे मैं परिवर्तन के लिए सीएसएस फ़ाइल को देखने के लिए उपयोग में आता हूँ.`build`स्क्रिप्ट एक है जिसे मैं उत्पादन के लिए सीएसएस फ़ाइल बनाने के लिए उपयोग में आता है.

Gudjes खंड आपके suget पैकेज के लिए की तरह हैं. वे उन पैकेज हैं जो सीएसएस फ़ाइल बनाने के लिए इस्तेमाल किया जाता है.

### परीक्षा.config.japan. kgm

ये लोग भी उनके साथ दौड़े चले जा रहे हैं`tailwind.config.js`फ़ाइल जो परियोजना के रूट में है. यह फ़ाइल अत्यंत कठिन सीएसएस को कॉन्फ़िगर करने के लिए उपयोग में लिया जाएगा. यहाँ पर है`tailwind.config.js`फ़ाइल जो मैं इस्तेमाल करता हूं:

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

यह फ़ाइल फ़्लिप सीएसएस को कॉन्फ़िगर करने के लिए उपयोग में लिया जाएगा.`content`भाग का उपयोग करके सीएसएस क्लासों को बताने के लिए किया जाता है कि आप अपनी परियोजना में उपयोग कर रहे हैं.`Pages`, `Components`, और`Views`फ़ोल्डर. आप यह भी नोट करेंगे कि यह किस चीज़ के लिए उपयोग में लिया गया है.
एक 'ल हवा के लिए खींच लिया' है कि आप शामिल करने के लिए कोईoe हो सकता है` <div class="hidden></div> `समूह जो आप शामिल करने के लिए उपयोग में सभी आवश्यक सीस क्लासों को शामिल करें 'इस' में आप अपने मार्कअप में नहीं है (जैसे, जोड़े गए कोड का उपयोग करते हैं).

वह`safelist`खंड का उपयोग सीएसएस फ़ाइल में शामिल होने के लिए फ़ाइलों का उपयोग किया जाता है.`darkMode`खंड का उपयोग अंधेरे मोड क्लासों का उपयोग करने के लिए सीएसएस को बताने के लिए उपयोग में लिया जाता है.`theme`फ़ाइल का आकार बढ़ाने के लिए इस्तेमाल में लिया जाता है.`plugins`खंड का उपयोग आपकी परियोजना में उपयोग कर रहे हैं. यह तब उपयोग में आता है जैसे कि सीएसएस फ़ाइल को कम्पाइल करने में:

"स्टिंग: Sttw: "npssssscss - asss/ assss. scs - as. cass / sys/ rets/ asssss. Cass: s - mass - mphs - "mps"

### सीप-प

इस का अंतिम भाग सSPON फ़ाइल में है. इसमें बन्द करने के लिए खण्ड को दाएँ दिए जाने से पहले शामिल है`<Project> `टैग:

```xml

    <Target Name="BuildCss" BeforeTargets="Compile">
        <Exec Command="npm run dev" Condition=" '$(Configuration)' == 'Debug' " />
        <Exec Command="npm run build" Condition=" '$(Configuration)' == 'Release' " EnvironmentVariables="NODE_ENV=production" />
    </Target>

```

जो आप देख सकते हैं कि किस तरह के स्क्रिप्ट का मतलब है प्रत्येक परियोजना पर सीएसएस को फिर से निर्माण करने के लिए.