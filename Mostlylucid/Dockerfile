# Base image for running the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build image with SDK for building the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release

# Install Node.js v20.x
RUN apt-get update && apt-get install -y curl \
    && curl -fsSL https://deb.nodesource.com/setup_20.x -o nodesource_setup.sh \
    && bash nodesource_setup.sh \
    && apt-get install -y nodejs \
    && rm -f nodesource_setup.sh \
    && rm -rf /var/lib/apt/lists/*

# Set the working directory to /src
WORKDIR /src

# Copy the solution file and all projects to the container
COPY ["Mostlylucid/Mostlylucid.csproj", "Mostlylucid/"]
COPY ["Mostlylucid.Test/Mostlylucid.Test.csproj", "Mostlylucid.Test/"]

# Restore dependencies for all projects in the solution
RUN dotnet restore "./Mostlylucid/Mostlylucid.csproj"

# Copy the entire source code to the container
COPY ./Mostlylucid/ ./Mostlylucid/
COPY ./Mostlylucid.Test/ ./Mostlylucid.Test/

# Install NPM dependencies
WORKDIR "/src/Mostlylucid"
COPY package*.json ./
RUN npm install

# Build the project
RUN dotnet build "./Mostlylucid.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Run tests
WORKDIR "/src/Mostlylucid.Test"
RUN dotnet test "./Mostlylucid.Test.csproj" 

# Publish the application, ensuring all referenced projects are included
FROM build AS publish
WORKDIR /src/Mostlylucid
RUN dotnet publish "./Mostlylucid.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

# Final image
FROM base AS final
WORKDIR /app

# Copy the published application from the build stage
COPY --from=publish /app/publish .

# Copy additional resources
COPY ./Mostlylucid/Markdown /app/Markdown
COPY ./Mostlylucid/Email/Templates /app/Email/Templates

# Start the application
ENTRYPOINT ["dotnet", "Mostlylucid.dll"]