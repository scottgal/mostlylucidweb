# Perätuuli CSS & ASP.NET Core

<datetime class="hidden">2024-07-30T13:30</datetime>

<!--category-- ASP.NET, Tailwind -->
Kääntötuulella CSS on käyttökelpoinen ensimmäinen CSS-kehys, jonka avulla voidaan nopeasti rakentaa mukautetut mallit. Se on erittäin muokattavissa oleva, matalatasoinen CSS-kehys, joka antaa sinulle kaikki ne rakennuspalikat, jotka sinun on rakennettava mittatilaustyönä ilman ärsyttäviä mielipiteitä herättäviä tyylejä, joita sinun on taisteltava ohittaaksesi.

Yksi myötätuulen suurista hyödyistä "perinteisten" CSS-kehysten, kuten Bootstrapin, yli on se, että Tailswindiin sisältyy "skannaus" ja rakennusvaihe, joten se sisältää vain CSS:n, jota todella käytät projektissasi. Tämä tarkoittaa, että voit liittää projektiisi koko Tailwind CSS -kirjaston, etkä ole huolissasi CSS-tiedoston koosta.

## Asennus

Yksi suuri haittapuoli Bootstrapiin verrattuna on se, että Tailwind ei ole CSS-tiedoston pudotus. Sinun täytyy asentaa se npm:llä tai langalla (osa on peräisin [tämä](https://tailwindcss.com/docs/installation)).

```bash
npm install -D tailwindcss
npx tailwindcss init
```

Tämä asentaa Tailwind CSS:n ja luo [`tailwind.config.js` ](#tailwindconfigjs) Arkistoi projektisi ydin. Tätä tiedostoa käytetään Tailwind CSS:n määrittelyyn.

### Package.json

Jos katsot... [tämän projektin lähde](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid) Huomaat, että minulla on `package.json` tiedosto, joka sisältää seuraavat'script' ja 'devViences' -määritelmät:

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

Näillä "kirjoituksilla" rakennan Tailwind CSS -tiedoston. Erytropoietiini `dev` Käsikirjoitus on se, jolla rakennan CSS-tiedoston kehitystä varten. Erytropoietiini `watch` Käsikirjoitus on se, jolla katson CSS-tiedostoa muutosten varalta ja rakennan sen uudelleen. Erytropoietiini `build` Käsikirjoitus on se, jolla rakennan CSS-tiedoston tuotantoa varten.

DevDependences-osio on kuin.net-projektiesi huikeita paketteja. Ne ovat ne paketit, joita käytetään CSS-tiedoston rakentamiseen.

### Perätuuli.config.js

Näitä käytetään yhdessä darbepoetiini alfan ja darbepoetiini alfan kanssa. `tailwind.config.js` tiedosto, joka on projektin ytimessä. Tätä tiedostoa käytetään Tailwind CSS:n määrittelyyn. Tässä on: `tailwind.config.js` Tiedosto, jota käytän:

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

Tätä tiedostoa käytetään Tailwind CSS:n määrittelyyn. Erytropoietiini `content` Osiossa kerrotaan Tailwind CSS:lle, mistä voit etsiä CSS-tunteja, joita käytät projektissasi. ASP.NET-ytimessä tähän sisältyy yleensä `Pages`, `Components`, ja `Views` kansiot. Huomaat myös nämä tölkit "cshtml"-tiedostot.
Yksi "otettu" myötätuuleen on, että voit nooe mukaan lukien ` <div class="hidden></div> ` Osien avulla varmistetaan, että kaikki vaadittavat CSS-luokat sisällytetään "rakennukseen", jota sinulla ei ole markissasi (esim. lisätty koodilla).

Erytropoietiini `safelist` Osiota käytetään kertomaan Tailwind CSS:lle, mitkä luokat sisällytetään CSS-tiedostoon. Erytropoietiini `darkMode` Osaa käytetään käskemään Tailwind CSS:ää käyttämään pimeän tilan luokkia. Erytropoietiini `theme` Tailwind CSS:n teeman määrittelyssä käytetään osiota. Erytropoietiini `plugins` Osiossa käytetään projektissasi käyttämiäsi liitännäisiä. Tämän jälkeen Tailwind käyttää CSS-tiedoston kokoamiseen seuraavasti:

"Rakennus:tw": "npx myötätuulilasia -i./src/css/main.css -o./wwwroot/css/dist/main.css --minify"

### CSPROJ

Tämän viimeinen osa on itse CSProj-kansiossa. Tämä sisältää osion juuri ennen sulkemista  `<Project> ` lappu:

```xml

    <Target Name="BuildCss" BeforeTargets="Compile">
        <Exec Command="npm run dev" Condition=" '$(Configuration)' == 'Debug' " />
        <Exec Command="npm run build" Condition=" '$(Configuration)' == 'Release' " EnvironmentVariables="NODE_ENV=production" />
    </Target>

```

Kuten näette, se viittaa rakennuskäsikirjoitukseen, jolla CSS rakennetaan uudelleen jokaiseen projektinrakennukseen.