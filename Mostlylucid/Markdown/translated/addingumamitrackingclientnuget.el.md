# Προσθήκη πακέτου Nuget πελατών παρακολούθησης Umami

<!--category-- ASP.NET, Umami, Nuget -->
<datetime class="hidden">2024-08-28T02:00</datetime>

# Εισαγωγή

Τώρα έχω τον πελάτη Umami, πρέπει να το πακετάρω και να το κάνω διαθέσιμο ως πακέτο Nuget. Αυτή είναι μια αρκετά απλή διαδικασία, αλλά υπάρχουν μερικά πράγματα που πρέπει να γνωρίζουμε.

[TOC]

# Δημιουργία του πακέτου Nuget

## Εκδοση

Αποφάσισα να αντιγράψω. [ΚαλίντCity name (optional, probably does not need a translation)](https://khalidabuhakmeh.com/) και χρησιμοποιήστε το εξαιρετικό πακέτο Minver για να εκδώσετε το πακέτο Nuget μου. Αυτό είναι ένα απλό πακέτο που χρησιμοποιεί την ετικέτα έκδοσης git για να καθορίσει τον αριθμό έκδοσης.

Για να το χρησιμοποιήσω απλά πρόσθεσα τα ακόλουθα στην `Umami.Net.csproj` αρχείο:

```xml
    <PropertyGroup>
    <MinVerTagPrefix>v</MinVerTagPrefix>
</PropertyGroup>
```

Έτσι μπορώ να βάλω ετικέτα στην εκδοχή μου. `v` και το πακέτο θα εκδοθεί σωστά.

```bash
 git tag v0.0.8       
 git push origin v0.0.8

```

Θα ωθήσει αυτή την ετικέτα, τότε έχω ένα GitHub Action setup να περιμένει για την ετικέτα και να χτίσει το πακέτο Nuget.

## Κατασκευή του πακέτου Nuget

Έχω μια δράση GitHub που φτιάχνει το πακέτο Nuget και το σπρώχνει στο αποθετήριο του πακέτου GitHub. Αυτή είναι μια απλή διαδικασία που χρησιμοποιεί την `dotnet pack` εντολή για την κατασκευή του πακέτου και στη συνέχεια η `dotnet nuget push` Εντολές να το πιέσουμε στο αποθετήριο των Νιούγκετ.

```yaml
name: Publish Umami.NET
on:
  push:
    tags:
      - 'v*.*.*'  # This triggers the action for any tag that matches the pattern v1.0.0, v2.1.3, etc.

jobs:
  publish:
    runs-on: ubuntu-latest

    steps:
    - name: Checkout code
      uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.x' # Specify the .NET version you need

    - name: Restore dependencies
      run: dotnet restore ./Umami.Net/Umami.Net.csproj

    - name: Build project
      run: dotnet build --configuration Release ./Umami.Net/Umami.Net.csproj --no-restore

    - name: Pack project
      run: dotnet pack --configuration Release ./Umami.Net/Umami.Net.csproj --no-build --output ./nupkg

    - name: Publish to NuGet
      run: dotnet nuget push ./nupkg/*.nupkg --source https://api.nuget.org/v3/index.json --api-key ${{ secrets.UMAMI_NUGET_API_KEY }}
      env:
        NUGET_API_KEY: ${{ secrets.UMAMI_NUGET_API_KEY }}
```

### Προσθήκη εικονιδίου και ανάγνωσης

Αυτό είναι πολύ απλό, προσθέτω ένα `README.md` αρχείο στη ρίζα του έργου και `icon.png` αρχείο στη ρίζα του έργου. Η `README.md` το αρχείο χρησιμοποιείται ως περιγραφή του πακέτου και `icon.png` το αρχείο χρησιμοποιείται ως το εικονίδιο για το πακέτο.

```xml
    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
        <LangVersion>12</LangVersion>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>

        <IsPackable>true</IsPackable>
        <PackageId>Umami.Net</PackageId>
        <Authors>Scott Galloway</Authors>
        <PackageIcon>icon.png</PackageIcon>
        <RepositoryUrl>https://github.com/scottgal/mostlylucidweb/tree/main/Umami.Net</RepositoryUrl>
        <PackageLicenseExpression>MIT</PackageLicenseExpression>
        <PackageTags>web</PackageTags>
        <PackageReadmeFile>README.md</PackageReadmeFile>
        <Description>
           Adds a simple Umami endpoint to your ASP.NET Core application.
        </Description>
    </PropertyGroup>
```

Στο αρχείο README.md έχω ένα σύνδεσμο με το αποθετήριο GitHub και μια περιγραφή του πακέτου.

Αναπαράγονται παρακάτω:

# Umami.Net

Αυτό είναι ένα.NET Core πελάτη για την παρακολούθηση Umami API.
Βασίζεται στον πελάτη του κόμβου Umami, ο οποίος μπορεί να βρεθεί [Ορίστε.](https://github.com/umami-software/node).

Μπορείτε να δείτε πώς να ρυθμίσετε Umami ως δοχείο docker [Ορίστε.](https://www.mostlylucid.net/blog/usingumamiforlocalanalytics).
Μπορείτε να διαβάσετε περισσότερες λεπτομέρειες σχετικά με τη δημιουργία του στο blog μου [Ορίστε.](https://www.mostlylucid.net/blog/addingumamitrackingclientfollowup).

Για να χρησιμοποιήσετε αυτόν τον πελάτη χρειάζεστε τις ακόλουθες ρυθμίσεις.json:

```json
{
  "Analytics":{
    "UmamiPath" : "https://umamilocal.mostlylucid.net",
    "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee"
  },
}
```

Πού; `UmamiPath` είναι το μονοπάτι για την περίπτωσή σας Umami και `WebsiteId` είναι η ταυτότητα της ιστοσελίδας που θέλετε να παρακολουθείτε.

Για να χρησιμοποιήσετε τον πελάτη θα πρέπει να προσθέσετε τα ακόλουθα `Program.cs`:

```csharp
using Umami.Net;

services.SetupUmamiClient(builder.Configuration);
```

Αυτό θα προσθέσει τον πελάτη Umami στη συλλογή υπηρεσιών.

Μπορείτε στη συνέχεια να χρησιμοποιήσετε τον πελάτη με δύο τρόπους:

1. Ένεση `UmamiClient` στην τάξη σας και να καλέσετε το `Track` μέθοδος:

```csharp
 // Inject UmamiClient umamiClient
 await umamiClient.Track("Search", new UmamiEventData(){{"query", encodedQuery}});
```

2. Χρήση του `UmamiBackgroundSender` να παρακολουθείτε τα γεγονότα στο παρασκήνιο (αυτό χρησιμοποιεί ένα `IHostedService` για την αποστολή γεγονότων στο παρασκήνιο:

```csharp
 // Inject UmamiBackgroundSender umamiBackgroundSender
await umamiBackgroundSender.Track("Search", new UmamiEventData(){{"query", encodedQuery}});
```

Ο πελάτης θα στείλει την εκδήλωση στο API Umami και θα αποθηκευτεί.

Η `UmamiEventData` είναι ένα λεξικό των ζευγών βασικών τιμών που θα σταλούν στο Umami API ως τα δεδομένα γεγονότων.

Υπάρχουν επιπλέον πιο χαμηλές μέθοδοι που μπορούν να χρησιμοποιηθούν για την αποστολή γεγονότων στο Umami API.

Και στις δύο περιπτώσεις... `UmamiClient` και `UmamiBackgroundSender` Μπορείτε να καλέσετε την ακόλουθη μέθοδο.

```csharp


 Send(UmamiPayload? payload = null, UmamiEventData? eventData = null,
        string eventType = "event")
```

Αν δεν περάσεις σε ένα `UmamiPayload` αντικείμενο, ο πελάτης θα δημιουργήσει ένα για σας χρησιμοποιώντας το `WebsiteId` από τα ορεκτικά. Τζέισον.

```csharp
    public  UmamiPayload GetPayload(string? url = null, UmamiEventData? data = null)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var request = httpContext?.Request;

        var payload = new UmamiPayload
        {
            Website = settings.WebsiteId,
            Data = data,
            Url = url ?? httpContext?.Request?.Path.Value,
            IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString(),
            UserAgent = request?.Headers["User-Agent"].FirstOrDefault(),
            Referrer = request?.Headers["Referer"].FirstOrDefault(),
           Hostname = request?.Host.Host,
        };
        
        return payload;
    }

```

Μπορείς να δεις ότι αυτό έχει πληθυσμό... `UmamiPayload` αντικείμενο με το `WebsiteId` από τις ομορφιές. json, το `Url`, `IpAddress`, `UserAgent`, `Referrer` και `Hostname` από την `HttpContext`.

ΣΗΜΕΙΩΣΗ: Το eventType μπορεί να είναι μόνο "event" ή "idify" σύμφωνα με το Umami API.

# Συμπέρασμα

Έτσι, αυτό είναι που μπορείτε τώρα να εγκαταστήσετε Umami.Net από Nuget και να το χρησιμοποιήσετε σε ASP.NET Core εφαρμογή σας. Ελπίζω να το βρεις χρήσιμο. Θα συνεχίσω να ψάχνω και να προσθέτω εξετάσεις σε μελλοντικές θέσεις.