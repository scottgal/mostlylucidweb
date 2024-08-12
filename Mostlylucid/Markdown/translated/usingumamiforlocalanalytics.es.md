# Uso de Umami para análisis locales

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024-08-08T15:53</datetime>

## Introducción

Una de las cosas que me molestaba acerca de mi configuración actual era tener que utilizar Google Analytics para obtener datos de visitantes (¿qué poco hay de ello?). Así que quería encontrar algo que pudiera auto-anfitriona que no pasara datos a Google o a cualquier otro tercero.[Umami](https://umami.is/)que es una solución de análisis web sencilla y auto-anfitriona. Es una gran alternativa a Google Analytics y es (relativamente) fácil de configurar.

[TOC]

## Instalación

La instalación es PRETTY simple, pero tomó un poco de juguetear para realmente ponerse en marcha...

### Docker Composite

Como quería añadir Umami a mi configuración actual docker-compose necesitaba añadir un nuevo servicio a mi`docker-compose.yml`archivo. He añadido lo siguiente a la parte inferior del archivo:

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

Este archivo docker-compose.yml contiene la siguiente configuración:

1. Un nuevo servicio llamado`umami`que utiliza la`ghcr.io/umami-software/umami:postgresql-latest`imagen. Este servicio se utiliza para ejecutar el servicio de análisis de Umami.
2. Un nuevo servicio llamado`db`que utiliza la`postgres:16-alpine`imagen. Este servicio se utiliza para ejecutar la base de datos Postgres que Umami utiliza para almacenar sus datos.
   Nota para este servicio Lo he asignado a un directorio en mi servidor para que los datos persistan entre reinicios.

```yaml
    volumes:
      - /mnt/umami/postgres:/var/lib/postgresql/data
```

Necesitarás que este director exista y que el usuario de Docker pueda escribir en tu servidor (¡otra vez no es un experto en Linux, por lo que es probable que 777 sea exagerado aquí!).

```shell
chmod 777 /mnt/umami/postgres
```

3. Un nuevo servicio llamado`cloudflaredumami`que utiliza la`cloudflare/cloudflared:latest`imagen. Este servicio se utiliza para el túnel del servicio Umami a través de Cloudflare para permitir el acceso desde Internet.

### Archivo env

Para apoyar esto también actualicé mi`.env`archivo para incluir lo siguiente:

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

Esto configura la configuración para la composición del docker (la`<>`Elemets obviamente necesita reemplazar con sus propios valores).`cloudflaredumami`el servicio se utiliza para el túnel del servicio de Umami a través de Cloudflare para permitir que se acceda desde Internet. Es posible utilizar un BASE_PATH pero para Umami necesita molestamente una reconstrucción para cambiar el camino base así que lo he dejado como el camino raíz por ahora.

### Túnel de la nube

Para configurar el túnel cloudflare para esto (que actúa como la ruta para el archivo js utilizado para el análisis - getinfo.js) Utilicé el sitio web:

![Túnel de la nube](umamisetup.png)

Esto establece el túnel al servicio de Umami y permite que se acceda a él desde Internet.`umami`servicio en el archivo docker-compose (como está en la misma red que el túnel cloudflared es un nombre válido).

### Configuración de Umami en la página

Para habilitar la ruta para el script (llamado`getinfo`en mi configuración anterior) He añadido una entrada de configuración a mis aplicaciones

```json
 "Analytics":{
   "UmamiPath" : "https://umamilocal.mostlylucid.net/getinfo",
   "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee"
 },
```

También puede añadirlos a su archivo.env y pasarlos como variables de entorno al archivo docker-compose.

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

Usted configura el WebsiteId en el panel de Umami cuando configura el sitio. (Tenga en cuenta el nombre de usuario predeterminado y la contraseña para el servicio de Umami es`admin`y`umami`, usted NECESITA cambiar estos después de la configuración).
![Panel de control de Umami](umamiaddwebsite.png)

Con el archivo cs de configuración asociado:

```csharp
public class AnalyticsSettings : IConfigSection
{
    public static string Section => "Analytics";
    public string? UmamiPath { get; set; }
}
```

De nuevo esto utiliza mi material de configuración de POCO ([aquí](/blog/addingidentityfreegoogleauth#configuring-google-auth-with-poco)) para configurar la configuración.
Configúralo en mi program.cs:

```csharp
builder.Configure<AnalyticsSettings>();
```

Y finalmente en mi`BaseController.cs` `OnGet`método He añadido lo siguiente para establecer la ruta para el script de análisis:

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

Esto establece la ruta para el script de análisis que se utilizará en el archivo de diseño.

### Archivo de diseño

Finalmente, he añadido lo siguiente a mi archivo de diseño para incluir el script de análisis:

```html
<script defer src="@ViewBag.UmamiPath" data-website-id="@ViewBag.UmamiWebsiteId"></script>
```

Esto incluye el script en la página y establece el id del sitio web para el servicio de análisis.

## Excluyéndose de la analítica

Con el fin de excluir sus propias visitas de los datos analíticos, puede añadir el siguiente almacenamiento local en su navegador:

En las herramientas de desarrollo de Chrome (Ctrl+Mayús+I en las ventanas) se puede añadir lo siguiente a la consola:

```javascript
localStorage.setItem("umami.disabled", 1)
```

## Conclusión

Esto fue un poco de un faff para configurar, pero estoy feliz con el resultado. Ahora tengo un servicio de análisis auto-anfitrión que no pasa datos a Google o cualquier otro tercero. Es un poco de un dolor para configurar, pero una vez que se ha hecho es bastante fácil de usar. Estoy feliz con el resultado y se lo recomendaría a cualquier persona que busca una solución de análisis auto-anfitrión.