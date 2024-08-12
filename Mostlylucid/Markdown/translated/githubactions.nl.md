# GitHub-acties gebruiken om een image-bestand te bouwen en te pushen

<datetime class="hidden">2024-07-30T13:30</datetime>

Dit is een eenvoudig voorbeeld van hoe GitHub Acties te gebruiken om een docker image te bouwen en te pushen naar een container register.

## Vereisten

- Er bestaat een dockerbestand voor het project dat u wilt bouwen en pushen.
- Er bestaat een GitHub repository voor het project.
- Er bestaat een containerregister om de afbeelding naar toe te duwen.
- Gebruikersnaam en wachtwoord van een dockerregister (in GuitHub Secrets)

Voor dit project ben ik begonnen met het basic.NET Core ASP.NET project en de standaard Dockerfile gemaakt door Rider.

### Dockerbestand

Deze Dockerfile is een multi-stage build die het project bouwt en vervolgens de uitvoer kopieert naar een runtime image.

Voor deze proect, als ik TailwindCSS gebruik, moet ik ook Node.js installeren en het TailwindCSS build commando uitvoeren.

```dockerfile
# Install Node.js v20.x
RUN apt-get update && apt-get install -y curl \
    && curl -fsSL https://deb.nodesource.com/setup_20.x -o nodesource_setup.sh \
    && bash nodesource_setup.sh \
    && apt-get install -y nodejs \
    && rm -f nodesource_setup.sh \
    && rm -rf /var/lib/apt/lists/*
```

Dit downloadt de nieuwste (op het moment van schrijven) versie van Node.js en installeert het in de build image.

Later in het bestand

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

### GitHub-acties

De GitHub Action voor deze site is een eenvoudige build en push actie die wordt geactiveerd op een push naar de hoofdbranch.
https://github.com/scottgal/mostlylucidweb/blob/main/.github/workflows/docker-image.yml

Deze actie controleert de repository, logt in Docker Hub, zet Docker Buildx op, caches de Docker lagen, bouwt en tags de Docker afbeelding, en dan duwt de afbeelding naar Docker Hub.

In het dockerbestand samenstellen

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
