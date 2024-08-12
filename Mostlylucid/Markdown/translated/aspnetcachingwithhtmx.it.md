# ASP.NET Core Caching con HTMX

<!--category-- ASP.NET, HTMX -->
<datetime class="hidden">2024-08-12T00:50</datetime>

## Introduzione

La cache è una tecnica importante sia per migliorare l'esperienza dell'utente caricando i contenuti più velocemente che per ridurre il carico sul server. In questo articolo vi mostrerò come utilizzare le funzionalità di cache integrate di ASP.NET Core con HTMX per la cache dei contenuti sul lato client.

[TOC]

## Configurazione

In ASP.NET Core, ci sono due tipi di Caching offerti

- Response Cache - Si tratta di dati che vengono memorizzati sul client o in server procy intermedi (o entrambi) e vengono utilizzati per nascondere l'intera risposta per una richiesta.
- Output Cache - Si tratta di dati che vengono memorizzati sul server e utilizzati per nascondere l'output di un'azione controller.

Per impostare questi in ASP.NET Core è necessario aggiungere un paio di servizi nel vostro`Program.cs`file

### Caching di risposta

```csharp
builder.Services.AddResponseCaching();
app.UseResponseCaching();
```

### Caching di output

```csharp
services.AddOutputCache();
app.UseOutputCache();
```

## Caching di risposta

Mentre è possibile impostare Response Caching nel vostro`Program.cs`è spesso un po 'inflessibile (soprattutto quando si utilizzano le richieste HTMX come ho scoperto). È possibile impostare Response Caching nelle azioni del controller utilizzando il`ResponseCache`attributo.

```csharp
        [ResponseCache(Duration = 300, VaryByHeader = "hx-request", VaryByQueryKeys = new[] {"page", "pageSize"}, Location = ResponseCacheLocation.Any)]
```

Questo cacherà la risposta per 300 secondi e varierà la cache dal`hx-request`intestazione e la`page`e`pageSize`parametri di query. Stiamo anche impostando il`Location`a`Any`che significa che la risposta può essere memorizzata sul client, sui server proxy intermedi, o entrambi.

Ecco...`hx-request`header è l'intestazione che HTMX invia ad ogni richiesta. Questo è importante in quanto consente di nascondere la risposta in modo diverso in base al fatto che si tratti di una richiesta HTMX o di una richiesta normale.

Questa è la nostra corrente.`Index`Yo ucan vedere che accettiamo una pagina e pageSize parametro qui e abbiamo aggiunto questi come varia per chiavi di query nel`ResponseCache`attributo. Significa che le risposte sono "indexate" da queste chiavi e memorizzano contenuti diversi in base a queste.

In azione abbiamo anche`if(Request.IsHtmx())`Questo è basato sulla[Pacchetto HDMX.Net](https://github.com/khalidabuhakmeh/Htmx.Net)ed essenzialmente controlli per lo stesso`hx-request`intestazione che stiamo usando per variare la cache. Qui restituiamo una vista parziale se la richiesta è da HTMX.

```csharp
    public async Task<IActionResult> Index(int page = 1,int pageSize = 5)
    {
            var authenticateResult = GetUserInfo();
            var posts =await blogService.GetPosts(page, pageSize);
            posts.LinkUrl= Url.Action("Index", "Home");
            if (Request.IsHtmx())
            {
                return PartialView("_BlogSummaryList", posts);
            }
            var indexPageViewModel = new IndexPageViewModel
            {
                Posts = posts, Authenticated = authenticateResult.LoggedIn, Name = authenticateResult.Name,
                AvatarUrl = authenticateResult.AvatarUrl
            };
            
            return View(indexPageViewModel);
    }
```

## Caching di output

Output Caching è l'equivalente lato server di Response Caching. Caca l'output di un'azione controller. In sostanza il server web memorizza il risultato di una richiesta e la serve per le richieste successive.

```csharp
       [OutputCache(Duration = 3600,VaryByHeaderNames = new[] {"hx-request"},VaryByQueryKeys = new[] {"page", "pageSize"})]
```

Qui stiamo caching l'uscita dell'azione controller per 3600 secondi e variando la cache dal`hx-request`intestazione e la`page`e`pageSize`parametri di query.
Poiché stiamo memorizzando il lato del server dati per un tempo significativo (i messaggi di aggiornamento solo con un docker push) questo è impostato a più lungo rispetto alla Cache di risposta; potrebbe effettivamente essere infinito nel nostro caso, ma 3600 secondi è un buon compromesso.

Come con la cache di risposta stiamo usando il`hx-request`header per variare la cache in base al fatto che la richiesta è da HTMX o no.

## Conclusione

La cache è uno strumento potente per migliorare le prestazioni dell'applicazione. Utilizzando le funzionalità di cache integrato di ASP.NET Core è possibile facilmente cache contenuto sul lato client o server. Utilizzando HTMX è possibile cache contenuto sul lato client e fornire viste parziali per migliorare l'esperienza utente.