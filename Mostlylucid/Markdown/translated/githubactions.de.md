# Mit GitHub Aktionen ein Docker-Image erstellen und schieben

<datetime class="hidden">2024-07-30T13:30</datetime>

Dies ist ein einfaches Beispiel dafür, wie man GitHub Actions zum Erstellen und Schieben eines Docker-Images in eine Container-Registry verwendet.

## Voraussetzungen

- Für das Projekt, das Sie erstellen und schieben möchten, existiert eine Docker-Datei.
- Für das Projekt existiert ein GitHub-Repository.
- Eine Container-Registrierung existiert, um das Bild zu schieben.
- Benutzername und Passwort einer docker registry (in GuitHub Secrets)

Für dieses Projekt begann ich mit dem grundlegenden.NET Core ASP.NET Projekt und dem Standard Dockerfile, das von Rider erstellt wurde.

### Dockerfile

Dieses Dockerfile ist ein mehrstufiger Build, der das Projekt erstellt und dann die Ausgabe in ein Laufzeitbild kopiert.

Für diesen Proect, da ich TailwindCSS verwende, muss ich auch Node.js installieren und den TailwindCSS Build Befehl ausführen.

```dockerfile
# Install Node.js v20.x
RUN apt-get update && apt-get install -y curl \
    && curl -fsSL https://deb.nodesource.com/setup_20.x -o nodesource_setup.sh \
    && bash nodesource_setup.sh \
    && apt-get install -y nodejs \
    && rm -f nodesource_setup.sh \
    && rm -rf /var/lib/apt/lists/*
```

Dies lädt die neueste (zum Zeitpunkt des Schreibens) Version von Node.js herunter und installiert sie in das Build-Image.

Später in der Datei

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

### GitHub-Maßnahmen

Die GitHub Action für diese Seite ist eine einfache Build- und Push-Aktion, die auf einem Push zum Hauptzweig ausgelöst wird.
Veröffentlichungen der Europäischen Gemeinschaften: Januar 2002.

Diese Aktion überprüft das Projektarchiv, loggt sich in Docker Hub ein, richtet Docker Buildx ein, speichert die Docker-Ebenen, baut und taggt das Docker-Image und schiebt das Bild in Docker Hub.

In der docker-Datei komponieren

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
