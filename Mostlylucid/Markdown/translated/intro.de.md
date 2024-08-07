# Den Rauch säubern

## Cloud-freie Systeme für Startups.

<!--category-- Clearing the smoke, introduction -->
<datetime class="hidden">2024-07-30T13:30</datetime>

Zuerst sage ich NICHT, dass die Cloud irgendwie böse oder unnötig ist, nur dass es für viele Startups unnötig sein kann.
entweder Ihre Haupt- oder dev / Testsysteme.

### Warum Cloud-basierte Dienste nutzen?

1. Admin...das ist mein Hauptgrund, warum Cloud-Dienste eine gute Idee für Startups sein könnten*Sie wollen nur Ihr System zum Laufen bringen, Sie haben wenig Devops Erfahrung und keine Toleranz für Ausfallzeiten.
2. Scaling - das wird vor allem für Statups überbewertet.*Seien Sie realistisch über Ihre Skala / Wachstum*.
3. Compliance - es ist einfacher und schneller, die vollständige ISO 9001:2015-Compliance zu erreichen, wenn sie in der Cloud läuft (viele wie[Azure macht diese Art der Berichterstattung / Prüfung bereits](https://learn.microsoft.com/en-us/azure/compliance/offerings/offering-iso-9001))

### Warum nicht Cloud-basierte Dienste nutzen?

1. Kosten - sobald Ihr System ny Art von Komplexität erreicht Ihre Kosten beginnen können, zu skyrocket. Selbst für einfache Dienstleistungen, was Sie zahlen verus, was Sie in Bezug auf Leistung erhalten, ist wild überteuert in der Cloud zum Beispiel
   wenn Sie ein ASP.NET System in der Cloud mit 4 Kernen, 7GB RAM & 10GB(!) Speicher betreiben möchten (siehe später, für diesen Preis können Sie einen FULL Hetzner Server für 5 Monate kaufen!)

![img.png](img.png?width=500&format=webp)

2. Portabilität - wenn Sie ein komplexes System (z.B. Azure Tabellen, Storage Queues, SQL Server etc.) erstellen, können Sie im Wesentlichen mit diesen Systemen stecken bleiben und bezahlen, was auch immer Microsoft diktiert.

3. Skillset - auch wenn Sie vermieden haben, eine DevOps-Rolle in Ihrem Team für die Verwaltung Ihres eigenen Systems zu haben, benötigen Sie trotzdem Azure-Management-Fähigkeiten, um ein Azure-System zu entwerfen, zu bauen und zu pflegen.

Dieses 'Blog' (ich fühle mich so alt) wird detailliert, was Sie als.NET-Entwickler wissen müssen, um aufstehen und laufen mit sogar ziemlich komplexen Systemen auf eigene (Utility) Hardware.

Es wird viele Aspekte dieser "Bootstrap"-Entwicklung von Docker & Docker Compose abdecken, die Dienstleistungen auswählen, Systeme mit Caddy, OpenSearch, Postgres, ASP.NET, HTMX und Alpine.js konfigurieren.