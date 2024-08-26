# Χρήση Umami για τοπική αναλυτική

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-08T15:53</datetime>

## Εισαγωγή

Ένα από τα πράγματα που με ενόχλησε σχετικά με την τρέχουσα ρύθμιση μου ήταν ότι έπρεπε να χρησιμοποιήσω το Google Analytics για να πάρω δεδομένα επισκεπτών (πόσο λίγο υπάρχει από αυτό;). Οπότε ήθελα να βρω κάτι που να μπορώ να αυτο-ξεχωρίσω που να μην δίνει δεδομένα στο Google ή σε οποιοδήποτε άλλο τρίτο μέρος. Βρήκα... [ΟυμάμιCity name (optional, probably does not need a translation)](https://umami.is/) η οποία είναι μια απλή, αυτο-φιλοξενούμενη λύση ανάλυσης ιστού. Είναι μια μεγάλη εναλλακτική λύση για το Google Analytics και είναι (σχετικά) εύκολο να συσταθεί.

[TOC]

## Εγκατάσταση

Η εγκατάσταση είναι πολύ απλή αλλά πήρε ένα δίκαιο κομμάτι βιολί για να...

### Συγκρότημα DockerName

Καθώς ήθελα να προσθέσω Umami στην τρέχουσα Docker-σύνθεση ρύθμιση μου έπρεπε να προσθέσω μια νέα υπηρεσία μου `docker-compose.yml` Φάκελος. Πρόσθεσα τα ακόλουθα στο κάτω μέρος του αρχείου:

```yaml
  umami:
    image: ghcr.io/umami-software/umami:postgresql-latest
    env_file: .env
    environment:
      DATABASE_URL: ${DATABASE_URL}
      DATABASE_TYPE: ${DATABASE_TYPE}
      HASH_SALT: ${HASH_SALT}
      APP_SECRET: ${APP_SECRET}
      TRACKER_SCRIPT_NAME: getinfo
      API_COLLECT_ENDPOINT: all
    ports:
      - "3000:3000"
    depends_on:
      - db
    networks:
      - app_network
    restart: always
  db:
    image: postgres:16-alpine
    env_file:
      - .env
    networks:
      - app_network
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER}"]
      interval: 5s
      timeout: 5s
      retries: 5
    volumes:
      - /mnt/umami/postgres:/var/lib/postgresql/data
    restart: always
  cloudflaredumami:
    image: cloudflare/cloudflared:latest
    command: tunnel --no-autoupdate run --token ${CLOUDFLARED_UMAMI_TOKEN}
    env_file:
      - .env
    restart: always
    networks:
      - app_network


```

Αυτό το αρχείο Docker-compose.yml περιέχει την ακόλουθη ρύθμιση:

1. Μια νέα υπηρεσία που ονομάζεται `umami` που χρησιμοποιεί το `ghcr.io/umami-software/umami:postgresql-latest` Εικόνα. Αυτή η υπηρεσία χρησιμοποιείται για τη λειτουργία της υπηρεσίας ανάλυσης Umami.
2. Μια νέα υπηρεσία που ονομάζεται `db` που χρησιμοποιεί το `postgres:16-alpine` Εικόνα. Αυτή η υπηρεσία χρησιμοποιείται για να τρέξει τη βάση δεδομένων Postgres που Umami χρησιμοποιεί για να αποθηκεύσει τα δεδομένα της.
   Σημείωση για αυτή την υπηρεσία είμαι χαρτογραφημένος σε έναν κατάλογο στο διακομιστή μου έτσι ώστε τα δεδομένα επιμένουν μεταξύ επανεκκίνησης.

```yaml
    volumes:
      - /mnt/umami/postgres:/var/lib/postgresql/data
```

Θα πρέπει αυτός ο σκηνοθέτης να υπάρχει και να είναι γραμμένος από το χρήστη docker στον διακομιστή σας (και πάλι δεν είναι ειδικός Linux έτσι το 777 είναι πιθανώς υπερβολή εδώ!).

```shell
chmod 777 /mnt/umami/postgres
```

3. Μια νέα υπηρεσία που ονομάζεται `cloudflaredumami` που χρησιμοποιεί το `cloudflare/cloudflared:latest` Εικόνα. Αυτή η υπηρεσία χρησιμοποιείται για τη σήραγγα της υπηρεσίας Umami μέσω Cloudflare για να επιτρέψει την πρόσβαση από το διαδίκτυο.

### Αρχείο Env

Για να υποστηρίξω αυτό, ενημέρωσα επίσης το `.env` αρχείο που περιλαμβάνει τα ακόλουθα:

```shell
CLOUDFLARED_UMAMI_TOKEN=<cloudflaretoken>
DATABASE_TYPE=postgresql
HASH_SALT=<salt>

POSTGRES_DB=postgres
POSTGRES_USER=<postgresuser>
POSTGRES_PASSWORD=<postgrespassword>
UMAMI_SECRET=<umamisecret>

APP_SECRET=${UMAMI_SECRET}
UMAMI_USER=${POSTGRES_USER}
UMAMI_PASS=${POSTGRES_PASSWORD}
DATABASE_URL=postgresql://${UMAMI_USER}:${UMAMI_PASS}@db:5432/${POSTGRES_DB}
```

Αυτό δημιουργεί τη διαμόρφωση για το Docker συνθέτουν (το `<>` elemets προφανώς χρειάζεται αντικατάσταση με τις δικές σας αξίες). Η `cloudflaredumami` υπηρεσία χρησιμοποιείται για τη σήραγγα της υπηρεσίας Umami μέσω Cloudflare για να επιτρέψει την πρόσβαση από το διαδίκτυο. Είναι πιθανό να χρησιμοποιηθεί ένα BASE_PATH αλλά για Umami χρειάζεται ενοχλητικά μια ανοικοδόμηση για να αλλάξει το βασικό μονοπάτι, έτσι έχω αφήσει ως το βασικό μονοπάτι προς το παρόν.

### Σήραγγα Cloudflare

Για τη δημιουργία της σήραγγας cloudflare για αυτό (η οποία λειτουργεί ως το μονοπάτι για το αρχείο js που χρησιμοποιείται για την ανάλυση - getinfo.js) Χρησιμοποίησα ιστοσελίδα:

![Σήραγγα Cloudflare](umamisetup.png)

Αυτό δημιουργεί τη σήραγγα στην υπηρεσία Umami και επιτρέπει την πρόσβαση από το διαδίκτυο. Σημειωτέον, δείχνω αυτό στο `umami` υπηρεσία στο docker-compose αρχείο (όπως είναι στο ίδιο δίκτυο με το cloudflared σήραγγα είναι ένα έγκυρο όνομα).

### Ρύθμιση Umami στη σελίδα

Για να καταστεί δυνατή η διαδρομή για το σενάριο (καλείται `getinfo` στο setup μου παραπάνω) Έχω προσθέσει μια εισαγωγή config στις ρυθμίσεις μου

```json
 "Analytics":{
   "UmamiPath" : "https://umamilocal.mostlylucid.net/getinfo",
   "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee"
 },
```

Μπορείτε επίσης να προσθέσετε αυτά στο αρχείο.env σας και να τα περάσετε ως μεταβλητές περιβάλλοντος στο αρχείο Docker-compose.

```shell
ANALYTICS__UMAMIPATH="https://umamilocal.mostlylucid.net/getinfo"
ANALYTICS_WEBSITEID="32c2aa31-b1ac-44c0-b8f3-ff1f50403bee"
```

```yaml
  mostlylucid:
    image: scottgal/mostlylucid:latest
    ports:
      - 8080:8080
    restart: always
    environment:
    ...
      - Analytics__UmamiPath=${ANALYTICS_UMAMIPATH}
      - Analytics__WebsiteId=${ANALYTICS_WEBSITEID}
```

Μπορείτε να ρυθμίσετε το WebsiteId στο ταμπλό Umami όταν ρυθμίσετε το site. (Σημειώστε το προκαθορισμένο όνομα χρήστη και τον κωδικό πρόσβασης για την υπηρεσία Umami είναι `admin` και `umami`, θα πρέπει να τα αλλάξετε αυτά μετά την εγκατάσταση).
![Umami DashboardCity name (optional, probably does not need a translation)](umamiaddwebsite.png)

Με τις σχετικές ρυθμίσεις cs αρχείο:

```csharp
public class AnalyticsSettings : IConfigSection
{
    public static string Section => "Analytics";
    public string? UmamiPath { get; set; }
}
```

Και πάλι αυτό χρησιμοποιεί τα πράγματά μου από την POCO ([Ορίστε.](/blog/addingidentityfreegoogleauth#configuring-google-auth-with-poco)) για να ρυθμίσετε τις ρυθμίσεις.
Ρυθμίστε το στο πρόγραμμά μου.cs:

```csharp
builder.Configure<AnalyticsSettings>();
```

ΚΑΙ επιτέλους στο... `BaseController.cs` `OnGet` μέθοδος Έχω προσθέσει τα ακόλουθα για να καθορίσει το μονοπάτι για το σενάριο analytics:

```csharp
   public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        if (!Request.IsHtmx())
        {
            ViewBag.UmamiPath = _analyticsSettings.UmamiPath;
            ViewBag.UmamiWebsiteId = _analyticsSettings.WebsiteId;
        }
        base.OnActionExecuting(filterContext);
    }
    
```

Αυτό καθορίζει την πορεία για το σενάριο ανάλυσης που θα χρησιμοποιηθεί στο αρχείο διάταξης.

### Αρχείο ρύθμισης

Τέλος, έχω προσθέσει τα ακόλουθα στο αρχείο διάταξης μου για να συμπεριλάβει το σενάριο ανάλυσης:

```html
<script defer src="@ViewBag.UmamiPath" data-website-id="@ViewBag.UmamiWebsiteId"></script>
```

Αυτό περιλαμβάνει το σενάριο στη σελίδα και καθορίζει την ταυτότητα της ιστοσελίδας για την υπηρεσία ανάλυσης.

## Εκτός από τον εαυτό σας από την αναλυτική

Για να αποκλείσετε τις δικές σας επισκέψεις από τα δεδομένα ανάλυσης μπορείτε να προσθέσετε την ακόλουθη τοπική αποθήκευση στο πρόγραμμα περιήγησής σας:

Στα εργαλεία Chrome dev (Ctrl+Shift+I στα παράθυρα) μπορείτε να προσθέσετε τα ακόλουθα στην κονσόλα:

```javascript
localStorage.setItem("umami.disabled", 1)
```

## Συμπέρασμα

Αυτό ήταν ένα κομμάτι από ένα faff για να συσταθεί αλλά είμαι ευχαριστημένος με το αποτέλεσμα. Τώρα έχω μια αυτο-φιλοξενούμενη υπηρεσία ανάλυσης που δεν μεταδίδει δεδομένα στο Google ή σε οποιοδήποτε άλλο τρίτο μέρος. Είναι λίγο μπελάς να το φτιάξεις, αλλά μόλις το κάνεις είναι πολύ εύκολο να το χρησιμοποιήσεις. Είμαι ευχαριστημένος με το αποτέλεσμα και θα το συνιστούσα σε όποιον ψάχνει για μια αυτο-φιλοξενούμενη λύση ανάλυσης.