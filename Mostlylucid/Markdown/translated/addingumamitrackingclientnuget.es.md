# Añadiendo paquete Nuget de cliente de seguimiento de Umami

<!--category-- ASP.NET, Umami, Nuget -->
<datetime class="hidden">2024-08-28T02:00</datetime>

# Introducción

Ahora tengo el cliente Umami; necesito empaquetarlo y ponerlo disponible como un paquete Nuget. Este es un proceso bastante simple, pero hay algunas cosas de las que ser conscientes.

[TOC]

# Crear el paquete Nuget

## Versión

Decidí copiar [Khalid](@khalidabuhakmeh@mastodon.social) y utilizar el excelente paquete minver para la versión de mi paquete Nuget. Este es un paquete simple que utiliza la etiqueta git version para determinar el número de versión.

Para usarlo, simplemente añadí lo siguiente a mi `Umami.Net.csproj` archivo:

```xml
    <PropertyGroup>
    <MinVerTagPrefix>v</MinVerTagPrefix>
</PropertyGroup>
```

De esa manera puedo etiquetar mi versión con un `v` y el paquete será versionado correctamente.

```bash
 git tag v0.0.8       
 git push origin v0.0.8

```

Empujará esta etiqueta, entonces tengo una configuración de GitHub Acción para esperar a que la etiqueta y construir el paquete Nuget.

## Construyendo el paquete Nuget

Tengo una acción GitHub que construye el paquete Nuget y lo empuja al repositorio de paquetes GitHub. Este es un proceso simple que utiliza el `dotnet pack` comando para construir el paquete y luego el `dotnet nuget push` para empujarlo al repositorio de pepitas.

```yaml
name: Publish Umami.NET
on:
  push:
    tags:
      - 'v*.*.*'  # This triggers the action for any tag that matches the pattern v1.0.0, v2.1.3, etc.

jobs:
  publish:
    runs-on: ubuntu-latest

    steps:
    - name: Checkout code
      uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.x' # Specify the .NET version you need

    - name: Restore dependencies
      run: dotnet restore ./Umami.Net/Umami.Net.csproj

    - name: Build project
      run: dotnet build --configuration Release ./Umami.Net/Umami.Net.csproj --no-restore

    - name: Pack project
      run: dotnet pack --configuration Release ./Umami.Net/Umami.Net.csproj --no-build --output ./nupkg

    - name: Publish to NuGet
      run: dotnet nuget push ./nupkg/*.nupkg --source https://api.nuget.org/v3/index.json --api-key ${{ secrets.UMAMI_NUGET_API_KEY }}
      env:
        NUGET_API_KEY: ${{ secrets.UMAMI_NUGET_API_KEY }}
```

### Añadiendo Readme e Icono

Esto es bastante simple, añado un `README.md` archivo a la raíz del proyecto y un `icon.png` archivo a la raíz del proyecto. Los `README.md` archivo se utiliza como la descripción del paquete y el `icon.png` archivo se utiliza como el icono para el paquete.

```xml
    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
        <LangVersion>12</LangVersion>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>

        <IsPackable>true</IsPackable>
        <PackageId>Umami.Net</PackageId>
        <Authors>Scott Galloway</Authors>
        <PackageIcon>icon.png</PackageIcon>
        <RepositoryUrl>https://github.com/scottgal/mostlylucidweb/tree/main/Umami.Net</RepositoryUrl>
        <PackageLicenseExpression>MIT</PackageLicenseExpression>
        <PackageTags>web</PackageTags>
        <PackageReadmeFile>README.md</PackageReadmeFile>
        <Description>
           Adds a simple Umami endpoint to your ASP.NET Core application.
        </Description>
    </PropertyGroup>
```

En mi archivo README.md tengo un enlace al repositorio GitHub y una descripción del paquete.

Reproducido a continuación:

# Umami.Net

Este es un cliente.NET Core para la API de seguimiento de Umami.
Se basa en el cliente Umami Node, que se puede encontrar [aquí](https://github.com/umami-software/node).

Puedes ver cómo configurar Umami como contenedor de contenedores [aquí](https://www.mostlylucid.net/blog/usingumamiforlocalanalytics).
Puedes leer más detalles sobre su creación en mi blog [aquí](https://www.mostlylucid.net/blog/addingumamitrackingclientfollowup).

Para utilizar este cliente necesita la siguiente configuración de appsettings.json:

```json
{
  "Analytics":{
    "UmamiPath" : "https://umamilocal.mostlylucid.net",
    "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee"
  },
}
```

Dónde `UmamiPath` es el camino a su instancia Umami y `WebsiteId` es el id del sitio web que desea rastrear.

Para utilizar el cliente que necesita para añadir lo siguiente a su `Program.cs`:

```csharp
using Umami.Net;

services.SetupUmamiClient(builder.Configuration);
```

Esto añadirá el cliente Umami a la colección de servicios.

A continuación, puede utilizar el cliente de dos maneras:

1. Inyecte la `UmamiClient` en su clase y llamar a la `Track` método:

```csharp
 // Inject UmamiClient umamiClient
 await umamiClient.Track("Search", new UmamiEventData(){{"query", encodedQuery}});
```

2. Usar la `UmamiBackgroundSender` para realizar un seguimiento de los acontecimientos en segundo plano (esto utiliza un `IHostedService` para enviar eventos en segundo plano:

```csharp
 // Inject UmamiBackgroundSender umamiBackgroundSender
await umamiBackgroundSender.Track("Search", new UmamiEventData(){{"query", encodedQuery}});
```

El cliente enviará el evento a la API de Umami y se almacenará.

Los `UmamiEventData` es un diccionario de pares de valores clave que se enviará a la API de Umami como los datos del evento.

Además, hay métodos de nivel más bajo que se pueden utilizar para enviar eventos a la API de Umami.

En ambos `UmamiClient` y `UmamiBackgroundSender` puede llamar al siguiente método.

```csharp


 Send(UmamiPayload? payload = null, UmamiEventData? eventData = null,
        string eventType = "event")
```

Si no pasas en un `UmamiPayload` objeto, el cliente creará uno para usted utilizando el `WebsiteId` de la appsettings.json.

```csharp
    public  UmamiPayload GetPayload(string? url = null, UmamiEventData? data = null)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var request = httpContext?.Request;

        var payload = new UmamiPayload
        {
            Website = settings.WebsiteId,
            Data = data,
            Url = url ?? httpContext?.Request?.Path.Value,
            IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString(),
            UserAgent = request?.Headers["User-Agent"].FirstOrDefault(),
            Referrer = request?.Headers["Referer"].FirstOrDefault(),
           Hostname = request?.Host.Host,
        };
        
        return payload;
    }

```

Se puede ver que esto pobla el `UmamiPayload` objeto con el `WebsiteId` de la appsettings.json, la `Url`, `IpAddress`, `UserAgent`, `Referrer` y `Hostname` desde el `HttpContext`.

NOTA: eventType sólo puede ser "evento" o "identificar" según la API de Umami.

# Conclusión

Así que ahora puede instalar Umami.Net desde Nuget y usarlo en su aplicación ASP.NET Core. Espero que le resulte útil. Seguiré retocando y agregando pruebas en posts futuros.