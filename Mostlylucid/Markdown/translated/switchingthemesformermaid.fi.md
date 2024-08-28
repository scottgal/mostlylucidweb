# Teemojen vaihtaminen merenneidolle

<!--category-- Mermaid, Markdown, Javascript -->
<datetime class="hidden">2024-08-26T20:36</datetime>

## Johdanto

Käytän Mermaid.js luoda huumeita kaavioita näet muutamassa viestissä. Kuten alla.
Minua kuitenkin ärsytti se, että se ei reagoinut teemojen vaihtamiseen (pimeä/kevyt) ja sen saavuttamisesta näytti olevan hyvin vähän tietoa.

Tämä on seurausta muutaman tunnin kaivamisesta ja yrittämisestä selvittää, miten se tehdään.

**<span style="color:red"> HUOMAUTUS: Tämä ei toimi luotettavasti. Yritän yhä selvittää miksi.</span>**

[TÄYTÄNTÖÖNPANO

## Kaavio

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

## Ongelma

Ongelmana on, että merenneito pitää alustaa teemaan, eikä sitä voi muuttaa sen jälkeen. Jos haluat kuitenkin käynnistää sen uudelleen jo luodulla kaaviolla, se ei voi tehdä kaaviota uudelleen, koska dataa ei tallenneta DOM:iin.

## Ratkaisu

Joten sen jälkeen, kun oli kaivanut paljon ja yrittänyt selvittää, miten tämä tehdään, löysin ratkaisun [tämä GitHub-julkaisun postaus](https://github.com/mermaid-js/mermaid/issues/1945)

Sillä oli kuitenkin vielä muutama ongelma, joten minun oli muokattava sitä hieman, jotta se toimisi.

### Teemat

Sivusto perustuu Tailwind-teemaan, johon tuli aika karmea teemavaihtaja.

Huomaat, että tämä tekee erilaisia juttuja teeman vaihtamisen ympärillä, asettaa teeman paikalliseen tallennukseen, vaihtaa pari tyyliä simppeliin & fashion.js:ään ja sitten soveltaa teemaa.

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

## Asetukset

Merenneidon teemavaihtajan tärkeimmät lisät ovat seuraavat:

```javascript
  document.body.dispatchEvent(new CustomEvent('dark-theme-set'));
    document.body.dispatchEvent(new CustomEvent('light-theme-set'));
```

Näitä kahta tapahtumaa käytetään ThemeSwitcher-komponentissamme merenneitokaavioiden uudelleenaloittamisessa.

### OnLoad / htmx:afterSwap

Omassa `main.js` Tiedoston asetin teemanvaihtajan. Tuon myös `mdeswitch` Tiedosto, joka sisältää koodin teemojen vaihtamiseen.

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

Tämä on tiedosto, joka sisältää koodin teemojen vaihtamiseksi merenneidolle.
(Kammottavia [yllä oleva kaavio](#the-diagram) näyttää tapahtumasarjan, joka tapahtuu, kun teema on vaihdettu)

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

Alhaalta huipulle.

1. `init` - Funktio on päätoiminto, jota kutsutaan, kun sivu on ladattu.

Se tallentaa ensin Merenneidon kaavioiden alkuperäisen sisällön; tämä oli kysymys versiossa, josta kopioin sen, he käyttivät "sisäHTML:ää", joka ei toiminut minulle, koska jotkut diagrammit luottavat uusiin riveihin, jotka riisuvat.

Sen jälkeen siihen lisätään kaksi tapahtuman kuuntelijaa. `dark-theme-set` sekä `light-theme-set` tapahtumat. Kun nämä tapahtumat käynnistetään, se nollaa prosessoidun datan ja käynnistää merenneidon kaaviot uudelleen uudella teemalla.

Sen jälkeen se tarkistaa teeman paikallisen tallennuksen ja alustelee Merenneidon kaaviot sopivalla teemalla.

```javascript
let isDarkMode = localStorage.theme === 'dark';
        if(isDarkMode) {
            loadMermaid('dark');
         }
         else{
             loadMermaid('default')
         }
```

### Tallenna alkuperäiset tiedot

Avain tähän on tallentaa ja palauttaa sitten renderoidun sisällön sisältö. `<div class="mermaid"><div>` jotka sisältävät merenneitomarginaaleja sivuiltamme.

Huomaat, että tämä vain muodostaa Promise, joka yhdistää kaikki elementit ja tallentaa alkuperäisen sisällön `data-original-code` Ominaisuus.

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

`resetProcessed` on sama paitsi käänteisessä, jossa se ottaa korotuksen `data-original-code` Määrittele ja aseta se takaisin elementille.
Huomaa se myös `encodeURIComponent` Arvostan sitä, että joitakin naruja ei ole tallennettu oikein.

### Initiaatio

Nyt meillä on kaikki tämä data, jonka voimme käynnistää merenneidon uudelleen, jotta voimme soveltaa uutta teemaamme ja palauttaa SVG-kaavion HTML-tuotteeseen.

```javascript
 const loadMermaid = function(theme) {
        window.mermaid.initialize({theme})
        window.mermaid.run()
    }
```

## Johtopäätöksenä

Tätä oli vaikea selvittää, mutta olen iloinen, että selvitin. Toivon, että tämä auttaa jotakuta toista, joka yrittää samaa.