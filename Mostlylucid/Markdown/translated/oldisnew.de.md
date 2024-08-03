# Was alt ist, ist wieder neu

## Dev-Modelle für Web-Anwendungen

<datetime class="hidden">2024-07-30T13:30</datetime>

In meiner LONG (30 Jahre) Geschichte des Aufbaus von Web-Anwendungen gab es viele Möglichkeiten, eine Web-App zu bauen.

1. Pure HTML 1990-> - der allererste (wenn Sie BBS / Text basierte Systeme ignorieren) Mechanismus für den Aufbau von Web-Apps war Plain Old HTML. Aufbau einer Webseite, Liste einer Reihe von Produkten und eine E-Mail in Adresse, Telefonnummer oder sogar E-Mail, um Bestellungen zu senden.
   Dies hatte einige Vorteile und (viele) Nachteile.

- Zunächst war es einfach; Sie gaben gerade eine Liste von einer Reihe von Produkten, der Benutzer wählte, was sie wollten, schickte dann einen Scheck an die Adresse und wartete, um Ihre Waren zu bekommen
- Es schnell gerendert (wichtig in jenen Tagen, da die meisten Leute das Web über Modems zugegriffen, Sie sprechen *Kilobytes* In der zweiten Hälfte der 90er Jahre war die Zahl der Beschäftigten höher als in der zweiten Hälfte der 90er Jahre.
- Es war *fair* einfach zu aktualisieren. Sie würden nur die HTML-Datei aktualisieren und sie auf jeden Server hochladen, den Sie verwenden (mit FTP am häufigsten)
- Wie auch immer es SLOW war... der Postdienst ist nicht schnell, Schecks sind langsam zu Bargeld etc....

2. [CGI](https://webdevelopmenthistory.com/1993-cgi-scripts-and-early-server-side-web-programming/)  1993-> - wohl die erste „aktive" Technologie, die für das Netz verwendet wird. Sie würden entweder C (die erste Sprache, die ich benutzte) oder so etwas wie Perl verwenden, um HTML-Inhalte zu generieren

- Schließlich müssen Sie die Anfänge des'modernen' Webs nutzen, diese würden eine Vielzahl von 'Daten' Formaten verwenden, um Inhalte und später frühe Datenbanken zu halten, um das Niveau der Interaktion zu ermöglichen, das mit'modernen' Anwendungen vergleichbar ist.

- Sie waren komplex zu kodieren und zu aktualisieren. Diese waren CODE, während es in letzter Zeit Vorlagensprachen für die Ausgabe von HTML verwendet wurden, waren die immer noch nicht einfach.

- Nein *real* Debugging.

- In den frühen Tagen, während Sie Kreditkarten akzeptieren konnten, waren diese Transaktionen *relativ* unsicher und die frühen Zahlungs-Gateways waren immer noch ein bisschen wild-westlich.

3. Die „template" Sprachen (~1995->). Die Likes von PHP, ColdFusion und ja ASP (kein.net!) waren der Beginn der Zulassung "Rapid Development" für Web-Anwendungen.

- Sie waren relativ schnell zu aktualisieren (noch meistens mit FTP)
- Zu dieser Zeit SSL war weit verbreitet für E-Commerce-Websites angenommen, so dass Sie schließlich in der Lage, einigermaßen sicher Eingabe von Zahlungsdaten online.
- Datenbanken hatten begonnen zu reifen, so dass es nun möglich war, ein "richtiges" Datenbanksystem zum Umgang mit Produktdaten, Kundendaten etc. zu haben.
- Es schürte den ersten 'dotcom boom' - VIELE neue Websites und Geschäfte tauchten auf, viele scheiterten (MOST wirklich bis in die frühen 2000er Jahre), es war ein bisschen ein wilder Westen.

4. Die Neuzeit (2001->). Nach diesem ersten Ansturm von E-Commerce Aufregung mehr'reife' Web-Programmierung Frameworks begann zu erscheinen. Diese erlaubten die Verwendung von mehr etablierten Mustern und Ansätzen.

- [MVC](https://en.wikipedia.org/wiki/Model%E2%80%93view%E2%80%93controller) - das Modell-View-Controller-Muster. Dies war wirklich ein Weg, Code zu organisieren, der die Trennung von Verantwortlichkeiten in konsequente Segmente des Anwendungsdesigns ermöglichte. Meine erste Erfahrung davon war in den Tagen von J2EE & JSP.
- [RAD](https://en.wikipedia.org/wiki/Rapid_application_development) - Schnelle Anwendungsentwicklung. Wie der Name schon sagt, war dies auf 'Getting Zeug arbeiten' schnell konzentriert. Dies war der Ansatz, der in ASP.NET (Form 1999->) mit dem WebForms-Rahmen verfolgt wurde.