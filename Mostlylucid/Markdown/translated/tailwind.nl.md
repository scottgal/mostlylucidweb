# CSS & ASP.NET-kern achterwind

<datetime class="hidden">2024-07-30T13:30</datetime>

<!--category-- ASP.NET, Tailwind -->
Tailwind CSS is een utility-first CSS framework voor het snel bouwen van aangepaste ontwerpen. Het is een zeer aanpasbare, low-level CSS framework dat geeft u alle van de bouwstenen die u nodig hebt om op maat ontwerpen te bouwen zonder enige vervelende eigenzinnige stijlen die u moet vechten om override.

Een van de grote voordelen van Tailwind over 'traditionele' CSS-kaders zoals Bootstrap is dat Tailwind een'scanning' en bouwstap bevat dus alleen de CSS die je daadwerkelijk gebruikt in je project. Dit betekent dat je de hele Tailwind CSS bibliotheek in je project kunt opnemen en je geen zorgen kunt maken over de grootte van het CSS bestand.

## Installatie

Een groot nadeel in vergelijking met Bootstrap is dat Tailwind geen 'drop in' CSS-bestand is. U moet het installeren met behulp van npm of garen (volgende sectie is van [dit](https://tailwindcss.com/docs/installation)).

```bash
npm install -D tailwindcss
npx tailwindcss init
```

Dit zal installeren Tailwind CSS en een [`tailwind.config.js` ](#tailwindconfigjs) filein de root van uw project. Dit bestand wordt gebruikt om Tailwind CSS in te stellen.

### Pakket.json

Als je kijkt naar de [bron van dit project](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid) Je zult zien dat ik een `package.json` bestand met de volgende definities van'script' en 'devDependentcies':

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

Dit zijn de'scripts' die ik gebruik om het Tailwind CSS-bestand te bouwen. De `dev` script is degene die ik gebruik om het CSS-bestand te bouwen voor ontwikkeling. De `watch` script is degene die ik gebruik om het CSS-bestand te bekijken voor wijzigingen en het opnieuw op te bouwen. De `build` script is degene die ik gebruik om het CSS-bestand te bouwen voor productie.

De devDependencies sectie zijn als je nuget pakketten voor je.NET projecten. Zij zijn de pakketten die worden gebruikt om het CSS-bestand te bouwen.

### Achterwind.config.js

Deze worden samen met de `tailwind.config.js` bestand dat in de root van het project staat. Dit bestand wordt gebruikt om Tailwind CSS in te stellen. Hier is de `tailwind.config.js` bestand dat ik gebruik:

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

Dit bestand wordt gebruikt om Tailwind CSS in te stellen. De `content` sectie wordt gebruikt om Tailwind CSS te vertellen waar te zoeken naar de CSS klassen die u gebruikt in uw project. In ASP.NET Core zal dit over het algemeen de `Pages`, `Components`, en `Views` Mappen. U zult merken dat dit ook 'cshtml' bestanden kan.
Een 'gotcha' voor de achterwind is dat je niet hoeft mee te nemen ` <div class="hidden></div> ` secties om ervoor te zorgen dat u alle vereiste css klassen in de 'build' die u niet hebt in uw markup (bijv., toegevoegd met behulp van code) op te nemen.

De `safelist` sectie wordt gebruikt om Tailwind CSS te vertellen welke klassen in het CSS-bestand moeten worden opgenomen. De `darkMode` sectie wordt gebruikt om Tailwind CSS te vertellen de donkere modus klassen te gebruiken. De `theme` sectie wordt gebruikt om het thema van Tailwind CSS te configureren. De `plugins` sectie wordt gebruikt om de plugins die u gebruikt in uw project op te nemen. Dit wordt vervolgens door Tailwind gebruikt om het CSS-bestand te compileren zoals dat is beschreven in:

"build:tw": "npx tailwindcss -i./src/css/main.css -o./wwwroot/css/dist/main.css --minify"

### CSPROJ

Het laatste deel hiervan is in het CSProj bestand zelf. Dit omvat een sectie vlak voor de sluiting  `<Project> ` label:

```xml

    <Target Name="BuildCss" BeforeTargets="Compile">
        <Exec Command="npm run dev" Condition=" '$(Configuration)' == 'Debug' " />
        <Exec Command="npm run build" Condition=" '$(Configuration)' == 'Release' " EnvironmentVariables="NODE_ENV=production" />
    </Target>

```

Wat zoals je kunt zien verwijst naar het bouwscript om de CSS op elk project te bouwen.