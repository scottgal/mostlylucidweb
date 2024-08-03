# Core CSS & ASP.NET del vento di coda

<datetime class="hidden">2024-07-30T13:30</datetime>
Tailwind CSS è un framework CSS utility-first per la rapida costruzione di disegni personalizzati. Si tratta di un framework CSS altamente personalizzabile e di basso livello che ti dà tutti i blocchi di costruzione che devi costruire su misura senza alcun fastidioso stile di opinione che devi combattere per sovrascrivere.

Uno dei grandi vantaggi di Tailwind su 'tradizionale' framework CSS come Bootstrap è che Tailwind include una'scanning' e la costruzione passo in modo da include solo il CSS che si effettivamente utilizzare nel vostro progetto. Questo significa che puoi includere l'intera libreria CSS Tailwind nel tuo progetto e non preoccuparti delle dimensioni del file CSS.

## Installazione

Un grosso svantaggio rispetto a Bootstrap è che Tailwind non è un file CSS 'drop in'. È necessario installarlo utilizzando npm o filato (la sezione successiva è da [Questo](https://tailwindcss.com/docs/installation)).

```bash
npm install -D tailwindcss
npx tailwindcss init
```

Questo installerà Tailwind CSS e creerà un [`tailwind.config.js` ](#tailwindconfigjs) file nella radice del tuo progetto. Questo file è usato per configurare Tailwind CSS.

### Package.json

Se si guarda il [fonte di questo progetto](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid) Vedrai che ho un... `package.json` file che include le seguenti definizioni di'script' e 'dependences':

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

Questi sono gli'script' che uso per costruire il file CSS Tailwind. La `dev` script è quello che uso per costruire il file CSS per lo sviluppo. La `watch` script è quello che uso per guardare il file CSS per le modifiche e ricostruirlo. La `build` script è quello che uso per costruire il file CSS per la produzione.

La sezione devDependences è come i tuoi pacchetti Nuget per i tuoi progetti.NET. Sono i pacchetti che vengono utilizzati per costruire il file CSS.

### Coilwind.config.js

Questi sono utilizzati insieme con il `tailwind.config.js` file che è nella radice del progetto. Questo file è usato per configurare Tailwind CSS. Qui c'è il `tailwind.config.js` file che uso:

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

Questo file è usato per configurare Tailwind CSS. La `content` la sezione è usata per dire a Tailwind CSS dove cercare le classi CSS che state usando nel vostro progetto. In ASP.NET Core questo includerà generalmente il `Pages`, `Components`, e `Views` cartelle. Noterete che anche i file 'cshtml' sono in lattina.
Un 'gotcha' per vento di coda è che si può nooe per includere ` <div class="hidden></div> ` sezioni per assicurarti di includere tutte le classi css richieste nella 'build' che non hai nel tuo markup (ad esempio, aggiunto usando il codice).

La `safelist` la sezione è usata per dire a Tailwind CSS quali classi includere nel file CSS. La `darkMode` la sezione è usata per dire a Tailwind CSS di usare le classi di modalità scura. La `theme` la sezione è usata per configurare il tema di Tailwind CSS. La `plugins` sezione è utilizzata per includere i plugin che si sta utilizzando nel vostro progetto. Questo viene poi utilizzato da Tailwind per compilare il file CSS come seppified in:

"build:tw": "npx tailwindcss -i./src/css/main.css -o./wwwroot/css/dist/main.css --minify"

### CSPROJ

La parte finale di questo è nel file CSProj stesso. Questo include una sezione a destra prima della chiusura  `<Project> ` etichetta:

```xml

    <Target Name="BuildCss" BeforeTargets="Compile">
        <Exec Command="npm run dev" Condition=" '$(Configuration)' == 'Debug' " />
        <Exec Command="npm run build" Condition=" '$(Configuration)' == 'Release' " EnvironmentVariables="NODE_ENV=production" />
    </Target>

```

Che come potete vedere si riferisce allo script build per ricostruire il CSS su ogni progetto build.