# Vanha on taas uutta

## Web-sovellusten Dev-mallit

<datetime class="hidden">2024-07-30T13:30</datetime>

Kauan kestäneessä (30 vuoden) verkkosovellusten rakennushistoriassani on ollut monia tapoja rakentaa verkkosovellus.

1. Puhtaat HTML 1990-> - ensimmäinen (jos sivuutat BBS- ja tekstipohjaiset järjestelmät) mekanismi verkkosovellusten rakentamiseksi oli Plain Old HTML. Rakentaa web-sivun, listata joukon tuotteita ja lähettää sähköpostia osoitteeseen, puhelinnumeroon tai jopa sähköpostia tilausten lähettämiseen.
   Tällä oli muutamia etuja ja (monia) haittoja.

- Ensinnäkin se oli yksinkertaista. Annoit vain listan tuotteista, käyttäjä valitsi mitä halusi, ja lähetti sitten shekin osoitteeseen ja odotti, että saisit tavarasi.
- Se palautui nopeasti (tärkeää niinä päivinä, kun useimmat ihmiset pääsivät verkkoon modeemien kautta, puhut *kilotavuja* Sekuntia kohti).
- Niin oli. *reilusti* suoraviivaista päivittämistä. Päivität vain HTML-tiedoston ja lataat sen sille palvelimelle, jota käytit (useimmin FTP:n kautta)
- Mutta se oli SLOW...posti ei ole nopeaa, sekit ovat hitaita käteistä jne....

2. [CGI](https://webdevelopmenthistory.com/1993-cgi-scripts-and-early-server-side-web-programming/)  1993-> - epäilemättä ensimmäinen verkossa käytetty "aktiivinen" teknologia. Käyttäisit joko C:tä (ensimmäistä kieltä, jota käytin) tai jotain Perlin kaltaista HTML-sisällön tuottamiseen

- Lopulta pääsit käyttämään "modernin" verkon alkua, nämä käyttäisivät erilaisia "dataformaatteja" pitääkseen sisältöä ja viime aikoina varhaisempia tietokantoja, jotta vuorovaikutuksen taso olisi verrattavissa "moderneihin" sovelluksiin.

- Ne olivat monimutkaisia koodata ja päivittää. Nämä olivat koodia, kun taas viime aikoina HTML:n tuottamisessa käytettiin mallikieliä, jotka eivät olleet yksinkertaisia.

- Ei tarvitse. *Real* Vianetsintä.

- Alkuaikoina, kun pystyit hyväksymään luottokortteja, nämä tapahtumat olivat *suhteellisesti* epävarmat ja varhaiset maksuväylät olivat vielä vähän villiä länttä.

3. "Template" -kielet (~1995->). PHP:n, ColdFusionin ja kyllä ASP:n kaltaiset (no.net!) Ne olivat alku sille, että verkkosovelluksille sallittiin "nopea kehitys".

- Ne päivitettiin suhteellisen nopeasti (edelleen enimmäkseen FTP:n avulla)
- SSL:stä oli tähän mennessä tullut laajalle levinnyt verkkokauppasivusto, joten pystyit vihdoin olemaan kohtuullisen turvallinen syöttäessäsi maksutiedot verkkoon.
- Tietokannat olivat alkaneet kypsyä, joten nyt oli mahdollista saada "asianmukainen" tietokanta, joka käsittelee tuotetietoja, asiakastietoja jne.
- Se vauhditti ensimmäistä "dotcom-buumia" - monet uudet verkkosivut ja myymälät ilmestyivät, monet epäonnistuivat (MOST todella 2000-luvun alussa), se oli hieman villi länsi.

4. Nykyaika (2001->). Tämän ensimmäisen verkkokauppahuuman jälkeen alkoi näkyä "kypsempiä" verkko-ohjelmointikehyksiä. Ne mahdollistivat vakiintuneempien mallien ja lähestymistapojen käytön.

- [MVC](https://en.wikipedia.org/wiki/Model%E2%80%93view%E2%80%93controller) - Model-View-Controller -malli. Tämä oli todella tapa järjestää koodi, joka mahdollisti vastuunjaon kookkaisiin sovellusmuotoilun osiin. Ensimmäinen kokemukseni tästä oli J2EE & JSP:n aikoihin.
- [RAD](https://en.wikipedia.org/wiki/Rapid_application_development) - Nopeaa soveltamista. Nimi viittaa siihen, että tässä keskityttiin siihen, että asiat saadaan toimimaan nopeasti. Tätä lähestymistapaa noudatettiin ASP.NETissä (lomake 1999->) WebForms-puitteiden yhteydessä.