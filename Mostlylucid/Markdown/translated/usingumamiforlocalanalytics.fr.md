# Utilisation d'Umami pour l'analyse locale

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-08T15:53</datetime>

## Présentation

L'une des choses qui m'a ennuyé à propos de ma configuration actuelle était d'avoir à utiliser Google Analytics pour obtenir des données de visiteur (quelle qu'il y en ait peu??). Donc, je voulais trouver quelque chose que je pouvais m'auto-héberger qui ne transmettait pas les données à Google ou à tout autre tiers. J'ai trouvé[Umami](https://umami.is/)C'est une excellente alternative à Google Analytics et est (relativement) facile à configurer.

[TOC]

## Installation

L'installation est PRETTY simple, mais a pris un peu de violon pour vraiment aller...

### Composez Docker

Comme je voulais ajouter Umami à ma configuration actuelle, je devais ajouter un nouveau service à mon`docker-compose.yml`fichier. J'ai ajouté ce qui suit au bas du fichier:

```yaml
  umami:
    image: ghcr.io/umami-software/umami:postgresql-latest
    env_file: .env
    environment:
      DATABASE_URL: ${DATABASE_URL}
      DATABASE_TYPE: ${DATABASE_TYPE}
      HASH_SALT: ${HASH_SALT}
      APP_SECRET: ${APP_SECRET}
      TRACKER_SCRIPT_NAME: getinfo
      API_COLLECT_ENDPOINT: all
    ports:
      - "3000:3000"
    depends_on:
      - db
    networks:
      - app_network
    restart: always
  db:
    image: postgres:16-alpine
    env_file:
      - .env
    networks:
      - app_network
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER}"]
      interval: 5s
      timeout: 5s
      retries: 5
    volumes:
      - /mnt/umami/postgres:/var/lib/postgresql/data
    restart: always
  cloudflaredumami:
    image: cloudflare/cloudflared:latest
    command: tunnel --no-autoupdate run --token ${CLOUDFLARED_UMAMI_TOKEN}
    env_file:
      - .env
    restart: always
    networks:
      - app_network


```

Ce fichier docker-compose.yml contient la configuration suivante:

1. Un nouveau service appelé`umami`qui utilise les`ghcr.io/umami-software/umami:postgresql-latest`image. Ce service est utilisé pour exécuter le service d'analyse Umami.
2. Un nouveau service appelé`db`qui utilise les`postgres:16-alpine`image. Ce service est utilisé pour exécuter la base de données Postgres que Umami utilise pour stocker ses données.
   Note pour ce service Je suis cartographié vers un répertoire sur mon serveur de sorte que les données sont persistantes entre les redémarrages.

```yaml
    volumes:
      - /mnt/umami/postgres:/var/lib/postgresql/data
```

Vous aurez besoin de ce directeur pour exister et être enregistrable par l'utilisateur de docker sur votre serveur (encore pas un expert Linux donc 777 est probablement trop kill ici!).

```shell
chmod 777 /mnt/umami/postgres
```

3. Un nouveau service appelé`cloudflaredumami`qui utilise les`cloudflare/cloudflared:latest`image. Ce service est utilisé pour tunneler le service Umami à travers Cloudflare pour lui permettre d'être accessible à partir d'Internet.

### Fichier Env

Pour soutenir cela, j'ai également mis à jour mon`.env`fichier pour inclure les éléments suivants:

```shell
CLOUDFLARED_UMAMI_TOKEN=<cloudflaretoken>
DATABASE_TYPE=postgresql
HASH_SALT=<salt>

POSTGRES_DB=postgres
POSTGRES_USER=<postgresuser>
POSTGRES_PASSWORD=<postgrespassword>
UMAMI_SECRET=<umamisecret>

APP_SECRET=${UMAMI_SECRET}
UMAMI_USER=${POSTGRES_USER}
UMAMI_PASS=${POSTGRES_PASSWORD}
DATABASE_URL=postgresql://${UMAMI_USER}:${UMAMI_PASS}@db:5432/${POSTGRES_DB}
```

Ceci met en place la configuration pour le docker composer (le`<>`Il est évident que les valeurs doivent être remplacées par vos propres valeurs.`cloudflaredumami`service est utilisé pour tunneler le service Umami via Cloudflare pour lui permettre d'être accédé à partir d'Internet. Il est POSSIBLE d'utiliser un BASE_PATH mais pour Umami il a agaçant besoin d'une reconstruction pour changer le chemin de base donc je l'ai laissé comme chemin racine pour le moment.

### Tunnel Cloudflare

Pour configurer le tunnel cloudflare pour cela (qui agit comme chemin pour le fichier js utilisé pour l'analyse - getinfo.js) J'ai utilisé le site web:

![Tunnel Cloudflare](umamisetup.png)

Cela met en place le tunnel au service Umami et permet d'accéder à celui-ci à partir d'Internet.`umami`le service dans le fichier composé de docker (comme il est sur le même réseau que le tunnel nuageux c'est un nom valide).

### Umami Configuration dans la page

Pour activer le chemin pour le script (appelé`getinfo`dans ma configuration ci-dessus) J'ai ajouté une entrée de configuration à mes appsettings

```json
 "Analytics":{
   "UmamiPath" : "https://umamilocal.mostlylucid.net/getinfo",
   "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee"
 },
```

Vous pouvez aussi les ajouter à votre fichier.env et les transmettre en tant que variables d'environnement au fichier composé de docker.

```shell
ANALYTICS__UMAMIPATH="https://umamilocal.mostlylucid.net/getinfo"
ANALYTICS_WEBSITEID="32c2aa31-b1ac-44c0-b8f3-ff1f50403bee"
```

```yaml
  mostlylucid:
    image: scottgal/mostlylucid:latest
    ports:
      - 8080:8080
    restart: always
    environment:
    ...
      - Analytics__UmamiPath=${ANALYTICS_UMAMIPATH}
      - Analytics__WebsiteId=${ANALYTICS_WEBSITEID}
```

Vous avez configuré le WebsiteId dans le tableau de bord Umami lorsque vous avez configuré le site. (Notez le nom d'utilisateur et le mot de passe par défaut pour le service Umami est`admin`et`umami`, vous avez besoin de modifier ces après la configuration).
![Tableau de bord Umami](umamiaddwebsite.png)

Avec le fichier cs de paramètres associés :

```csharp
public class AnalyticsSettings : IConfigSection
{
    public static string Section => "Analytics";
    public string? UmamiPath { get; set; }
}
```

Encore une fois, cela utilise mon matériel de config POCO ([Ici.](/blog/addingidentityfreegoogleauth#configuring-google-auth-with-poco)) pour configurer les paramètres.
Installez-le dans mon programme.cs:

```csharp
builder.Configure<AnalyticsSettings>();
```

Et enfin dans mon`BaseController.cs` `OnGet`méthode J'ai ajouté ce qui suit pour définir le chemin pour le script analytique:

```csharp
   public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        if (!Request.IsHtmx())
        {
            ViewBag.UmamiPath = _analyticsSettings.UmamiPath;
            ViewBag.UmamiWebsiteId = _analyticsSettings.WebsiteId;
        }
        base.OnActionExecuting(filterContext);
    }
    
```

Cela définit le chemin pour le script analytique à utiliser dans le fichier de mise en page.

### Mise en page du fichier

Enfin, j'ai ajouté ce qui suit à mon fichier de mise en page pour inclure le script analytique :

```html
<script defer src="@ViewBag.UmamiPath" data-website-id="@ViewBag.UmamiWebsiteId"></script>
```

Cela inclut le script dans la page et définit l'identifiant du site Web pour le service d'analyse.

## Vous exclure de l'analyse

Afin d'exclure vos propres visites des données analytiques, vous pouvez ajouter le stockage local suivant dans votre navigateur :

Dans les outils Chrome dev (Ctrl+Shift+I sur les fenêtres) vous pouvez ajouter ce qui suit à la console:

```javascript
localStorage.setItem("umami.disabled", 1)
```

## Conclusion

C'était un peu un faff à configurer mais je suis heureux avec le résultat. J'ai maintenant un service d'analyse auto-hosted qui ne transmet pas les données à Google ou à tout autre tiers. C'est un peu une douleur à configurer mais une fois qu'il est fait, il est assez facile à utiliser. Je suis heureux avec le résultat et le recommanderait à toute personne cherchant une solution d'analyse auto-hosted.