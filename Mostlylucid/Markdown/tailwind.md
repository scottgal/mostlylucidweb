# Tailwind CSS

Tailwind CSS is a utility-first CSS framework for rapidly building custom designs. It is a highly customizable, low-level CSS framework that gives you all of the building blocks you need to build bespoke designs without any annoying opinionated styles you have to fight to override.

One of the big benefits of Tailwind over 'traditional' CSS frameworks like Bootstrap is that Tailwind includes a 'scanning' and building step so only includes the CSS you actually use in your project. This means that you can include the entire Tailwind CSS library in your project and not worry about the size of the CSS file.

## Installation
One biig drawback compared to Bootstrap is that Tailwind is not a 'drop in' CSS file. You need to install it using npm or yarn. 

If you look at the source of this project you'll see that I have a `package.json` file that includes the following 'script' and 'devDependencies' definitions:

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

These are the 'scripts' that I use to build the Tailwind CSS file. The `dev` script is the one that I use to build the CSS file for development. The `watch` script is the one that I use to watch the CSS file for changes and rebuild it. The `build` script is the one that I use to build the CSS file for production.

The devDependencies section are like your nuget packages for your .NET projects. They are the packages that are used to build the CSS file.

These are used along with the `tailwind.config.js` file that is in the root of the project. This file is used to configure Tailwind CSS. Here is the `tailwind.config.js` file that I use:

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

This file is used to configure Tailwind CSS. The `content` section is used to tell Tailwind CSS where to look for the CSS classes that you are using in your project. The `safelist` section is used to tell Tailwind CSS which classes to include in the CSS file. The `darkMode` section is used to tell Tailwind CSS to use the dark mode classes. The `theme` section is used to configure the theme of Tailwind CSS. The `plugins` section is used to include the plugins that you are using in your project. This is then used by Tailwind to compile the CSS file as sepcified in:

"build:tw": "npx tailwindcss -i ./src/css/main.css -o ./wwwroot/css/dist/main.css --minify"