# syntax=docker/dockerfile:1
# Build context = repository root (so the shared props + all referenced projects are available).
#   docker build -f src/BengalTex.ERP.Api/Dockerfile -t bengaltex-api .

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first (cached layer) — copy solution-level files + every csproj, then restore.
COPY *.sln Directory.Build.props Directory.Packages.props global.json ./
COPY src/ ./src/
RUN dotnet restore src/BengalTex.ERP.Api/BengalTex.ERP.Api.csproj

# Publish a trimmed, Release output.
RUN dotnet publish src/BengalTex.ERP.Api/BengalTex.ERP.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Kestrel listens on 8080 inside the container (TLS terminated by the reverse proxy / nginx).
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

# Persisted uploads live under /app/uploads — mount a volume here (see docker-compose).
ENTRYPOINT ["dotnet", "BengalTex.ERP.Api.dll"]
