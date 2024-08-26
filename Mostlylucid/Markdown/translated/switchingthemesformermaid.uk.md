# Перемикання тем для Mer покоївки

<!--category-- Mermaid, Markdown, Javascript -->
<datetime class="hidden">2024- 08- 26T20: 36</datetime>

## Вступ

Я використовую Mer покоївку.js, щоб створити схеми наркотиків, які ви бачите в декількох повідомленнях. Як та, що знизу.
Проте мене дратувало те, що це не було реагуючим до перемикання тем (темно/світло) і здавалося, що там була дуже погана інформація про це.

Це результат кількох годин копання і спроби з'ясувати, як це зробити.

[TOC]

## Діаграма

```mermaid
sequenceDiagram
    participant Window as window
    participant Mermaid as mermaid
    participant Document as document
    participant LocalStorage as localStorage

    Window->>Init: initMermaid()

    Init->>SaveOriginalData: Call saveOriginalData()
    SaveOriginalData->>Document: querySelectorAll(elementCode)
    SaveOriginalData->>Element: Set 'data-original-code' for each element
    SaveOriginalData->>Init: Resolve saveOriginalData promise

    Init->>Document: Add event listener for 'dark-theme-set'
    Init->>Document: Add event listener for 'light-theme-set'

    Note over Init: Event Listener for 'dark-theme-set'
    Document->>ResetProcessed: Trigger resetProcessed() on dark-theme-set
    ResetProcessed->>Document: querySelectorAll(elementCode)
    ResetProcessed->>Element: Reset processed state and restore textContent
    ResetProcessed->>Init: Resolve resetProcessed promise
    Init->>LoadMermaid: Call loadMermaid('dark')
    LoadMermaid->>Mermaid: Initialize and run with 'dark' theme

    Note over Init: Event Listener for 'light-theme-set'
    Document->>ResetProcessed: Trigger resetProcessed() on light-theme-set
    ResetProcessed->>Document: querySelectorAll(elementCode)
    ResetProcessed->>Element: Reset processed state and restore textContent
    ResetProcessed->>Init: Resolve resetProcessed promise
    Init->>LoadMermaid: Call loadMermaid('default')
    LoadMermaid->>Mermaid: Initialize and run with 'default' theme

    Note over Init: Check local storage theme
    Init->>LocalStorage: Retrieve localStorage.theme
    LocalStorage->>Init: Return 'dark' or other
    Init->>LoadMermaid: Call loadMermaid based on theme
    LoadMermaid->>Mermaid: Initialize and run with theme

```

## Проблема

Проблема в тому, що вам потрібно ініціалізувати Mer покоївку, щоб встановити тему, і ви не можете її після цього змінити. ЯК ЖЕ, якщо ви хочете відновити її на вже створеній діаграмі, вона не зможе повторити діаграму, оскільки дані не зберігаються в DOM.

## Розв'язання

Тож після того, як я багато копав і намагався зрозуміти, як це зробити, я знайшов рішення в [повідомлення про випуск цього випуску GitHub](https://github.com/mermaid-js/mermaid/issues/1945)

Як би там не було, у мене все ще було кілька проблем, тож мені потрібно було трохи змінити їх, щоб вони працювали.

### Теми

Цей сайт засновано на темі Tailwind, яка постачається досить жахливим перемикачем тем.

Ви побачите, що це виконує різноманітні функції навколо перемикання теми, встановлення теми для того, що зберігається у локальному сховищі, зміни декількох стилів для простого підсвічування і підсвічування.js, а потім застосування теми.

```javascript
export  function globalSetup() {
    const lightStylesheet = document.getElementById('light-mode');
    const darkStylesheet = document.getElementById('dark-mode');
    const simpleMdeDarkStylesheet = document.getElementById('simplemde-dark');
    const simpleMdeLightStylesheet = document.getElementById('simplemde-light');
    return {
        isMobileMenuOpen: false,
        isDarkMode: false,
        // Function to initialize the theme based on localStorage or system preference
        themeInit() {
            if (
                localStorage.theme === "dark" ||
                (!("theme" in localStorage) &&
                    window.matchMedia("(prefers-color-scheme: dark)").matches)
            ) {
                localStorage.theme = "dark";
                document.documentElement.classList.add("dark");
                document.documentElement.classList.remove("light");
                this.isDarkMode = true;
              
                this.applyTheme(); // Apply dark theme stylesheets
            } else {
                localStorage.theme = "base";
                document.documentElement.classList.remove("dark");
                document.documentElement.classList.add("light");
                this.isDarkMode = false;
                this.applyTheme(); // Apply light theme stylesheets
            }
        },

        // Function to switch the theme and update the stylesheets accordingly
        themeSwitch() {
            if (localStorage.theme === "dark") {
                localStorage.theme = "light";
                document.body.dispatchEvent(new CustomEvent('light-theme-set'));
                document.documentElement.classList.remove("dark");
                document.documentElement.classList.add("light");
                this.isDarkMode = false;
            } else {
                localStorage.theme = "dark";
                document.body.dispatchEvent(new CustomEvent('dark-theme-set'));
                document.documentElement.classList.add("dark");
                document.documentElement.classList.remove("light");
                this.isDarkMode = true;
            }
            this.applyTheme(); // Apply the theme stylesheets after switching
        },

        // Function to apply the appropriate stylesheets based on isDarkMode
        applyTheme() {
         
            if (this.isDarkMode) {
                // Enable dark mode stylesheets
                lightStylesheet.disabled = true;
                darkStylesheet.disabled = false;
                simpleMdeLightStylesheet.disabled = true;
                simpleMdeDarkStylesheet.disabled = false;
            } else {
                // Enable light mode stylesheets
                lightStylesheet.disabled = false;
                darkStylesheet.disabled = true;
                simpleMdeLightStylesheet.disabled = false;
                simpleMdeDarkStylesheet.disabled = true;
            }
        }
    };
}
```

## Налаштування

Основними додатками перемикача теми Mercanow є такі:

```javascript
  document.body.dispatchEvent(new CustomEvent('dark-theme-set'));
    document.body.dispatchEvent(new CustomEvent('light-theme-set'));
```

Ці дві події використовуються у нашому компоненті "ThemeSwitcher" для відновлення діаграм Мерпокоївки.

### OnLoad / htmx: afterSwap

В моєму `main.js` файл, який я налаштую перемикач тем. Я також імпортую `mdeswitch` файл, у якому міститься код перемикання тем.

```javascript
import "./mdeswitch";
addEventListener("DOMContentLoaded", () => {
    window.initMermaid();
});
addEventListener('htmx:afterSwap', function(evt) {
    window.initMermaid();
});
```

## MDESwich

Це файл, у якому міститься код для перемикання тем для Mer покоївки.
(The dreaking) [діаграма вище](#the-diagram) показує послідовність подій, які відбуваються під час перемикання теми)

```javascript
(function(window){
    'use strict'

    const elementCode = 'div.mermaid'
    const loadMermaid = function(theme) {
        window.mermaid.initialize({theme})
        window.mermaid.run()
    }
    const saveOriginalData = function(){
        return new Promise((resolve, reject) => {
            try {
                var els = document.querySelectorAll(elementCode),
                    count = els.length;
                if(!els || count ===0 ) resolve ();
                els.forEach(element => {
                    element.setAttribute('data-original-code',encodeURIComponent( element.textContent));
                    count--
                    if(count == 0){
                        resolve()
                    }
                });
            } catch (error) {
                reject(error)
            }
        })
    }
    const resetProcessed = function(){
        return new Promise((resolve, reject) => {
            try {
                var els = document.querySelectorAll(elementCode),
                    count = els.length;
                if(!els || count ===0 ) resolve ();
                els.forEach(element => {
                    if(element.getAttribute('data-original-code') != null){
                        element.removeAttribute('data-processed')
                        element.textContent =decodeURIComponent( element.getAttribute('data-original-code'));
                    }
                    count--
                    if(count == 0){
                        resolve()
                    }
                });
            } catch (error) {
                reject(error)
            }
        })
    }

    const init = ()=>{

        saveOriginalData()
            .catch( console.error )
        document.body.addEventListener('dark-theme-set', ()=>{
            resetProcessed()
                .then(() =>{
                    loadMermaid('dark');
                    console.log("dark theme set")})
                .catch(console.error)
        })
        document.body.addEventListener('light-theme-set', ()=>{
            resetProcessed()
                .then(() =>{
                    loadMermaid('default');
                    console.log("dark theme set")})
                .catch(console.error)
        })
        let isDarkMode = localStorage.theme === 'dark';
        if(isDarkMode) {
            loadMermaid('dark');
        }
        else{
            loadMermaid('default')
        }

    }
    window.initMermaid = init
})(window);
```

Йдучи трохи знизу сюди.

1. `init` - функція є основною функцією, яка викликається під час завантаження сторінки.

Спочатку він зберігає оригінальний зміст діаграм Мернір; це була проблема у версії, з якої я переписав її, вони використовували "природну HTML," яка не спрацювала для мене, як деякі діаграми покладаються на нові лінії, з яких вона складається.

Тоді вона додає дві події для слухачів `dark-theme-set` і `light-theme-set` події. Коли ці події буде звільнено, програма відновить оброблені дані, а потім відновить діаграму Мерпокої на нову тему.

Потім він перевіряє місцеве зберігання цієї теми і започатковує діаграми " Мернір " відповідною темою.

```javascript
let isDarkMode = localStorage.theme === 'dark';
        if(isDarkMode) {
            loadMermaid('dark');
         }
         else{
             loadMermaid('default')
         }
```

### Зберегти початкові дані

Ключем до всього цього є збереження, а потім відновлення вмісту, що міститься у відтворенні `<div class="mermaid"><div>` де є розмітка русалки з наших постів.

Ви побачите, що це просто встановлює обіцянку, яка проходить через всі елементи і зберігає оригінальний зміст в `data-original-code` атрибут.

```javascript
    const saveOriginalData = function(){
        return new Promise((resolve, reject) => {
            try {
                var els = document.querySelectorAll(elementCode),
                    count = els.length;
                if(!els || count ===0 ) resolve ();
                els.forEach(element => {
                    element.setAttribute('data-original-code',encodeURIComponent(element.textContent))
                    count--
                    if(count == 0){
                        resolve()
                    }
                });
            } catch (error) {
                reject(error)
            }
        })
    }
```

`resetProcessed` те саме, хіба що навпаки, де він забирає розмітку з `data-original-code` атрибут і повертає його елементу.
Зверніть увагу також на це. `encodeURIComponent` що деякі рядки зберігалися неправильно.

### Ініціалізація

Тепер ми маємо всі ці дані, ми можемо відновити покоївку, щоб застосувати нашу нову тему і переробити діаграму SVG до нашого виводу HTML.

```javascript
 const loadMermaid = function(theme) {
        window.mermaid.initialize({theme})
        window.mermaid.run()
    }
```

## Включення

Было немного больно разобраться, но я рада, что сделала это. Надеюсь, это поможет кому-то еще, кто пытается сделать то же самое.