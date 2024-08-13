# SSS & ASP.net

<datetime class="hidden">2024-07-30TT 13:30</datetime>

<!--category-- ASP.NET, Tailwind -->
ويعد نظام " تاليند " (CSS) إطاراً مفيداً أولاً لنظام " سي إس إس " لتصاميم التصميمات الخاصة بالبناء السريع. إنه إطار منخفض المستوى ومكيف جداً والذي يعطيكم كل لبنات البناء التي تحتاجونها لبناء تصاميم مسمّية بدون أي أساليب مثيرة للإزعاج يجب أن تحاربوا من أجل تجاوزها.

واحدة من الفوائد الكبيرة للتايلود على أطر CSS "تقليدية" مثل Botstrap هي أن Tayilwind تتضمن "المسح" وخطوة البناء لذلك فقط تشمل CSS التي تستخدمها فعلا في مشروعك. هذا يعني أنه يمكنك إدراج مكتبة Tailwind CSS بأكملها في مشروعك وعدم القلق حول حجم ملف CSS.

## عدد أفراد

أحد العيوب الكبيرة مقارنة بـ Botstrap هو أن Tayilwind ليس 'إسقاط في' ملف CSS. تحتاج إلى تثبيته باستخدام npm أو anr (القسم الفرعي هو من [هذا](https://tailwindcss.com/docs/installation)).

```bash
npm install -D tailwindcss
npx tailwindcss init
```

هذا سيثبّت تثبيت Tayilwind CSS و إنشاء a [`tailwind.config.js` ](#tailwindconfigjs) في ملفّ جذر مشروعك. هذا ملفّ هو مُستخدَم إلى مُعَدّل tailwind CSS.

### مجموعة عناصر.

إذا نظرت إلى [من هذا المشروع](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid) سترين أن لدي `package.json` ملفّ يحتوي التالي "tex" و "devDevDependaties" تعريفات:

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

هذه هي "المخطوطات" التي أستخدمها لبناء ملف CSS ذيل ويند. الـ `dev` السيناريو هو الذي أستخدمه لبناء ملف CSS من أجل التطوير. الـ `watch` هو النص الذي استخدمه لمشاهدة CSS ملف التغييرات وإعادة بنائها. الـ `build` هو الذي استخدمه لبناء ملف CSS للانتاج.

قسم devDependations هو مثل حزمة nuget لـ.Net مشروعات. هي الحزم التي استخدمت لبناء ملف CSS.

### تايل ويند.config.js

هذه تستخدم جنباً إلى جنب مع `tailwind.config.js` ملفّ هو بوصة جذر من مشروع. هذا ملفّ هو مُستخدَم إلى مُعَدّل tailwind CSS. هذا هو `tailwind.config.js` أستخدَم ملفّاً:

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

هذا ملفّ هو مُستخدَم إلى مُعَدّل tailwind CSS. الـ `content` القسم مُستخدَم لإخبار Tayilwind CSS أين تبحث عن فصول CSS التي تستخدمها في مشروعك. SPSPSP.net as سوف يشمل هذا بوجه عام ما يلي: `Pages`, `Components`، و ، ، ، ، ، ، ، ، ، ، ، ، ، ، `Views` المجلدات. ستلاحظون هذا أيضاً ملفات Csshtml.
واحد 'يجعلك' لذيل الرياح هو أنك قد لا ترغب في إدراج ` <div class="hidden></div> ` للتأكد من أنك تشمل جميع فئات csss المطلوبة في 'البناء' التي لا يكون لديك في العلام الخاص بك (على سبيل المثال، إضافة استخدام الرمز).

الـ `safelist` القسم مُستخدَم إلى إخبار Taylewind CSS أيّ الفئات إلى تضمين بوصة CSS ملفّ. الـ `darkMode` يُستخدم القسم لأخبار Tailwind CSS باستخدام فئات النمط المظلم. الـ `theme` يُستخدَم جزء إلى إعداد موضوع من tyilwind CSS. الـ `plugins` القسم مُستخدَم لتضمين الملحقات التي تستخدمها في مشروعك. ثم يستخدم هذا من قبل Tayilwind لتجميع ملف CSS كما تم تثبيطه في:

"البنية: tw": "npx tealwindcss-i../src/css/main.css-o./wwwunt/csss/dist/main.cs-minife"

### كل سنة

الجزء الأخير من هذا هو في ملف CSProj نفسه. وهذا يشمل قسماً قبل الإقفال مباشرة.  `<Project> ` :::::::

```xml

    <Target Name="BuildCss" BeforeTargets="Compile">
        <Exec Command="npm run dev" Condition=" '$(Configuration)' == 'Debug' " />
        <Exec Command="npm run build" Condition=" '$(Configuration)' == 'Release' " EnvironmentVariables="NODE_ENV=production" />
    </Target>

```

والتي كما ترون تشير إلى نص البناء لإعادة بناء CSS على كل مشروع بناء.