# Cache ASP.NET avec HTMX

<!--category-- ASP.NET, HTMX -->
<datetime class="hidden">2024-08-12T00:50</datetime>

## Présentation

La mise en cache est une technique importante pour améliorer l'expérience utilisateur en chargeant le contenu plus rapidement et pour réduire la charge sur votre serveur. Dans cet article, je vais vous montrer comment utiliser les fonctionnalités de mise en cache intégrées d'ASP.NET Core avec HTMX pour mettre en cache le contenu du côté client.

[TOC]

## Configuration

Dans ASP.NET Core, il y a deux types de caches offerts

- Réponse Cache - Il s'agit de données qui sont mises en cache sur le client ou dans les serveurs intermédiaires de procy (ou les deux) et qui sont utilisées pour mettre en cache l'ensemble de la réponse pour une requête.
- Cache de sortie - Il s'agit de données qui sont mises en cache sur le serveur et qui sont utilisées pour mettre en cache la sortie d'une action du contrôleur.

Pour les configurer dans ASP.NET Core, vous devez ajouter quelques services dans votre`Program.cs`fichier

### Réponse en cache

```csharp
builder.Services.AddResponseCaching();
app.UseResponseCaching();
```

### Cache de sortie

```csharp
services.AddOutputCache();
app.UseOutputCache();
```

## Réponse en cache

Bien qu'il soit possible de configurer le cache de réponse dans votre`Program.cs`Il est souvent un peu inflexible (surtout lors de l'utilisation des requêtes HTMX comme je l'ai découvert).`ResponseCache`attribut.

```csharp
        [ResponseCache(Duration = 300, VaryByHeader = "hx-request", VaryByQueryKeys = new[] {"page", "pageSize"}, Location = ResponseCacheLocation.Any)]
```

Ceci va mettre en cache la réponse pendant 300 secondes et varier le cache par le`hx-request`l'en-tête et l'en-tête`page`et`pageSize`paramètres de requête. Nous définissons également le`Location`à l'annexe I du règlement (UE) no 1308/2013 du Parlement européen et du Conseil du`Any`ce qui signifie que la réponse peut être mise en cache sur le client, sur les serveurs mandataires intermédiaires, ou les deux.

Ici, le`hx-request`header est l'en-tête que HTMX envoie avec chaque requête. Ceci est important car il vous permet de mettre en cache la réponse différemment selon qu'il s'agit d'une requête HTMX ou d'une requête normale.

C'est notre époque actuelle.`Index`méthode d'action. Yo ucan voir que nous acceptons un paramètre page et pageSize ici et nous avons ajouté ceux-ci comme variable par les touches de requête dans le`ResponseCache`attribut. Signifiant que les réponses sont 'indexées' par ces clés et stockent différents contenus basés sur celles-ci.

Dans l'action, nous avons aussi`if(Request.IsHtmx())`c'est basé sur le[Paquet HTMX.Net](https://github.com/khalidabuhakmeh/Htmx.Net)et contrôle essentiellement pour les mêmes`hx-request`header que nous utilisons pour varier le cache. Ici, nous renvoyons une vue partielle si la requête est de HTMX.

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

## Cache de sortie

Caching de sortie est l'équivalent côté serveur de Caching de réponse. Il cache la sortie d'une action de contrôleur. En substance, le serveur web stocke le résultat d'une requête et le sert pour les requêtes ultérieures.

```csharp
       [OutputCache(Duration = 3600,VaryByHeaderNames = new[] {"hx-request"},VaryByQueryKeys = new[] {"page", "pageSize"})]
```

Ici, nous encaissons la sortie de l'action du contrôleur pendant 3600 secondes et varions le cache par le`hx-request`l'en-tête et l'en-tête`page`et`pageSize`paramètres de requête.
Comme nous stockons le côté serveur de données pendant un temps significatif (les messages ne mettant à jour qu'avec une poussée de docker), cela est réglé à plus long que le cache de réponse; il pourrait en fait être infini dans notre cas, mais 3600 secondes est un bon compromis.

Comme pour le Cache de réponse, nous utilisons le`hx-request`l'en-tête pour modifier le cache en fonction de si la requête est de HTMX ou non.

## Conclusion

La mise en cache est un outil puissant pour améliorer les performances de votre application. En utilisant les fonctionnalités de mise en cache intégrées d'ASP.NET Core, vous pouvez facilement mettre en cache du contenu du côté client ou serveur. En utilisant HTMX, vous pouvez mettre en cache du contenu du côté client et servir des vues partielles pour améliorer l'expérience utilisateur.