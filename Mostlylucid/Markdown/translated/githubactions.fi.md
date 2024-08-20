# GitHub-toimintojen käyttäminen docker-kuvan rakentamiseen ja työntämiseen

<datetime class="hidden">2024-07-30T13:30</datetime>

Tämä on yksinkertainen esimerkki siitä, miten GitHub-toiminnoilla rakennetaan ja työnnetään docker-kuva konttirekisteriin.

## Edeltävät opinnot

- Projektia varten, jonka haluat rakentaa ja työntää, on olemassa docker-tiedosto.
- Projektia varten on olemassa GitHub-arkisto.
- Kuvan siirtämiseksi konttirekisteriin on olemassa konttirekisteri.
- Docker-rekisterin käyttäjätunnus ja salasana (GuitHub Secrets)

Tässä projektissa aloitin perus.NET Core ASP.NET -projektilla ja Riderin luomalla oletus Dockerfilellä.

### Dockerfile

Tämä Dockerfile on monivaiheinen rakennelma, joka rakentaa projektin ja kopioi sitten ulostulon aikakuvaan.

Tätä varten, kun käytän TailwindCSS:ää, minun täytyy asentaa myös Node.js ja pyörittää TailwindCSS:n rakennuskomentoa.

```dockerfile
# Install Node.js v20.x
RUN apt-get update && apt-get install -y curl \
    && curl -fsSL https://deb.nodesource.com/setup_20.x -o nodesource_setup.sh \
    && bash nodesource_setup.sh \
    && apt-get install -y nodejs \
    && rm -f nodesource_setup.sh \
    && rm -rf /var/lib/apt/lists/*
```

Tämä lataa viimeisimmän (kirjoitushetkellä) version Node.js:sta ja asentaa sen rakennuskuvaan.

Myöhemmin tiedostossa

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

### GitHub-toimet

Tämän sivuston GitHub-toiminto on yksinkertainen rakenne ja työntöliike, joka käynnistetään työnnöllä päähaaraan.
https://github.com/scottgal/mostlylucidweb/blob/main/.github/workflows/docker-image.yml

Tämä toiminto tarkistaa arkiston, kirjautuu Docker Hubiin, perustaa Docker Buildexin, piilottaa Dockerin kerrokset, rakentaa ja tagittaa Dockerin kuvan ja työntää kuvan Docker Hubiin.

Dockerin sävelletyssä tiedostossa

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
