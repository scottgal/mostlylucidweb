FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Mostlylucid.SchedulerService/Mostlylucid.SchedulerService.csproj", "Mostlylucid.SchedulerService/"]
COPY ["Mostlylucid.Shared/Mostlylucid.Shared.csproj", "Mostlylucid.Shared/"]
COPY ["Mostlylucid.DbContext/Mostlylucid.DbContext.csproj", "Mostlylucid.DbContext/"]
COPY ["Mostlylucid.Services/Mostlylucid.Services.csproj", "Mostlylucid.Services/"]

RUN dotnet restore "Mostlylucid.SchedulerService/Mostlylucid.SchedulerService.csproj"

# Copy the entire source code to the container
COPY ./Mostlylucid.SchedulerService/ ./Mostlylucid.SchedulerService/
COPY ./Mostlylucid.Shared/ ./Mostlylucid.Shared/
COPY ./Mostlylucid.Services/ ./Mostlylucid.Services/
COPY ./Mostlylucid.DbContext/ ./Mostlylucid.DbContext/

WORKDIR "/src/Mostlylucid.SchedulerService"
RUN dotnet build "Mostlylucid.SchedulerService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
RUN dotnet publish "Mostlylucid.SchedulerService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Mostlylucid.SchedulerService.dll"]