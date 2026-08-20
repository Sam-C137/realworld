# Reusable .NET + SPA Project Blueprint

Use this document to bootstrap a production-oriented .NET application with a
React or Solid SPA, Aspire orchestration, PostgreSQL, Redis, OpenTelemetry,
object storage, email, integration tests, containers, and branch-based CI/CD.

This is an opinionated starting point, not a command sequence to follow without
review. Replace `DifferentProject` everywhere, choose the client template, pin
current compatible package and image versions, and remove infrastructure the
product does not need.

## Baseline Decisions

- `.NET 10` and the XML `.slnx` solution format.
- `pnpm` and Vite with either `solid-ts` or `react-ts`.
- `DifferentProject.Core` is the executable API, feature code, and composition
  root. For a larger domain, split it later into `Api`, `Application`, and
  `Domain` projects instead of turning every folder into a project immediately.
- `DifferentProject.Infrastructure` is a class library containing EF Core,
  migrations, and adapters for external systems. It is not another web host.
- `DifferentProject.AppHost` orchestrates local development only. Production
  runs the published API image and its dependencies through Compose or another
  scheduler.
- `DifferentProject.ServiceDefaults` owns health checks, service discovery,
  HTTP resilience, and OpenTelemetry wiring shared by executable services.
- Unit tests do not start infrastructure. Integration tests use
  `WebApplicationFactory` and Testcontainers for real PostgreSQL and Redis.
- The production image contains the built SPA in the API's `wwwroot`; no Node
  server is required at runtime.
- Database migrations are tested against both an empty database and the schema
  from the branch/revision being upgraded. CI never migrates production.
- Production secrets live in the deployment platform, never in committed env
  files, Compose files, or GitHub variables.

## Resulting Structure

```text
DifferentProject/
|-- .github/
|   `-- workflows/
|       |-- ci.yml
|       `-- deploy.yml
|-- deploy/
|   `-- postgres/
|       `-- init-database-roles.sh
|-- scripts/
|   |-- add-migration.sh
|   |-- check.sh
|   `-- generate-secrets.sh
|-- src/
|   |-- DifferentProject.AppHost/
|   |-- DifferentProject.Client/
|   |-- DifferentProject.Core/
|   |-- DifferentProject.Infrastructure/
|   `-- DifferentProject.ServiceDefaults/
|-- tests/
|   |-- DifferentProject.IntegrationTests/
|   `-- DifferentProject.UnitTests/
|-- .dockerignore
|-- .env.example
|-- .gitignore
|-- compose.yml
|-- Directory.Build.props
|-- Directory.Packages.props
|-- Dockerfile
|-- DifferentProject.slnx
|-- global.json
`-- README.md
```

## Prerequisites

Install and verify:

```bash
dotnet --version
node --version
pnpm --version
docker version
git --version
```

Use a current .NET SDK, Aspire templates compatible with that SDK, and Node LTS
or the current project-approved Node version. Pin them in `global.json`, CI,
and the Dockerfile so local and remote builds agree.

## Bootstrap Script

Run this from the directory that should contain the new repository:

```bash
#!/usr/bin/env bash
set -euo pipefail

PROJECT="${1:-DifferentProject}"
CLIENT_TEMPLATE="${2:-solid-ts}" # solid-ts or react-ts

if [[ ! "$PROJECT" =~ ^[A-Za-z][A-Za-z0-9]*$ ]]; then
  echo "PROJECT must be a C#-safe identifier such as BillingApi" >&2
  exit 1
fi

if [[ "$CLIENT_TEMPLATE" != "solid-ts" && "$CLIENT_TEMPLATE" != "react-ts" ]]; then
  echo "Client template must be solid-ts or react-ts" >&2
  exit 1
fi

PROJECT_SLUG=$(printf '%s' "$PROJECT" | tr '[:upper:]' '[:lower:]')
ROOT="$PWD/$PROJECT"

if [[ -e "$ROOT" ]]; then
  echo "$ROOT already exists" >&2
  exit 1
fi

mkdir -p "$ROOT/src" "$ROOT/tests" "$ROOT/scripts" "$ROOT/deploy/postgres"
cd "$ROOT"

git init
dotnet new globaljson --sdk-version "$(dotnet --version)" --roll-forward latestFeature
dotnet new sln --format slnx --name "$PROJECT"

dotnet new webapi --use-controllers \
  --name "$PROJECT.Core" \
  --output "src/$PROJECT.Core"

dotnet new classlib \
  --name "$PROJECT.Infrastructure" \
  --output "src/$PROJECT.Infrastructure"

dotnet new xunit \
  --name "$PROJECT.UnitTests" \
  --output "tests/$PROJECT.UnitTests"

dotnet new xunit \
  --name "$PROJECT.IntegrationTests" \
  --output "tests/$PROJECT.IntegrationTests"

dotnet new install Aspire.ProjectTemplates

dotnet new aspire-apphost \
  --name "$PROJECT.AppHost" \
  --output "src/$PROJECT.AppHost"

dotnet new aspire-servicedefaults \
  --name "$PROJECT.ServiceDefaults" \
  --output "src/$PROJECT.ServiceDefaults"

# Enable central package management only after templates have restored their
# built-in packages. Then move those package versions into the central file.
dotnet new packagesprops

remove_template_package() {
  local project_file="$1"
  local package="$2"
  dotnet package remove "$package" --project "$project_file"
}

add_central_package() {
  local project_file="$1"
  local package="$2"
  dotnet package add "$package" --project "$project_file"
}

core_project="src/$PROJECT.Core/$PROJECT.Core.csproj"
unit_project="tests/$PROJECT.UnitTests/$PROJECT.UnitTests.csproj"
integration_project="tests/$PROJECT.IntegrationTests/$PROJECT.IntegrationTests.csproj"
service_defaults="src/$PROJECT.ServiceDefaults/$PROJECT.ServiceDefaults.csproj"

remove_template_package "$core_project" Microsoft.AspNetCore.OpenApi

for test_project in \
  "$unit_project" \
  "$integration_project"; do
  remove_template_package "$test_project" coverlet.collector
  remove_template_package "$test_project" Microsoft.NET.Test.Sdk
  remove_template_package "$test_project" xunit
  remove_template_package "$test_project" xunit.runner.visualstudio
done

remove_template_package "$service_defaults" Microsoft.Extensions.Http.Resilience
remove_template_package "$service_defaults" Microsoft.Extensions.ServiceDiscovery
remove_template_package "$service_defaults" OpenTelemetry.Exporter.OpenTelemetryProtocol
remove_template_package "$service_defaults" OpenTelemetry.Extensions.Hosting
remove_template_package "$service_defaults" OpenTelemetry.Instrumentation.AspNetCore
remove_template_package "$service_defaults" OpenTelemetry.Instrumentation.Http
remove_template_package "$service_defaults" OpenTelemetry.Instrumentation.Runtime

add_central_package "$core_project" Microsoft.AspNetCore.OpenApi

for test_project in \
  "$unit_project" \
  "$integration_project"; do
  add_central_package "$test_project" coverlet.collector
  add_central_package "$test_project" Microsoft.NET.Test.Sdk
  add_central_package "$test_project" xunit
  add_central_package "$test_project" xunit.runner.visualstudio
done

add_central_package "$service_defaults" Microsoft.Extensions.Http.Resilience
add_central_package "$service_defaults" Microsoft.Extensions.ServiceDiscovery
add_central_package "$service_defaults" OpenTelemetry.Exporter.OpenTelemetryProtocol
add_central_package "$service_defaults" OpenTelemetry.Extensions.Hosting
add_central_package "$service_defaults" OpenTelemetry.Instrumentation.AspNetCore
add_central_package "$service_defaults" OpenTelemetry.Instrumentation.Http
add_central_package "$service_defaults" OpenTelemetry.Instrumentation.Runtime

# Vite package names must be npm-safe. Scaffold under a lowercase temporary
# name, then move it into the C#-style solution folder.
CLIENT_TMP="${PROJECT_SLUG}-client"
pnpm create vite "$CLIENT_TMP" \
  --template "$CLIENT_TEMPLATE" \
  --no-immediate \
  --no-interactive
mv "$CLIENT_TMP" "src/$PROJECT.Client"
pnpm --dir "src/$PROJECT.Client" install

cat > "src/$PROJECT.Client/$PROJECT.Client.esproj" <<'EOF'
<Project Sdk="Microsoft.VisualStudio.JavaScript.Sdk/1.0.4338480">
  <PropertyGroup>
    <ShouldRunNpmInstall>false</ShouldRunNpmInstall>
    <ShouldRunBuildScript>false</ShouldRunBuildScript>
    <StartupCommand>pnpm dev</StartupCommand>
    <BuildCommand>pnpm build</BuildCommand>
    <TestCommand>pnpm test</TestCommand>
  </PropertyGroup>
</Project>
EOF

dotnet sln "$PROJECT.slnx" add \
  "src/$PROJECT.AppHost/$PROJECT.AppHost.csproj" \
  "src/$PROJECT.Client/$PROJECT.Client.esproj" \
  "src/$PROJECT.Core/$PROJECT.Core.csproj" \
  "src/$PROJECT.Infrastructure/$PROJECT.Infrastructure.csproj" \
  "src/$PROJECT.ServiceDefaults/$PROJECT.ServiceDefaults.csproj" \
  "tests/$PROJECT.UnitTests/$PROJECT.UnitTests.csproj" \
  "tests/$PROJECT.IntegrationTests/$PROJECT.IntegrationTests.csproj"

dotnet add "src/$PROJECT.Core/$PROJECT.Core.csproj" reference \
  "src/$PROJECT.Infrastructure/$PROJECT.Infrastructure.csproj" \
  "src/$PROJECT.ServiceDefaults/$PROJECT.ServiceDefaults.csproj"

dotnet add "src/$PROJECT.AppHost/$PROJECT.AppHost.csproj" reference \
  "src/$PROJECT.Core/$PROJECT.Core.csproj"

dotnet add "tests/$PROJECT.UnitTests/$PROJECT.UnitTests.csproj" reference \
  "src/$PROJECT.Core/$PROJECT.Core.csproj"

dotnet add "tests/$PROJECT.IntegrationTests/$PROJECT.IntegrationTests.csproj" reference \
  "src/$PROJECT.Core/$PROJECT.Core.csproj" \
  "src/$PROJECT.Infrastructure/$PROJECT.Infrastructure.csproj"

dotnet package add Aspire.Hosting.JavaScript --project "src/$PROJECT.AppHost/$PROJECT.AppHost.csproj"
dotnet package add Aspire.Hosting.PostgreSQL --project "src/$PROJECT.AppHost/$PROJECT.AppHost.csproj"
dotnet package add Aspire.Hosting.Redis --project "src/$PROJECT.AppHost/$PROJECT.AppHost.csproj"

dotnet package add Microsoft.EntityFrameworkCore --project "src/$PROJECT.Core/$PROJECT.Core.csproj"
dotnet package add Npgsql.EntityFrameworkCore.PostgreSQL --project "src/$PROJECT.Core/$PROJECT.Core.csproj"
dotnet package add Microsoft.Extensions.Caching.StackExchangeRedis --project "src/$PROJECT.Core/$PROJECT.Core.csproj"
dotnet package add StackExchange.Redis --project "src/$PROJECT.Core/$PROJECT.Core.csproj"

dotnet package add Microsoft.EntityFrameworkCore --project "src/$PROJECT.Infrastructure/$PROJECT.Infrastructure.csproj"
dotnet package add Microsoft.EntityFrameworkCore.Design --project "src/$PROJECT.Infrastructure/$PROJECT.Infrastructure.csproj"
dotnet package add Npgsql.EntityFrameworkCore.PostgreSQL --project "src/$PROJECT.Infrastructure/$PROJECT.Infrastructure.csproj"
dotnet package add AWSSDK.S3 --project "src/$PROJECT.Infrastructure/$PROJECT.Infrastructure.csproj"

dotnet package add Npgsql.OpenTelemetry --project "src/$PROJECT.ServiceDefaults/$PROJECT.ServiceDefaults.csproj"

dotnet package add Microsoft.AspNetCore.Mvc.Testing --project "tests/$PROJECT.IntegrationTests/$PROJECT.IntegrationTests.csproj"
dotnet package add Testcontainers --project "tests/$PROJECT.IntegrationTests/$PROJECT.IntegrationTests.csproj"
dotnet package add Testcontainers.PostgreSql --project "tests/$PROJECT.IntegrationTests/$PROJECT.IntegrationTests.csproj"

dotnet restore "$PROJECT.slnx"
dotnet build "$PROJECT.slnx" --no-restore
dotnet test "$PROJECT.slnx" --no-build

echo "Created $ROOT"
echo "Next: continue with the configuration sections in the project blueprint"
```

The JavaScript SDK version in the generated `.esproj` is an example pin. Check
for the current stable `Microsoft.VisualStudio.JavaScript.Sdk` before starting a
new project.

## Shared Build and Package Configuration

Keep package versions only in `Directory.Packages.props`. Project files should
contain versionless `<PackageReference />` entries. Packages that must move in
lockstep, especially Aspire and EF Core packages, should use the same compatible
release line.

Add `Directory.Build.props`:

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AnalysisLevel>latest</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <ContinuousIntegrationBuild Condition="'$(CI)' == 'true'">true</ContinuousIntegrationBuild>
  </PropertyGroup>
</Project>
```

Scan each `.csproj` before freezing the first dependency set:

```bash
find src tests -name '*.csproj' -print0 |
  while IFS= read -r -d '' project; do
    dotnet package list --project "$project" --outdated
    dotnet package list --project "$project" --vulnerable --include-transitive
  done
```

Scanning the whole solution can fail when it contains the JavaScript `.esproj`,
so keep this check scoped to NuGet `PackageReference` projects. Do not suppress a
vulnerable transitive package merely because it is transitive; pin a patched
compatible version directly when necessary.

Common optional package groups:

| Concern | Typical packages |
| --- | --- |
| Validation | `FluentValidation`, `FluentValidation.AspNetCore` |
| Mapping | `Mapster`, `Mapster.DependencyInjection` |
| Errors | `ErrorOr` or another single result/error convention |
| Auth | `Microsoft.AspNetCore.Authentication.JwtBearer` |
| OpenAPI | `Microsoft.AspNetCore.OpenApi`, Scalar or Swagger UI |
| Images | `SixLabors.ImageSharp` |
| Object storage | `AWSSDK.S3` for S3, R2, MinIO, and compatible providers |
| Email | Provider SDK or `MailKit`; keep it behind an application interface |
| Testing | `Microsoft.AspNetCore.Mvc.Testing`, `Testcontainers` |

## Project Boundaries

Use these ownership rules until complexity justifies another project:

- `Core`: API endpoints, request/response contracts, validators, use cases,
  authorization, middleware, and dependency registration.
- `Infrastructure`: `DbContext`, entities/configurations, migrations, SQL,
  caches requiring infrastructure APIs, object storage, email, and other
  provider adapters.
- `ServiceDefaults`: telemetry, health checks, service discovery, and outbound
  HTTP resilience. It must not contain product behavior.
- `AppHost`: local resource topology only. It must not become a second source of
  production configuration.
- `Client`: browser application and generated route tree.

If domain rules become difficult to test without ASP.NET or EF Core, introduce
`DifferentProject.Domain` and `DifferentProject.Application`. Move business
rules and ports inward; let Infrastructure implement those ports and let Core
remain the web composition root.

## Local Aspire Topology

Use real local containers for PostgreSQL and Redis. The API and client start
together through the AppHost:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();
var database = postgres.AddDatabase("database");

var redis = builder.AddRedis("redis")
    .WithDataVolume();

var api = builder.AddProject<Projects.DifferentProject_Core>("api")
    .WithReference(database)
    .WithReference(redis)
    .WaitFor(database)
    .WaitFor(redis)
    .WithHttpHealthCheck("/health");

builder.AddViteApp("client", "../DifferentProject.Client")
    .WithPnpm()
    .WithReference(api)
    .WithEnvironment("VITE_API_BASE_URL", api.GetEndpoint("https"))
    .WaitFor(api);

builder.Build().Run();
```

Start everything with:

```bash
dotnet run --project "src/$PROJECT.AppHost/$PROJECT.AppHost.csproj"
```

### Optional Local Object Storage

Use MinIO locally and an S3-compatible provider such as R2 in deployed
environments. Keep both behind the same `IObjectStorage` interface.

```csharp
var storageUser = builder.AddParameter("storage-user", secret: true);
var storagePassword = builder.AddParameter("storage-password", secret: true);

var storage = builder.AddContainer("object-storage", "minio/minio")
    .WithArgs("server", "/data", "--console-address", ":9001")
    .WithEnvironment("MINIO_ROOT_USER", storageUser)
    .WithEnvironment("MINIO_ROOT_PASSWORD", storagePassword)
    .WithHttpEndpoint(targetPort: 9000, name: "s3")
    .WithHttpEndpoint(targetPort: 9001, name: "console")
    .WithVolume("object-storage-data", "/data");

api.WithEnvironment("ObjectStorage__Endpoint", storage.GetEndpoint("s3"))
   .WithEnvironment("ObjectStorage__AccessKey", storageUser)
   .WithEnvironment("ObjectStorage__SecretKey", storagePassword)
   .WaitFor(storage);
```

Create the development bucket idempotently on startup or through a dedicated
initializer. Never run development seed/upload logic in Production.

For uploaded images:

1. Set a hard request-size limit.
2. Verify decoded image content or magic bytes, not only extension/MIME type.
3. Decode with a maintained image library.
4. Resize to an explicit maximum dimension while preserving aspect ratio.
5. Transcode to a controlled format such as WebP.
6. Enforce the final encoded-size limit.
7. Generate a unique key and upload the normalized bytes.
8. Replace object storage with an in-memory fake in integration tests.

### Optional Local Email

Run Mailpit locally so no development email leaves the machine:

```csharp
var mail = builder.AddContainer("mail", "axllent/mailpit")
    .WithEndpoint(targetPort: 1025, name: "smtp", scheme: "tcp")
    .WithHttpEndpoint(targetPort: 8025, name: "ui");

api.WithEnvironment("Email__SmtpEndpoint", mail.GetEndpoint("smtp"))
   .WaitFor(mail);
```

Define an `IEmailSender` port with a provider implementation, a Mailpit/SMTP
implementation for local use, and a recording fake for tests. Queue email with
an outbox or background worker when delivery must survive API restarts; do not
hold database transactions open while calling an email provider.

## Service Defaults and OpenTelemetry

Start from the generated ServiceDefaults project and retain:

- ASP.NET Core, outbound HTTP, runtime, and Npgsql instrumentation.
- Structured `ILogger` export with formatted messages and scopes.
- `/health` readiness and `/alive` liveness endpoints.
- HTTP client service discovery and standard resilience.
- OTLP export only when `OTEL_EXPORTER_OTLP_ENDPOINT` is configured.

Use stable service names and resource attributes:

```text
OTEL_SERVICE_NAME=DifferentProject.Api
OTEL_RESOURCE_ATTRIBUTES=deployment.environment.name=development,service.version=<commit-sha>
```

Local Aspire provides its dashboard automatically. Production Compose can run
the standalone Aspire Dashboard for live diagnostics:

```yaml
aspire-dashboard:
  image: ${ASPIRE_DASHBOARD_IMAGE:-mcr.microsoft.com/dotnet/aspire-dashboard:latest}
  restart: unless-stopped
  environment:
    ASPNETCORE_FORWARDEDHEADERS_ENABLED: "true"
    Dashboard__Otlp__AuthMode: ApiKey
    Dashboard__Otlp__PrimaryApiKey: ${ASPIRE_DASHBOARD_OTLP_API_KEY:?required}
  expose:
    - "18888"
    - "18889"
```

Configure the API with:

```text
OTEL_EXPORTER_OTLP_ENDPOINT=http://aspire-dashboard:18889
OTEL_EXPORTER_OTLP_PROTOCOL=grpc
OTEL_EXPORTER_OTLP_HEADERS=x-otlp-api-key=<same-api-key>
```

Expose only dashboard port `18888` through the reverse proxy. Keep OTLP port
`18889` internal. Aspire Dashboard telemetry is in-memory and disappears on
restart, so graduate to an OpenTelemetry Collector plus a durable backend when
retention, alerting, or cross-service analysis matters.

## API Composition

Keep startup readable by grouping registrations into focused extensions:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services
    .AddDatabase(builder.Configuration)
    .AddRedis(builder.Configuration)
    .AddObjectStorage(builder.Configuration)
    .AddEmail(builder.Configuration)
    .AddApplicationFeatures()
    .AddAuthenticationAndAuthorization(builder.Configuration)
    .AddValidatedOptions(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors("Frontend");
app.MapControllers();
app.MapOpenApi();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
```

Expose `Program` for `WebApplicationFactory`. If SPA fallback must not hide
missing API/tooling routes, use a custom fallback that returns `404` for
`/api`, `/health`, `/openapi`, and `/docs` before serving `index.html`.

Bind settings to options, validate data annotations/custom rules, and call
`ValidateOnStart()`. Prefer one configuration vocabulary across local Aspire,
tests, Compose, and deployment:

```text
ConnectionStrings__DefaultConnection
ConnectionStrings__Redis
ObjectStorage__Endpoint
ObjectStorage__AccessKey
ObjectStorage__SecretKey
ObjectStorage__BucketName
ObjectStorage__PublicBaseUrl
Email__Provider
Email__FromAddress
Auth__Issuer
Auth__Audience
Auth__SigningKey
Frontend__AllowedOrigins__0
```

For cookie-based authentication, retain CSRF protection on state-changing
requests. CORS does not replace CSRF protection. Configure secure, HTTP-only,
same-site cookies deliberately for the deployment topology.

## SPA Integration

Configure Vite to build directly into the API web root:

```ts
import { defineConfig } from "vite";

const apiBaseUrl = process.env.VITE_API_BASE_URL ?? "https://localhost:7001";

export default defineConfig({
  build: {
    outDir: "../DifferentProject.Core/wwwroot",
    emptyOutDir: true,
  },
  server: {
    port: 5173,
    strictPort: true,
    proxy: {
      "/api": {
        target: apiBaseUrl,
        changeOrigin: true,
        secure: false,
      },
    },
  },
});
```

Add scripts for `dev`, `build`, `test`, `lint`, `format`, and `check`. Commit
the pnpm lockfile. The browser should call relative `/api/...` URLs in
production so the SPA and API share an origin; use the Vite proxy locally.

Install the shared client test and formatting tools, then the framework-specific
testing library:

```bash
pnpm --dir "src/$PROJECT.Client" add --save-dev vitest jsdom @biomejs/biome

if [[ "$CLIENT_TEMPLATE" == "solid-ts" ]]; then
  pnpm --dir "src/$PROJECT.Client" add --save-dev @solidjs/testing-library
else
  pnpm --dir "src/$PROJECT.Client" add --save-dev \
    @testing-library/react @testing-library/jest-dom
fi
```

Use this minimum `package.json` script surface:

```json
{
  "scripts": {
    "dev": "vite",
    "build": "vite build",
    "test": "vitest run",
    "lint": "biome lint .",
    "format": "biome format --write .",
    "check": "biome check ."
  }
}
```

Do not run the Vite preview server in Production. The Docker build compiles the
client, copies `wwwroot` into the API publish output, and serves one deployable
image.

## Test Strategy

### Unit Tests

- Exercise domain rules, validators, parsers, transformations, and cache-key
  logic without Docker or network calls.
- Use deterministic `TimeProvider`, IDs, and random sources.
- Mock at external boundaries, not every class.

### Integration Tests

- Use one collection fixture to start PostgreSQL and Redis containers.
- Run migrations once during fixture initialization.
- Reset tables between tests with `TRUNCATE ... RESTART IDENTITY CASCADE` or a
  proven database reset library.
- Start the API with `WebApplicationFactory<Program>` and override connection
  strings and external adapters.
- Replace object storage and email with recording fakes. Never upload to R2 or
  send real email from tests.
- Assert HTTP status, response body, headers/cookies, authorization, database
  state, cache invalidation, and relevant side effects.
- Use public APIs to arrange relationships when those APIs exist. Keep temporary
  direct-database helpers narrow and named after intent, such as `FollowUser`,
  so they can later be replaced by API calls.
- Include anonymous, unauthorized, forbidden, not-found, conflict, validation,
  pagination, filtering, ordering, fuzzy/similarity search, idempotency, and
  concurrent-request cases where applicable.

Keep playful domain data if it improves readability, but assertions should
explain behavior rather than depend on the reference being recognized.

## Migration Workflow

Create a helper script instead of repeatedly typing project paths:

```bash
#!/usr/bin/env bash
set -euo pipefail

PROJECT="${PROJECT:-DifferentProject}"
NAME="${1:?Usage: scripts/add-migration.sh MigrationName}"

dotnet ef migrations add "$NAME" \
  --project "src/$PROJECT.Infrastructure/$PROJECT.Infrastructure.csproj" \
  --startup-project "src/$PROJECT.Core/$PROJECT.Core.csproj" \
  --output-dir Migrations
```

Every migration PR should prove three things:

1. The complete migration chain builds an empty database.
2. The current migrations upgrade the schema from the PR base/deployed
   revision without data loss or invalid SQL.
3. `dotnet ef migrations has-pending-model-changes` succeeds.

For upgrade testing, choose the base revision explicitly:

```bash
BASE_SHA="${BASE_SHA:?Set BASE_SHA to the PR base or previous deployed SHA}"
PREVIOUS_SOURCE="$(mktemp -d)"

git worktree add --detach "$PREVIOUS_SOURCE" "$BASE_SHA"

# A NuGet package cache does not create obj/project.assets.json. Restore each
# worktree before invoking EF tooling.
dotnet restore "$PREVIOUS_SOURCE/src/$PROJECT.Core/$PROJECT.Core.csproj"
dotnet restore "src/$PROJECT.Core/$PROJECT.Core.csproj"

dotnet ef database update \
  --project "$PREVIOUS_SOURCE/src/$PROJECT.Infrastructure/$PROJECT.Infrastructure.csproj" \
  --startup-project "$PREVIOUS_SOURCE/src/$PROJECT.Core/$PROJECT.Core.csproj"

dotnet ef database update \
  --project "src/$PROJECT.Infrastructure/$PROJECT.Infrastructure.csproj" \
  --startup-project "src/$PROJECT.Core/$PROJECT.Core.csproj"

git worktree remove --force "$PREVIOUS_SOURCE"
```

In GitHub Actions use the PR base SHA for pull requests and
`github.event.before` for pushes. Do not assume `HEAD^` represents the deployed
schema after merge commits.

For a small single-replica service, guarded startup migrations are acceptable:

```text
Database__ApplyMigrationsOnStartup=true
Database__SeedDatabaseOnStartup=false
```

For multiple replicas, use an EF migration bundle or one-shot migrator job
before rolling out the API. Never let every replica race to migrate. Production
seed logic must be separate, explicit, idempotent, and disabled by default.

## Container Build

Use a three-stage Dockerfile:

1. Node builds the locked SPA.
2. .NET SDK restores project files first, then publishes with the SPA output.
3. ASP.NET runtime runs as a non-root user with only the published output.

Important cache rule: a BuildKit cache mount is safe for pnpm downloads, but do
not put the entire NuGet package directory only in a cache mount while later
running `dotnet publish --no-restore`. A remote layer cache can restore the
assets file without restoring the mount contents, producing `NETSDK1064`
missing-package failures. Let NuGet packages live in the restore layer and let
BuildKit cache that layer.

Dockerfile outline:

```dockerfile
# syntax=docker/dockerfile:1

FROM node:24-bookworm-slim AS client-build
WORKDIR /src
RUN npm install --global pnpm@<pin>
COPY src/DifferentProject.Client/package.json src/DifferentProject.Client/pnpm-lock.yaml ./src/DifferentProject.Client/
RUN --mount=type=cache,id=pnpm-store,target=/root/.local/share/pnpm/store \
    pnpm --dir src/DifferentProject.Client install --frozen-lockfile
COPY src/DifferentProject.Client ./src/DifferentProject.Client
RUN mkdir -p src/DifferentProject.Core \
    && pnpm --dir src/DifferentProject.Client build

FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS api-build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props DifferentProject.slnx ./
COPY src/DifferentProject.Core/DifferentProject.Core.csproj ./src/DifferentProject.Core/
COPY src/DifferentProject.Infrastructure/DifferentProject.Infrastructure.csproj ./src/DifferentProject.Infrastructure/
COPY src/DifferentProject.ServiceDefaults/DifferentProject.ServiceDefaults.csproj ./src/DifferentProject.ServiceDefaults/
RUN dotnet restore src/DifferentProject.Core/DifferentProject.Core.csproj
COPY src ./src
COPY --from=client-build /src/src/DifferentProject.Core/wwwroot ./src/DifferentProject.Core/wwwroot
RUN dotnet publish src/DifferentProject.Core/DifferentProject.Core.csproj \
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
ENV ASPNETCORE_ENVIRONMENT=Production DOTNET_EnableDiagnostics=0
EXPOSE 8080 8081
HEALTHCHECK --interval=15s --timeout=5s --start-period=30s --retries=5 \
  CMD curl --fail --silent http://localhost:8081/health || exit 1
USER $APP_UID
ENTRYPOINT ["dotnet", "DifferentProject.Core.dll"]
```

Use `.dockerignore` to exclude `.git`, IDE state, `bin`, `obj`, `node_modules`,
coverage, local env files, and generated `wwwroot`.

## Production Compose

Production Compose should contain:

| Service | Public? | Persistence | Notes |
| --- | --- | --- | --- |
| API | Reverse proxy only | None | Ports `8080` app and `8081` health |
| PostgreSQL | No | Named volume + off-host backups | App uses a non-superuser role |
| Redis | No | Named volume if cache/session durability matters | Enable auth if network trust is insufficient |
| Aspire Dashboard | UI only | In-memory telemetry | OTLP remains internal and API-key protected |
| Database browser | Normally off | Optional config volume | Read-only role, profile-gated, SSH tunnel |

External production dependencies such as R2 and an email provider are usually
configured through secrets rather than hosted in this Compose stack.

Production rules:

- Use service DNS names such as `postgres` and `redis`; do not invent public
  database URLs.
- Do not publish PostgreSQL, Redis, or OTLP ports.
- Use `expose` for reverse-proxied application ports.
- Set `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` behind TLS termination.
- Make the API filesystem read-only and mount a small `/tmp` tmpfs if required.
- Add health checks and dependency conditions, but do not make API availability
  depend on an optional dashboard.
- Use separate bootstrap-admin, application-owner, and read-only database roles.
- Initialization scripts run only for fresh PostgreSQL volumes; provide an
  explicit upgrade procedure for existing volumes.
- Pin images or validated digests rather than relying indefinitely on `latest`.
- Configure automated off-host backups and perform restore drills.

Keep PostgreSQL role creation in a one-time container initializer or deployment
job, not application seed code. Creating roles requires elevated credentials;
giving those credentials to the long-running API would turn a convenience into
a standing production privilege. Application seed code may create idempotent
product data only when an explicit non-production flag enables it.

Commit `.env.example` with placeholders for:

```text
APP_IMAGE
APP_IMAGE_TAG
APP_ORIGIN
ALLOWED_HOSTS
POSTGRES_DB
POSTGRES_ADMIN_PASSWORD
APP_DB_USER
APP_DB_PASSWORD
READONLY_DB_USER
READONLY_DB_PASSWORD
REDIS_PASSWORD
AUTH_SIGNING_KEY
OBJECT_STORAGE_ACCESS_KEY
OBJECT_STORAGE_SECRET_KEY
OBJECT_STORAGE_ENDPOINT
OBJECT_STORAGE_BUCKET_NAME
OBJECT_STORAGE_PUBLIC_BASE_URL
EMAIL_PROVIDER
EMAIL_API_KEY
EMAIL_FROM_ADDRESS
ASPIRE_DASHBOARD_IMAGE
ASPIRE_DASHBOARD_OTLP_API_KEY
OTEL_EXPORTER_OTLP_ENDPOINT
```

Use `${VAR:?VAR is required}` for security-sensitive Compose substitutions so a
missing secret fails before containers start.

## CI Pipeline

Use `develop`, `staging`, and `main` as protected long-lived branches unless the
team has another explicit promotion model.

Trigger CI on:

```yaml
on:
  pull_request:
    branches: [develop, staging, main]
  push:
    branches: [develop, staging, main]
```

CI jobs:

1. Restore, build, and test the .NET solution in Release mode.
2. Install pnpm from the lockfile, lint/check, test, and build the SPA.
3. Scan direct and transitive NuGet dependencies for vulnerabilities.
4. Apply all migrations to an empty PostgreSQL service container.
5. Materialize the base SHA, restore both worktrees, create the previous schema,
   apply current migrations, and reject pending model changes.
6. Optionally build the Docker image without pushing as a final packaging check.

Cache safely:

- Cache `~/.nuget/packages` in ordinary .NET CI jobs using
  `Directory.Packages.props` and project files in the key.
- Cache the pnpm store using the lockfile.
- Use BuildKit `type=gha,mode=max` for Docker builds.
- Keep NuGet packages inside the Docker restore layer as described above.

Use workflow concurrency to cancel obsolete runs on the same branch. Grant the
smallest GitHub token permissions required.

## CD and Environment Promotion

Keep deployment in a reusable `workflow_call` workflow and invoke it only after
all CI jobs succeed on a push, never directly from a pull request:

| Branch | GitHub environment | Moving image tag | Typical purpose |
| --- | --- | --- | --- |
| `develop` | `development` | `dev` | Automatic integration deployment |
| `staging` | `staging` | `staging` | Release candidate and smoke tests |
| `main` | `production` | `production` | Approval-protected production deployment |

Always publish an immutable `sha-<full-commit>` tag as well. Deploy the moving
environment tag for convenience, record the immutable digest, and roll back by
selecting a previous SHA tag.

The reusable deploy workflow should:

1. Check out the exact commit that passed CI.
2. Validate required environment variables before invoking Buildx.
3. Authenticate to the registry with a scoped token.
4. Generate SHA and environment tags.
5. Build the required target platforms with BuildKit cache, provenance, and
   SBOM enabled.
6. Push the image.
7. Trigger the deployment platform webhook or API.
8. Run a post-deploy health/smoke check and surface failure clearly.

Keep shared registry credentials named generically, such as
`DOCKERHUB_USERNAME` and `DOCKERHUB_TOKEN`. Prefix application-specific
variables and webhooks to prevent collisions. Repository/image names are
GitHub environment variables; tokens and deployment webhooks are secrets.

Apply GitHub environment protection rules:

- `development`: automatic from `develop`.
- `staging`: limited to `staging`, optionally approval-protected.
- `production`: limited to `main`, required reviewers, no self-approval where
  policy supports it.

## Convenience Scripts

Create `scripts/check.sh`:

```bash
#!/usr/bin/env bash
set -euo pipefail

PROJECT="${PROJECT:-DifferentProject}"

dotnet restore "$PROJECT.slnx"
dotnet build "$PROJECT.slnx" --configuration Release --no-restore
dotnet test "$PROJECT.slnx" --configuration Release --no-build
pnpm --dir "src/$PROJECT.Client" install --frozen-lockfile
pnpm --dir "src/$PROJECT.Client" check
pnpm --dir "src/$PROJECT.Client" test
pnpm --dir "src/$PROJECT.Client" build
docker compose --env-file .env.example config --quiet
```

Create `scripts/generate-secrets.sh`:

```bash
#!/usr/bin/env bash
set -euo pipefail

for name in POSTGRES_ADMIN_PASSWORD APP_DB_PASSWORD READONLY_DB_PASSWORD \
  AUTH_SIGNING_KEY ASPIRE_DASHBOARD_OTLP_API_KEY; do
  printf '%s=%s\n' "$name" "$(openssl rand -hex 32)"
done
```

Mark scripts executable and keep them shellcheck-clean:

```bash
chmod +x scripts/*.sh deploy/postgres/*.sh
```

## First-Build Checklist

- [ ] Replace every `DifferentProject` placeholder.
- [ ] Select `solid-ts` or `react-ts` and remove unused client scaffolding.
- [ ] Pin SDK, pnpm, package, action, base-image, and service-image versions.
- [ ] Confirm central package management leaves no package versions in project files.
- [ ] Add validated options for auth, database, Redis, storage, email, and CORS.
- [ ] Add EF Core context, design-time factory, and first migration.
- [ ] Add local MinIO/Mailpit only when the product needs storage/email.
- [ ] Add fakes for object storage, email, time, and other external boundaries.
- [ ] Verify local AppHost starts API, client, database, Redis, and optional tools.
- [ ] Verify client routes work through direct navigation after production build.
- [ ] Verify missing API routes remain `404` rather than returning SPA HTML.
- [ ] Verify anonymous/unauthorized/forbidden behavior and CSRF policy.
- [ ] Run unit and integration tests from a clean checkout.
- [ ] Build and run the production Docker image locally.
- [ ] Validate Compose using `.env.example` placeholders.
- [ ] Verify migrations from an empty database and the base/deployed schema.
- [ ] Verify the API runs as non-root with a read-only filesystem.
- [ ] Verify PostgreSQL, Redis, database tools, and OTLP are not publicly exposed.
- [ ] Verify Aspire OTLP rejects requests without the API key.
- [ ] Configure branch protections and GitHub environments.
- [ ] Configure off-host database backups and test a restore.
- [ ] Document deployment, rollback, secret rotation, and incident access.

## Definition of Ready

The template is ready for feature work when one command starts the local system,
one command runs all checks, a clean production image serves both API and SPA,
integration tests make no external provider calls, migrations are upgrade-tested,
and each protected branch can promote the exact CI-tested commit to its matching
environment.
