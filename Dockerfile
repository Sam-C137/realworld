# syntax=docker/dockerfile:1

FROM node:24-bookworm-slim AS client-build

WORKDIR /src
RUN npm install --global pnpm@10.15.0

COPY src/RealWorldApi.Client/package.json src/RealWorldApi.Client/pnpm-lock.yaml ./src/RealWorldApi.Client/
RUN --mount=type=cache,id=pnpm-store,target=/root/.local/share/pnpm/store \
    pnpm --dir src/RealWorldApi.Client install --frozen-lockfile

COPY src/RealWorldApi.Client ./src/RealWorldApi.Client
RUN mkdir -p src/RealWorldApi.Core \
    && pnpm --dir src/RealWorldApi.Client build


FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS api-build

WORKDIR /src
COPY Directory.Packages.props RealWorldApi.slnx ./
COPY src/RealWorldApi.Core/RealWorldApi.Core.csproj ./src/RealWorldApi.Core/
COPY src/RealWorldApi.Infrastructure/RealWorldApi.Infrastructure.csproj ./src/RealWorldApi.Infrastructure/
COPY src/RealWorldApi.ServiceDefaults/RealWorldApi.ServiceDefaults.csproj ./src/RealWorldApi.ServiceDefaults/
RUN dotnet restore src/RealWorldApi.Core/RealWorldApi.Core.csproj

COPY src ./src
COPY --from=client-build /src/src/RealWorldApi.Core/wwwroot ./src/RealWorldApi.Core/wwwroot
RUN dotnet publish src/RealWorldApi.Core/RealWorldApi.Core.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false


FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS runtime

RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=api-build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080 8081
HEALTHCHECK --interval=15s --timeout=5s --start-period=30s --retries=5 \
    CMD curl --fail --silent http://localhost:8081/health || exit 1

USER $APP_UID
ENTRYPOINT ["dotnet", "RealWorldApi.Core.dll"]
