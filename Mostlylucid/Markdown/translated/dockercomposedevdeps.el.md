# Χρήση Docker Compose για την ανάπτυξη εξαρτήσεων

<!--category-- Docker -->
<datetime class="hidden">2024-08-09T17:17</datetime>

# Εισαγωγή

Κατά την ανάπτυξη λογισμικού παραδοσιακά θα περιστρέψουμε μια βάση δεδομένων, μια ουρά μηνυμάτων, μια κρύπτη, και ίσως μερικές άλλες υπηρεσίες. Αυτό μπορεί να είναι ένας πόνος για να διαχειριστεί, ειδικά αν εργάζεστε σε πολλαπλά έργα. Docker Compose είναι ένα εργαλείο που σας επιτρέπει να καθορίσετε και να εκτελέσετε εφαρμογές πολλαπλών οχημάτων Docker. Είναι ένας πολύ καλός τρόπος για να διαχειριστείς τις αναπτυξιακές σου εξαρτήσεις.

Σε αυτή τη θέση, θα σας δείξω πώς να χρησιμοποιήσετε Docker Compose για να διαχειριστείτε την ανάπτυξη εξαρτήσεων σας.

[TOC]

# Προαπαιτούμενα

Πρώτα θα πρέπει να εγκαταστήσετε docker desktop σε οποιαδήποτε πλατφόρμα χρησιμοποιείτε. Μπορείτε να το κατεβάσετε από [Ορίστε.](https://www.docker.com/products/docker-desktop).

**ΣΗΜΕΙΩΣΗ: Έχω διαπιστώσει ότι στα Windows θα πρέπει πραγματικά να τρέξει Docker Desktop εγκαταστάτης ως διαχειριστής για να εξασφαλίσει ότι εγκαθιστά σωστά.**

# Δημιουργία αρχείου Compose Docker

Docker Compose χρησιμοποιεί ένα αρχείο YAML για να καθορίσει τις υπηρεσίες που θέλετε να εκτελέσετε. Εδώ είναι ένα παράδειγμα ενός απλού `devdeps-docker-compose.yml` αρχείο που ορίζει μια υπηρεσία βάσης δεδομένων και μια υπηρεσία ηλεκτρονικού ταχυδρομείου:

```yaml
services: 
  smtp4dev:
    image: rnwood/smtp4dev
    ports:
      - "3002:80"
      - "2525:25"
    volumes:
      # This is where smtp4dev stores the database..
      - e:/smtp4dev-data:/smtp4dev
    restart: always
  postgres:
    image: postgres:16-alpine
    container_name: postgres
    ports:
      - "5432:5432"
    env_file:
      - .env
    volumes:
      - e:/data:/var/lib/postgresql/data  # Map e:\data to the PostgreSQL data folder
    restart: always	
networks:
  mynetwork:
        driver: bridge
```

Σημειώστε εδώ Έχω καθορίσει τόμους για την επιμονή των δεδομένων για κάθε υπηρεσία, εδώ έχω καθορίσει

```yaml
    volumes:
      # This is where smtp4dev stores the database..
      - e:/smtp4dev-data:/smtp4dev
    volumes:
        - e:/data:/var/lib/postgresql/data  # Map e:\data to the PostgreSQL data folder
```

Με τον τρόπο αυτό διασφαλίζεται ότι τα δεδομένα παραμένουν μεταξύ των ροών των εμπορευματοκιβωτίων.

(Το Σώμα εγκρίνει την πρόταση ψηφίσματος) `env_file` για την `postgres` Υπηρεσία. Αυτό είναι ένα αρχείο που περιέχει μεταβλητές περιβάλλοντος που περνούν στο δοχείο.
Μπορείτε να δείτε μια λίστα με τις μεταβλητές περιβάλλοντος που μπορούν να περάσουν στο δοχείο PostgreSQL [Ορίστε.](https://www.docker.com/blog/how-to-use-the-postgres-docker-official-image/#1-Environment-variables).
Ορίστε ένα παράδειγμα... `.env` αρχείο:

```shell
POSTGRES_DB=postgres
POSTGRES_USER=postgres
POSTGRES_PASSWORD=<somepassword>
```

Αυτό ρυθμίζει μια προεπιλεγμένη βάση δεδομένων, τον κωδικό πρόσβασης και το χρήστη για PostgreSQL.

Εδώ εκτελώ επίσης την υπηρεσία SMTP4Dev, αυτό είναι ένα μεγάλο εργαλείο για τη δοκιμή της λειτουργίας ηλεκτρονικού ταχυδρομείου στην εφαρμογή σας. Μπορείτε να βρείτε περισσότερες πληροφορίες σχετικά με αυτό [Ορίστε.](https://github.com/rnwood/smtp4dev/wiki/Installation#how-to-run-smtp4dev-in-docker).

Αν κοιτάξεις μέσα μου... `appsettings.Developmet.json` αρχείο θα δείτε Έχω τις ακόλουθες ρυθμίσεις για το διακομιστή SMTP:

```json
  "SmtpSettings":
{
"Server": "localhost",
"Port": 2525,
"SenderName": "Mostlylucid",
"Username": "",
"SenderEmail": "scott.galloway@gmail.com",
"Password": "",
"EnableSSL": "false",
"EmailSendTry": 3,
"EmailSendFailed": "true",
"ToMail": "scott.galloway@gmail.com",
"EmailSubject": "Mostlylucid"

}
```

Αυτό λειτουργεί για SMTP4Dev και μου επιτρέπει να δοκιμάσω αυτή τη λειτουργικότητα (μπορώ να στείλω σε οποιαδήποτε διεύθυνση, και να δω το email στη διεπαφή SMTP4Dev στο http://localhost:3002/).

Μόλις είστε σίγουροι ότι όλα λειτουργούν μπορείτε να δοκιμάσετε σε ένα πραγματικό διακομιστή SMTP όπως GMAIL (π.χ., δείτε [Ορίστε.](addingasyncsendingforemails) για το πώς να το κάνουμε αυτό)

# Εκτέλεση των υπηρεσιών

Για την εκτέλεση των υπηρεσιών που ορίζονται στο `devdeps-docker-compose.yml` αρχείο, θα πρέπει να εκτελέσετε την ακόλουθη εντολή στον ίδιο κατάλογο με το αρχείο:

```shell
docker compose -f .\devdeps-docker-compose.yml up -d
```

Σημειώστε ότι θα πρέπει να το τρέξει αρχικά έτσι? Αυτό εξασφαλίζει μπορείτε να δείτε τα στοιχεία config περάσει από το `.env` Φάκελος.

```shell
docker compose -f .\devdeps-docker-compose.yml config
```

Τώρα αν κοιτάξετε στο Docker Desktop μπορείτε να δείτε αυτές τις υπηρεσίες που λειτουργούν

![Επιφάνεια εργασίας DockerName](dockerdesktopdev.png)