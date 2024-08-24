# Coilwind CSS & ASP.NET Core

<datetime class="hidden">2024-07-30T13:30</datetime>

<!--category-- ASP.NET, Tailwind -->
Tailwind CSS es un framework CSS de primera utilidad para la construcción rápida de diseños personalizados. Es un marco CSS altamente personalizable, de bajo nivel que le da todos los bloques de construcción que necesita para construir diseños a medida sin estilos molestos de opinión que tiene que luchar para anular.

Uno de los grandes beneficios de Tailwind sobre los marcos CSS 'tradicionales' como Bootstrap es que Tailwind incluye un 'escáner' y un paso de construcción por lo que sólo incluye el CSS que realmente utiliza en su proyecto. Esto significa que puede incluir toda la biblioteca CSS de Tailwind en su proyecto y no preocuparse por el tamaño del archivo CSS.

## Instalación

Un gran inconveniente en comparación con Bootstrap es que Tailwind no es un archivo CSS 'drop in'. Usted necesita instalarlo usando npm o hilo (sección posterior es de [esto](https://tailwindcss.com/docs/installation)).

```bash
npm install -D tailwindcss
npx tailwindcss init
```

Esto instalará Tailwind CSS y creará un [`tailwind.config.js` ](#tailwindconfigjs) archivo en la raíz de su proyecto. Este archivo se utiliza para configurar Tailwind CSS.

### Paquete.json

Si usted mira a la [fuente de este proyecto](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid) Ya verás que tengo un `package.json` archivo que incluye las siguientes definiciones de'script' y 'devDependencias':

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

Estos son los'scripts' que utilizo para construir el archivo CSS de Tailwind. Los `dev` script es el que utilizo para construir el archivo CSS para el desarrollo. Los `watch` script es el que utilizo para ver el archivo CSS para los cambios y reconstruirlo. Los `build` script es el que utilizo para construir el archivo CSS para la producción.

La sección devDependencias es como tus paquetes de nuget para tus proyectos.NET. Son los paquetes que se utilizan para construir el archivo CSS.

### Tailwind.config.js

Estos se utilizan junto con el `tailwind.config.js` archivo que está en la raíz del proyecto. Este archivo se utiliza para configurar Tailwind CSS. Aquí está el `tailwind.config.js` archivo que utilizo:

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

Este archivo se utiliza para configurar Tailwind CSS. Los `content` sección se utiliza para decirle a Tailwind CSS dónde buscar las clases CSS que está utilizando en su proyecto. En ASP.NET Core esto generalmente incluirá el `Pages`, `Components`, y `Views` carpetas. Notará esto también puede archivos 'cshtml'.
Una 'ganada' para el viento de cola es que usted puede nooe para incluir ` <div class="hidden></div> ` secciones para asegurarse de que incluye todas las clases de css necesarias en la 'construcción' que no tiene en su marcado (por ejemplo, añadido usando código).

Los `safelist` sección se utiliza para decirle a Tailwind CSS qué clases incluir en el archivo CSS. Los `darkMode` sección se utiliza para decirle a Tailwind CSS que utilice las clases de modo oscuro. Los `theme` sección se utiliza para configurar el tema de Coilwind CSS. Los `plugins` sección se utiliza para incluir los plugins que está utilizando en su proyecto. Esto es usado por Tailwind para compilar el archivo CSS como sepcificado en:

"construir:tw": "npx railwindcss -i./src/css/main.css -o./wwwroot/css/dist/main.css --minify"

### CSPROJ

La parte final de esto está en el archivo CSProj en sí mismo. Esto incluye una sección justo antes del cierre  `<Project> ` etiqueta:

```xml

    <Target Name="BuildCss" BeforeTargets="Compile">
        <Exec Command="npm run dev" Condition=" '$(Configuration)' == 'Debug' " />
        <Exec Command="npm run build" Condition=" '$(Configuration)' == 'Release' " EnvironmentVariables="NODE_ENV=production" />
    </Target>

```

Como puede ver se refiere al script de compilación para reconstruir el CSS en cada construcción de proyecto.