# Sgombrare il fumo

## Sistemi Cloud free per startup.

<!--category-- Clearing the smoke, introduction -->
<datetime class="hidden">2024-07-30T13:30</datetime>

In primo luogo non sto dicendo che la nuvola è in qualche modo male o inutile solo che per molte startup può essere inutile / spese per
sia il vostro principale o dev / sistemi di prova.

### Perché utilizzare i servizi basati su cloud?

1. Admin...questo è il mio primo motivo per cui i servizi cloud potrebbero essere una buona idea per le startup *Vuoi solo far funzionare il tuo sistema, hai poca esperienza di devop e nessuna tolleranza per i tempi di inattività.
2. Scala - questo è sovrautilizzato soprattutto per le statups. *Sii realistico sulla tua scala / crescita*.
3. Compliance - è più facile e veloce raggiungere la piena conformità ISO 9001:2015 quando si esegue nel cloud (molti come [Azure già fare questo tipo di reporting / test](https://learn.microsoft.com/en-us/azure/compliance/offerings/offering-iso-9001))

### Perché non utilizzare i servizi basati su cloud?

1. Costo - una volta che il sistema raggiunge ny tipo di complessità i costi possono iniziare a salire alle stelle. Anche per i servizi semplici ciò che si paga Verus ciò che si ottiene in termini di prestazioni è estremamente costoso nel cloud per esempio
   se si desidera eseguire un sistema ASP.NET nel cloud con 4 core, 7GB di RAM & 10GB(!) storage (vedi più avanti, per questo prezzo è possibile acquistare un server FULL Hetzner per 5 mesi!)

![img.png](img.png?width=500&format=webp)

2. Portabilità - una volta che si costruisce un sistema complesso (diciamo, usando Azure Tables, Coda di archiviazione, SQL Server ecc) si può essenzialmente essere bloccati utilizzando questi sistemi e pagando ciò che Microsoft detta.

3. Skillset - anche se hai evitato di avere un ruolo DevOps nel tuo team per la gestione del tuo sistema avrai ancora bisogno di abilità di gestione Azure per progettare, costruire e mantenere un sistema Azure. Questo è spesso trascurato quando si fa la scelta.

Questo 'blog' (mi sento così vecchio) dettaglierà quello che dovete sapere come sviluppatore.NET per alzarsi e funzionare con sistemi anche abbastanza complessi sul proprio hardware (utility).

Coprirà molti aspetti di questo tipo di sviluppo "bootstrap" da Docker & Docker Componi, seleziona i servizi, configura i sistemi utilizzando Caddy, OpenSearch, Postgres, ASP.NET, HTMX e Alpine.js