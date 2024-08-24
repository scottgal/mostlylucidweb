# Het opruimen van de rook

## Cloud gratis systemen voor startups.

<!--category-- Clearing the smoke, introduction -->
<datetime class="hidden">2024-07-30T13:30</datetime>

Eerst zeg ik NIET dat de cloud is op de een of andere manier kwaadaardig of onnodig gewoon dat voor veel startups kan het onnodig / uitgeven voor
uw hoofd- of dev-/testsystemen.

### Waarom cloudgebaseerde diensten gebruiken?

1. Admin...dit is mijn belangrijkste reden waarom cloud services misschien een goed idee zijn voor startups *Je wilt gewoon je systeem aan de praat krijgen, je hebt weinig devops ervaring en geen tolerantie voor downtime.
2. Schalen - dit wordt vooral gebruikt voor standbeelden. *Wees realistisch over uw schaal / groei*.
3. Compliance - het is gemakkelijker en sneller om volledige ISO 9001:2015 naleving te bereiken bij het draaien in de cloud (veel van [Azure doet al dit soort rapportage / testen](https://learn.microsoft.com/en-us/azure/compliance/offerings/offering-iso-9001))

### Waarom geen cloud-gebaseerde diensten gebruiken?

1. Kosten - zodra uw systeem bereikt ny soort complexiteit uw kosten kunnen beginnen te stijgen. Zelfs voor eenvoudige diensten wat je betaalt verus wat je krijgt in termen van prestaties is wild te duur in de cloud bijvoorbeeld
   als u een ASP.NET-systeem wilt draaien in de cloud met 4 kernen, 7GB RAM & 10GB(!) opslag (zie later, voor deze prijs kunt u een FULL Hetzner server kopen voor 5 maanden!)

![img.png](img.png?width=500&format=webp)

2. Portabiliteit - als je eenmaal een complex systeem bouwt (met behulp van Azure Tables, Storage Queues, SQL Server etc) kun je in wezen vastzitten met behulp van deze systemen & betalen wat Microsoft dicteert.

3. Skillset - zelfs als u hebt voorkomen dat een DevOps rol in uw team voor het beheer van uw eigen systeem hebt u nog steeds Azure management vaardigheden nodig om een Azure-systeem te ontwerpen, bouwen en onderhouden. Dit wordt vaak over het hoofd gezien bij het maken van de keuze.

Deze 'blog' (ik voel me zo oud) zal detailleren wat je moet weten als een.NET Developer om op te staan en te draaien met zelfs vrij complexe systemen op je eigen (utility) hardware.

Het zal betrekking hebben op vele aspecten van dit soort 'bootstrap' ontwikkeling van Docker & Docker Compose, het selecteren van diensten, het configureren van systemen met behulp van Caddy, OpenSearch, Postgres, ASP.NET, HTMX en Alpine.js