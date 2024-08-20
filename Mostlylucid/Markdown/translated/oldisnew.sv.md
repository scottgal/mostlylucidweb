# Det gamla är nytt igen.

## Dev-modeller för webbapplikationer

<datetime class="hidden">2024-07-30T13:30 Ordförande</datetime>

I min LÅNG (30 år) historia av att bygga webbapplikationer har det funnits många sätt att bygga en webbapp.

1. Ren HTML 1990-> - den allra första (om du ignorerar BBS / textbaserade system) mekanismen för att bygga webbappar var Plain Old HTML. Bygga en webbsida, lista en massa produkter och ge en e-post i adress, telefonnummer eller ens e-post att skicka beställningar till.
   Detta hade några fördelar och (många) nackdelar.

- För det första var det enkelt; du gav bara en lista över en massa produkter, användaren valde vad de ville sedan skickade en check till adressen och väntade på att få dina varor
- Det återges snabbt (viktig på den tiden som de flesta människor kom åt webben över modem, du pratar *KVÄVNADER* per sekund).
- Det var jag. *rättvist* Enkelt att uppdatera. Du skulle bara uppdatera HTML-filen och ladda upp den till vilken server du än använder (med FTP oftast)
- Men det var SLOW... posttjänsten är inte snabb, checkar är långsamma till kontanter etc...

2. [CGI-uppgifter](https://webdevelopmenthistory.com/1993-cgi-scripts-and-early-server-side-web-programming/)  1993-> - tveklöst den första "aktiva" tekniken som används för webben. Du skulle använda antingen C (det första språket jag använde) eller något som Perl för att generera HTML-innehåll

- Du fick äntligen använda början av den "moderna" webben, dessa skulle använda en mängd olika "data" format för att hålla innehåll och senare tidiga databaser för att tillåta graden av interaktion jämförbar med "moderna" program.

- De var komplicerade att koda och uppdatera. Dessa var CODE, medan senare det fanns mallade språk som används för att mata ut HTML var det fortfarande inte enkelt.

- Ej tillämpligt *äkta* Avlusning.

- I början medan du kunde acceptera kreditkort dessa transaktioner var *relativt* Osäkra och de tidiga betalningsportarna var fortfarande lite av en vild väst.

3. "Template"-språken (~1995->). De gillar PHP, ColdFusion och ja ASP (no.net!) var början på att tillåta "Rapid Development" för webbapplikationer.

- De var relativt snabba att uppdatera (fortfarande mest med FTP)
- Vid den här tiden SSL hade blivit allmänt antagen för e-handel webbplatser så att du äntligen kunde vara någorlunda säker att ange betalningsuppgifter online.
- Databaser hade börjat mogna så det var nu möjligt att ha ett "riktigt" databassystem för att hantera produktdata, kunddata etc.
- Det underblåste den första "dotcom boom" - MÅNGA nya webbplatser och butiker dök upp, många misslyckades (MOST verkligen i början av 2000-talet) det var lite av en vild väst.

4. Den moderna eran (2001->). Efter denna första rusning av e-handel började mer "mogna" webbprogrammeringsramar dyka upp. Dessa gjorde det möjligt att använda mer etablerade mönster och metoder.

- [MVC](https://en.wikipedia.org/wiki/Model%E2%80%93view%E2%80%93controller) - Modell-View-Controller-mönstret. Detta var i själva verket ett sätt att organisera en kod som gjorde det möjligt att dela upp ansvaret i cogent-segment av tillämpningsdesign. Min första erfarenhet av detta var tillbaka på J2EE & JSP:s tid.
- [RISKVÄGT EXPONERINGSBELOPP](https://en.wikipedia.org/wiki/Rapid_application_development) - Snabb applikationsutveckling. Som namnet antyder var detta inriktat på att få saker att fungera snabbt. Detta var det tillvägagångssätt som användes i ASP.NET (blankett 1999->) med ramen för WebForms.