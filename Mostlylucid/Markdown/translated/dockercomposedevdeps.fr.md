# Utilisation de Docker Compose pour les dépendances de développement

<!--category-- Docker -->
<datetime class="hidden">2024-08-09T17:17</datetime>

# Présentation

Lorsque nous développons des logiciels traditionnellement, nous créions une base de données, une file d'attente de messages, un cache et peut-être quelques autres services. Cela peut être une douleur à gérer, surtout si vous travaillez sur plusieurs projets. Docker Compose est un outil qui vous permet de définir et d'exécuter des applications Docker multi-conteneurs. C'est un excellent moyen de gérer vos dépendances en matière de développement.

Dans ce post, je vais vous montrer comment utiliser Docker Compose pour gérer vos dépendances de développement.

[TOC]

# Préalables

D'abord, vous aurez besoin d'installer le bureau Docker sur n'importe quelle plate-forme que vous utilisez. Vous pouvez le télécharger depuis [Ici.](https://www.docker.com/products/docker-desktop).

**REMARQUE: J'ai trouvé que sur Windows vous avez vraiment besoin d'exécuter l'installateur Docker Desktop en tant qu'administrateur pour s'assurer qu'il s'installe correctement.**

# Création d'un fichier Docker Compose

Docker Compose utilise un fichier YAML pour définir les services que vous souhaitez exécuter. Voici un exemple de simple `devdeps-docker-compose.yml` fichier qui définit un service de base de données et un service de courriel:

```yaml
services: 
  smtp4dev:
    image: rnwood/smtp4dev
    ports:
      - "3002:80"
      - "2525:25"
    volumes:
      # This is where smtp4dev stores the database..
      - e:/smtp4dev-data:/smtp4dev
    restart: always
  postgres:
    image: postgres:16-alpine
    container_name: postgres
    ports:
      - "5432:5432"
    env_file:
      - .env
    volumes:
      - e:/data:/var/lib/postgresql/data  # Map e:\data to the PostgreSQL data folder
    restart: always	
networks:
  mynetwork:
        driver: bridge
```

Note ici J'ai spécifié des volumes pour la persistance des données pour chaque service, ici J'ai spécifié

```yaml
    volumes:
      # This is where smtp4dev stores the database..
      - e:/smtp4dev-data:/smtp4dev
    volumes:
        - e:/data:/var/lib/postgresql/data  # Map e:\data to the PostgreSQL data folder
```

Cela garantit la persistance des données entre les parcours des conteneurs.

Je précise également une `env_file` pour la `postgres` le service. Il s'agit d'un fichier qui contient des variables d'environnement qui sont transmises au conteneur.
Vous pouvez voir une liste de variables d'environnement qui peuvent être transmises au conteneur PostgreSQLTM [Ici.](https://www.docker.com/blog/how-to-use-the-postgres-docker-official-image/#1-Environment-variables).
Voici un exemple de `.env` fichier & #160;:

```shell
POSTGRES_DB=postgres
POSTGRES_USER=postgres
POSTGRES_PASSWORD=<somepassword>
```

Cela configure une base de données, un mot de passe et un utilisateur par défaut pour PostgreSQLTM.

Ici, j'exécute également le service SMTP4Dev, c'est un excellent outil pour tester la fonctionnalité d'email dans votre application. Vous pouvez trouver plus d'informations à ce sujet [Ici.](https://github.com/rnwood/smtp4dev/wiki/Installation#how-to-run-smtp4dev-in-docker).

Si tu regardes dans mes yeux `appsettings.Developmet.json` fichier que vous verrez J'ai la configuration suivante pour le serveur SMTP:

```json
  "SmtpSettings":
{
"Server": "localhost",
"Port": 2525,
"SenderName": "Mostlylucid",
"Username": "",
"SenderEmail": "scott.galloway@gmail.com",
"Password": "",
"EnableSSL": "false",
"EmailSendTry": 3,
"EmailSendFailed": "true",
"ToMail": "scott.galloway@gmail.com",
"EmailSubject": "Mostlylucid"

}
```

Cela fonctionne pour SMTP4Dev et il me permet de tester cette fonctionnalité (je peux envoyer à n'importe quelle adresse, et voir l'e-mail dans l'interface SMTP4Dev à http://localhost:3002/).

Une fois que vous êtes sûr que tout fonctionne, vous pouvez tester sur un vrai serveur SMTP comme GMAIL (par exemple, voir [Ici.](addingasyncsendingforemails) pour comment faire cela)

# Gestion des services

Pour exécuter les services définis dans le `devdeps-docker-compose.yml` fichier, vous devez exécuter la commande suivante dans le même répertoire que le fichier:

```shell
docker compose -f .\devdeps-docker-compose.yml up -d
```

Notez que vous devriez l'exécuter au départ comme ceci ; ceci vous assure de voir les éléments de configuration passés de la `.env` fichier.

```shell
docker compose -f .\devdeps-docker-compose.yml config
```

Maintenant, si vous regardez dans Docker Desktop vous pouvez voir ces services en cours d'exécution

![Bureau Docker](dockerdesktopdev.png)