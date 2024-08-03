# CSS & ASP.NET 核心

<datetime class="hidden">2024-07-30-13:30</datetime>
尾风 CSS是快速建立定制设计的第一个通用 CSS 框架。 这是一个高度可定制的,低级的 CSS 框架, 给了你所有需要构建的建筑构件 不受任何烦恼的 见解风格影响的设计

尾风对“传统”CSS框架(如“诱饵”)的一大好处是,尾风包括“扫描”和建设步骤,所以只包括您在项目中实际使用的 CSS。 这意味着您可以将整个尾风 CSS 库包含在您的工程中,而不必担心 CSS 文件的大小 。

## 安装安装

与“启动陷阱”相比,一个大缺点是,“尾风”不是“投放到” CSS 文件。 您需要使用 npm 或线条安装它( 后继部分来自 [这笔](https://tailwindcss.com/docs/installation)).

```bash
npm install -D tailwindcss
npx tailwindcss init
```

这将安装尾风 CSS 并创建 [`tailwind.config.js` ](#tailwindconfigjs) 在您的工程根中存档 。 此文件用于配置尾风 CSS 。

### 软件包.json

如果你看一看 [此项目源源](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid) 你会看到我有一个 `package.json` 包括以下“ 脚本” 和“ devDependies” 定义的文件 :

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

这些是我用来构建尾风 CSS 文件的“ 脚本 ” 。 缩略 `dev` 脚本是我用来构建 CSS 文件用于开发的脚本 。 缩略 `watch` 脚本是我用来查看 CSS 文件更改并重建的脚本 。 缩略 `build` 用于制作 CSS 文件的脚本 。

DevDependies 部分就像您的.NET 工程的金字塔包。 它们是用于构建 CSS 文件的软件包 。

### 尾风. config.js

这些工具与这些工具一起使用。 `tailwind.config.js` 在工程根部的文件 。 此文件用于配置尾风 CSS 。 这儿就是 `tailwind.config.js` 我使用的文件 :

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

此文件用于配置尾风 CSS 。 缩略 `content` 区域用于告诉尾风 CSS 如何查找您在工程中使用的 CSS 类 。 ASP.NET核心部分一般包括: `Pages`, `Components`, 和 `Views` 文件夹。 你也会注意到这里面还有"cshtml"文件
尾风的“得到的”是,你也许可以把“得到的”包括进去。 ` <div class="hidden></div> ` 区域可以确保您在“ 建设” 中包含所有所需的 cs 类, 而您的标记中没有这些 cs 类( 例如, 使用代码添加 ) 。

缩略 `safelist` 用于告诉尾风 CSS 中包含哪类内容的 CSS 文件 。 缩略 `darkMode` 区域用于告诉尾风 CSS 使用暗模式类 。 缩略 `theme` 用于配置尾风 CSS 主题的一节。 缩略 `plugins` 区域用于包含您在工程中使用的插件 。 然后由尾风将 CSS 文件编译为 :

“建设:tw”:“npx尾风-i./src/css/main.cs-o./wwwroot/css/dist/main.cs-minify”

### CSPROJ 保护

最后一部分在 CSProj 文件本身 。 其中包括在闭幕前的一节。  `<Project> ` 标签 :

```xml

    <Target Name="BuildCss" BeforeTargets="Compile">
        <Exec Command="npm run dev" Condition=" '$(Configuration)' == 'Debug' " />
        <Exec Command="npm run build" Condition=" '$(Configuration)' == 'Release' " EnvironmentVariables="NODE_ENV=production" />
    </Target>

```

正如你所看到的,它指重建每个项目建设的 CSS 的建筑脚本。