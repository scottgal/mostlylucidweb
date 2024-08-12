# Ajout de la recherche avec HTMX et ASP.NET Core avec TagHelper

<!--category-- ASP.NET, HTMX -->
<datetime class="hidden">2024-08-09T12:50</datetime>

## Présentation

Maintenant que j'ai un tas de billets de blog la page d'accueil devenait assez longue alors j'ai décidé d'ajouter un mécanisme de recherche pour les billets de blog.

Cela va de pair avec l'ajout de cache complet pour les billets de blog pour faire de cela un site rapide et efficace.

Voir[Blog Source du service](https://github.com/scottgal/mostlylucidweb/blob/main/Mostlylucid/Services/Markdown/MarkdownBlogService.cs)pour la façon dont cela est mis en œuvre; c'est vraiment assez simple en utilisant l'IMemoryCache.

[TOC]

### TagHelper

J'ai décidé d'utiliser un TagHelper pour implémenter le mécanisme de pagination. C'est une excellente façon d'encapsuler la logique de pagination et de la rendre réutilisable.
Il s'agit de[Taghelper de pagination de Darrel O'Neill](https://github.com/darrel-oneil/PaginationTagHelper)Ceci est inclus dans le projet en tant que paquet nuget.

Ceci est ensuite ajouté à la_Le fichier ViewImports.cshtml est donc disponible pour toutes les vues.

```razor
@addTagHelper *,PaginationTagHelper.AspNetCore
```

### Le TagHelper

Dans le_BlogSommaryList.cshtml vue partielle J'ai ajouté le code suivant pour rendre le mécanisme de recherche.

```razor
<pager link-url="@Model.LinkUrl"
       hx-boost="true"
       hx-push-url="true"
       hx-target="#content"
       hx-swap="show:none"
       page="@Model.Page"
       page-size="@Model.PageSize"
       total-items="@Model.TotalItems" ></pager>
```

Voici quelques choses notables :

1. `link-url`Cela permet au taghelper de générer l'url correct pour les liens de recherche. Dans la méthode HomeController Index ceci est défini à cette action.

```csharp
   var posts = blogService.GetPostsForFiles(page, pageSize);
   posts.LinkUrl= Url.Action("Index", "Home");
   if (Request.IsHtmx())
   {
      return PartialView("_BlogSummaryList", posts);
   }
```

Et dans le contrôleur Blog

```csharp
    public IActionResult Index(int page = 1, int pageSize = 5)
    {
        var posts = blogService.GetPostsForFiles(page, pageSize);
        if(Request.IsHtmx())
        {
            return PartialView("_BlogSummaryList", posts);
        }
        posts.LinkUrl = Url.Action("Index", "Blog");
        return View("Index", posts);
    }
```

Ceci est réglé sur cette URl. Cela garantit que l'aide à la pagination peut fonctionner pour l'une ou l'autre des méthodes de haut niveau.

### Propriétés HTMX

`hx-boost`, `hx-push-url`, `hx-target`, `hx-swap`ce sont toutes les propriétés HTMX qui permettent à la pagination de travailler avec HTMX.

```razor
     hx-boost="true"
       hx-push-url="true"
       hx-target="#content"
       hx-swap="show:none"
```

Ici nous utilisons`hx-boost="true"`Cela permet au taghelper de pagination de ne pas avoir besoin de modifications en interceptant sa génération normale d'URL et en utilisant l'URL courante.

`hx-push-url="true"`pour s'assurer que l'URL est échangée dans l'historique URL du navigateur (ce qui permet un lien direct avec les pages).

`hx-target="#content"`c'est le div cible qui sera remplacé par le nouveau contenu.

`hx-swap="show:none"`Il s'agit de l'effet swap qui sera utilisé lors du remplacement du contenu. Dans ce cas, il empêche l'effet « saut » normal que HTMX utilise sur le swap de contenu.

#### Système de gestion de l'information (SSC)

J'ai aussi ajouté des styles au main.css dans mon répertoire /src, ce qui m'a permis d'utiliser les classes CSS Tailwind pour les liens de pagination.

```css
.pagination {
    @apply py-2 flex list-none p-0 m-0 justify-center items-center;
}

.page-item {
    @apply mx-1 text-black  dark:text-white rounded;
}

.page-item a {
    @apply block rounded-md transition duration-300 ease-in-out;
}

.page-item a:hover {
    @apply bg-blue-dark text-white;
}

.page-item.disabled a {
    @apply text-blue-dark pointer-events-none cursor-not-allowed;
}

```

### Contrôleur

`page`, `page-size`, `total-items`sont les propriétés que le taghelper de pagination utilise pour générer les liens de pagination.
Ceux-ci sont passés dans la vue partielle du contrôleur.

```csharp
 public IActionResult Index(int page = 1, int pageSize = 5)
```

### Service de blogs

Ici page et pageTaille sont passés à partir de l'URL et le total des éléments sont calculés à partir du service de blog.

```csharp
    public PostListViewModel GetPostsForFiles(int page=1, int pageSize=10)
    {
        var model = new PostListViewModel();
        var posts = GetPageCache().Values.Select(GetListModel).ToList();
        model.Posts = posts.OrderByDescending(x => x.PublishedDate).Skip((page - 1) * pageSize).Take(pageSize).ToList();
        model.TotalItems = posts.Count();
        model.PageSize = pageSize;
        model.Page = page;
        return model;
    }
```

Ici, nous obtenons simplement les messages à partir du cache, les commandons par date et puis sautons et prenons le nombre correct de messages pour la page.

### Le présent règlement entre en vigueur le vingtième jour suivant celui de sa publication au Journal officiel de l'Union européenne.

C'était un ajout simple au site mais il le rend beaucoup plus utilisable. L'intégration HTMX rend le site plus réactif tout en n'ajoutant pas plus de JavaScript au site.