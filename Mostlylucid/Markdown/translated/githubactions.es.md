# Usando las acciones de GitHub para construir y empujar una imagen Docker

<datetime class="hidden">2024-07-30T13:30</datetime>

Este es un ejemplo sencillo de cómo utilizar las acciones de GitHub para construir y empujar una imagen docker a un registro de contenedores.

## Requisitos previos

- Existe un archivo docker para el proyecto que desea construir y empujar.
- Para el proyecto existe un repositorio GitHub.
- Existe un registro de contenedores para empujar la imagen.
- Nombre de usuario y contraseña de un registro Docker (en GuitHub Secrets)

Para este proyecto empecé con el proyecto básico.NET Core ASP.NET y el Dockerfile por defecto creado por Rider.

### Dockerfile

Este Dockerfile es una compilación multietapa que construye el proyecto y luego copia la salida a una imagen en tiempo de ejecución.

Para este proyecto, como uso TailwindCSS, también necesito instalar Node.js y ejecutar el comando de compilación TailwindCSS.

```dockerfile
# Install Node.js v20.x
RUN apt-get update && apt-get install -y curl \
    && curl -fsSL https://deb.nodesource.com/setup_20.x -o nodesource_setup.sh \
    && bash nodesource_setup.sh \
    && apt-get install -y nodejs \
    && rm -f nodesource_setup.sh \
    && rm -rf /var/lib/apt/lists/*
```

Esto descarga la última versión (en el momento de escribir) de Node.js y la instala en la imagen de compilación.

Más tarde en el archivo

```dockerfile

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release

# Install Node.js v20.x
RUN apt-get update && apt-get install -y curl \
    && curl -fsSL https://deb.nodesource.com/setup_20.x -o nodesource_setup.sh \
    && bash nodesource_setup.sh \
    && apt-get install -y nodejs \
    && rm -f nodesource_setup.sh \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /src
COPY ["Mostlylucid/Mostlylucid.csproj", "Mostlylucid/"]
RUN dotnet restore "Mostlylucid/Mostlylucid.csproj"
COPY . .
WORKDIR "/src/Mostlylucid"

# Copy package.json and package-lock.json and install npm dependencies
COPY package*.json ./
RUN npm install

# Ensure npm-run-all is available and install npm dependencies
RUN npm --version
RUN npx tailwindcss -i ./src/css/main.css -o ./wwwroot/css/dist/main.css

RUN dotnet build "Mostlylucid.csproj" -c $BUILD_CONFIGURATION -o /app/build


FROM build AS publish
RUN dotnet publish "Mostlylucid.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Mostlylucid.dll"]
```

### Acciones de GitHub

La acción GitHub para este sitio es una simple acción de construcción y empuje que se activa en un empuje a la rama principal.
https://github.com/scottgal/mostlylucidweb/blob/main/.github/workflows/docker-image.yml

Esta acción comprueba el repositorio, registra el Docker Hub, configura Docker Buildx, almacena las capas Docker, construye y etiqueta la imagen Docker, y luego empuja la imagen al Docker Hub.

En el archivo de composición Docker

```yaml
name: Docker Image CI

on:
  push:
    branches: [ "main" ]
  pull_request:
    branches: [ "main" ]

jobs:
  build-and-push:
    runs-on: ubuntu-latest

    steps:
    - name: Check out the repository
      uses: actions/checkout@v4

    - name: Log in to Docker Hub
      run: echo "${{ secrets.DOCKER_HUB_ACCESS_TOKEN }}" | docker login -u "${{ secrets.DOCKER_HUB_USER_NAME }}" --password-stdin

    - name: Set up Docker Buildx
      uses: docker/setup-buildx-action@v2

    - name: Cache Docker layers
      uses: actions/cache@v3
      with:
        path: /tmp/.buildx-cache
        key: ${{ runner.os }}-buildx-${{ github.sha }}
        restore-keys: |
          ${{ runner.os }}-buildx-

    - name: Build and tag the Docker image
      id: build
      run: |
        TIMESTAMP=$(date +%s)
        echo "TIMESTAMP=$TIMESTAMP" >> $GITHUB_ENV
        docker build . --file Mostlylucid/Dockerfile --tag ${{ secrets.DOCKER_HUB_USER_NAME }}/mostlylucid:latest --tag ${{ secrets.DOCKER_HUB_USER_NAME }}/mostlylucid:$TIMESTAMP

    - name: Push the Docker image to Docker Hub
      run: |
        docker push ${{ secrets.DOCKER_HUB_USER_NAME }}/mostlylucid:latest
        docker push ${{ secrets.DOCKER_HUB_USER_NAME }}/mostlylucid:${{ env.TIMESTAMP }}
```

<!--category-- Docker, GitHub Actions -->
