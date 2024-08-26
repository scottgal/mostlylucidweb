# Συγκρότημα DockerName

<datetime class="hidden">2024-07-30T13:30</datetime>

<!--category-- Docker -->
Docker Compose είναι ένα εργαλείο για τον καθορισμό και την εκτέλεση πολλαπλών εφαρμογών Docker. Με τη Compose, χρησιμοποιείτε ένα αρχείο YAML για να ρυθμίσετε τις υπηρεσίες της εφαρμογής σας. Στη συνέχεια, με μια ενιαία εντολή, δημιουργείτε και ξεκινάτε όλες τις υπηρεσίες από τη διαμόρφωση σας.

Αυτή τη στιγμή χρησιμοποιώ Docker Compose για να εκτελέσω μερικές υπηρεσίες στο διακομιστή μου.

- Κυρίως διαυγής - το blog μου (αυτό)
- Cloudflared - μια υπηρεσία που οι σήραγγες μεταφέρουν στον διακομιστή μου
- Η Σκοπιά - μια υπηρεσία που ελέγχει τις ενημερώσεις των εμπορευματοκιβωτίων μου και τις επανεκκινεί αν είναι απαραίτητο.

Εδώ είναι το... `docker-compose.yml` αρχείο που χρησιμοποιώ για να εκτελώ αυτές τις υπηρεσίες:

```yaml
services:
  mostlylucid:
    image: scottgal/mostlylucid:latest
    labels:
        - "com.centurylinklabs.watchtower.enable=true"
  cloudflared:
    image: cloudflare/cloudflared:latest
    command: tunnel --no-autoupdate run --token ${CLOUDFLARED_TOKEN}
    env_file:
      - .env
        
  watchtower:
    image: containrrr/watchtower
    container_name: watchtower
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
    environment:
      - WATCHTOWER_CLEANUP=true
      - WATCHTOWER_LABEL_ENABLE=true
    command: --interval 300 # Check for updates every 300 seconds (5 minutes)
```