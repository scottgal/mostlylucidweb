# Uso de Docker Compose para Dependencias de Desarrollo

<!--category-- Docker -->
<datetime class="hidden">2024-08-09T17:17</datetime>

# Introducción

Cuando desarrollamos software tradicionalmente hacíamos girar una base de datos, una cola de mensajes, una caché y tal vez algunos otros servicios. Esto puede ser un dolor para manejar, especialmente si usted está trabajando en varios proyectos. Docker Compose es una herramienta que le permite definir y ejecutar aplicaciones Docker multicontenedor. Es una gran manera de gestionar sus dependencias de desarrollo.

En este post, te mostraré cómo usar Docker Compose para gestionar tus dependencias de desarrollo.

[TOC]

# Requisitos previos

Primero tendrás que instalar el escritorio Docker en cualquier plataforma que estés usando. Puedes descargarlo desde [aquí](https://www.docker.com/products/docker-desktop).

**NOTA: He encontrado que en Windows realmente necesita ejecutar Docker Desktop instalador como administrador para asegurarse de que se instala correctamente.**

# Crear un archivo Docker Compose

Docker Compose utiliza un archivo YAML para definir los servicios que desea ejecutar. Aquí hay un ejemplo de un simple `devdeps-docker-compose.yml` archivo que define un servicio de base de datos y un servicio de correo electrónico:

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

Nota aquí He especificado volúmenes para persistir los datos para cada servicio, aquí he especificado

```yaml
    volumes:
      # This is where smtp4dev stores the database..
      - e:/smtp4dev-data:/smtp4dev
    volumes:
        - e:/data:/var/lib/postgresql/data  # Map e:\data to the PostgreSQL data folder
```

Esto asegura que los datos persistan entre los recorridos de los contenedores.

También especifico un `env_file` para la `postgres` servicio. Este es un archivo que contiene variables de entorno que se pasan al contenedor.
Puede ver una lista de variables de entorno que se pueden pasar al contenedor PostgreSQL [aquí](https://www.docker.com/blog/how-to-use-the-postgres-docker-official-image/#1-Environment-variables).
Aquí hay un ejemplo de un `.env` archivo:

```shell
POSTGRES_DB=postgres
POSTGRES_USER=postgres
POSTGRES_PASSWORD=<somepassword>
```

Esto configura una base de datos predeterminada, contraseña y usuario para PostgreSQL.

Aquí también ejecuto el servicio SMTP4Dev, esta es una gran herramienta para probar la funcionalidad de correo electrónico en su aplicación. Puedes encontrar más información al respecto. [aquí](https://github.com/rnwood/smtp4dev/wiki/Installation#how-to-run-smtp4dev-in-docker).

Si miras en mi `appsettings.Developmet.json` archivo que verá Tengo la siguiente configuración para el servidor SMTP:

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

Esto funciona para SMTP4Dev y me permite probar esta funcionalidad (puedo enviar a cualquier dirección, y ver el correo electrónico en la interfaz SMTP4Dev en http://localhost:3002/).

Una vez que estés seguro de que todo está funcionando puedes probar en un servidor SMTP real como GMAIL (por ejemplo, ver [aquí](addingasyncsendingforemails) para saber cómo hacerlo)

# Funcionamiento de los servicios

Para ejecutar los servicios definidos en el `devdeps-docker-compose.yml` archivo, es necesario ejecutar el siguiente comando en el mismo directorio que el archivo:

```shell
docker compose -f .\devdeps-docker-compose.yml up -d
```

Tenga en cuenta que debe ejecutarlo inicialmente de esta manera; esto asegura que puede ver los elementos de configuración pasados desde el `.env` archivo.

```shell
docker compose -f .\devdeps-docker-compose.yml config
```

Ahora si usted mira en Docker Desktop usted puede ver estos servicios en ejecución

![Escritorio Docker](dockerdesktopdev.png)