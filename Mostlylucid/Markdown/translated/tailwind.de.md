# Endwind CSS & ASP.NET Core

<datetime class="hidden">2024-07-30T13:30</datetime>

<!--category-- ASP.NET, Tailwind -->
Tailwind CSS ist ein Utility-First CSS-Framework zum schnellen Erstellen von benutzerdefinierten Designs. Es ist ein sehr anpassbares, Low-Level CSS-Framework, das Ihnen alle Bausteine gibt, die Sie brauchen, um maßgeschneiderte Designs ohne lästige opinionierte Stile zu bauen, die Sie kämpfen müssen, um zu überschreiben.

Einer der großen Vorteile von Tailwind über 'traditionelle' CSS-Frameworks wie Bootstrap ist, dass Tailwind einen 'Scanning' und einen Bauschritt enthält, also nur den CSS, den Sie tatsächlich in Ihrem Projekt verwenden. Das bedeutet, dass Sie die gesamte Tailwind CSS-Bibliothek in Ihr Projekt integrieren können und sich keine Sorgen über die Größe der CSS-Datei machen.

## Installation

Ein großer Nachteil im Vergleich zu Bootstrap ist, dass Tailwind keine 'drop in' CSS-Datei ist. Sie müssen es mit npm oder Garn installieren (nachfolgender Abschnitt ist von [diese](https://tailwindcss.com/docs/installation)).

```bash
npm install -D tailwindcss
npx tailwindcss init
```

Dies wird Tailwind CSS installieren und eine [`tailwind.config.js` ](#tailwindconfigjs) filein der Wurzel Ihres Projekts. Diese Datei wird verwendet, um Tailwind CSS zu konfigurieren.

### Paket.json

Wenn Sie sich die [Quelle dieses Projekts](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid) Sie werden sehen, dass ich eine `package.json` Datei, die die folgenden Definitionen'script' und 'devDependencies' enthält:

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

Dies sind die 'Skripte' die ich zum Erstellen der Tailwind CSS-Datei verwende. Das `dev` script ist das, mit dem ich die CSS-Datei für die Entwicklung baue. Das `watch` Skript ist das, das ich benutze, um die CSS-Datei auf Änderungen zu sehen und sie neu aufzubauen. Das `build` script ist das, das ich benutze, um die CSS-Datei für die Produktion zu erstellen.

Der Abschnitt devDependencies ist wie Ihre Nuget-Pakete für Ihre.NET-Projekte. Sie sind die Pakete, die verwendet werden, um die CSS-Datei zu bauen.

### Endwind.config.js

Diese werden zusammen mit der `tailwind.config.js` Datei, die in der Wurzel des Projekts ist. Diese Datei wird verwendet, um Tailwind CSS zu konfigurieren. Hier ist die `tailwind.config.js` Datei, die ich benutze:

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

Diese Datei wird verwendet, um Tailwind CSS zu konfigurieren. Das `content` Abschnitt wird verwendet, um Tailwind CSS zu sagen, wo Sie nach den CSS-Klassen suchen, die Sie in Ihrem Projekt verwenden. In ASP.NET Core wird dies in der Regel die `Pages`, `Components`, und `Views` Ordner. Sie werden auch diese Dosen 'cshtml' Dateien beachten.
Ein "gotcha" für Rückenwind ist, dass Sie nicht mitmachen können ` <div class="hidden></div> ` Abschnitte, um sicherzustellen, dass Sie alle erforderlichen css-Klassen in das 'build' aufnehmen, das Sie nicht in Ihrem Markup haben (z.B. mit Code hinzugefügt).

Das `safelist` Abschnitt wird verwendet, um Tailwind CSS zu sagen, welche Klassen in die CSS-Datei aufgenommen werden sollen. Das `darkMode` Abschnitt wird verwendet, um Tailwind CSS zu sagen, um die Dunkelmodus-Klassen zu verwenden. Das `theme` Abschnitt wird verwendet, um das Thema von Tailwind CSS zu konfigurieren. Das `plugins` Abschnitt wird verwendet, um die Plugins, die Sie in Ihrem Projekt verwenden. Dies wird dann von Tailwind verwendet, um die CSS-Datei als sepcified in zu kompilieren:

"build:tw": "npx tailwindcss -i./src/css/main.css -o./wwwroot/css/dist/main.css --minify"

### CSPROJJ

Der letzte Teil davon ist in der CSProj-Datei selbst. Dies beinhaltet einen Abschnitt direkt vor dem Abschluss  `<Project> ` Tag:

```xml

    <Target Name="BuildCss" BeforeTargets="Compile">
        <Exec Command="npm run dev" Condition=" '$(Configuration)' == 'Debug' " />
        <Exec Command="npm run build" Condition=" '$(Configuration)' == 'Release' " EnvironmentVariables="NODE_ENV=production" />
    </Target>

```

Was, wie Sie sehen können, auf das Build-Skript zum Wiederaufbau des CSS auf jedem Projekt Build bezieht.