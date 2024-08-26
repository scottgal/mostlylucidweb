# Ανεμοστρόβιλος CSS & ASP.NET Core

<datetime class="hidden">2024-07-30T13:30</datetime>

<!--category-- ASP.NET, Tailwind -->
Tailwind CSS είναι ένα βοηθητικό-πρώτο πλαίσιο CSS για την ταχεία οικοδόμηση έθιμο σχέδια. Είναι ένα εξαιρετικά προσαρμόσιμο, χαμηλό επίπεδο πλαίσιο CSS που σας δίνει όλα τα δομικά στοιχεία που χρειάζεστε για να οικοδομήσουμε bespoke σχέδια χωρίς ενοχλητικά στυλ άποψης που πρέπει να παλέψουμε για να παρακάμψουμε.

Ένα από τα μεγάλα οφέλη του Tailwind πάνω από τα "παραδοσιακά" πλαίσια CSS όπως το Bootstrap είναι ότι το Tailwind περιλαμβάνει ένα'scanning' και ένα οικοδομικό βήμα έτσι περιλαμβάνει μόνο το CSS που πραγματικά χρησιμοποιείτε στο πρόγραμμά σας. Αυτό σημαίνει ότι μπορείτε να συμπεριλάβετε ολόκληρη τη βιβλιοθήκη CSS Tailwind στο έργο σας και να μην ανησυχείτε για το μέγεθος του αρχείου CSS.

## Εγκατάσταση

Ένα μεγάλο μειονέκτημα σε σύγκριση με Bootstrap είναι ότι το Tailwind δεν είναι ένα αρχείο CSS 'drop in'. Θα πρέπει να το εγκαταστήσετε χρησιμοποιώντας npm ή νήματα (υποσυνείδητο τμήμα είναι από [Αυτό...](https://tailwindcss.com/docs/installation)).

```bash
npm install -D tailwindcss
npx tailwindcss init
```

Αυτό θα εγκαταστήσει Tailwind CSS και να δημιουργήσει ένα [`tailwind.config.js` ](#tailwindconfigjs) Φάκελος στη ρίζα του έργου σας. Αυτό το αρχείο χρησιμοποιείται για τη ρύθμιση CSS Tailwind.

### Package.json

Αν κοιτάξετε το [πηγή αυτού του έργου](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid) Θα δεις ότι έχω... `package.json` αρχείο που περιλαμβάνει τους ακόλουθους ορισμούς "script" και "devD εξαρτήσεις":

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

Αυτά είναι τα "σενάρια" που χρησιμοποιώ για να φτιάξω το αρχείο CSS του Tailwind. Η `dev` Το σενάριο είναι αυτό που χρησιμοποιώ για να φτιάξω το αρχείο CSS για την ανάπτυξη. Η `watch` Το σενάριο είναι αυτό που χρησιμοποιώ για να βλέπω το αρχείο CSS για αλλαγές και να το ξαναφτιάχνω. Η `build` Το σενάριο είναι αυτό που χρησιμοποιώ για να φτιάξω το αρχείο CSS για την παραγωγή.

Το devD εξαρτήσεις τμήμα είναι σαν πακέτα nuget σας για.NET έργα σας. Είναι τα πακέτα που χρησιμοποιούνται για την κατασκευή του αρχείου CSS.

### Ανεμοστρόβιλος. config.js

Αυτά χρησιμοποιούνται μαζί με το `tailwind.config.js` αρχείο που βρίσκεται στη ρίζα του έργου. Αυτό το αρχείο χρησιμοποιείται για τη ρύθμιση CSS Tailwind. Εδώ είναι το... `tailwind.config.js` αρχείο που χρησιμοποιώ:

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

Αυτό το αρχείο χρησιμοποιείται για τη ρύθμιση CSS Tailwind. Η `content` το τμήμα χρησιμοποιείται για να πει Tailwind CSS πού να αναζητήσετε τα μαθήματα CSS που χρησιμοποιείτε στο έργο σας. Σε ASP.NET πυρήνα που θα περιλαμβάνει γενικά `Pages`, `Components`, και `Views` Φάκελοι. Θα σημειώσετε ότι αυτό επίσης κουτάκια 'cshtml' αρχεία.
Ένα "gotcha" για τον άνεμο της ουράς είναι ότι μπορείτε nooe να συμπεριλάβετε ` <div class="hidden></div> ` τμήματα για να διασφαλίσετε ότι θα συμπεριλάβετε όλες τις απαιτούμενες css τάξεις στο 'building' που δεν έχετε στο markup σας (π.χ., προστίθεται χρησιμοποιώντας τον κωδικό).

Η `safelist` το τμήμα χρησιμοποιείται για να πει Tailwind CSS ποιες κατηγορίες να συμπεριλάβετε στο αρχείο CSS. Η `darkMode` Το τμήμα χρησιμοποιείται για να πει στην Tailwind CSS να χρησιμοποιήσει τις τάξεις σκοτεινής λειτουργίας. Η `theme` Το τμήμα χρησιμοποιείται για τη ρύθμιση του θέματος του CSS Tailwind. Η `plugins` το τμήμα χρησιμοποιείται για να συμπεριλάβει τα plugins που χρησιμοποιείτε στο έργο σας. Αυτό χρησιμοποιείται στη συνέχεια από το Tailwind για τη σύνταξη του αρχείου CSS, όπως sepcified in:

"build:tw": "npx tailwindcss -i./src/css/main.css -o./wwwroot/css/dist/main.css --minify"

### CSPROJ

Το τελικό μέρος αυτού είναι στο ίδιο το αρχείο CSProj. Αυτό περιλαμβάνει ένα τμήμα ακριβώς πριν από το κλείσιμο  `<Project> ` ετικέτα:

```xml

    <Target Name="BuildCss" BeforeTargets="Compile">
        <Exec Command="npm run dev" Condition=" '$(Configuration)' == 'Debug' " />
        <Exec Command="npm run build" Condition=" '$(Configuration)' == 'Release' " EnvironmentVariables="NODE_ENV=production" />
    </Target>

```

Το οποίο όπως μπορείτε να δείτε αναφέρεται στο σενάριο κατασκευής για την ανοικοδόμηση της CSS σε κάθε έργο κατασκευής.