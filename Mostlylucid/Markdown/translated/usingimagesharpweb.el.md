# Χρήση ImageSharp.Web με ASP.NET Core

<datetime class="hidden">2024-08-13T14:16</datetime>

<!--category-- ASP.NET, ImageSharp -->
## Εισαγωγή

[ImageSharp](https://docs.sixlabors.com/index.html) είναι μια ισχυρή βιβλιοθήκη επεξεργασίας εικόνων που σας επιτρέπει να χειραγωγείτε εικόνες με διάφορους τρόπους. ImageSharp.Web είναι μια επέκταση του ImageSharp που παρέχει επιπλέον λειτουργικότητα για την εργασία με εικόνες σε εφαρμογές ASP.NET Core. Σε αυτό το φροντιστήριο, θα εξερευνήσουμε πώς να χρησιμοποιήσετε ImageSharp.Web για να αλλάξετε μέγεθος, καλλιέργεια, και να διαμορφώσετε εικόνες σε αυτή την εφαρμογή.

[TOC]

## ImageSharp.Web Εγκατάσταση

Για να ξεκινήσετε με το ImageSharp.Web, θα πρέπει να εγκαταστήσετε τα ακόλουθα πακέτα NuGet:

```bash
dotnet add package SixLabors.ImageSharp
dotnet add package SixLabors.ImageSharp.Web
```

## Ρυθμίσεις ImageSharp.Web

Στο αρχείο μας Program.cs στη συνέχεια εγκαταστήσαμε ImageSharp.Web. Στην περίπτωσή μας αναφερόμαστε και αποθηκεύουμε τις εικόνες μας σε ένα φάκελο που ονομάζεται "εικόνες" στο wwwroot του έργου μας. Στη συνέχεια, ρυθμίσαμε το ImageSharp.Web μεσαίο λογισμικό για να χρησιμοποιήσετε αυτό το φάκελο ως την πηγή των εικόνων μας.

ImageSharp.Web χρησιμοποιεί επίσης ένα φάκελο 'cache' για να αποθηκεύσει τα επεξεργασμένα αρχεία (αυτό αποτρέπει την επαναπλήρωση αρχείων κάθε φορά).

```csharp
services.AddImageSharp().Configure<PhysicalFileSystemCacheOptions>(options => options.CacheFolder = "cache");
```

Αυτοί οι φάκελοι είναι σχετικά με το wwwroot έτσι έχουμε την ακόλουθη δομή:

![Δομή φακέλου](/cachefolder.png)

ImageSharp.Web έχει πολλαπλές επιλογές για το πού αποθηκεύετε τα αρχεία σας και caching (δείτε εδώ για όλες τις λεπτομέρειες: [https://docs.sixlabors.com/articles/imagesharp.web/imageproviders.html?tabs=tabid-1%2Ctabid-1a](https://docs.sixlabors.com/articles/imagesharp.web/imageproviders.html?tabs=tabid-1%2Ctabid-1a))

Για παράδειγμα, για να αποθηκεύσετε τις εικόνες σας σε ένα δοχείο Azure Blob (handy for scaling) θα χρησιμοποιήσετε τον Azure Provider με AzureBlobCacheOptions:

```bash
dotnet add SixLabors.ImageSharp.Web.Providers.Azure
```

```csharp
// Configure and register the containers.  
// Alteratively use `appsettings.json` to represent the class and bind those settings.
.Configure<AzureBlobStorageImageProviderOptions>(options =>
{
    // The "BlobContainers" collection allows registration of multiple containers.
    options.BlobContainers.Add(new AzureBlobContainerClientOptions
    {
        ConnectionString = {AZURE_CONNECTION_STRING},
        ContainerName = {AZURE_CONTAINER_NAME}
    });
})
.AddProvider<AzureBlobStorageImageProvider>()
```

## ImageSharp.Web Usage

Τώρα που το έχουμε φτιάξει είναι πολύ απλό να το χρησιμοποιήσουμε μέσα στην αίτησή μας. Για παράδειγμα, αν θέλουμε να σερβίρουμε μια εικόνα μεγέθους θα μπορούσαμε να κάνουμε οποιαδήποτε χρήση [το TagHelper](https://sixlabors.com/posts/announcing-imagesharp-web-300/#imagetaghelper) ή να καθορίσετε άμεσα το URL.

TagHelper:

```razor
<img
    src="sixlabors.imagesharp.web.png"
    imagesharp-width="300"
    imagesharp-height="200"
    imagesharp-rmode="ResizeMode.Pad"
    imagesharp-rcolor="Color.LimeGreen" />

```

Παρατηρήστε ότι με αυτό επαναδιαμορφώνουμε την εικόνα, ρυθμίζοντας το πλάτος και το ύψος, και επίσης ρυθμίζοντας το RisizeMode και επαναχρωματίζοντας την εικόνα.

Σε αυτή την εφαρμογή ακολουθούμε τον απλούστερο τρόπο και απλά χρησιμοποιούμε παραμέτρους querystring. Για το markdown χρησιμοποιούμε μια επέκταση που μας επιτρέπει να καθορίσουμε το μέγεθος και τη μορφή της εικόνας.

```csharp
    public void ChangeImgPath(MarkdownDocument document)
    {
        foreach (var link in document.Descendants<LinkInline>())
            if (link.IsImage)
            {
                if(link.Url.StartsWith("http")) continue;
                
                if (!link.Url.Contains("?"))
                {
                   link.Url += "?format=webp&quality=50";
                }

                link.Url = "/articleimages/" + link.Url;
            }
               
    }
```

Αυτό μας δίνει τη δυνατότητα είτε τον προσδιορισμό αυτών στις θέσεις όπως

```markdown
![image](/image.jpg?format=webp&quality=50)
```

Από πού θα έρθει αυτή η εικόνα `wwwroot/articleimages/image.jpg` και να αυξηθεί σε 50% ποιότητα και σε μορφή webp.

Ή μπορούμε απλά να χρησιμοποιήσουμε την εικόνα όπως είναι και θα αναδιαμορφωθεί και θα μορφοποιηθεί όπως καθορίζεται στο ερώτημα.

## ΝτόκερCity name (optional, probably does not need a translation)

Σημειώστε το `cache` Φόρλντερ που χρησιμοποίησα παραπάνω, πρέπει να γραφτεί από την αίτηση. Αν χρησιμοποιείς τον Ντόκερ, πρέπει να βεβαιωθείς ότι είναι έτσι τα πράγματα.
Βλέπεις; [το προηγούμενο πόστο μου.](/blog/imagesharpwithdocker) για το πώς το χειρίζομαι αυτό χρησιμοποιώντας έναν χαρτογραφημένο όγκο.

## Συμπέρασμα

Όπως έχετε δει ImageSharp.Web μας δίνει μια μεγάλη δυνατότητα να αλλάξουμε μέγεθος και να διαμορφώσουμε εικόνες στις εφαρμογές μας ASP.NET Core. Είναι εύκολο να δημιουργηθεί και να χρησιμοποιηθεί και παρέχει πολλή ευελιξία στον τρόπο με τον οποίο μπορούμε να χειραγωγούμε τις εικόνες στις εφαρμογές μας.