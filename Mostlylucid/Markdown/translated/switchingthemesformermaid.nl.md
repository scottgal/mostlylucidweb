# Thema's voor Mermaid wisselen

<!--category-- Mermaid, Markdown, Javascript -->
<datetime class="hidden">2024-08-26T20:36</datetime>

## Inleiding

Ik gebruik Mermaid.js om de dope diagrammen te maken die je in een paar berichten ziet. Zoals die onderaan.
Maar iets wat me ergerde is dat het niet reageerde op het veranderen van thema's (donker/licht) en er leek zeer slechte informatie te zijn over het bereiken van dit.

Dit is het resultaat van een paar uur graven en proberen uit te zoeken hoe dit te doen.

[TOC]

## Het diagram

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

## Het probleem

Het probleem is dat je Mermaid moet initialiseren om het thema in te stellen, en je kunt het daarna niet veranderen. Hoe dan ook als je het opnieuw wilt initialiseren op een reeds aangemaakt diagram; het kan het diagram niet opnieuw doen omdat de gegevens niet in de DOM worden opgeslagen.

## De oplossing

Dus na Much graven en proberen uit te zoeken hoe dit te doen, vond ik een oplossing in [deze GitHub nummer post](https://github.com/mermaid-js/mermaid/issues/1945)

Maar het had nog een paar problemen, dus ik moest het een beetje aanpassen om het aan het werk te krijgen.

### Thema's

Deze site is gebaseerd op een Tailwind thema dat kwam met een vrij verschrikkelijke thema switcher.

Je zult zien dat dit verschillende dingen doet rond het veranderen van het thema, het instellen van het thema voor wat is opgeslagen in de lokale opslag, het veranderen van een paar stylesheers voor simplemde & highlight.js en vervolgens het toepassen van het thema.

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

## Instellen

De belangrijkste toevoegingen voor de Mermaid thema switcher zijn de volgende:

```javascript
  document.body.dispatchEvent(new CustomEvent('dark-theme-set'));
    document.body.dispatchEvent(new CustomEvent('light-theme-set'));
```

Deze twee gebeurtenissen worden gebruikt in onze ThemeSwitcher component om de Mermaid diagrammen te herinitialiseren.

### OnLoad / htmx:afterSwap

In mijn `main.js` bestand Ik heb de thema switcher ingesteld. Ik importeer ook de `mdeswitch` bestand dat de code voor het schakelen van thema's bevat.

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

Dit is het bestand dat de code bevat voor het schakelen van de thema's voor Mermaid.
(De verschrikkelijke) [diagram boven](#the-diagram) toont de volgorde van gebeurtenissen die gebeuren wanneer het thema is geschakeld)

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

Ik ga hier van onder naar boven.

1. `init` - functie is de belangrijkste functie die wordt aangeroepen wanneer de pagina wordt geladen.

Het slaat eerst de oorspronkelijke inhoud van de Mermaid diagrammen op; dit was een probleem in de versie waaruit ik het kopieerde, ze gebruikten 'innerHTML' die niet voor mij werkte als sommige diagrammen vertrouwen op nieuwe lijnen die dat strips.

Het voegt dan twee gebeurtenis luisteraars voor de `dark-theme-set` en `light-theme-set` gebeurtenissen. Wanneer deze gebeurtenissen worden afgevuurd resetten de verwerkte gegevens en vervolgens opnieuw initialiseren van de Zeemeermin diagrammen met het nieuwe thema.

Het controleert vervolgens de lokale opslag voor het thema en initialiseert de Zeemeermin diagrammen met het juiste thema.

```javascript
let isDarkMode = localStorage.theme === 'dark';
        if(isDarkMode) {
            loadMermaid('dark');
         }
         else{
             loadMermaid('default')
         }
```

### Originele gegevens opslaan

De sleutel tot dit hele ding is opslaan dan het herstellen van de inhoud in de weergegeven `<div class="mermaid"><div>` die de zeemeermin markering van onze posten bevatten.

Je zult zien dat dit gewoon een Belofte opzet die door alle elementen loopt en de originele inhoud opslaat in een `data-original-code` attribuut.

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

`resetProcessed` is hetzelfde, behalve in omgekeerde waar het neemt de markup van de `data-original-code` attribuut en zet het terug naar het element.
Let er ook op. `encodeURIComponent` de waarde als ik foud dat sommige strings niet correct werden opgeslagen.

### Init

Nu hebben we al deze gegevens die we kunnen herinitialiseren zeemeermin om ons nieuwe thema toe te passen en het SVG diagram in onze HTML-uitvoer te herschrijven.

```javascript
 const loadMermaid = function(theme) {
        window.mermaid.initialize({theme})
        window.mermaid.run()
    }
```

## Conclusie

Dit was een beetje vervelend om uit te zoeken, maar ik ben blij dat ik het deed. Ik hoop dat dit iemand helpt die hetzelfde probeert te doen.