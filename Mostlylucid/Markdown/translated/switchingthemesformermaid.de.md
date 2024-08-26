# Wechseln Themen für Meerjungfrau

<!--category-- Mermaid, Markdown, Javascript -->
<datetime class="hidden">2024-08-26T20:36</datetime>

## Einleitung

Ich benutze Mermaid.js, um die Dope-Diagramme zu erstellen, die Sie in ein paar Beiträgen sehen. Wie die unten.
Aber etwas, das mich ärgerte, ist, dass es nicht reaktiv war, Themen zu wechseln (Dunkel/Licht) und es schien sehr schlechte Informationen da draußen zu geben, um dies zu erreichen.

Dies ist das Ergebnis von ein paar Stunden zu graben und zu versuchen, herauszufinden, wie dies zu tun.

[TOC]

## Das Diagramm

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

## Das Problem

Das Problem ist, dass Sie Mermaid initialisieren müssen, um das Thema zu setzen, und Sie können es danach nicht ändern. WIE auch immer, wenn Sie es auf einem bereits erstellten Diagramm wieder initialisieren wollen; es kann das Diagramm nicht wiederholen, da die Daten nicht im DOM gespeichert sind.

## Die Lösung

Also, nachdem MUCH graben und versuchen, herauszufinden, wie dies zu tun, fand ich eine Lösung in [diese GitHub Ausgabe post](https://github.com/mermaid-js/mermaid/issues/1945)

Allerdings hatte es noch ein paar Probleme, so dass ich es ein wenig ändern musste, um es an die Arbeit zu bekommen.

### Themen

Diese Website basiert auf einem Tailwind-Thema, das mit einem ziemlich schrecklichen Thema Switcher kam.

Sie werden sehen, dass dies verschiedene Sachen um das Thema zu wechseln, die Einstellung des Themas für das, was im lokalen Speicher gespeichert ist, ändern ein paar Stylesheers für simplemde & highlight.js und dann die Anwendung des Themas.

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

## Einrichtung

Die wichtigsten Ergänzungen für die Mermaid Thema-Switcher sind die folgenden:

```javascript
  document.body.dispatchEvent(new CustomEvent('dark-theme-set'));
    document.body.dispatchEvent(new CustomEvent('light-theme-set'));
```

Diese beiden Ereignisse werden in unserer ThemeSwitcher-Komponente verwendet, um die Mermaid-Diagramme neu zu initialisieren.

### OnLoad / htmx:afterSwap

In meinem `main.js` Datei Ich habe den Themen-Schalter eingerichtet. Ich importiere auch die `mdeswitch` Datei, die den Code für das Umschalten von Themen enthält.

```javascript
import "./mdeswitch";
addEventListener("DOMContentLoaded", () => {
    window.initMermaid();
});
addEventListener('htmx:afterSwap', function(evt) {
    window.initMermaid();
});
```

## MDESwtich

Dies ist die Datei, die den Code für das Umschalten der Themen für Mermaid enthält.
(Das schreckliche [Abbildung oben](#the-diagram) zeigt die Reihenfolge der Ereignisse, die passieren, wenn das Thema gewechselt wird)

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

Hier geht es irgendwie nach unten.

1. `init` - Funktion ist die Hauptfunktion, die beim Laden der Seite aufgerufen wird.

Es speichert zunächst den ursprünglichen Inhalt der Mermaid-Diagramme; dies war ein Problem in der Version, von der ich es kopierte, sie verwendeten 'innereHTML', die für mich nicht funktionierte, da einige Diagramme auf neue Linien, die Streifen verlassen.

Es fügt dann zwei Event-Hörer für die `dark-theme-set` und `light-theme-set` Veranstaltungen. Wenn diese Ereignisse abgefeuert werden, werden die verarbeiteten Daten zurückgesetzt und dann die Mermaid-Diagramme mit dem neuen Thema neu initialisiert.

Es überprüft dann den lokalen Speicher für das Thema und initialisiert die Mermaid Diagramme mit dem entsprechenden Thema.

```javascript
let isDarkMode = localStorage.theme === 'dark';
        if(isDarkMode) {
            loadMermaid('dark');
         }
         else{
             loadMermaid('default')
         }
```

### Originaldaten speichern

Der Schlüssel zu dieser ganzen Sache ist die Speicherung dann die Wiederherstellung des Inhalts in der gerenderten enthalten `<div class="mermaid"><div>` die die Meerjungfrau Markup von unseren Pfosten enthalten.

Sie werden sehen, dass dies nur ein Versprechen, das Schleifen durch alle Elemente und speichert den ursprünglichen Inhalt in einem `data-original-code` Attribut.

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

`resetProcessed` ist die gleiche, außer in umgekehrt, wo es nimmt die Markup von der `data-original-code` Attribut und setzt es auf das Element zurück.
Beachten Sie es auch `encodeURIComponent` der Wert, während ich foud, dass einige Strings nicht richtig gespeichert wurden.

### Init

Jetzt haben wir alle diese Daten, die wir wieder initialisieren mermaid, um unser neues Thema und rerender das SVG-Diagramm in unsere HTML-Ausgabe.

```javascript
 const loadMermaid = function(theme) {
        window.mermaid.initialize({theme})
        window.mermaid.run()
    }
```

## Schlussfolgerung

Das war ein bisschen nervig, aber ich bin froh, dass ich es getan habe. Ich hoffe, das hilft jemandem, der versucht, das Gleiche zu tun.