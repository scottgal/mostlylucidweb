# Αυτόματη μετάφραση αρχείων Markdown με EasyNMT

<datetime class="hidden">2024-08-03T13:30</datetime>

<!--category-- EasyNMT, Markdown -->
## Εισαγωγή

EasyNMT είναι μια τοπική υπηρεσία που παρέχει μια απλή διεπαφή σε μια σειρά από υπηρεσίες αυτόματης μετάφρασης. Σε αυτό το φροντιστήριο, θα χρησιμοποιήσουμε το EasyNMT για να μεταφράσουμε αυτόματα ένα αρχείο Markdown από τα αγγλικά σε πολλές γλώσσες.

Μπορείτε να βρείτε όλα τα αρχεία για αυτό το tutorial στο [Αποθετήριο GitHub](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/MarkdownTranslator) για αυτό το έργο.

Η παραγωγή αυτού δημιούργησε ένα BUNCH των νέων αρχείων markdown στις γλώσσες-στόχους. Αυτός είναι ένας πολύ απλός τρόπος για να πάρει μια δημοσίευση blog μεταφραστεί σε πολλές γλώσσες.

[Μεταφρασμένες Δημοσιεύσεις](/translatedposts.png)

[TOC]

## Προαπαιτούμενα

Μια εγκατάσταση του EasyNMT απαιτείται για να ακολουθήσει αυτό το φροντιστήριο. Συνήθως το εκτελώ ως υπηρεσία Ντόκερ. Μπορείτε να βρείτε τις οδηγίες εγκατάστασης [Ορίστε.](https://github.com/UKPLab/EasyNMT/blob/main/docker/README.md) που καλύπτει το πώς να το εκτελέσετε ως υπηρεσία docker.

```shell
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0-cpu
```

Ή εάν έχετε διαθέσιμο GPU NVIDIA:

```shell
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0.2-cuda11.3
```

Οι μεταβλητές περιβάλλοντος MAX_WORKERS_BACKEND και MAX_WORKERS_FRONTEND καθορίζουν τον αριθμό των εργαζομένων που θα χρησιμοποιήσει το EasyNMT. Μπορείτε να ρυθμίσετε αυτά για να ταιριάζει στο μηχάνημά σας.

Σημείωση: EasyNMT δεν είναι η SMOOTHEST υπηρεσία για να τρέξει, αλλά είναι το καλύτερο που έχω βρει για το σκοπό αυτό. Είναι λίγο persnickety σχετικά με τη συμβολοσειρά εισόδου που έχει περάσει, έτσι μπορεί να χρειαστεί να κάνετε κάποια προ-επεξεργασία του κειμένου εισόδου σας πριν το περάσετε στο EasyNMT.

Και επίσης μετέφρασε "Συμπλήρωση" σε κάποιες ανοησίες σχετικά με την υποβολή της πρότασης στην ΕΕ... προδίδοντας το σετ εκπαίδευσης.

## Αφελής Προσέγγιση για τη Φόρτωση Εξισορρόπησης

Easy NMT είναι ένα θηρίο δίψας όσον αφορά τους πόρους, έτσι στο MarkdownTranslatorService μου έχω ένα σούπερ απλό τυχαίο επιλογέα IP που μόλις περιστρέφεται μέσω της λίστας των IPs μιας λίστας των μηχανών που χρησιμοποιώ για να εκτελώ EasyNMT.

Αρχικά, αυτό κάνει ένα get on the `model_name` μέθοδος για την υπηρεσία EasyNMT, αυτός είναι ένας γρήγορος, απλός τρόπος για να ελέγξετε αν η υπηρεσία είναι επάνω. Εάν είναι, προσθέτει την IP σε μια λίστα των εργασιών IP. Αν δεν είναι, δεν το προσθέτει στη λίστα.

```csharp
    private string[] IPs = translateServiceConfig.IPs;
    public async ValueTask<bool> IsServiceUp(CancellationToken cancellationToken)
    {
        var workingIPs = new List<string>();

        try
        {
            foreach (var ip in IPs)
            {
                logger.LogInformation("Checking service status at {IP}", ip);
                var response = await client.GetAsync($"{ip}/model_name", cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    workingIPs.Add(ip);
                }
            }

            IPs = workingIPs.ToArray();
            if (!IPs.Any()) return false;
            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error checking service status");
            return false;
        }
    }
```

Στη συνέχεια, μέσα στο `Post` Τρόπος χορήγησης `MarkdownTranslatorService` Γυρίζουμε μέσα από τις IPs εργασίας για να βρούμε την επόμενη.

```csharp
          if(!IPs.Any())
            {
                logger.LogError("No IPs available for translation");
                throw new Exception("No IPs available for translation");
            }
            var ip = IPs[currentIPIndex];
            
            logger.LogInformation("Sending request to {IP}", ip);
        
            // Update the index for the next request
            currentIPIndex = (currentIPIndex + 1) % IPs.Length;
```

Αυτός είναι ένας πολύ απλός τρόπος για να φορτώσετε την ισορροπία των αιτημάτων σε μια σειρά από μηχανές. Δεν είναι τέλειο (δεν οφείλεται σε μια σούπερ πολυάσχολη μηχανή για exampel), αλλά είναι αρκετά καλό για τους σκοπούς μου.

Το ηλίθιο... ` currentIPIndex = (currentIPIndex + 1) % IPs.Length;` απλά περιστρέφεται μέσω της λίστας των IPs ξεκινώντας από το 0 και πηγαίνοντας στο μήκος της λίστας.

## Μετάφραση αρχείου Markdown

Αυτός είναι ο κωδικός που έχω στο αρχείο MarkdownTranslatorService.cs. Είναι μια απλή υπηρεσία που παίρνει μια συμβολοσειρά markdown και μια γλώσσα στόχου και επιστρέφει τη μεταφρασμένη συμβολοσειρά markdown.

```csharp
    public async Task<string> TranslateMarkdown(string markdown, string targetLang, CancellationToken cancellationToken)
    {
        var document = Markdig.Markdown.Parse(markdown);
        var textStrings = ExtractTextStrings(document);
        var batchSize = 10;
        var stringLength = textStrings.Count;
        List<string> translatedStrings = new();
        for (int i = 0; i < stringLength; i += batchSize)
        {
            var batch = textStrings.Skip(i).Take(batchSize).ToArray();
            translatedStrings.AddRange(await Post(batch, targetLang, cancellationToken));
        }


        ReinsertTranslatedStrings(document, translatedStrings.ToArray());
        return document.ToMarkdownString();
    }
```

Όπως μπορείτε να δείτε έχει μια σειρά από βήματα:

1. `  var document = Markdig.Markdown.Parse(markdown);` - Αυτό κατατάσσει τη χορδή σε ένα έγγραφο.
2. `  var textStrings = ExtractTextStrings(document);` - Αυτό αφαιρεί τις χορδές του κειμένου από το έγγραφο.
   Αυτό χρησιμοποιεί τη μέθοδο

```csharp
  private bool IsWord(string text)
    {
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".svg" };
        if (imageExtensions.Any(text.Contains)) return false;
        return text.Any(char.IsLetter);
    } 
```

Αυτό ελέγχει εάν η λέξη είναι πραγματικά ένα έργο? τα ονόματα εικόνας μπορούν να χαλάσουν την πρόταση διαίρεσης λειτουργικότητα στο EasyNMT.

3. `  var batchSize = 10;` - Αυτό καθορίζει το μέγεθος της παρτίδας για τη μεταφραστική υπηρεσία. EasyNMT έχει ένα όριο στον αριθμό των λέξεων που μπορεί να μεταφράσει σε μία πάει (περίπου 500, έτσι 10 γραμμές είναι γενικά ένα καλό μέγεθος παρτίδας εδώ).
4. `csharp await Post(batch, targetLang, cancellationToken)`
   Αυτό καλεί τη μέθοδο που στη συνέχεια αναρτά την παρτίδα στην υπηρεσία EasyNMT.

```csharp
    private async Task<string[]> Post(string[] elements, string targetLang, CancellationToken cancellationToken)
    {
        try
        {
            var postObject = new PostRecord(targetLang, elements);
            var response = await client.PostAsJsonAsync("/translate", postObject, cancellationToken);

            var phrase = response.ReasonPhrase;
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<PostResponse>(cancellationToken: cancellationToken);

            return result.translated;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error translating markdown: {Message} for strings {Strings}", e.Message, string.Concat( elements, Environment.NewLine));
            throw;
        }
    }
```

5. `  ReinsertTranslatedStrings(document, translatedStrings.ToArray());` - Αυτό επαναφέρει τις μεταφρασμένες χορδές πίσω στο έγγραφο. Χρησιμοποιώντας την ικανότητα του MarkDig να περπατάει το έγγραφο και να αντικαθιστά τις συμβολοσειρές κειμένου.

```csharp

    private void ReinsertTranslatedStrings(MarkdownDocument document, string[] translatedStrings)
    {
        int index = 0;

        foreach (var node in document.Descendants())
        {
            if (node is LiteralInline literalInline && index < translatedStrings.Length)
            {
                var content = literalInline.Content.ToString();
         
                if (!IsWord(content)) continue;
                literalInline.Content = new Markdig.Helpers.StringSlice(translatedStrings[index]);
                index++;
            }
        }
    }
```

## Hosted Service

Για να τρέξω όλα αυτά χρησιμοποιώ ένα IHostedLifetimeService το οποίο ξεκίνησε στο αρχείο Program.cs. Αυτή η υπηρεσία διαβάζει σε ένα αρχείο markdown, το μεταφράζει σε διάφορες γλώσσες και γράφει τα μεταφρασμένα αρχεία έξω στο δίσκο.

```csharp
  public async Task StartedAsync(CancellationToken cancellationToken)
    {
        if(!await blogService.IsServiceUp(cancellationToken))
        {
            logger.LogError("Translation service is not available");
            return;
        }
        ParallelOptions parallelOptions = new() { MaxDegreeOfParallelism = blogService.IPCount, CancellationToken = cancellationToken};
        var files = Directory.GetFiles(markdownConfig.MarkdownPath, "*.md");

        var outDir = markdownConfig.MarkdownTranslatedPath;

        var languages = translateServiceConfig.Languages;
        foreach(var language in languages)
        {
            await Parallel.ForEachAsync(files, parallelOptions, async (file,ct) =>
            {
                var fileChanged = await file.IsFileChanged(outDir);
                var outName = Path.GetFileNameWithoutExtension(file);

                var outFileName = $"{outDir}/{outName}.{language}.md";
                if (File.Exists(outFileName) && !fileChanged)
                {
                    return;
                }

                var text = await File.ReadAllTextAsync(file, cancellationToken);
                try
                {
                    logger.LogInformation("Translating {File} to {Language}", file, language);
                    var translatedMarkdown = await blogService.TranslateMarkdown(text, language, ct);
                    await File.WriteAllTextAsync(outFileName, translatedMarkdown, cancellationToken);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error translating {File} to {Language}", file, language);
                }
            });
        }
       
```

Όπως μπορείτε να δείτε, ελέγχει επίσης το χασίς του αρχείου για να δείτε αν έχει αλλάξει πριν από τη μετάφραση του. Αυτό είναι για να αποφύγετε τη μετάφραση αρχείων που δεν έχουν αλλάξει.

Αυτό γίνεται με τον υπολογισμό ενός γρήγορου χασίς του αρχικού αρχείου markdown στη συνέχεια δοκιμή για να δείτε αν αυτό το αρχείο έχει αλλάξει πριν από την προσπάθεια να το μεταφράσει.

```csharp
    private static async Task<string> ComputeHash(string filePath)
    {
        await using var stream = File.OpenRead(filePath);
        stream.Position = 0;
        var bytes = new byte[stream.Length];
        await stream.ReadAsync(bytes);
        stream.Position = 0;
        var hash = XxHash64.Hash(bytes);
        var hashString = Convert.ToBase64String(hash);
        hashString = InvalidCharsRegex.Replace(hashString, "_");
        return hashString;
    }
```

Η ρύθμιση στο πρόγραμμα.cs είναι αρκετά απλή:

```csharp

    builder.Services.AddHostedService<BackgroundTranslateService>();
services.AddHttpClient<MarkdownTranslatorService>(options =>
{
    options.Timeout = TimeSpan.FromMinutes(15);
});
```

Εγκατέστησα το HostedService (BackgroundTranslateService) και το HttpClient για το MarkdownTranslatorService.
Μια Hosted Service είναι μια μακροχρόνια υπηρεσία που τρέχει στο παρασκήνιο. Είναι ένα καλό μέρος για να τοποθετήσετε υπηρεσίες που πρέπει να τρέχει συνεχώς στο παρασκήνιο ή απλά να πάρει λίγο χρόνο για να ολοκληρωθεί. Το νέο IHostedLifetimeService interface είναι λίγο πιο ευέλικτο από το παλιό IHostedService interface και μας επιτρέπει να εκτελούμε τις εργασίες εντελώς στο παρασκήνιο πιο εύκολα από το παλαιότερο IHostedService.

Εδώ μπορείτε να δείτε ότι ρυθμίζω το χρονοδιάγραμμα για το HttpClient σε 15 λεπτά. Αυτό συμβαίνει επειδή το EasyNMT μπορεί να είναι λίγο αργό να ανταποκριθεί (ιδιαίτερα την πρώτη φορά που χρησιμοποιεί ένα μοντέλο γλώσσας). Θέτω επίσης τη βασική διεύθυνση στη διεύθυνση IP του μηχανήματος που εκτελεί την υπηρεσία EasyNMT.

## Συμπέρασμα

Αυτός είναι ένας πολύ απλός τρόπος για να μεταφράσει ένα αρχείο markdown σε πολλές γλώσσες. Δεν είναι τέλειο, αλλά είναι μια καλή αρχή. Γενικά τρέχω αυτό για κάθε νέο blog post και χρησιμοποιείται στο `MarkdownBlogService` για να τραβήξει τα μεταφρασμένα ονόματα για κάθε δημοσίευση blog.