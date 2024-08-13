# استخدام إجراءات GigTHub إلى بناء و دفع a صورة

<datetime class="hidden">2024-07-30TT 13:30</datetime>

هذا مثال بسيط لكيفية استخدام إجراءات GigtHub لبناء ودفع صورة docker إلى سجل حاويات.

## النفقات قبل الاحتياجات

- يوجد ملف Docrker للمشروع الذي تريد بناؤه ودفعه.
- يوجد مستودع جيت هوب للمشروع.
- ويوجد سجل للحاويات لدفع الصورة إليه.
- اسم المستخدم و كلمة السر (في سرر GeetHub)

وبالنسبة لهذا المشروع، بدأت بمشروعي الأساسي.NET الأساسي ASP.NET ومشروع Dockerfile الافتراضي الذي أنشأه رايدر.

### 

هذا Dokerfile هو a متعدد المراحل بناء مشروع ثم نسخ مخرجات إلى a وقت صورة.

من أجل هذه النتيجة، كما أستخدم TaylwindCSS، أنا بحاجة أيضا لتركيب نودي.js وتشغيل قيادة بناء TailwindCSS.

```dockerfile
# Install Node.js v20.x
RUN apt-get update && apt-get install -y curl \
    && curl -fsSL https://deb.nodesource.com/setup_20.x -o nodesource_setup.sh \
    && bash nodesource_setup.sh \
    && apt-get install -y nodejs \
    && rm -f nodesource_setup.sh \
    && rm -rf /var/lib/apt/lists/*
```

هذه تنزيلات أحدث (في وقت كتابة) النسخة من Node.js وتثبيتها في صورة البناء.

لاحقاً في الملف

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

### جو_____

عمل GitHub لهذا الموقع هو مجرد عمل بناء ودفع بسيط الذي يتم تشغيله على دفع إلى الفرع الرئيسي.
https://github.com/scottgal/forstallylucidweb/blob/main/.gigethub/workfrows/docer-image.yml

هذا الإجراء يفحص المستودع، يسجّل إلى Doker hub، يُثبّت Doker Boyx، يُخبأ طبقات Doker، يُبني ويُلصق صورة Doker، ثمّ يدفع الصورة إلى Doker hop.

في ملفّ المُثَمّر

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
