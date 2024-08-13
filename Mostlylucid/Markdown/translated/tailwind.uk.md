# Ядро Tailwind CSS & ASP. NET

<datetime class="hidden">2024- 07- 30T13: 30</datetime>

<!--category-- ASP.NET, Tailwind -->
CSS- tailwind - це утиліта-перша оболонка CSS для швидкого побудови нетипових дизайнів. Це дуже гнучка, низькорівнева система CSS, яка надає вам всі будівельні блоки, які потрібні для побудови конструювання дизайнів без будь-яких набридливих стилів, які ви повинні боротися, щоб контролювати.

Однією з великих переваг Tailwindin над "традиційних" CSS оболонки, як Bootspiet є те, що Tailwin включає'scanning' і будівництво крок так що включає тільки CSS, який ви насправді використовуєте у вашому проекті. Це означає, що ви можете включити всю бібліотеку CSS Tailwind у ваш проект і не перейматися розміром файла CSS.

## Встановлення

Одним великим недоліком у порівнянні з Притопом є те, що Tailwindin не є "скидання в" файл CSS. Вам слід встановити його за допомогою npm або pid (відносний розділ можна отримати за [цей](https://tailwindcss.com/docs/installation)).

```bash
npm install -D tailwindcss
npx tailwindcss init
```

Це встановить CSS Tailwind і створить [`tailwind.config.js` ](#tailwindconfigjs) file in the root of your project. Цей файл використовується для налаштування CSS Tailwind.

### Package. json

Якщо ви подивитесь на [джерело цього проекту](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid) ти побачиш, що в мене є `package.json` файл, який містить такі " скрипти " і " devDependance " визначення:

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

Це "написи," які я використовую для побудови CSS Tailwin. The `dev` скрипт - це той, який я використовую для збирання файла CSS для розробки. The `watch` Скрипт - це той, який я використовую для перегляду файла CSS для змін і перебудування його. The `build` скрипт - це той, який я використовую для збирання файла CSS для виробництва.

Розділ devDecendance є схожим на ваші пакунки nuget для ваших проектів.NET. Це пакунки, які використовуються для збирання файла CSS.

### Tailwind.config.js

Вони використовуються разом з `tailwind.config.js` файл, який знаходиться у кореневій частині проекту. Цей файл використовується для налаштування CSS Tailwind. Ось, будь ласка. `tailwind.config.js` файл, який я використовую:

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

Цей файл використовується для налаштування CSS Tailwind. The `content` розділ використовується для позначення CSS Tailwind, де шукати класи CSS, які ви використовуєте у вашому проекті. У ядрі ASP.NET це, загалом, включатиме `Pages`, `Components`, і `Views` теки. Ви також можете зауважити цей файл cans 'cshtml' cshtml.
Один "гость" за попутний вітер це те, що ви, можливо, носої, щоб включити ` <div class="hidden></div> ` Розділи, призначені для того, щоб переконатися, що ви включили всі потрібні класи css у " build," яких у вашій розмітці немає (наприклад, додані за допомогою коду).

The `safelist` section використовується для визначення CSS Tailwind, які класи слід включити до файла CSS. The `darkMode` розділ використовується для того, щоб наказати CSS Tailwind використовувати класи темного режиму. The `theme` розділ використовується для налаштування теми CSS Tailwin. The `plugins` розділ використовується для включення додатків, якими ви користуєтеся у вашому проекті. Це потім використовується Tailwind для компіляції файла CSS як specified у:

" build: tw ": " npx whiftcs - i./ src/ css/ main. css - o./ wwwroot/css/ dist/ main. css -- minify "

### CSPROJ

Остання частина - це файл CSProj. Це включає розділ безпосередньо перед закриттям  `<Project> ` tag:

```xml

    <Target Name="BuildCss" BeforeTargets="Compile">
        <Exec Command="npm run dev" Condition=" '$(Configuration)' == 'Debug' " />
        <Exec Command="npm run build" Condition=" '$(Configuration)' == 'Release' " EnvironmentVariables="NODE_ENV=production" />
    </Target>

```

Який, як ви можете бачити, стосується скрипту збирання для відновлення CSS під час кожного збирання проекту.