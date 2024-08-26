# Χρήση ενεργειών GitHub για την κατασκευή και ώθηση μιας εικόνας docker

<datetime class="hidden">2024-07-30T13:30</datetime>

Αυτό είναι ένα απλό παράδειγμα για το πώς να χρησιμοποιήσετε τις δράσεις GitHub για να δημιουργήσετε και να ωθήσει μια εικόνα docker σε ένα μητρώο εμπορευματοκιβωτίων.

## Προαπαιτούμενα

- Ένα αρχείο Docker υπάρχει για το έργο που θέλετε να χτίσει και να ωθήσει.
- Υπάρχει αποθετήριο GitHub για το έργο.
- Ένα μητρώο εμπορευματοκιβωτίων υπάρχει για να ωθήσει την εικόνα σε.
- Όνομα χρήστη και κωδικός μητρώου Docker (στα GuitHub Secrets)

Για αυτό το έργο άρχισα με το βασικό έργο.NET Core ASP.NET και την προεπιλεγμένη Dockerfile δημιουργήθηκε από Rider.

### Φάκελος Docker

Αυτό το αρχείο Dockerfile είναι μια πολυβάθμια κατασκευή που χτίζει το έργο και στη συνέχεια αντιγράφει την έξοδο σε μια εικόνα runtime.

Για αυτό το proect, καθώς χρησιμοποιώ το TailwindCSS, πρέπει επίσης να εγκαταστήσω το Node.js και να εκτελέσω την εντολή κατασκευής του TailwindCSS.

```dockerfile
# Install Node.js v20.x
RUN apt-get update && apt-get install -y curl \
    && curl -fsSL https://deb.nodesource.com/setup_20.x -o nodesource_setup.sh \
    && bash nodesource_setup.sh \
    && apt-get install -y nodejs \
    && rm -f nodesource_setup.sh \
    && rm -rf /var/lib/apt/lists/*
```

Αυτό κατεβάζει την τελευταία (κατά τη στιγμή της γραφής) έκδοση του Node.js και την εγκαθιστά στην εικόνα κατασκευής.

Αργότερα στο αρχείο

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

### Ενέργειες GitHub

Η δράση GitHub για αυτό το site είναι μια απλή κατασκευή και ώθηση δράση που ενεργοποιείται σε μια ώθηση προς το κύριο υποκατάστημα.
https://github.com/scottgal/mostlyclearweb/blob/main/.github/workflows/docker-image.yml

Αυτή η δράση ελέγχει το αποθετήριο, καταγράφει στο Docker Hub, στήνει Docker Buildx, caches τα στρώματα Docker, χτίζει και ετικέτες την εικόνα Docker, και στη συνέχεια σπρώχνει την εικόνα στο Docker Hub.

Στο Docker συνθέτουν το αρχείο

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
