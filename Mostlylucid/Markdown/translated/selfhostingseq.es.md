# Auto Hosting Seq para ASP.NET Logging

<datetime class="hidden">2024-08-28T09:37</datetime>

<!--category-- ASP.NET, Seq, Serilog, Docker -->
# Introducción

Seq es una aplicación que le permite ver y analizar registros. Es una gran herramienta para depurar y monitorear tu aplicación. En este post cubriré cómo configuré Seq para registrar mi aplicación ASP.NET Core.
Nunca puede tener demasiados tableros :)

![SeqDashboard](seqdashboard.png)

[TOC]

# Configuración de Seq

Seq viene en un par de sabores. Puede utilizar la versión en la nube o auto hospedarla. Elegí ser el anfitrión ya que quería mantener mis registros privados.

Primero empecé visitando el sitio web de Seq y encontrando el [Instrucciones de instalación Docker](https://docs.datalust.co/docs/getting-started-with-docker).

## Localmente

Para ejecutar localmente primero necesita obtener una contraseña de hashed. Puede hacerlo ejecutando el siguiente comando:

```bash
echo '<password>' | docker run --rm -i datalust/seq config hash
```

Para ejecutarlo localmente puede utilizar el siguiente comando:

```bash
docker run --name seq -d --restart unless-stopped -e ACCEPT_EULA=Y -e SEQ_FIRSTRUN_ADMINPASSWORDHASH=<hashfromabove> -v C:\seq:/data -p 5443:443 -p 45341:45341 -p 5341:5341 -p 82:80 datalust/seq

```

En mi máquina local de Ubuntu hice esto en un guión sh:

```shell
#!/bin/bash
PH=$(echo 'Abc1234!' | docker run --rm -i datalust/seq config hash)

mkdir -p /mnt/seq
chmod 777 /mnt/seq

docker run \
  --name seq \
  -d \
  --restart unless-stopped \
  -e ACCEPT_EULA=Y \
  -e SEQ_FIRSTRUN_ADMINPASSWORDHASH="$PH" \
  -v /mnt/seq:/data \
  -p 5443:443 \
  -p 45341:45341 \
  -p 5341:5341 \
  -p 82:80 \
  datalust/seq
```

Entonces

```shell
chmod +x seq.sh
./seq.sh
```

Esto te va a poner en marcha y luego ir a `http://localhost:82` / `http://<machineip>:82` para ver su siguiente instalación (contraseña de administrador por defecto es la que ha introducido para <password> arriba.

## En Docker

He añadido siguientes a mi Docker componer el archivo de la siguiente manera:

```docker
  seq:
    image: datalust/seq
    container_name: seq
    restart: unless-stopped
    environment:
      ACCEPT_EULA: "Y"
      SEQ_FIRSTRUN_ADMINPASSWORDHASH: ${SEQ_DEFAULT_HASH}
    volumes:
      - /mnt/seq:/data
    networks:
      - app_network
```

Tenga en cuenta que tengo un directorio llamado `/mnt/seq` (para ventanas, utilice una ruta de ventanas). Aquí es donde se almacenarán los registros.

También tengo un `SEQ_DEFAULT_HASH` variable de entorno que es la contraseña hashed para el usuario administrador en mi archivo.env.

# Configuración de ASP.NET Core

Como uso [Serilog](https://serilog.net/) Para mi registro es bastante fácil configurar Seq. Incluso tiene documentos sobre cómo hacer esto. [aquí](https://docs.datalust.co/docs/using-serilog).

Básicamente usted acaba de agregar el fregadero a su proyecto:

```shell
dotnet add package Serilog.Sinks.Seq
```

Prefiero usar `appsettings.json` para mi configuración así que sólo tengo la configuración 'estándar' en mi `Program.cs`:

```csharp
builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
    Serilog.Debugging.SelfLog.Enable(Console.Error);
    Console.WriteLine($"Serilog Minimum Level: {configuration.MinimumLevel.ToString()}");
});
```

Entonces en mi 'appsettings.json' tengo esta configuración

```json
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File", "Serilog.Sinks.Seq"],
    "MinimumLevel": "Warning",
    "WriteTo": [
        {
          "Name": "Seq",
          "Args":
          {
            "serverUrl": "http://seq:5341",
            "apiKey": ""
          }
        },
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/applog-.txt",
          "rollingInterval": "Day"
        }
      }

    ],
    "Enrich": ["FromLogContext", "WithMachineName"],
    "Properties": {
      "ApplicationName": "mostlylucid"
    }
  }

```

Ya verás que tengo un `serverUrl` de `http://seq:5341`. Esto es porque tengo ssigu corriendo en un contenedor de contenedores llamado `seq` y está en el puerto `5341`. Si lo está ejecutando localmente, puede utilizar `http://localhost:5341`.
También uso la clave API para poder usar la clave para especificar el nivel de registro dinámicamente (puede establecer una clave para aceptar sólo un cierto nivel de mensajes de registro).

Usted lo estableció en su siguiente instancia por ir a `http://<machine>:82` y haciendo clic en el engranaje de configuración en la parte superior derecha. A continuación, haga clic en el botón `API Keys` y añadir una nueva clave. Usted puede entonces utilizar esta clave en su `appsettings.json` archivo.

![Seq](seqapikey.png)

# Docker Composite

Ahora tenemos esta configuración que necesitamos para configurar nuestra aplicación ASP.NET para recoger una clave. Utilizo un `.env` archivo para almacenar mis secretos.

```dotenv
SEQ_DEFAULT_HASH="<adminpasswordhash>"
SEQ_API_KEY="<apikey>"
```

Entonces en mi archivo de composición Docker especifico que el valor debe ser inyectado como una variable de entorno en mi aplicación ASP.NET:

```docker
services:
  mostlylucid:
    image: scottgal/mostlylucid:latest
    ports:
      - 8080:8080
    restart: always
    labels:
        - "com.centurylinklabs.watchtower.enable=true"
    env_file:
      - .env
    environment:
      - Auth__GoogleClientId=${AUTH_GOOGLECLIENTID}
      - Auth__GoogleClientSecret=${AUTH_GOOGLECLIENTSECRET}
      - Auth__AdminUserGoogleId=${AUTH_ADMINUSERGOOGLEID}
      - SmtpSettings__UserName=${SMTPSETTINGS_USERNAME}
      - SmtpSettings__Password=${SMTPSETTINGS_PASSWORD}
      - Analytics__UmamiPath=${ANALYTICS_UMAMIPATH}
      - Analytics__WebsiteId=${ANALYTICS_WEBSITEID}
      - ConnectionStrings__DefaultConnection=${POSTGRES_CONNECTIONSTRING}
      - TranslateService__ServiceIPs=${EASYNMT_IPS}
      - Serilog__WriteTo__0__Args__apiKey=${SEQ_API_KEY}
    volumes:
      - /mnt/imagecache:/app/wwwroot/cache
      - /mnt/markdown/comments:/app/Markdown/comments
      - /mnt/logs:/app/logs
    networks:
      - app_network
```

Tenga en cuenta que la `Serilog__WriteTo__0__Args__apiKey` se establece en el valor de `SEQ_API_KEY` desde el `.env` archivo. El '0' es el índice de la `WriteTo` array en la ventana `appsettings.json` archivo.

# Caddy

Nota tanto para Seq como para mi aplicación ASP.NET He especificado que ambos pertenecen a mi `app_network` network. Esto es porque uso Caddy como un proxy inverso y está en la misma red. Esto significa que puedo usar el nombre del servicio como URL en mi archivo Caddy.

```caddy
{
    email scott.galloway@gmail.com
}
seq.mostlylucid.net
{
   reverse_proxy seq:80
}

http://seq.mostlylucid.net
{
   redir https://{host}{uri}
}
```

Así que esto es capaz de mapear `seq.mostlylucid.net` a mi siguiente instancia.

# Conclusión

Seq es una gran herramienta para registrar y monitorear su aplicación. Es fácil de configurar y utilizar e integra bien con Serilog. Lo he encontrado invaluable en la depuración de mis aplicaciones y estoy seguro de que usted también lo hará.