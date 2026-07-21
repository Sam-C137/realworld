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

## Production Deployment

The root `Dockerfile` builds the Solid client first, copies it into the API's
`wwwroot`, publishes the API, and produces one non-root ASP.NET runtime image.
Aspire remains the local development orchestrator; `compose.yml` is the
production contract for the API, PostgreSQL, Redis, and optional database
browser.

### Environment

`.env.test` contains safe placeholder values matching every variable used by
Compose. Copy the entries into Dokploy or use it locally as a starting point:

```bash
docker compose --env-file .env.test config
```

Do not replace the placeholders in the committed file. Real database, JWT,
R2, Gateway, and deployment credentials belong in Dokploy. Keep the database
passwords URL-safe because Drizzle Gateway receives a PostgreSQL URL.

The API image is configured to trust forwarded headers because TLS terminates
at the Dokploy proxy. Do not publish the API container port directly to the
internet; assign a Dokploy domain to the API service on container port `8080`.
PostgreSQL and Redis have no host port mappings.

The API applies EF Core migrations during startup and aborts startup if a
migration fails. It never seeds data in Production. Keep the API at one replica
while startup migration is enabled.

### Drizzle Gateway

Gateway is in the Compose `tools` profile, so an ordinary deployment does not
start it. It has no public domain and binds its host port only to `127.0.0.1`.
It also connects as `realworld_reader`, a role restricted to reading tables and
sequences. `deploy/postgres/init-database-roles.sh` creates that reader and the
non-superuser `realworld_app` database owner used by the API when a fresh
Postgres volume is first initialized. The bootstrap `postgres` superuser is not
used by the API or Gateway.

For a Postgres volume that already existed before the initializer was added,
run the mounted initializer once:

```bash
docker compose --env-file .env exec postgres \
  sh /docker-entrypoint-initdb.d/20-init-database-roles.sh
```

To browse production temporarily, enable the profile in Dokploy by setting
`COMPOSE_PROFILES=tools` and redeploying. Do not assign Gateway a domain. Open
an SSH tunnel from your workstation:

```bash
ssh -N -L 4983:127.0.0.1:4983 deploy@your-vps
```

Visit `http://localhost:4983`, authenticate with `GATEWAY_MASTERPASS`, and set
`COMPOSE_PROFILES` back to an empty value when finished. The Gateway container
then disappears on the next redeploy while its configuration volume remains.
Before the first production start, replace the Gateway image's `latest` tag in
`DRIZZLE_GATEWAY_IMAGE` with the digest you validated so later pulls cannot
silently change the database tool.

### Continuous Delivery

`.github/workflows/ci.yml` runs for pushes to `dotnet-10-aspnetcore`. It builds
and tests .NET, checks and builds the client, then creates a database with the
previous commit's migrations and applies the current migrations over it. This
tests the same schema transition production will perform without connecting CI
to production data.

After every CI job passes, CI calls `.github/workflows/deploy.yml`. The reusable
workflow builds the exact successful commit and publishes both immutable
`sha-<commit>` and moving `dotnet-10-aspnetcore` tags to Docker Hub. If the
optional deployment webhook is configured, it asks Dokploy to pull the moving
tag after publication.

Create these GitHub Actions variables:

```text
DOCKERHUB_USERNAME=your-dockerhub-user
RW_DOTNET_DOCKERHUB_IMAGE=realworld-dotnet
```

Create these GitHub Actions secrets:

```text
DOCKERHUB_TOKEN=your-dockerhub-access-token
RW_DOTNET_DEPLOY_WEBHOOK_URL=optional-dokploy-deployment-webhook
```

Configure Dokploy's `RW_DOTNET_IMAGE` as the full image name, for example
`docker.io/your-dockerhub-user/realworld-dotnet`, and leave
`RW_DOTNET_IMAGE_TAG=dotnet-10-aspnetcore` for automatic deployments. To roll
back, replace that tag with a previously published `sha-<commit>` tag and
redeploy.

Before the first real deployment, configure automated PostgreSQL backups to
storage outside the VPS and perform at least one restore drill. A named Docker
volume survives container replacement, but it is not a backup.
