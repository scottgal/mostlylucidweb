# Använda GitHub åtgärder för att bygga och driva en docker bild

<datetime class="hidden">2024-07-30T13:30 Ordförande</datetime>

Detta är ett enkelt exempel på hur man använder GitHub-åtgärder för att bygga och skjuta en dockerbild till ett containerregister.

## Förutsättningar

- En dockerfil finns för projektet du vill bygga och driva.
- Ett GitHub arkiv finns för projektet.
- Ett containerregister finns för att trycka bilden till.
- Ett dockerregisters användarnamn och lösenord (i GuitHub Secrets)

För detta projekt började jag med det grundläggande.NET Core ASP.NET-projektet och standard Dockerfile skapad av Rider.

### Dockerfil

Denna Dockerfil är en flerstegsbyggnad som bygger projektet och sedan kopierar utdata till en runtime-bild.

För denna proekt, som jag använder TailwindCSS, Jag måste också installera Node.js och köra TailwindCSS byggkommandot.

```dockerfile
# Install Node.js v20.x
RUN apt-get update && apt-get install -y curl \
    && curl -fsSL https://deb.nodesource.com/setup_20.x -o nodesource_setup.sh \
    && bash nodesource_setup.sh \
    && apt-get install -y nodejs \
    && rm -f nodesource_setup.sh \
    && rm -rf /var/lib/apt/lists/*
```

Detta laddar ner den senaste (vid tidpunkten för skrivande) versionen av Node.js och installerar den i byggavbildningen.

Senare i filen

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

### GitHub-åtgärder

GitHub Action för denna webbplats är en enkel bygga och push åtgärder som utlöses på en push till huvudgrenen.
https://github.com/scottgal/mestlylucidweb/blob/main/.github/workflows/docker-image.yml

Denna åtgärd kontrollerar arkivet, loggar in i Docker Hub, sätter upp Docker Buildx, caches Docker lager, bygger och taggar Docker bilden, och sedan skjuter bilden till Docker Hub.

I Docker komponera fil

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
