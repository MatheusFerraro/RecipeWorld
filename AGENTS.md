# AGENTS.md

This file provides guidance to Codex (Codex.ai/code) when working with code in this repository.

## Project Overview

Recipe World is an ASP.NET Core MVC (.NET 8) recipe management app with ASP.NET Core Identity (cookie auth for the site), a JWT-authenticated REST API documented via Swagger, and an EF Core (SQL Server, code-first) data layer. Web project: `src/worldRecipeMvc`; tests: `src/worldRecipeMvc.Tests`. Both are in the root `worldRecipeMvc.sln`.

## Commands

```bash
# Run the app (Web UI at https://localhost:7008, Swagger at /swagger)
dotnet run --project src/worldRecipeMvc

# Build + run all tests (65 unit + integration tests)
dotnet build worldRecipeMvc.sln
dotnet test worldRecipeMvc.sln

# Run a single test class or test
dotnet test src/worldRecipeMvc.Tests/worldRecipeMvc.Tests.csproj --filter "FullyQualifiedName~RecipeServiceTests"

# EF Core migrations (run from src/worldRecipeMvc)
dotnet ef migrations add <Name>

# Docker (app on http://localhost:8080 + SQL Server container)
cp .env.example .env   # first time only; set MSSQL_SA_PASSWORD & JWT_KEY
docker compose up --build
```

Migrations are applied automatically at startup (`Database.MigrateAsync()` on SQL Server; `EnsureCreated` on other providers — that guard is what lets integration tests run on SQLite). Roles and sample data (6 recipes, categories, ingredients, demo/admin users) are seeded at startup; seed account passwords come from the `SeedData` config section (dev values in `appsettings.Development.json`, Docker via env vars) — accounts are skipped if no password is configured.

## Architecture

Layering is strict: Controllers → Services → `ApplicationDbContext`. Controllers contain no business logic or DbContext access.

- **Services return FluentResults `Result<T>`** with typed errors from `Services/Errors/DomainErrors.cs` (`NotFoundError`, `ForbiddenError`, `ValidationError`, `ConflictError`). API controllers map these to HTTP 404/403/400/409 via `Controllers/Api/ResultExtensions.ToActionResult()`; MVC controllers pattern-match errors to `NotFound()`/`Forbid()`/ModelState.
- **Ownership/authorization checks live in the services** (methods take `userId`, `isAdmin`), so MVC and API share one enforcement path. Recipe visibility: `Draft`/`Private` only for owner/admin; statuses are string constants in `Models/RecipeStatus.cs` (`Draft/Private/Public/Unlisted`) — never hardcode status strings.
- **Two presentation surfaces:** MVC controllers (`Controllers/`, ViewModels) and REST API (`Controllers/Api/`, DTOs from `DTOs/ApiDtos.cs` — input DTOs deliberately omit ID/IsApproved/Owner fields to block over-posting).
- **API auth is JWT-only:** `[ApiAuthorize]` (in `Controllers/Api/`) pins the JwtBearer scheme, so the Identity cookie never satisfies API auth (CSRF protection). Tokens issued by `POST /api/auth/login` (`Services/TokenService`). Do NOT set default schemes when touching `AddAuthentication()` — that breaks cookie MVC login.
- **Rate limiting:** global per-IP fixed window + stricter `"auth"` policy on the login endpoint; limits read from the `RateLimiting` config section (integration tests raise them).
- **Caching:** `IMemoryCache` inside services with keys in `Services/CacheKeys.cs` — dropdown lists (evicted on writes) and home trending (5-min TTL only). No OutputCache (pages vary per user).
- **Approval workflow:** user-created `Ingredient`/`Category` have `IsApproved` (null = pending); any edit resets to pending; only admins (or owners while unapproved) may edit/delete. Owners can only publish a recipe once all its ingredients + category are approved.
- **Favorites/Ratings:** `Favorite` (composite PK UserId+RecipeID), `Rating` (unique index RecipeID+UserId, 1-5 stars, owners can't self-rate). User-side FKs are `Restrict` — cascading both paths would trip SQL Server's multiple-cascade-path error.
- **Lazy-loading proxies are enabled globally** — navigation properties must be `virtual`; services still use explicit `Include`/projections + `AsNoTracking` for reads.
- **Images:** `Services/ImageStorageService` validates uploads by extension, size (5MB), and magic bytes; stores under `ImageUpload:StoragePath` (falls back to `wwwroot/images/recipes`), served at `/images/recipes` via a second `UseStaticFiles`.
- **Errors:** `Middleware/ErrorHandlingMiddleware` returns ProblemDetails JSON for `/api` paths and rethrows for MVC (outer `UseExceptionHandler` renders HTML). Services don't catch generic exceptions.
- **Logging:** Serilog (console sink), configured from the `Serilog` section. Health endpoint at `/health` (used by the Docker healthcheck).

## Testing

- Unit tests (`src/worldRecipeMvc.Tests/Services/`): xUnit + EF InMemory, real service instances with `new MemoryCache(...)` + `Mock.Of<ILogger<...>>()`.
- Integration tests (`src/worldRecipeMvc.Tests/Integration/`): `CustomWebApplicationFactory` boots the real app on **SQLite in-memory** (relational constraints enforced; one open `SqliteConnection` kept for the factory's lifetime). Config overrides must go through `builder.UseSetting(...)` — `ConfigureAppConfiguration` is applied too late for values read in `Program.Main`. Rate-limit tests use `RateLimitedWebApplicationFactory`.

## Docker

`compose.yaml` runs `server` (port 8080, HTTP only — `UseHttpsRedirection=false`) + `db` (SQL Server 2022, healthcheck-gated). Secrets come from `.env` (gitignored; template in `.env.example`). Named volumes persist DB data and uploaded images; the Dockerfile pre-creates `/app/uploads` owned by the non-root `$APP_UID`. Startup fails fast outside Development if migration/seeding fails (compose restarts until the DB is up).
