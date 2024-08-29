# Αλλαγή θεμάτων για τη Γοργόνα (Ενημέρωση)

<!--category-- Mermaid, Markdown, Javascript -->
<datetime class="hidden">2024-08-29T05:00</datetime>

## Εισαγωγή

Χρησιμοποιώ το Mermaid.js για να δημιουργήσω τα διαγράμματα ναρκωτικών που βλέπετε σε μερικές θέσεις. Σαν αυτό από κάτω.
Ωστόσο, κάτι που με ενόχλησε είναι ότι δεν ήταν αντιδραστικό στην αλλαγή θεμάτων (σκοτεινό / φως) και φαινόταν να υπάρχουν πολύ ανεπαρκείς πληροφορίες εκεί έξω για την επίτευξη αυτού.

Αυτό είναι το αποτέλεσμα μερικών ωρών σκάψιμο και προσπαθεί να καταλάβει πώς να το κάνει αυτό.

Μπορείτε να βρείτε την πηγή για mdeswitcher εδώ:
[mdeswitcher.js](https://github.com/scottgal/mostlylucidweb/blob/main/Mostlylucid/src/js/mdeswitch.js).

**<span style="color:green"> ΣΗΜΕΙΩΣΗ: Το έχω ενημερώσει αυτό ουσιαστικά.</span>**

[TOC]

## Το Διάγραμμα

```mermaid
graph LR
    A[Start] --> B[Initialize Mermaid with Theme]
    B --> C{Are there any elements matching 'div.mermaid'?}
    C --> |No| D[Exit]
    C --> |Yes| E[Save Original Data]
    E --> F{Did saving data succeed?}
    F --> |No| D[Exit]
    F --> |Yes| G[Set up Theme Event Listeners]
    G --> H[Check Local Storage for Dark Mode]
    H --> I{Is Dark Mode enabled?}
    I --> |Yes| J[Load Mermaid with Dark Theme]
    I --> |No| K[Load Mermaid with Default Theme]
    J --> L[Wait for Events]
    K --> L[Wait for Events]
    L --> M{Event Triggered?}
    M --> |Dark Theme Set| N[Reset Processed Data]
    N --> O[Load Mermaid with Dark Theme]
    M --> |Light Theme Set| P[Reset Processed Data]
    P --> Q[Load Mermaid with Default Theme]
    O --> L
    Q --> L
    L --> D[Exit]


```

## Το Πρόβλημα

Το θέμα είναι ότι πρέπει να αρχικοποιήσεις την Γοργόνα για να βάλεις το θέμα, και δεν μπορείς να το αλλάξεις μετά από αυτό. ΠΩΣ αν θέλετε να το επανεκκινήσετε σε ένα ήδη δημιουργημένο διάγραμμα; δεν μπορεί να ξανακάνει το διάγραμμα καθώς τα δεδομένα δεν αποθηκεύονται στο DOM.

## Η Λύση

Έτσι, μετά το σκάψιμο πολλών και προσπαθώντας να βρω πώς να το κάνω αυτό, βρήκα μια λύση σε [αυτό το άρθρο έκδοσης GitHub](https://github.com/mermaid-js/mermaid/issues/1945)

Ωστόσο, είχε ακόμη μερικά ζητήματα, έτσι έπρεπε να το τροποποιήσω λίγο για να το κάνω να λειτουργήσει.

### Θέματα

Αυτή η ιστοσελίδα βασίζεται σε ένα θέμα Tailwind που ήρθε με ένα αρκετά τρομερό διακόπτη θέμα.

Θα δείτε ότι αυτό είναι κάνει διάφορα πράγματα γύρω από την αλλαγή του θέματος, που το θέμα για ό, τι είναι αποθηκευμένο στην τοπική αποθήκευση, αλλάζοντας ένα ζευγάρι stylesheers για simplemde & highlight.js και στη συνέχεια την εφαρμογή του θέματος.

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

## Ρύθμιση

Οι κύριες προσθήκες για τον διακόπτη θέματος της Γοργόνας είναι οι εξής:

```javascript
  document.body.dispatchEvent(new CustomEvent('dark-theme-set'));
    document.body.dispatchEvent(new CustomEvent('light-theme-set'));
```

Αυτά τα δύο γεγονότα χρησιμοποιούνται στο εξάρτημα μας ThemeSwitcher για να επανεκκινήσουν τα διαγράμματα Γοργόνας.

### OnLoad / htmx:afterSwap

Στο............................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................................. `main.js` Αρχείο που ρύθμισα τον διακόπτη θέματος. Εισάγω επίσης το `mdeswitch` αρχείο που περιέχει τον κωδικό για την αλλαγή θεμάτων.

```javascript
//Important: Memraid will ALWAYS intialize on window.onload, so we need to make sure we disable this behaviour:
import mermaid from "mermaid";

window.mermaid=mermaid;
mermaid.initialize({startOnLoad:false});

window.mermaidinit = function() {
    mermaid.initialize({ startOnLoad: false });
    try {
        window.initMermaid().then(r => console.log('Mermaid initialized'));
    } catch (e) {
        console.error('Failed to initialize Mermaid:', e);
    }

}

document.body.addEventListener('htmx:afterSwap', function(evt) {
    mermaidinit();
    //This should be called after the mermaid diagrams have been rendered.
    hljs.highlightAll();
});

window.onload = function(ev) {
    if(document.readyState === 'complete') {
        mermaidinit();
        hljs.highlightAll();
    }
};
```

## MDESwtich

Αυτό είναι το αρχείο που περιέχει τον κωδικό για την αλλαγή των θεμάτων για την Γοργόνα.
(Το φρικτό [διάγραμμα ανωτέρω](#the-diagram) δείχνει την ακολουθία των γεγονότων που συμβαίνουν όταν το θέμα αλλάζει)

```javascript
(function(window) {
    'use strict';

    const elementCode = 'div.mermaid';

    const loadMermaid = async (theme) => {

        mermaid.initialize({startOnLoad: false, theme: theme });
        console.log("Loading mermaid with theme:", theme);
        await mermaid.run({
            querySelector: elementCode,
        });
    };

    const saveOriginalData = async () => {
        try {
            console.log("Saving original data");
            const elements = document.querySelectorAll(elementCode);
            const count = elements.length;

            if (count === 0) return;

            const promises = Array.from(elements).map((element) => {
                if (element.getAttribute('data-processed') != null) {
                    console.log("Element already processed");
                    return;
                }
                element.setAttribute('data-original-code', element.innerHTML);
            });

            await Promise.all(promises);
        } catch (error) {
            console.error(error);
            throw error;
        }
    };

    const resetProcessed = async () => {
        try {
            console.log("Resetting processed data");
            const elements = document.querySelectorAll(elementCode);
            const count = elements.length;

            if (count === 0) return;

            const promises = Array.from(elements).map((element) => {
                if (element.getAttribute('data-original-code') != null) {
                    element.removeAttribute('data-processed');
                    element.innerHTML = element.getAttribute('data-original-code');
                }
                else {
                    console.log("Element already reset");
                }
            });

            await Promise.all(promises);
        } catch (error) {
            console.error(error);
            throw error;
        }
    };

    window.initMermaid = async () => {
        const mermaidElements = document.querySelectorAll(elementCode);
        if (mermaidElements.length === 0) return;

        try {
            await saveOriginalData();
        } catch (error) {
            console.error("Error saving original data:", error);
            return; // Early exit if saveOriginalData fails
        }

        const handleDarkThemeSet = async () => {
            try {
                await resetProcessed();
                await loadMermaid('dark');
                console.log("Dark theme set");
            } catch (error) {
                console.error("Error during dark theme set:", error);
            }
        };

        const handleLightThemeSet = async () => {
            try {
                await resetProcessed();
                await loadMermaid('default');
                console.log("Light theme set");
            } catch (error) {
                console.error("Error during light theme set:", error);
            }
        };
        document.body.removeEventListener('dark-theme-set', handleDarkThemeSet);
        document.body.removeEventListener('light-theme-set', handleLightThemeSet);
        document.body.addEventListener('dark-theme-set', handleDarkThemeSet);
        document.body.addEventListener('light-theme-set', handleLightThemeSet);

        const isDarkMode = localStorage.theme === 'dark';
        await loadMermaid(isDarkMode ? 'dark' : 'default').then(r => console.log('Initial load complete'));


    };

})(window);
```

Πάει κάπως από κάτω προς τα πάνω εδώ.

1. `init` - λειτουργία είναι η κύρια λειτουργία που ονομάζεται όταν η σελίδα είναι φορτωμένη.

Αρχικά σώζει το αρχικό περιεχόμενο των διαγραμμάτων της Γοργόνας· αυτό ήταν ένα θέμα στην έκδοση που το αντιγράφω, χρησιμοποίησαν το "innerHTML" που δεν λειτούργησε για μένα, καθώς ορισμένα διαγράμματα βασίζονται σε νέες γραμμές από τις οποίες ταινία.

Στη συνέχεια προσθέτει δύο ακροατές εκδηλώσεων για την `dark-theme-set` και `light-theme-set` Γεγονότα. Όταν αυτά τα γεγονότα απολύονται επαναφέρει τα επεξεργασμένα δεδομένα και στη συνέχεια επανακινεί τα διαγράμματα Γοργόνας με το νέο θέμα.

Στη συνέχεια ελέγχει την τοπική αποθήκευση για το θέμα και αρχικοποιεί τα διαγράμματα Γοργόνας με το κατάλληλο θέμα.

```javascript
let isDarkMode = localStorage.theme === 'dark';
        if(isDarkMode) {
            loadMermaid('dark');
         }
         else{
             loadMermaid('default')
         }
```

### Αποθήκευση πρωτότυπων δεδομένων

Το κλειδί για όλο αυτό το πράγμα είναι η αποθήκευση στη συνέχεια την αποκατάσταση του περιεχομένου που περιέχεται στην απόδοση `<div class="mermaid"><div>` Που περιέχουν το σημάδι γοργόνας από τις θέσεις μας.

Θα δείτε αυτό ακριβώς δημιουργεί μια υπόσχεση ότι βρόχοι μέσα από όλα τα στοιχεία και αποθηκεύει το αρχικό περιεχόμενο σε ένα `data-original-code` γνώριμη ιδιότητα.

```javascript
    const saveOriginalData = async () => {
    try {
        console.log("Saving original data");
        const elements = document.querySelectorAll(elementCode);
        const count = elements.length;

        if (count === 0) return;

        const promises = Array.from(elements).map((element) => {
            if (element.getAttribute('data-processed') != null) {
                console.log("Element already processed");
                return;
            }
            element.setAttribute('data-original-code', element.innerHTML);
        });

        await Promise.all(promises);
    } catch (error) {
        console.error(error);
        throw error;
    }
};
```

`resetProcessed` είναι το ίδιο, εκτός από το αντίστροφο, όπου παίρνει το σημάδι από το `data-original-code` Το αποδίδω και το επαναφέρω στο στοιχείο.

### Init@ info: whatsthis

Τώρα έχουμε όλα αυτά τα δεδομένα μπορούμε να επανεκκινήσουμε γοργόνα για να εφαρμόσουμε το νέο μας θέμα και να ανασυνθέσουμε το διάγραμμα SVG στην έξοδο HTML μας.

```javascript
    const elementCode = 'div.mermaid';

const loadMermaid = async (theme) => {

    mermaid.initialize({startOnLoad: false, theme: theme });
    console.log("Loading mermaid with theme:", theme);
    await mermaid.run({
        querySelector: elementCode,
    });
};
```

## Συμπέρασμα

Αυτό ήταν ένα κομμάτι του πόνου για να καταλάβω, αλλά είμαι ευτυχής που το έκανα. Ελπίζω αυτό να βοηθήσει κάποιον άλλο εκεί έξω που προσπαθεί να κάνει το ίδιο πράγμα.