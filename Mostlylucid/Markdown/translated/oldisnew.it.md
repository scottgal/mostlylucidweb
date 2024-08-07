# Cio' che e' vecchio e' nuovo di nuovo.

## Modelli Dev per applicazioni web

<datetime class="hidden">2024-07-30T13:30</datetime>

Nel mio LONG (30 anni) storia della costruzione di applicazioni web ci sono stati molti modi per costruire una web app.

1. Pure HTML 1990-> - il primo meccanismo (se si ignorano i sistemi basati su BBS / testo) per la costruzione di applicazioni web è stato Plain Old HTML. Costruire una pagina web, elencare un gruppo di prodotti e fornire una mail in indirizzo, numero di telefono o anche e-mail per inviare ordini a.
   Questo ha avuto alcuni vantaggi e (molti) svantaggi.

- In primo luogo era semplice; hai appena dato una lista di un gruppo di prodotti, l'utente ha selezionato quello che volevano poi inviato un assegno all'indirizzo e ha aspettato per ottenere la vostra merce
- Ha reso rapidamente (importante in quei giorni come la maggior parte delle persone hanno accesso al web su modem, si sta parlando*kilobyte*al secondo).
- E' stato...*abbastanza*semplice da aggiornare. Basta aggiornare il file HTML e caricarlo su qualsiasi server si stesse utilizzando (utilizzando FTP più comunemente)
- Tuttavia era SLOW... il servizio di posta non è veloce, gli assegni sono lenti a incassare ecc...

2. [CGICity name (optional, probably does not need a translation)](https://webdevelopmenthistory.com/1993-cgi-scripts-and-early-server-side-web-programming/)1993-> - probabilmente la prima tecnologia 'attiva' utilizzata per il web. Si userebbe sia C (la prima lingua che ho usato) o qualcosa come Perl per generare contenuti HTML

- È finalmente possibile utilizzare gli inizi del web'moderno', questi utilizzerebbero una varietà di formati 'dati' per contenere contenuti e, infine, i primi database per consentire il livello di interazione paragonabile alle applicazioni'moderne'.

- Erano complessi da codificare e aggiornare. Questi erano CODE, mentre infine ci sono stati linguaggi templated usati per l'uscita HTML l'ancora non erano semplici.

- No.*reale*Debugging.

- Nei primi giorni, mentre si poteva accettare carte di credito queste transazioni erano*relativamente*Insicuro e i primi gateway di pagamento erano ancora un po 'di un selvaggio-ovest.

3. I linguaggi 'template' (~1995->). I simili di PHP, ColdFusion e sì ASP (no.net!) sono stati l'inizio di consentire 'rapido sviluppo' per le applicazioni web.

- Sono stati relativamente veloci da aggiornare (ancora per lo più utilizzando FTP)
- A questo punto SSL era diventato ampiamente adottato per i siti di e-commerce in modo da essere finalmente in grado di essere ragionevolmente sicuro inserendo i dettagli di pagamento online.
- Le banche dati avevano iniziato a maturare quindi era ora possibile avere un sistema di banche dati "proprio" per gestire i dati dei prodotti, dei dati dei clienti, ecc.
- Ha alimentato il primo 'boom dotcom' - sono spuntati molti nuovi siti web e negozi, molti falliti (Più realmente nei primi anni 2000) è stato un po 'di un west selvaggio.

4. L'era moderna (2001->).A seguito di questa prima corsa di emozione e-commerce più'maturo' i quadri di programmazione web hanno cominciato a comparire. Essi hanno permesso l'uso di modelli e approcci più consolidati.

- [MVC](https://en.wikipedia.org/wiki/Model%E2%80%93view%E2%80%93controller)- il modello Model-View-Controller. Questo è stato davvero un modo per organizzare un codice che permettesse la separazione delle responsabilità in segmenti cogenti di progettazione delle applicazioni. La mia prima esperienza di questo è stata ai tempi di J2EE & JSP.
- [RADCity name (optional, probably does not need a translation)](https://en.wikipedia.org/wiki/Rapid_application_development)- Rapido sviluppo delle applicazioni. Come suggerisce il nome questo è stato focalizzato sul 'ottenere roba che funziona' rapidamente. Questo è stato l'approccio seguito in ASP.NET (formulario 1999->) con il framework WebForms.