# Ohjelmistokehittäjien stressitön haastattelu

<!--category-- Interviewing -->
<datetime class="hidden">2024-09-03T19:00</datetime>

# Johdanto

Yksi monista minua häiritsevistä LinkedInin näkökohdista on "miten haastatella kehittäjiä". Heitä on TON ja he ovat kolmessa leirissä:

1. Erytropoietiini [Miten liikuttaisit Fuji-vuorta?](https://amzn.to/3ZbvgBp) lähesty logiikan arvoituksia ja aivokiusaajia.
2. Katsotaan, kuinka paljon muistat tietokonetieteen tutkinnon lähestymistavasta algoritmisilla kysymyksillä.
3. "Katso, kun kirjoitat koodia" -lähestymistapa koodausharjoituksilla.

Olen vuosien varrella haastatellut kymmeniä kertoja ja palkannut satoja kehittäjiä eri yrityksille Microsoftista Delliin ja pieniin startup-yrityksiin. Olen myös oudosti ollut psykometriaan erikoistunut tutkimuspsykologi (mentaalisten kykyjen mittaustiede) ja ohjelmistokehittäjä. Olen nähnyt prosessin joka puolelta.

[TOC]

# Ongelma

Koodaus ei useinkaan ole sosiaalista toimintaa. Toki "pehmeät taidot" ovat elintärkeitä, mutta usein ne ovat ortogonaalisia käyttäjäongelmien ratkaisemiseksi käytettävän koodin kirjoittamisen käytännölle. Miten joku haastatellaan tehtävään, jossa on kyse lähinnä koodin kirjoittamisesta?

Ammattimme on täynnä myös huijarisyndroomaa. Olen nähnyt sen itsessäni ja monissa muissakin. Se on aito juttu. Miten voit haastatella huijaria, joka on jo alkanut tuntea itsensä huijariksi?

Ammattimme on täynnä yhteiskunnallisesti kiusallisia (ja kyllä minunlaiseni hieman autistisia) haastatteluja, jotka ovat sekä stressaavia että vaativat sosiaalista vuorovaikutusta koodausongelmien ratkaisemisen yhteydessä. Miten haastattelette ketään, joka on sosiaalisesti kiusallinen?

# Ratkaisu

Lue ensin heidän ansioluettelonsa; älä edes puhu heille, jos heidän ansioluettelonsa ei tee selväksi, että heillä on riittävästi kokemusta palkkaamaasi työpaikkaa varten. Tämä ei ole vain kunnioittavaa heidän aikaansa kohtaan, vaan myös teidän aikaanne.

Toiseksi haastattelun (tai haastattelujen) järjestämisen tulisi olla osallistujille mahdollisimman stressitöntä. Tämä tarkoittaa:

1. Anna riittävä varoitusaika. Älä anna haastattelua henkilölle, jolla on 24 tunnin varoitusaika (tai joka on huonompi samana päivänä).
2. Tee selväksi, mikä haastatteluformaatti on, kuka tulee olemaan mukana odotetuissa lopputuloksissa (onko se finaali, tekninen näyttö jne.).
3. LUKI HENKILÖN RESURSSI. En voi korostaa tätä tarpeeksi. Jos haastattelet jotakuta, sinun pitäisi tuntea hänen ansioluettelonsa läpikotaisin. Tämä ei ole vain kunnioittavaa, vaan se antaa myös mahdollisuuden kysyä heidän kokemuksistaan.
4. Varmista, että saat tiedot postissa; jos kyseessä on Teams / Zoom, varmista, että heillä on yhteys. Jos se on henkilökohtaisesti, varmista, että he tietävät, minne mennä.

## Haastattelu

OLE AJASSA; mikään ei saa ihmistä jännittymään enemmän kuin haastattelun alkamisen odottaminen. Jos myöhästyt, aloitat jo väärällä jalalla. Jos he myöhästyvät, anna heille muutama minuutti aikaa; todennäköisesti he eivät ole olleet takavuosien tapaamisissa, joten heidän asetelmansa voi olla sekava.

Näyttävätkö he ihmiseltä, joka sopisi joukkueeseen temperamenttisesti; sopivatko he joukkueeseen? Tämä on tärkeää, voit saada maailman parhaan koodaajan, mutta jos he ovat ääliöitä, he eivät ole sen arvoisia.

Yksi keksimäni vinkki (vuosien Fibonacci-sarjan kysymysten jälkeen, sarjojen kääntäminen, linkitetyt listat jne.) on.

**KOODIT TAPAHTUVAT KOODISTA, JOITA TIEDÄT TAPAHTUVAT**

Tämä tarkoittaa, että jos tekee teknisen arvion, haastattelun suorittavan henkilön täytyy pystyä puhumaan näkemästään koodista.
Jos se on kehys, jota et tiedä (kuten minä haastattelen Angular Devsiä), älä ole liikaa huolissasi.

Joten ennen kuin haastattelussa on tarpeeksi ilmoitusta (5 päivää on yleensä hyvä), kerro heille, että pyydät heitä puhumaan kirjoittamastaan koodista. En yleisesti ottaen pyydä GitHub-linkkiä (monilla edes vanhemmilla tasoilla ei välttämättä ole tällaista; olen työskennellyt monissa projekteissa, jotka ovat yksityisomistuksessa ja joita ei voi jakaa).

Tee selväksi, ettet pyydä isoa projektia tai jotain hämmästyttävän innovatiivista koodia. Se on vain koodi, josta he voivat puhua. Monilla ihmisillä on tällaisia perhejuttuja, todennäköisesti he eivät ole olleet mukana 365 päivän ajan suuren Open Source -projektin rahoittajana.

**Et palkkaa sen perusteella, kuinka paljon vapaa-aikaa jollakulla on**

## Miksi?

Miksi siis pidän tätä lähestymistapaa parempana? Miksi tämä on mielestäni parempi tapa haastatella kehittäjiä?

1. Se on vähemmän stressaavaa. Haastateltava puhuu jostain, minkä hän tietää. He eivät yritä ratkaista ongelmaa, jota eivät ole koskaan ennen nähneet, oikeastaan suurimman osan ajasta, jolloin koodaaminen huolestuttaa sitä, miten he kirjoittavat koodin, kun heille annetaan aikaa sen kirjoittamiseen.
   Useimmat meistä eivät kehitä hulluja algoritmeja tehdäkseen asioita, joita valintakehys jo tekee meille.
2. Voit arvioida, valehtelevatko he kokemuksestaan. Jos he eivät voi puhua kirjoittamastaan koodista, he eivät luultavasti kirjoittaneet sitä.
3. Koodia voi kaivella luontevammin; miksi he valitsivat sen lähestymistavan eri tavalla, miksi he eivät käyttäneet kirjastoa jne.
4. Näet koodin, jonka he haluavat näyttää sinulle. Tämä on iso juttu. Jos pyydät heitä kirjoittamaan koodin paikalla, näet koodin, jota he kirjoittavat paineen alla. Jos pyydät heitä puhumaan koodista, jonka he ovat kirjoittaneet, näet koodin, jonka he ovat kirjoittaneet, kun heillä ei ole paineita. Toistan vielä, jos työpaikkasi ei useimmiten ole korin takana, kun et kirjoita koodia paineen alla.

# Poikkeukset sääntöön

Tässä on siis poikkeuksia, superjuniorikoodaajat tarvitsevat joskus pientä koodausharjoitusta, mutta etene hitaasti. On julmaa pyytää heitä tulkitsemaan ja korjaamaan jokin valtava koodipohja.
Heille voit kysyä peruskäsitteistä, kuten silmukoista, ehdoista jne. (pidä se kohdistettuna työhön, johon olet palkkaamassa).
Mallit? Olen nähnyt paljon ihmisiä, jotka eivät osaa nimetä kaavaa, mutta voivat kertoa, milloin he ovat käyttäneet sitä. Älä siis innostu liikaa tästä.

Loogisia pulmia? En ole koskaan nähnyt näiden tarkoitusta. En ole koskaan nähnyt työtä, jossa pitäisi siirtää Fuji-vuorta. En ole koskaan nähnyt työtä, jossa pitäisi tietää, kuinka monta golfpalloa mahtuu 747:ään. En ole koskaan nähnyt työtä, jossa sinun pitäisi tietää, kuinka monta pianoääntä New Yorkissa on.

# Seuraa

Haastattelun jälkeen varmista, että seuraat ehdokasta. Jos he eivät saaneet työtä, kerro heille, miksi (et näyttänyt tarpeeksi kokemustasi, et ollut selvä, kun selitit koodiasi jne.).
Tämä ei ole vain kunnioittavaa, vaan se myös auttaa heitä edistymään seuraavaa haastattelua varten.
Jos he saivat työn, varmista, että he tietävät, mitä odottaa seuraavaksi.