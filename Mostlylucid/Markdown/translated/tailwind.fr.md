# Tailwind CSS & ASP.NET Core

<datetime class="hidden">2024-07-30T13:30</datetime>

<!--category-- ASP.NET, Tailwind -->
Tailwind CSS est un framework CSS utilitaire pour construire rapidement des designs personnalisés. C'est un cadre CSS très personnalisable et de bas niveau qui vous donne tous les éléments de construction dont vous avez besoin pour construire des designs sur mesure sans aucun style d'opinion agaçant que vous devez combattre pour passer outre.

L'un des grands avantages de Tailwind sur les cadres CSS «traditionnels» comme Bootstrap est que Tailwind inclut une étape de «scanning» et de construction, donc seulement inclut la CSS que vous utilisez réellement dans votre projet. Cela signifie que vous pouvez inclure toute la bibliothèque CSS Tailwind dans votre projet et ne pas vous soucier de la taille du fichier CSS.

## Installation

Un gros inconvénient par rapport à Bootstrap est que Tailwind n'est pas un fichier CSS 'drop in'. Vous devez l'installer en utilisant npm ou fil (la section suivante est de [Voici](https://tailwindcss.com/docs/installation)).

```bash
npm install -D tailwindcss
npx tailwindcss init
```

Ceci installera Tailwind CSS et créera un [`tailwind.config.js` ](#tailwindconfigjs) fichier dans la racine de votre projet. Ce fichier est utilisé pour configurer Tailwind CSS.

### Paquet.json

Si vous regardez la [source de ce projet](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid) vous verrez que j'ai un `package.json` fichier qui comprend les définitions suivantes de'script' et de 'devDependences':

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

Ce sont les'scripts' que j'utilise pour construire le fichier CSS Tailwind. Les `dev` script est celui que j'utilise pour construire le fichier CSS pour le développement. Les `watch` script est celui que j'utilise pour regarder le fichier CSS pour les modifications et le reconstruire. Les `build` script est celui que j'utilise pour construire le fichier CSS pour la production.

La section devDependences est comme vos paquets nuget pour vos projets.NET. Ce sont les paquets qui sont utilisés pour construire le fichier CSS.

### Tailwind.config.js

Ceux-ci sont utilisés avec le `tailwind.config.js` fichier qui est dans la racine du projet. Ce fichier est utilisé pour configurer Tailwind CSS. Voici le `tailwind.config.js` fichier que j'utilise :

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

Ce fichier est utilisé pour configurer Tailwind CSS. Les `content` section est utilisée pour indiquer à Tailwind CSS où chercher les classes CSS que vous utilisez dans votre projet. Dans ASP.NET Core, il s'agit généralement de `Pages`, `Components`, et `Views` Les dossiers. Vous noterez que cela peut aussi des fichiers 'cshtml'.
Un 'gotcha' pour le vent arrière est que vous mai nooe à inclure ` <div class="hidden></div> ` sections pour vous assurer d'inclure toutes les classes de css obligatoires dans la « construction » que vous n'avez pas dans votre balisage (p. ex., ajouté en utilisant le code).

Les `safelist` section est utilisée pour dire à Tailwind CSS quelles classes inclure dans le fichier CSS. Les `darkMode` section est utilisé pour dire à Tailwind CSS d'utiliser les classes de mode sombre. Les `theme` section est utilisé pour configurer le thème de Tailwind CSS. Les `plugins` section est utilisé pour inclure les plugins que vous utilisez dans votre projet. Ceci est ensuite utilisé par Tailwind pour compiler le fichier CSS tel que sépcifié dans:

"build:tw": "npx tailwindcss -i./src/css/main.css -o./wwwroot/css/dist/main.css --minify"

### CSPRJ

La dernière partie est dans le fichier CSProj lui-même. Cela comprend une section juste avant la fermeture  `<Project> ` étiquette :

```xml

    <Target Name="BuildCss" BeforeTargets="Compile">
        <Exec Command="npm run dev" Condition=" '$(Configuration)' == 'Debug' " />
        <Exec Command="npm run build" Condition=" '$(Configuration)' == 'Release' " EnvironmentVariables="NODE_ENV=production" />
    </Target>

```

Ce qui, comme vous pouvez le voir, fait référence au script de construction pour reconstruire le CSS sur chaque construction de projet.