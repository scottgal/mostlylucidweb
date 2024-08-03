# Wat oud is, is weer nieuw.

## Dev modellen voor webtoepassingen

<datetime class="hidden">2024-07-30T13:30</datetime>

In mijn LONG (30 jaar) geschiedenis van het bouwen van webapplicaties zijn er vele manieren geweest om een webapp te bouwen.

1. Pure HTML 1990-> - het allereerste (als je BBS / tekst gebaseerde systemen negeert) mechanisme voor het bouwen van webapps was gewone oude HTML. Het bouwen van een webpagina, een lijst van een aantal producten en een mail in adres, telefoonnummer of zelfs e-mail om bestellingen naar te sturen.
   Dit had een paar voordelen en (veel) nadelen.

- Eerst was het eenvoudig; je gaf gewoon een lijst van een aantal producten, de gebruiker selecteerde wat ze wilden en stuurde vervolgens een cheque naar het adres en wachtte om uw goederen te krijgen
- Het maakte snel (belangrijk in die dagen als de meeste mensen toegang tot het web via modems, je praat*kilobytesunit synonyms for matching user input*per seconde).
- Het was...*redelijk*eenvoudig te updaten. Je zou gewoon het HTML-bestand updaten en uploaden naar welke server je ook gebruikt (gebruik FTP meestal)
- Maar het was SLOW...de postdienst is niet snel, cheques zijn traag naar contant geld etc...

2. [CGI](https://webdevelopmenthistory.com/1993-cgi-scripts-and-early-server-side-web-programming/)1993-> - misschien wel de eerste 'actieve' technologie gebruikt voor het web. Je zou ofwel C (de eerste taal die ik gebruikte) of iets als Perl gebruiken om HTML-inhoud te genereren

- Je moet eindelijk gebruik maken van het begin van het'moderne' web, deze zouden gebruik maken van een verscheidenheid van 'data' formaten om inhoud en later vroege databases te houden om het niveau van interactie vergelijkbaar met'moderne' toepassingen mogelijk te maken.

- Ze waren complex om te coderen en te updaten. Dit waren CODE, terwijl er later gesjableerde talen werden gebruikt om HTML uit te voeren die nog steeds niet eenvoudig waren.

- Nee*echt*Debuggen.

- In de vroege dagen, terwijl u creditcards kon accepteren deze transacties waren*relatief*onzeker en de vroege betaling gateways waren nog steeds een beetje wild-west.

3. De 'template' talen (~1995->). Zoals PHP, ColdFusion en ja ASP (no.net!) waren het begin van het toestaan van 'Rapid Development' voor webtoepassingen.

- Ze waren relatief snel te updaten (nog steeds meestal met behulp van FTP)
- Tegen deze tijd SSL was op grote schaal aangenomen voor e-commerce sites, zodat je eindelijk in staat om redelijk veilig het invoeren van betaling gegevens online.
- Databanken waren begonnen te rijpen, dus het was nu mogelijk om een 'juist' databasesysteem te hebben om productgegevens, klantgegevens etc. te verwerken.
- Het voedde de eerste 'dotcom boom' - veel nieuwe websites en winkels dook op, velen faalden (MOST echt tegen het begin van de jaren 2000) het was een beetje een wild westen.

4. De moderne tijd (2001->). Na deze eerste drukte van e-commerce-opwinding kwamen er meer 'volwassen' webprogrammerende kaders tevoorschijn die het gebruik van meer gevestigde patronen en benaderingen mogelijk maakten.

- [MVC](https://en.wikipedia.org/wiki/Model%E2%80%93view%E2%80%93controller)- de Model-View-Controller patroon. Dit was echt een manier van het organiseren van code waardoor de scheiding van verantwoordelijkheden in cognitieve segmenten van toepassing ontwerp. Mijn eerste ervaring van dit was terug in de tijd van J2EE & JSP.
- [RAD](https://en.wikipedia.org/wiki/Rapid_application_development)- Snelle Application Development. Zoals de naam al doet vermoeden was dit gericht op het snel aan de slag krijgen van spullen. Dit was de aanpak die in ASP.NET (form 1999->) met het WebForms-kader werd gevolgd.