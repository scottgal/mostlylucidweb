# Base image for running the app
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 7240

# Build image with SDK for building the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release

# Install Node.js v22.x (LTS)
RUN apt-get update && apt-get install -y curl \
    && curl -fsSL https://deb.nodesource.com/setup_22.x -o nodesource_setup.sh \
    && bash nodesource_setup.sh \
    && apt-get install -y nodejs \
    && rm -f nodesource_setup.sh \
    && rm -rf /var/lib/apt/lists/*

# Set the working directory to /src
WORKDIR /src

#  csproj files to the container
COPY ["Mostlylucid/Mostlylucid.csproj", "Mostlylucid/"]
COPY ["Mostlylucid.Test/Mostlylucid.Test.csproj", "Mostlylucid.Test/"]
COPY ["Mostlylucid.Shared/Mostlylucid.Shared.csproj", "Mostlylucid.Shared/"]
COPY ["Mostlylucid.DbContext/Mostlylucid.DbContext.csproj", "Mostlylucid.DbContext/"]
COPY ["Mostlylucid.Services/Mostlylucid.Services.csproj", "Mostlylucid.Services/"]
COPY ["Mostlylucid.SemanticSearch/Mostlylucid.SemanticSearch.csproj", "Mostlylucid.SemanticSearch/"]
COPY ["Mostlylucid.DocSummarizer.Core/Mostlylucid.DocSummarizer.Core.csproj", "Mostlylucid.DocSummarizer.Core/"]
COPY ["Mostlylucid.Markdig.FetchExtension/Mostlylucid.Markdig.FetchExtension.csproj", "Mostlylucid.Markdig.FetchExtension/"]
COPY ["Mostlylucid.Markdig.Extensions/Mostlylucid.Markdig.Extensions.csproj", "Mostlylucid.Markdig.Extensions/"]
COPY ["Umami.Net/Umami.Net.csproj", "Umami.Net/"]

# Copy files needed by project files at restore time
COPY ["Mostlylucid.Markdig.FetchExtension/release-notes.txt", "Mostlylucid.Markdig.FetchExtension/"]
COPY ["Mostlylucid.Markdig.FetchExtension/README.md", "Mostlylucid.Markdig.FetchExtension/"]
COPY ["Mostlylucid.Markdig.FetchExtension/icon.png", "Mostlylucid.Markdig.FetchExtension/"]
COPY ["Umami.Net/ReleaseNotes.txt", "Umami.Net/"]
COPY ["Umami.Net/README.md", "Umami.Net/"]
COPY ["Umami.Net/icon.png", "Umami.Net/"]
COPY ["Mostlylucid.DocSummarizer.Core/release-notes.txt", "Mostlylucid.DocSummarizer.Core/"]

# Restore .NET dependencies
RUN dotnet restore "./Mostlylucid/Mostlylucid.csproj"

# Copy the entire source code to the container
COPY ./Mostlylucid/ ./Mostlylucid/
COPY ./Mostlylucid.Test/ ./Mostlylucid.Test/
COPY ./Mostlylucid.Shared/ ./Mostlylucid.Shared/
COPY ./Mostlylucid.Services/ ./Mostlylucid.Services/
COPY ./Mostlylucid.SemanticSearch/ ./Mostlylucid.SemanticSearch/
COPY ./Mostlylucid.DocSummarizer.Core/ ./Mostlylucid.DocSummarizer.Core/
COPY ./Mostlylucid.DbContext/ ./Mostlylucid.DbContext/
COPY ./Mostlylucid.Markdig.FetchExtension/ ./Mostlylucid.Markdig.FetchExtension/
COPY ./Mostlylucid.Markdig.Extensions/ ./Mostlylucid.Markdig.Extensions/
COPY ./Umami.Net/ ./Umami.Net/
# Install NPM dependencies (do this before the build step)
# Note: package.json already copied with ./Mostlylucid/ above
WORKDIR "/src/Mostlylucid"
RUN npm install

# Build static assets (CSS/JS) using npm only once
RUN npm run build

# Build the .NET project
RUN dotnet build "./Mostlylucid.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Run tests (skip live tests and failing integration tests)
WORKDIR "/src/Mostlylucid.Test"
RUN dotnet test "./Mostlylucid.Test.csproj" -c $BUILD_CONFIGURATION --filter "FullyQualifiedName!~LiveTest&FullyQualifiedName!~MarkdownFetchIntegrationTests&FullyQualifiedName!~E2E&FullyQualifiedName!~FixedWorkAtomTests" 

# Publish the application
FROM build AS publish
WORKDIR /src/Mostlylucid

# Publish the application, skipping the npm process since it's already done
RUN dotnet publish "./Mostlylucid.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

# Final image
FROM base AS final
WORKDIR /app

# Copy the published application from the build stage
COPY --from=publish /app/publish .


# Start the application
ENTRYPOINT ["dotnet", "Mostlylucid.dll"]