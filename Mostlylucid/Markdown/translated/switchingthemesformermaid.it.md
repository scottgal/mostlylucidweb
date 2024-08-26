# Cambiare i temi per la sirena

<!--category-- Mermaid, Markdown, Javascript -->
<datetime class="hidden">2024-08-26T20:36</datetime>

## Introduzione

Uso Mermaid.js per creare i diagrammi di droga che vedete in alcuni post. Come quella qui sotto.
Tuttavia qualcosa che mi ha infastidito è che non è stato reattivo al cambiamento di temi (scuro/luce) e ci sembrava essere molto scarse informazioni là fuori sul raggiungimento di questo.

Questo è il risultato di alcune ore di scavare e cercare di capire come fare.

[TOC]

## Il diagramma

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

## Il problema

Il problema è che è necessario inizializzare Sirenetta per impostare il tema, e non è possibile cambiare dopo che. Tuttavia, se si desidera reinizializzare su un diagramma già creato, non è possibile rifare il diagramma poiché i dati non sono memorizzati nel DOM.

## La soluzione

Quindi dopo MOLTO scavare e cercare di capire come fare questo, ho trovato una soluzione in [questo post su GitHub](https://github.com/mermaid-js/mermaid/issues/1945)

Tuttavia ha avuto ancora alcuni problemi, quindi ho dovuto modificarlo un po 'per farlo funzionare.

### Temi

Questo sito è basato su un tema Tailwind che è venuto con uno switcher tema abbastanza terribile.

Vedrete che questo è fare varie cose intorno a cambiare il tema, impostare il tema per ciò che è memorizzato nello storage locale, cambiare un paio di stylesheers per simplemde & highlight.js e quindi applicare il tema.

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

## Configurazione

Le principali aggiunte per lo switcher tema Mermaid sono le seguenti:

```javascript
  document.body.dispatchEvent(new CustomEvent('dark-theme-set'));
    document.body.dispatchEvent(new CustomEvent('light-theme-set'));
```

Questi due eventi sono utilizzati nel nostro componente ThemeSwitcher per riinizializzare i diagrammi della Sirenetta.

### OnLoad / htmx:afterSwap

Nella mia `main.js` file Ho impostato lo switcher tema. Importo anche il `mdeswitch` file che contiene il codice per cambiare i temi.

```javascript
import "./mdeswitch";
addEventListener("DOMContentLoaded", () => {
    window.initMermaid();
});
addEventListener('htmx:afterSwap', function(evt) {
    window.initMermaid();
});
```

## MDESwtichCity name (optional, probably does not need a translation)

Questo è il file che contiene il codice per cambiare i temi per la Sirenetta.
(L'orribile [diagramma sopra](#the-diagram) mostra la sequenza degli eventi che accadono quando il tema è commutato)

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

Sta andando un po' in basso fino in cima.

1. `init` - funzione è la funzione principale che viene chiamata quando la pagina è caricata.

In primo luogo salva il contenuto originale dei diagrammi della Sirenetta; questo era un problema nella versione da cui l'ho copiato, hanno usato 'innerHTML' che non ha funzionato per me come alcuni diagrammi si basano su newlines che quelle strisce.

Poi aggiunge due ascoltatori di eventi per il `dark-theme-set` e `light-theme-set` eventi. Quando questi eventi vengono licenziati reimposta i dati elaborati e poi reinizializza i diagrammi della Sirenetta con il nuovo tema.

Controlla quindi l'archiviazione locale per il tema e inizializza i diagrammi della Sirenetta con il tema appropriato.

```javascript
let isDarkMode = localStorage.theme === 'dark';
        if(isDarkMode) {
            loadMermaid('dark');
         }
         else{
             loadMermaid('default')
         }
```

### Salva dati originali

La chiave di tutto questo è memorizzare poi ripristinare il contenuto contenuto nel rendering `<div class="mermaid"><div>` che contengono il marchio della sirena dai nostri posti.

Vedrete questo solo impostare una promessa che loop attraverso tutti gli elementi e memorizza il contenuto originale in un `data-original-code` attributo.

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

`resetProcessed` è lo stesso, tranne nel caso in cui inverso prende il markup dal `data-original-code` attributo e lo riporta all'elemento.
Notalo anche `encodeURIComponent` il valore come ho detto che alcune stringhe non venivano memorizzate correttamente.

### Init

Ora abbiamo tutti questi dati che possiamo riinizializzare la sirena per applicare il nostro nuovo tema e restituire il diagramma SVG nel nostro output HTML.

```javascript
 const loadMermaid = function(theme) {
        window.mermaid.initialize({theme})
        window.mermaid.run()
    }
```

## In conclusione

E' stato un po' un dolore da capire, ma sono felice di averlo fatto. Spero che questo aiuti qualcun altro là fuori che sta cercando di fare la stessa cosa.