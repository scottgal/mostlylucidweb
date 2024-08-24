# Erreurs de manipulation (non traitées) dans ASP.NET Core

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-17T02:00</datetime>

## Présentation

Dans n'importe quelle application web, il est important de gérer les erreurs gracieusement. Ceci est particulièrement vrai dans un environnement de production où vous voulez fournir une bonne expérience utilisateur et ne pas exposer d'informations sensibles. Dans cet article, nous examinerons comment gérer les erreurs dans une application ASP.NET Core.

[TOC]

## Le problème

Lorsqu'une exception non traitée se produit dans une application ASP.NET Core, le comportement par défaut est de retourner une page d'erreur générique avec un code d'état de 500. Ce n'est pas idéal pour un certain nombre de raisons:

1. C'est laid et ne fournit pas une bonne expérience utilisateur.
2. Il ne fournit aucune information utile à l'utilisateur.
3. Il est souvent difficile de déboguer le problème parce que le message d'erreur est si générique.
4. C'est laid ; la page d'erreur générique du navigateur n'est qu'un écran gris avec du texte.

## La solution

Dans ASP.NET Core il y a une construction de fonctionnalité soignée dans laquelle nous permet de gérer ce genre d'erreurs.

```csharp
app.UseStatusCodePagesWithReExecute("/error/{0}");
```

Nous avons mis ceci dans notre `Program.cs` fichier tôt dans le pipeline. Cela va attraper n'importe quel code de statut qui n'est pas un 200 et rediriger vers le `/error` route avec le code d'état comme paramètre.

Notre contrôleur d'erreur ressemblera à ceci :

```csharp
    [Route("/error/{statusCode}")]
    public IActionResult HandleError(int statusCode)
    {
        // Retrieve the original request information
        var statusCodeReExecuteFeature = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
        
        if (statusCodeReExecuteFeature != null)
        {
            // Access the original path and query string that caused the error
            var originalPath = statusCodeReExecuteFeature.OriginalPath;
            var originalQueryString = statusCodeReExecuteFeature.OriginalQueryString;

            
            // Optionally log the original URL or pass it to the view
            ViewData["OriginalUrl"] = $"{originalPath}{originalQueryString}";
        }

        // Handle specific status codes and return corresponding views
        switch (statusCode)
        {
            case 404:
            return View("NotFound");
            case 500:
            return View("ServerError");
            default:
            return View("Error");
        }
    }
```

Ce contrôleur gérera l'erreur et retournera une vue personnalisée basée sur le code d'état. Nous pouvons également enregistrer l'URL d'origine qui a causé l'erreur et la passer à la vue.
Si nous avions un service central d'enregistrement / d'analyse, nous pourrions enregistrer cette erreur à ce service.

Nos points de vue sont les suivants :

```razor
<h1>404 - Page not found</h1>

<p>Sorry that Url doesn't look valid</p>
@section Scripts {
    <script>
            document.addEventListener('DOMContentLoaded', function () {
                if (!window.hasTracked) {
                    umami.track('404', { page:'@ViewData["OriginalUrl"]'});
                    window.hasTracked = true;
                }
            });

    </script>
}
```

Plutôt simple, n'est-ce pas? Nous pouvons également enregistrer l'erreur dans un service de journalisation comme Application Insights ou Serilog. De cette façon, nous pouvons garder une trace des erreurs et les corriger avant qu'elles ne deviennent un problème.
Dans notre cas, nous enregistrons ceci comme un événement à notre service d'analyse Umami. De cette façon, nous pouvons garder une trace du nombre d'erreurs 404 que nous avons et d'où elles viennent.

Cela maintient également votre page conformément à votre mise en page et conception choisie.

![404 Les droits de l'homme et les droits de l'homme sont garantis par le Pacte international relatif aux droits économiques, sociaux et culturels, ainsi que par le Pacte international relatif aux droits économiques, sociaux et culturels, ainsi que par le Pacte international relatif aux droits économiques, sociaux et culturels, et par le Pacte international relatif aux droits économiques, sociaux et culturels, ainsi que par le Pacte international relatif aux droits économiques, sociaux et culturels, et par le Pacte international relatif aux droits économiques, sociaux et culturels, ainsi que par le Pacte international relatif aux droits économiques, sociaux et culturels, et par le Pacte international relatif aux droits économiques, sociaux et culturels.](new404.png)

## En conclusion

C'est une façon simple de gérer les erreurs dans une application ASP.NET Core. Il fournit une bonne expérience utilisateur et nous permet de garder une trace des erreurs. C'est une bonne idée d'enregistrer les erreurs à un service d'enregistrement afin que vous puissiez les suivre et les corriger avant qu'elles ne deviennent un problème.