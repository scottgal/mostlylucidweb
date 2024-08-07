# Usare le azioni GitHub per creare e premere un'immagine docker

<datetime class="hidden">2024-07-30T13:30</datetime>

Questo è un semplice esempio di come usare le azioni GitHub per costruire e spingere un'immagine docker in un registro dei container.

## Prerequisiti

- Esiste un file docker per il progetto che si desidera costruire e spingere.
- Un repository GitHub esiste per il progetto.
- Esiste un registro dei container su cui spingere l'immagine.
- Nome utente e password di un registro di sistema docker (in GuitHub Secrets)

Per questo progetto ho iniziato con il progetto base.NET Core ASP.NET e il file Docker predefinito creato da Rider.

### File di Docker

Questo Dockerfile è un build multi-stage che costruisce il progetto e poi copia l'output in un'immagine runtime.

Per questo proect, come uso TailwindCSS, ho anche bisogno di installare Node.js ed eseguire il comando TailwindCSS build.

```dockerfile
# Install Node.js v20.x
RUN apt-get update && apt-get install -y curl \
    && curl -fsSL https://deb.nodesource.com/setup_20.x -o nodesource_setup.sh \
    && bash nodesource_setup.sh \
    && apt-get install -y nodejs \
    && rm -f nodesource_setup.sh \
    && rm -rf /var/lib/apt/lists/*
```

Questo scarica la versione più recente (al momento della scrittura) di Node.js e la installa nell'immagine di compilazione.

Più tardi nel file

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

### Azioni GitHub

L'azione GitHub per questo sito è una semplice azione di build and push che viene attivata su una spinta al ramo principale.
https://github.com/scottgal/mostlylucidweb/blob/main/.github/workflows/docker-image.yml

Questa azione controlla il repository, registra in Docker Hub, imposta Docker Buildx, cache i livelli di Docker, costruisce e etichetta l'immagine di Docker, e poi spinge l'immagine in Docker Hub.

Nel file di composizione del docker

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
