# Utilisation de GitHub Actions pour construire et pousser une image de docker

<datetime class="hidden">2024-07-30T13:30</datetime>

C'est un exemple simple de la façon d'utiliser GitHub Actions pour construire et pousser une image Docker vers un registre de conteneurs.

## Préalables

- Un fichier docker existe pour le projet que vous voulez construire et pousser.
- Un dépôt GitHub existe pour le projet.
- Un registre de conteneurs existe pour pousser l'image à.
- Nom d'utilisateur et mot de passe d'un registre de docker (dans les secrets de GuitHub)

Pour ce projet, j'ai commencé avec le projet de base.NET Core ASP.NET et le Dockerfile par défaut créé par Rider.

### Dockerfile

Ce fichier Dockerfile est une compilation multi-étapes qui construit le projet et copie ensuite la sortie sur une image d'exécution.

Pour ce proct, comme j'utilise TailwindCSS, j'ai aussi besoin d'installer Node.js et d'exécuter la commande TailwindCSS build.

```dockerfile
# Install Node.js v20.x
RUN apt-get update && apt-get install -y curl \
    && curl -fsSL https://deb.nodesource.com/setup_20.x -o nodesource_setup.sh \
    && bash nodesource_setup.sh \
    && apt-get install -y nodejs \
    && rm -f nodesource_setup.sh \
    && rm -rf /var/lib/apt/lists/*
```

Ceci télécharge la dernière version (au moment de l'écriture) de Node.js et l'installe dans l'image de construction.

Plus tard dans le fichier

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

### Actions GitHub

L'action GitHub pour ce site est une action simple de construction et de poussée qui est déclenchée sur une poussée vers la branche principale.
https://github.com/scottgal/mostlylucidweb/blob/main/.github/workflows/docker-image.yml

Cette action vérifie le dépôt, se connecte dans Docker Hub, met en place Docker Buildx, cache les couches Docker, construit et tagge l'image Docker, puis pousse l'image vers Docker Hub.

Dans le fichier de composition Docker

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
