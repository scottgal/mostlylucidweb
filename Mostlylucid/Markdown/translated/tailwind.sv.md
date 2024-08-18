# Medvind CSS & ASP.NET Core

<datetime class="hidden">2024-07-30T13:30 Ordförande</datetime>

<!--category-- ASP.NET, Tailwind -->
Tailwind CSS är ett verktygs-första CSS ramverk för att snabbt bygga anpassade konstruktioner. Det är en mycket anpassningsbar, låg nivå CSS ram som ger dig alla de byggstenar du behöver för att bygga skräddarsydda mönster utan några irriterande åsikter stilar du måste kämpa för att åsidosätta.

En av de stora fördelarna med Tailwind över "traditionella" CSS ramar som Bootstrap är att Tailwind innehåller en "scanning" och byggsteg så endast inkluderar CSS du faktiskt använder i ditt projekt. Detta innebär att du kan inkludera hela Tailwind CSS-biblioteket i ditt projekt och inte oroa dig för storleken på CSS-filen.

## Anläggning

En stor nackdel jämfört med Bootstrap är att Tailwind inte är en "släpp i" CSS-fil. Du måste installera den med hjälp av npm eller garn (efterföljande avsnitt är från [detta](https://tailwindcss.com/docs/installation)).

```bash
npm install -D tailwindcss
npx tailwindcss init
```

Detta kommer att installera Tailwind CSS och skapa en [`tailwind.config.js` ](#tailwindconfigjs) Arkivera roten till ditt projekt. Den här filen används för att konfigurera Tailwind CSS.

### Paket.Json

Om du tittar på [Projektets källa](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid) Du ska få se att jag har en `package.json` Fil som innehåller följande definitioner av "skript" och "devDependencies":

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

Dessa är de "skript" som jag använder för att bygga Tailwind CSS-filen. I detta sammanhang är det viktigt att se till att `dev` skriptet är det jag använder för att bygga CSS-filen för utveckling. I detta sammanhang är det viktigt att se till att `watch` skriptet är det som jag använder för att titta på CSS-filen för ändringar och återuppbygga den. I detta sammanhang är det viktigt att se till att `build` skriptet är det som jag använder för att bygga CSS-filen för produktion.

DevDependencies-sektionen är som era nuget-paket för era.NET-projekt. De är paketen som används för att bygga CSS-filen.

### Tailwind.config.js

Dessa används tillsammans med `tailwind.config.js` fil som är roten till projektet. Den här filen används för att konfigurera Tailwind CSS. Här är `tailwind.config.js` fil som jag använder:

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

Den här filen används för att konfigurera Tailwind CSS. I detta sammanhang är det viktigt att se till att `content` avsnittet används för att berätta Tailwind CSS var du ska leta efter CSS klasser som du använder i ditt projekt. I ASP.NET Core kommer detta i allmänhet att omfatta `Pages`, `Components`, och `Views` mappar. Du kommer att notera detta också burkar 'cshtml'-filer.
En "gotcha" för medvind är att du kanske nooe att inkludera ` <div class="hidden></div> ` sektioner för att säkerställa att du inkluderar alla nödvändiga css-klasser i den "bygga" som du inte har i din markering (t.ex., läggs till med kod).

I detta sammanhang är det viktigt att se till att `safelist` Avsnittet används för att tala om för Tailwind CSS vilka klasser som ska ingå i CSS-filen. I detta sammanhang är det viktigt att se till att `darkMode` avsnittet används för att tala om för Tailwind CSS att använda den mörka läge klasser. I detta sammanhang är det viktigt att se till att `theme` sektionen används för att konfigurera temat Tailwind CSS. I detta sammanhang är det viktigt att se till att `plugins` Avsnittet används för att inkludera de plugins som du använder i ditt projekt. Detta används sedan av Tailwind för att kompilera CSS-filen som sepcified i:

"Build:tw": "npx medvindcss -i./src/css/main.css -o./wwwroot/css/dist/main.css --minify"

### KSPROJ

Den sista delen av detta finns i själva CSProj-filen. Detta inkluderar ett avsnitt precis före stängningen  `<Project> ` tagg:

```xml

    <Target Name="BuildCss" BeforeTargets="Compile">
        <Exec Command="npm run dev" Condition=" '$(Configuration)' == 'Debug' " />
        <Exec Command="npm run build" Condition=" '$(Configuration)' == 'Release' " EnvironmentVariables="NODE_ENV=production" />
    </Target>

```

Som du kan se hänvisar till byggskriptet för att återuppbygga CSS på varje projekt bygga.