# Self Hosting Seq για ASP.NET Logging

<datetime class="hidden">2024-08-28T09:37</datetime>

<!--category-- ASP.NET, Seq, Serilog, Docker -->
# Εισαγωγή

Seq είναι μια εφαρμογή που σας επιτρέπει να δείτε και να αναλύσετε τα αρχεία καταγραφής. Είναι ένα εξαιρετικό εργαλείο για την αποσφαλμάτωση και την παρακολούθηση της αίτησής σας. Σε αυτή τη θέση θα καλύψω το πώς έφτιαξα το Seq για να καταγράψω την εφαρμογή ASP.NET Core.
Ποτέ δεν μπορεί να έχει πάρα πολλά ταμπλό:)

![SeqDashboardCity name (optional, probably does not need a translation)](seqdashboard.png)

[TOC]

# Ρύθμιση Seq

Ο Seq έρχεται σε μερικές γεύσεις. Μπορείτε είτε να χρησιμοποιήσετε την έκδοση σύννεφο ή να το φιλοξενήσετε μόνος σας. Επέλεξα να το φιλοξενήσω μόνος μου, καθώς ήθελα να κρατήσω τα ημερολόγια μου κρυφά.

Πρώτα άρχισα να επισκέπτομαι την ιστοσελίδα Seq και να βρίσκω το [Οδηγίες εγκατάστασης Docker](https://docs.datalust.co/docs/getting-started-with-docker).

## Τοπικά

Για να τρέξετε τοπικά πρέπει πρώτα να πάρετε έναν κωδικό πρόσβασης hashed. Μπορείτε να το κάνετε αυτό εκτελώντας την ακόλουθη εντολή:

```bash
echo '<password>' | docker run --rm -i datalust/seq config hash
```

Για να το εκτελέσετε τοπικά μπορείτε να χρησιμοποιήσετε την ακόλουθη εντολή:

```bash
docker run --name seq -d --restart unless-stopped -e ACCEPT_EULA=Y -e SEQ_FIRSTRUN_ADMINPASSWORDHASH=<hashfromabove> -v C:\seq:/data -p 5443:443 -p 45341:45341 -p 5341:5341 -p 82:80 datalust/seq

```

Στο Ubuntu τοπικό μου μηχάνημα έκανα αυτό σε ένα σενάριο:

```shell
#!/bin/bash
PH=$(echo 'Abc1234!' | docker run --rm -i datalust/seq config hash)

mkdir -p /mnt/seq
chmod 777 /mnt/seq

docker run \
  --name seq \
  -d \
  --restart unless-stopped \
  -e ACCEPT_EULA=Y \
  -e SEQ_FIRSTRUN_ADMINPASSWORDHASH="$PH" \
  -v /mnt/seq:/data \
  -p 5443:443 \
  -p 45341:45341 \
  -p 5341:5341 \
  -p 82:80 \
  datalust/seq
```

Τότε...

```shell
chmod +x seq.sh
./seq.sh
```

Αυτό θα σε σηκώσει και θα σε βάλει σε λειτουργία και μετά θα πας στο `http://localhost:82` / `http://<machineip>:82` για να δείτε την επόμενή σας εγκατάσταση (προκαθορισμένο κωδικό πρόσβασης διαχειριστή είναι αυτό που εισάγετε για <password> Πάνω.

## Στο Ντόκερ.

Πρόσθεσα συνέχεια στο Docker συνθέτουν αρχείο μου ως εξής:

```docker
  seq:
    image: datalust/seq
    container_name: seq
    restart: unless-stopped
    environment:
      ACCEPT_EULA: "Y"
      SEQ_FIRSTRUN_ADMINPASSWORDHASH: ${SEQ_DEFAULT_HASH}
    volumes:
      - /mnt/seq:/data
    networks:
      - app_network
```

Σημειώστε ότι έχω έναν κατάλογο που ονομάζεται `/mnt/seq` (για τα παράθυρα, χρησιμοποιήστε ένα μονοπάτι παραθύρων). Εδώ θα αποθηκευτούν τα αρχεία καταγραφής.

Έχω κι εγώ ένα... `SEQ_DEFAULT_HASH` μεταβλητή περιβάλλοντος που είναι ο κωδικός πρόσβασης hashed για το χρήστη admin στο αρχείο.env μου.

# Ρύθμιση πυρήνα ASP.NET

Όπως χρησιμοποιώ [ΣεριλόγκCity name (optional, probably does not need a translation)](https://serilog.net/) Για την καταγραφή μου είναι στην πραγματικότητα αρκετά εύκολο να συσταθεί Seq. Έχει ακόμα και γιατρούς για το πώς να το κάνουμε αυτό [Ορίστε.](https://docs.datalust.co/docs/using-serilog).

Βασικά απλά προσθέτετε το νεροχύτη στο έργο σας:

```shell
dotnet add package Serilog.Sinks.Seq
```

Προτιμώ να χρησιμοποιώ `appsettings.json` για την κατάθεσή μου, έτσι απλά έχω το "τυπικό" στη διάθεσή μου `Program.cs`:

```csharp
builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
    Serilog.Debugging.SelfLog.Enable(Console.Error);
    Console.WriteLine($"Serilog Minimum Level: {configuration.MinimumLevel.ToString()}");
});
```

Στη συνέχεια, στα uppsettings μου.json' Έχω αυτή τη διαμόρφωση

```json
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File", "Serilog.Sinks.Seq"],
    "MinimumLevel": "Warning",
    "WriteTo": [
        {
          "Name": "Seq",
          "Args":
          {
            "serverUrl": "http://seq:5341",
            "apiKey": ""
          }
        },
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/applog-.txt",
          "rollingInterval": "Day"
        }
      }

    ],
    "Enrich": ["FromLogContext", "WithMachineName"],
    "Properties": {
      "ApplicationName": "mostlylucid"
    }
  }

```

Θα δεις ότι έχω... `serverUrl` της `http://seq:5341`. Αυτό συμβαίνει επειδή έχω συνέχεια τρέχει σε ένα δοχείο docker ονομάζεται `seq` Και είναι στο λιμάνι. `5341`. Εάν είστε τρέχει σε τοπικό επίπεδο μπορείτε να χρησιμοποιήσετε `http://localhost:5341`.
Χρησιμοποιώ επίσης το κλειδί API ώστε να μπορώ να χρησιμοποιήσω το κλειδί για να καθορίσω το επίπεδο καταγραφής δυναμικά (μπορείτε να ορίσετε ένα κλειδί για να αποδεχτείτε μόνο ένα συγκεκριμένο επίπεδο των μηνυμάτων καταγραφής).

Μπορείτε να το ρυθμίσετε στην επόμενή σας περίπτωση, πηγαίνοντας στο `http://<machine>:82` και κάνοντας κλικ στις ρυθμίσεις γρανάζι στο επάνω δεξί μέρος. Στη συνέχεια, κάντε κλικ στο `API Keys` καρτέλα και να προσθέσετε ένα νέο κλειδί. Στη συνέχεια, μπορείτε να χρησιμοποιήσετε αυτό το κλειδί σε σας `appsettings.json` Φάκελος.

![SeqCity name (optional, probably does not need a translation)](seqapikey.png)

# Συγκρότημα DockerName

Τώρα έχουμε αυτή τη ρύθμιση πρέπει να ρυθμίσουμε την εφαρμογή ASP.NET μας για να πάρει ένα κλειδί. Χρησιμοποιώ ένα... `.env` αρχείο για να αποθηκεύσω τα μυστικά μου.

```dotenv
SEQ_DEFAULT_HASH="<adminpasswordhash>"
SEQ_API_KEY="<apikey>"
```

Στη συνέχεια, στο docker συνθέτουν αρχείο μου διευκρινίζω ότι η τιμή θα πρέπει να ενεθεί ως μια μεταβλητή περιβάλλοντος στην εφαρμογή ASP.NET μου:

```docker
services:
  mostlylucid:
    image: scottgal/mostlylucid:latest
    ports:
      - 8080:8080
    restart: always
    labels:
        - "com.centurylinklabs.watchtower.enable=true"
    env_file:
      - .env
    environment:
      - Auth__GoogleClientId=${AUTH_GOOGLECLIENTID}
      - Auth__GoogleClientSecret=${AUTH_GOOGLECLIENTSECRET}
      - Auth__AdminUserGoogleId=${AUTH_ADMINUSERGOOGLEID}
      - SmtpSettings__UserName=${SMTPSETTINGS_USERNAME}
      - SmtpSettings__Password=${SMTPSETTINGS_PASSWORD}
      - Analytics__UmamiPath=${ANALYTICS_UMAMIPATH}
      - Analytics__WebsiteId=${ANALYTICS_WEBSITEID}
      - ConnectionStrings__DefaultConnection=${POSTGRES_CONNECTIONSTRING}
      - TranslateService__ServiceIPs=${EASYNMT_IPS}
      - Serilog__WriteTo__0__Args__apiKey=${SEQ_API_KEY}
    volumes:
      - /mnt/imagecache:/app/wwwroot/cache
      - /mnt/markdown/comments:/app/Markdown/comments
      - /mnt/logs:/app/logs
    networks:
      - app_network
```

Σημειώστε ότι η `Serilog__WriteTo__0__Args__apiKey` ορίζεται στην τιμή του `SEQ_API_KEY` από την `.env` Φάκελος. Το "0" είναι ο δείκτης του `WriteTo` Συστοιχία στην `appsettings.json` Φάκελος.

# Κάντι...

Σημείωση τόσο για Seq και μου ASP.NET εφαρμογή έχω καθορίσει ότι και οι δύο ανήκουν στο μου `app_network` δίκτυο. Αυτό συμβαίνει επειδή χρησιμοποιώ την Κάντι σαν αντίστροφο πληρεξούσιο και είναι στο ίδιο δίκτυο. Αυτό σημαίνει ότι μπορώ να χρησιμοποιήσω το όνομα υπηρεσίας ως URL στο Caddyfile μου.

```caddy
{
    email scott.galloway@gmail.com
}
seq.mostlylucid.net
{
   reverse_proxy seq:80
}

http://seq.mostlylucid.net
{
   redir https://{host}{uri}
}
```

Έτσι, αυτό είναι σε θέση να χαρτογραφήσει `seq.mostlylucid.net` Στην επόμενή μου περίπτωση.

# Συμπέρασμα

Seq είναι ένα μεγάλο εργαλείο για την καταγραφή και την παρακολούθηση της εφαρμογής σας. Είναι εύκολο να εγκαταστήσετε και να χρησιμοποιήσετε και ενσωματώνει καλά με Serilog. Το βρήκα ανεκτίμητο στην αποσφαλμάτωση των εφαρμογών μου και είμαι σίγουρος ότι θα το κάνεις κι εσύ.