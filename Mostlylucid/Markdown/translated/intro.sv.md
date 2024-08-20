# Rengöring av röken

## Molnfria system för startups.

<!--category-- Clearing the smoke, introduction -->
<datetime class="hidden">2024-07-30T13:30 Ordförande</datetime>

Först säger jag INTE att molnet på något sätt är ont eller onödigt bara att för många startups kan det vara onödigt / kostnad för
antingen ditt huvud- eller dev / testsystem.

### Varför använda molnbaserade tjänster?

1. Admin... detta är min främsta anledning till varför molntjänster kan vara en bra idé för startups *Du vill bara få igång ditt system, du har lite devops erfarenhet och ingen tolerans för stilleståndstid.
2. Skalning - detta är överanvänt särskilt för statups. *Var realistisk om din skala / tillväxt*.
3. Efterlevnad - det är enklare och snabbare att nå full efterlevnad enligt ISO 9001:2015 när du kör i molnet (många som [Azure gör redan denna typ av rapportering / testning](https://learn.microsoft.com/en-us/azure/compliance/offerings/offering-iso-9001))

### Varför inte använda molnbaserade tjänster?

1. Kostnad - när ditt system når ny typ av komplexitet dina kostnader kan börja skjuta i höjden. Även för enkla tjänster vad du betalar verus vad du får i form av prestanda är vilt överpriser i molnet till exempel
   om du vill köra ett ASP.NET-system i molnet med 4 kärnor, 7GB RAM & 10GB(!) lagring (se senare, för detta pris kan du köpa en FULL Hetzner server för 5 månader!)

![img.png](img.png?width=500&format=webp)

2. Portabilitet - när du bygger ett komplext system (säg, med Azure Bord, Storage Quues, SQL Server etc) kan du i huvudsak fastna med dessa system och betala vad Microsoft dikterar.

3. Skillset - även om du har undvikit att ha en DevOps roll i ditt team för att administrera ditt eget system behöver du fortfarande Azure hantera färdigheter för att designa, bygga och underhålla ett Azure-system. Detta förbises ofta när man gör valet.

Denna "blogg" (jag känner mig så gammal) kommer att specificera vad du behöver veta som en.NET-utvecklare för att komma igång med även ganska komplexa system på egen (användbarhet) hårdvara.

Det kommer att täcka många aspekter av denna typ av "bootstrap" utveckling från Docker & Docker Komposiera, välja tjänster, konfigurera system med Caddy, OpenSearch, Postgres, ASP.NET, HTMX och Alpine.js