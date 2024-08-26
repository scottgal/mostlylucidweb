# Κάνοντας το ASP.NET Core Website σας PWA

<!--category-- ASP.NET -->
<datetime class="hidden">2024-08-01T11:36</datetime>

Σε αυτό το άρθρο, θα σας δείξω πώς να κάνετε την ιστοσελίδα σας ASP.NET Core μια PWA (Progressive Web App).

## Προαπαιτούμενα

Είναι πραγματικά πολύ απλό δείτε https://github.com/madskristensen/WebEssentials.AspNetCore.ServiceWorker/tree/master

## ASP.NET Bits

Εγκατάσταση του πακέτου Nuget

```bash
dotnet add package WebEssentials.AspNetCore.PWA
```

Στο πρόγραμμά σας.cs προσθέστε:

```csharp
builder.Services.AddProgressiveWebApp();
```

Στη συνέχεια, δημιουργήστε μερικά φαβιόνια που ταιριάζουν με τα παρακάτω μεγέθη [Ορίστε.](https://realfavicongenerator.net/) είναι ένα εργαλείο που μπορείτε να χρησιμοποιήσετε για να τα δημιουργήσετε. Αυτά μπορεί να είναι πραγματικά οποιοδήποτε εικονίδιο (Χρησιμοποίησα ένα emoji ~)

Save these in your wwrroot folder as android-chrome-192x192.png and android-chrome-512x512.png (in the example below)

Τότε χρειάζεσαι ένα μανιφέστο.

```json
{
  "name": "mostlylucid",
  "short_name": "mostlylucid",
  "description": "The web site for mostlylucid limited",
  "icons": [
    {
      "src": "/android-chrome-192x192.png",
      "sizes": "192x192"
    },
    {
      "src": "/android-chrome-512x512.png",
      "sizes": "512x512"
    }
  ],
  "display": "standalone",
  "start_url": "/"
}
```