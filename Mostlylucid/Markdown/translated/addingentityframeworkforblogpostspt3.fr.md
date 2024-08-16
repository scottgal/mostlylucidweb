# Ajouter un cadre d'entité pour les billets de blog (Partie 3)

<!--category-- ASP.NET, Entity Framework -->
<datetime class="hidden">2024-08-16T18:00</datetime>

Vous pouvez trouver tout le code source pour les messages de blog sur [GitHub](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/Blog)

**Les parties 1 et 2 de la série sur l'ajout du cadre d'entité à un projet de base.NET.**

La première partie peut être trouvée [Ici.](/blog/addingentityframeworkforblogpostspt1).

La deuxième partie peut être trouvée [Ici.](/blog/addingentityframeworkforblogpostspt2).

## Présentation

Dans les parties précédentes, nous avons mis en place la base de données et le contexte de nos messages de blog, et ajouté les services pour interagir avec la base de données. Dans cet article, nous détaillerons comment ces services fonctionnent maintenant avec les contrôleurs et les vues existants.

[TOC]

## Contrôleurs

Les contrôleurs externes pour Blogs sont vraiment assez simples; en ligne avec éviter l'antipattern 'Fat Controller' (un modèle que nous avons idéalisé au début des jours ASP.NET MVC).

### Le modèle Fat Controller dans ASP.NET MVC

I MVC frameworks une bonne pratique est de faire le moins possible dans vos méthodes de contrôleur. C'est parce que le contrôleur est responsable du traitement de la demande et du retour d'une réponse. Il ne devrait pas être responsable de la logique opérationnelle de l'application. C'est la responsabilité du modèle.

L'antipattern 'Fat Controller' est l'endroit où le contrôleur fait trop. Cela peut entraîner un certain nombre de problèmes, notamment:

1. Duplication du code dans plusieurs actions:
   Une action doit être une unité de travail unique, se contenter de peupler le modèle et de retourner la vue. Si vous vous trouvez à répéter le code en plusieurs actions, c'est un signe que vous devriez refactorer ce code en une méthode séparée.
2. Code difficile à tester:
   En ayant des 'contrôleurs gras', vous pouvez rendre difficile de tester le code. Les tests devraient tenter de suivre tous les chemins possibles à travers le code, ce qui peut être difficile si le code n'est pas bien structuré et axé sur une seule responsabilité.
3. Code difficile à maintenir :
   L'entretien est une préoccupation clé lors de la construction d'applications. Avoir des méthodes d'action « évier de cuisine » peut facilement vous conduire ainsi que d'autres développeurs utilisant le code pour faire des changements qui brisent d'autres parties de l'application.
4. Code difficile à comprendre :
   C'est une préoccupation clé pour les développeurs. Si vous travaillez sur un projet avec une large base de code, il peut être difficile de comprendre ce qui se passe dans une action de contrôleur si elle fait trop.

### Le contrôleur du blog

Le contrôleur de blog est relativement simple. Il a 4 actions principales (et une 'action de compa' pour les anciens liens de blog). Il s'agit des éléments suivants:

```csharp
Task<IActionResult> Index(int page = 1, int pageSize = 5)

Task<IActionResult> Show(string slug, string language = BaseService.EnglishLanguage)

Task<IActionResult> Category(string category, int page = 1, int pageSize = 5)

Task<IActionResult> Language(string slug, string language)

IActionResult Compat(string slug, string language)
```

À leur tour, ces actions appellent `IBlogService` pour obtenir les données dont ils ont besoin. Les `IBlogService` est détaillée dans le [poste précédent](/blog/addingentityframeworkforblogpostspt2).

À leur tour, ces actions sont les suivantes :

- Index: Il s'agit de la liste des messages de blog (par défaut vers la langue anglaise; nous pouvons l'étendre plus tard pour permettre plusieurs langues). Tu verras qu'il faut `page` et `pageSize` comme paramètres. C'est pour la pagination. des résultats.
- Afficher : Ceci est le billet de blog individuel. Il faut `slug` du poste et du `language` comme paramètres. This est la méthode que vous utilisez actuellement pour lire ce billet de blog.
- Catégorie: Voici la liste des billets de blog pour une catégorie donnée. Il faut `category`, `page` et `pageSize` comme paramètres.
- Langue: Ceci montre un billet de blog pour une langue donnée. Il faut `slug` et `language` comme paramètres.
- Compat: Il s'agit d'une action de compatibilité pour les anciens liens de blog. Il faut `slug` et `language` comme paramètres.

### Cache

Comme mentionné dans une [poste antérieur](https://www.mostlylucid.net/blog/aspnetcachingwithhtmx) nous mettons en œuvre `OutputCache` et `ResponseCahce` pour mettre en cache les résultats des messages de blog. Cela améliore l'expérience utilisateur et réduit la charge sur le serveur.

Ceux-ci sont mis en œuvre à l'aide des décorateurs Action appropriés qui spécifient les paramètres utilisés pour l'Action (ainsi que `hx-request` pour les demandes HTMX). Pour examen avec `Index` nous les spécifions:

```csharp
    [ResponseCache(Duration = 300, VaryByHeader  = "hx-request", VaryByQueryKeys = new[] {nameof(page), nameof(pageSize)}, Location = ResponseCacheLocation.Any)]
    [OutputCache(Duration = 3600, VaryByHeaderNames = new[] {"hx-request"} ,VaryByQueryKeys = new[] { nameof(page), nameof(pageSize)})]
```

## Vues

Les vues pour le blog sont relativement simples. Ce ne sont surtout qu'une liste de billets de blog, avec quelques détails pour chaque billet. Les points de vue sont dans le `Views/Blog` dossier. Les principaux points de vue sont les suivants :

### `_PostPartial.cshtml`

C'est la vue partielle d'un seul billet de blog. Il est utilisé dans notre `Post.cshtml` vue.

```razor
@model Mostlylucid.Models.Blog.BlogPostViewModel

@{
    Layout = "_Layout";
}
<partial name="_PostPartial" model="Model"/>
```

### `_BlogSummaryList.cshtml`

C'est la vue partielle d'une liste de billets de blog. Il est utilisé dans notre `Index.cshtml` voir aussi bien que dans la page d'accueil.

```razor
@model Mostlylucid.Models.Blog.PostListViewModel
<div class="pt-2" id="content">

    @if (Model.TotalItems > Model.PageSize)
    {
        <pager
            x-ref="pager"
            link-url="@Model.LinkUrl"
               hx-boost="true"
               hx-push-url="true"
               hx-target="#content"
               hx-swap="show:none"
               page="@Model.Page"
               page-size="@Model.PageSize"
               total-items="@Model.TotalItems"
            class="w-full"></pager>
    }
    @if(ViewBag.Categories != null)
{
    <div class="pb-3">
        <h4 class="font-body text-lg text-primary dark:text-white">Categories</h4>
        <div class="flex flex-wrap gap-2 pt-2">
            @foreach (var category in ViewBag.Categories)
            {
                <a hx-controller="Blog" hx-action="Category" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-category="@category" href>
                    <span class="inline-block rounded-full dark bg-blue-dark px-2 py-1 font-body text-sm text-white outline-1 outline outline-green-dark dark:outline-white">@category</span>
                </a>
            }
        </div>
    </div>
}
@foreach (var post in Model.Posts)
{
    <partial name="_ListPost" model="post"/>
}
</div>
```

Il s'agit de `_ListPost` vue partielle pour afficher les billets de blog individuels avec le [aide à la recherche d'une balise](/blog/addpagingwithhtmx) qui nous permet de pager les messages de blog.

### `_ListPost.cshtml`

Les _Listpost vue partielle est utilisé pour afficher les messages de blog individuels dans la liste. Il est utilisé à l'intérieur de la `_BlogSummaryList` vue.

```razor
@model Mostlylucid.Models.Blog.PostListModel

<div class="border-b border-grey-lighter pb-8 mb-8">
 
    <a asp-controller="Blog" asp-action="Show" hx-boost="true"  hx-swap="show:window:top"  hx-target="#contentcontainer" asp-route-slug="@Model.Slug"
       class="block font-body text-lg font-semibold transition-colors hover:text-green text-blue-dark dark:text-white  dark:hover:text-secondary">@Model.Title</a>
    <div class="flex space-x-2 items-center py-4">
    @foreach (var category in Model.Categories)
    {
    <a hx-controller="Blog" hx-action="Category" class="rounded-full bg-blue-dark font-body text-sm text-white px-2 py-1 outline outline-1 outline-white" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-category="@category" href>@category
    </a>
    }

    @{ var languageModel = (Model.Slug, Model.Languages, Model.Language); }
        <partial name="_LanguageList" model="languageModel"/>
    </div>
    <div class="block font-body text-black dark:text-white">@Model.Summary</div>
    <div class="flex items-center pt-4">
        <p class="pr-2 font-body font-light text-primary light:text-black dark:text-white">
            @Model.PublishedDate.ToString("f")
        </p>
        <span class="font-body text-grey dark:text-white">//</span>
        <p class="pl-2 font-body font-light text-primary light:text-black dark:text-white">
            @Model.ReadingTime
        </p>
    </div>
</div>
```

Comme vous allez vous-même ici, nous avons un lien vers le billet de blog individuel, les catégories pour le billet, les langues dans lesquelles le billet est disponible, le résumé du billet, la date de publication et l'heure de lecture.

Nous avons également des balises de lien HTMX pour les catégories et les langues pour nous permettre d'afficher les messages localisés et les messages pour une catégorie donnée.

Nous avons deux façons d'utiliser HTMX ici, une qui donne l'URL complète et une qui est "HTML seulement" (i.e. pas d'URL). C'est parce que nous voulons utiliser l'URL complète pour les catégories et les langues, mais nous n'avons pas besoin de l'URL complète pour le blog individuel.

```razor
 <a asp-controller="Blog" asp-action="Show" hx-boost="true"  hx-swap="show:window:top"  hx-target="#contentcontainer" asp-route-slug="@Model.Slug"
```

Cette approche remplit une URL complète pour le blog individuel et utilise `hx-boost` pour « booster » la demande d'utilisation de HTMX (ceci définit le `hx-request` en-tête vers `true`).

```razor
  <a hx-controller="Blog" hx-action="Category" class="rounded-full bg-blue-dark font-body text-sm text-white px-2 py-1 outline outline-1 outline-white" hx-push-url="true" hx-get hx-target="#contentcontainer" hx-route-category="@category" href>@category
    </a>
```

Alternativement, cette approche utilise les balises HTMX pour obtenir les catégories pour les billets de blog. Il s'agit de `hx-controller`, `hx-action`, `hx-push-url`, `hx-get`, `hx-target` et `hx-route-category` tags pour obtenir les catégories pour les billets de blog pendant `hx-push-url` est réglé à `true` pour pousser l'URL à l'historique du navigateur.

Il est également utilisé au sein de notre `Index` Méthode d'action pour les demandes HTMX.

```csharp
  public async Task<IActionResult> Index(int page = 1, int pageSize = 5)
    {
        var posts =await  blogService.GetPagedPosts(page, pageSize);
        if(Request.IsHtmx())
        {
            return PartialView("_BlogSummaryList", posts);
        }
        posts.LinkUrl = Url.Action("Index", "Blog");
        return View("Index", posts);
    }
```

Lorsqu'il nous permet soit de retourner la vue complète, soit simplement la vue partielle pour les demandes HTMX, en donnant une expérience similaire à celle de « SPA ».

## Page d'accueil

Dans le `HomeController` nous nous référons également à ces services de blog pour obtenir les derniers billets de blog pour la page d'accueil. C'est ce qu'on fait dans le domaine de l'éducation et de la formation tout au long de la vie. `Index` méthode d'action.

```csharp
   public async Task<IActionResult> Index(int page = 1,int pageSize = 5)
    {
            var authenticateResult = GetUserInfo();
            var posts =await blogService.GetPagedPosts(page, pageSize);
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

Comme vous le verrez ici, nous utilisons les `IBlogService` pour obtenir les derniers billets de blog pour la page d'accueil. Nous utilisons également les `GetUserInfo` méthode pour obtenir les informations utilisateur pour la page d'accueil.

Encore une fois cela a une demande HTMX de retourner la vue partielle pour les messages de blog pour nous permettre de pager les messages de blog dans la page d'accueil.

## En conclusion

Dans notre prochaine partie, nous allons entrer dans le détail excruciant de la façon dont nous utilisons le `IMarkdownBlogService` pour remplir la base de données avec les messages de blog à partir des fichiers de balisage. C'est une partie clé de l'application car elle nous permet d'utiliser les fichiers de balisage pour remplir la base de données avec les messages de blog.