# RealWorld API

## Frontend Integration

The frontend currently runs as a static SPA hosted by `RealWorldApi.Core`.
This keeps deployment to one ASP.NET Core process while leaving the door open
for a later SSR migration if public article/profile SEO becomes important.

### Local Development

The easiest local path is to run the Aspire AppHost. It starts the API and the
Vite client together, and passes the API endpoint to Vite for the `/api` proxy:

```bash
dotnet run --project src/RealWorldApi.AppHost/RealWorldApi.AppHost.csproj
```

You can still run the frontend by itself when you are only working on UI:

```bash
cd src/RealWorldApi.Client
pnpm dev
```

Vite serves the client on port `5173` and proxies `/api` calls to the local
ASP.NET API configured in `src/RealWorldApi.Client/vite.config.ts`. Outside
Aspire, the proxy falls back to `https://localhost:7198`.

Aspire is currently used for local orchestration only. Production still uses
the static SPA build served by `RealWorldApi.Core`; keep building the client
before publishing the API.

### Production Build

Build the client before publishing or running the API as a production app:

```bash
cd src/RealWorldApi.Client
pnpm build
```

The Vite build writes directly to:

```text
src/RealWorldApi.Core/wwwroot
```

That replaces the manual copy step:

```bash
cp -r dist/* ../RealWorldApi.Core/wwwroot/
```

The API serves files from `wwwroot` and falls back to `index.html` for SPA
routes in non-development environments. Requests under `/api`, `/docs`,
`/openapi`, and `/health` are excluded from the SPA fallback so missing API or
tooling routes still return `404`.

### Docker Deployment Shape

A future Docker build should use a frontend build stage before publishing the
API:

```dockerfile
# frontend stage
WORKDIR /src/src/RealWorldApi.Client
RUN pnpm install --frozen-lockfile
RUN pnpm build

# api stage
WORKDIR /src
RUN dotnet publish src/RealWorldApi.Core/RealWorldApi.Core.csproj -c Release -o /app
```

The important invariant is that the client build runs before `dotnet publish`,
so the populated `RealWorldApi.Core/wwwroot` folder is included in the API
publish output.
