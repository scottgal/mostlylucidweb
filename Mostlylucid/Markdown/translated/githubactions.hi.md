# GiHHW क्रिया का उपयोग एक डॉकrer छवि को बनाने और धक्का देने के लिए करें

<datetime class="hidden">2024- 26- 302: 30</datetime>

यह एक सरल उदाहरण है कि कैसे GiHHWO क्रिया का प्रयोग करें ताकि एक पात्र के लिए एक डाक - चित्र बनाएँ और उसे धक्का दें ।

## पूर्वपाराईज़

- एक प्लगिन जो आप निर्माण और धक्का देने के लिए चाहते हैं के लिए मौजूद है.
- Git भंडार परियोजना के लिए मौजूद है.
- एक कंटेनर रजिस्ट्री छवि को पुश करने के लिए मौजूद है.
- एक डॉकer रजिस्ट्री उपयोगकर्ता का उपयोक्ता तथा पासवर्ड (बेबल गोपनीयों में)

इस परियोजना के लिए मैं मूल के साथ शुरू किया. वेनॉट A.NT परियोजना और डिफ़ॉल्ट डॉक फ़ाइल के द्वारा बनाया गया.

### डॉकनेवाला फ़ाइल

यह डॉकer फ़ाइल एक बहु चरण है कि परियोजना को बनाता है और फिर आउटपुट को एक स्थिर छवि में नक़ल करता है.

इस प्रोटॉट के लिए, जैसा कि मैं ऍल्रॉफ़ का उपयोग करता हूँ, मुझे नोड संस्थापित करने की भी ज़रूरत है.js और Toliz निर्माण कमांड चलाने के लिए.

```dockerfile
# Install Node.js v20.x
RUN apt-get update && apt-get install -y curl \
    && curl -fsSL https://deb.nodesource.com/setup_20.x -o nodesource_setup.sh \
    && bash nodesource_setup.sh \
    && apt-get install -y nodejs \
    && rm -f nodesource_setup.sh \
    && rm -rf /var/lib/apt/lists/*
```

यह डाउनलोड नोड के नवीनतम (फ़ाइल के समय में) नोड.js के संस्करण को डाउनलोड करता है और इसे छवि में स्थापित करता है.

फ़ाइल में बाद में

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

### Git क्रियाएँ

इस साइट के लिए GiHHHW क्रिया एक सरल निर्माण और धक्का कार्य है जो मुख्य शाखा के लिए एक धक्का पर शुरू होता है.
https://tka.com/seghk/ghk/ghk/ghttk/ kkhk/ kkthks.com

यह क्रिया भंडार की जाँच करती है, डॉक डॉकब में लॉग डॉक बिल्डर, डॉक बिल्डर परतों में कैश करता है, सुधार व डॉकर छवि को लोड करती है, और फिर छवि को डॉकएओने के लिए विवश करती है.

बंद करें

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
