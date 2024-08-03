# CSS & ASP.NET 核心

<datetime class="hidden">2024-07-30-13:30</datetime>
尾风 CSS 是用于快速建立自定义设计的 通用第一 CSS 框架 。 这是一个高度可定制的低水平 CSS 框架, 使您能够建立所有需要表达的构件设计, 而不需要任何令人讨厌的见解风格来推翻它 。

尾风对“传统” CSS 框架(如“诱杀装置”)的一大好处是,尾风包含一个“扫描”和建设步骤,因此只包含您在项目中实际使用的 CSS 。这意味着您可以在您的项目中包括整个 Lackwind CSS 库,而不必担心 CSS 文件的大小 。

## 安装安装

与“启动陷阱”相比,一个大缺点是,尾风不是“在” CSS 文件中的“ 滴入 ” CSS 文件。 您需要使用 npm 或线条安装它( 后继部分来自)[这笔](https://tailwindcss.com/docs/installation)).

```bash
npm install -D tailwindcss
npx tailwindcss init
```

这将安装尾风 CSS 并创建[`tailwind.config.js` ](#tailwindconfigjs)在您的工程根文件中的文件。 此文件用于配置 Tadewind CSS 。

### 软件包.json

如果你看一看[此项目源源](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid)你会看到我有一个`package.json`包括以下“ 脚本” 和“ devDependies” 定义的文件 :

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

这些是我用来构建尾风 CSS 文件的“ 脚本 ” 。`dev`脚本是我用来为开发构建 CSS 文件的脚本。`watch`脚本是我用来查看 CSS 文件修改并重建的脚本。`build`用于制作 CSS 文件的脚本 。

DevDependies 部分就像您的.NET 工程的 nuget 软件包。 它们是用于构建 CSS 文件的软件包 。

### 尾风. config.js

这些工具与这些工具一起使用。`tailwind.config.js`位于工程根底的文件。 此文件用于配置尾风 CSS 。 这是`tailwind.config.js`我使用的文件 :

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

此文件用于配置尾风 CSS 。 The`content`在 ASP.NET Core 中,它通常包括`Pages`, `Components`, 和`Views`文件夹。 您会注意到此选项还可以包含“ cshtml” 文件 。
尾风的“得到的”是,你也许可以把“得到的”包括进去。` <div class="hidden></div> `区域可以确保您在“ 建设” 中包含所有所需的 cs 类, 而您的标记中没有这些 cs 类( 例如, 使用代码添加 ) 。

缩略`safelist`区域用于告诉尾风 CSS 将包含哪类的 CSS 文件。`darkMode`区域用于告诉尾风 CSS 使用暗模式类。`theme`用于配置尾风 CSS 主题的一节。`plugins`区域用于包含您在工程中使用的插件。 然后由尾风将 CSS 文件编译为 :

“建设:tw”:“npx尾风-i./src/css/main.cs-o./wwwroot/css/dist/main.cs-minify”

### CSPROJ 保护

最后一部分在 CSProj 文件本身中。 这包括关闭前的一节`<Project> `标签 :

```xml

    <Target Name="BuildCss" BeforeTargets="Compile">
        <Exec Command="npm run dev" Condition=" '$(Configuration)' == 'Debug' " />
        <Exec Command="npm run build" Condition=" '$(Configuration)' == 'Release' " EnvironmentVariables="NODE_ENV=production" />
    </Target>

```

正如你所看到的,它指重建每个项目建设的 CSS 的建筑脚本。