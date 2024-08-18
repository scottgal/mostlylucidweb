# Savun puhdistaminen

## Pilvivapaat järjestelmät startup-yrityksille.

<!--category-- Clearing the smoke, introduction -->
<datetime class="hidden">2024-07-30T13:30</datetime>

Ensinnäkin en sano, että pilvi on jotenkin paha tai tarpeeton vain, että monille startupeille se voi olla tarpeetonta / kustannuksia
joko pää- tai dev / testijärjestelmäsi.

### Miksi käyttää pilvipalveluita?

1. Admin...tämä on tärkein syyni siihen, miksi pilvipalvelut voivat olla hyvä idea aloitteleville yrityksille *haluat vain saada järjestelmäsi käyntiin, sinulla on vähän devops-kokemusta, eikä sietokykyä seisokille.
2. Scaling - tätä käytetään liikaa erityisesti statups. *Ole realistinen mittasuhteesi / kasvusi suhteen*.
3. Compliance - on helpompaa ja nopeampaa saavuttaa täysi ISO 9001:2015 -standardi pilvipalvelussa (moni pitää [Azure tekee jo tällaista raportointia / testausta](https://learn.microsoft.com/en-us/azure/compliance/offerings/offering-iso-9001))

### Miksi emme käyttäisi pilvipalveluita?

1. Kustannukset - Kun järjestelmäsi saavuttaa tietynlaisen monimutkaisuuden, kustannukset voivat nousta pilviin. Jopa yksinkertaisista palveluista se, mitä maksat suoritustasosta, on hurjan ylihinnoiteltua esimerkiksi pilvipalveluissa.
   jos haluat ajaa ASP.NET-järjestelmää pilvessä 4 ytimellä, 7GB RAM & 10GB(!) Storage (katso myöhemmin, tästä hinnasta voit ostaa FULL Hetzner -palvelimen 5 kuukaudeksi!)

![img.png](img.png?width=500&format=webp)

2. Portability - kun rakentaa monimutkaisen järjestelmän (esimerkiksi Azure-pöydät, SQL-palvelimet, SQL-palvelimet jne.), voi käytännössä juuttua käyttämään näitä järjestelmiä ja maksamaan mitä tahansa Microsoft käskee.

3. Skillset - vaikka olisit välttänyt DevOps-roolin tiimissäsi oman järjestelmäsi hallinnoinnissa, tarvitset silti Azure-hallintataitoja Azure-järjestelmän suunnitteluun, rakentamiseen ja ylläpitoon. Tämä jätetään usein huomiotta valintojen yhteydessä.

Tässä "blogissa" (tunnen itseni niin vanhaksi) kerrotaan yksityiskohtaisesti, mitä sinun on tunnettava.NET-kehittäjänä, jotta pääset käyntiin jopa melko monimutkaisilla järjestelmillä omalla (käyttö) laitteistollasi.

Se kattaa monia tämänkaltaisen "bootstrap" -kehityksen osa-alueita Docker & Docker Composite -yhtiöltä, valitsee palveluita, konfiguroida järjestelmiä Caddyn, OpenSearchin, Postgresin, ASP.NETin, HTMX:n ja Alppien.js:n avulla.