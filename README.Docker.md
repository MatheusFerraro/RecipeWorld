# Running Recipe World with Docker

The compose stack runs two containers:

- **server** — the ASP.NET Core app on http://localhost:8080 (HTTP only; HTTPS
  redirection is disabled in the container via `UseHttpsRedirection: "false"`)
- **db** — SQL Server 2022. Migrations are applied automatically at startup and
  sample data (recipes, demo/admin accounts) is seeded on first run.

## First run

1. Copy the environment template and set your own values:

   ```bash
   cp .env.example .env
   ```

   `MSSQL_SA_PASSWORD` must satisfy SQL Server complexity rules (8+ chars with
   upper, lower, and digits/symbols), otherwise the db container exits during
   startup. `JWT_KEY` must be at least 32 characters.

2. Build and start:

   ```bash
   docker compose up --build
   ```

   The server waits for the database healthcheck before starting; the first
   boot takes a minute while SQL Server initializes and migrations run.

3. Open http://localhost:8080 — log in with the seeded demo or admin account
   (passwords come from `DEMO_PASSWORD` / `ADMIN_PASSWORD` in `.env`).
   Swagger UI: http://localhost:8080/swagger. Health: http://localhost:8080/health.

## Data persistence

- `mssql-data` volume — database files survive `docker compose down`.
- `recipe-uploads` volume — uploaded recipe images (`/app/uploads/recipes`).

To start completely fresh: `docker compose down -v`.

## Notes

- The SQL Server image is amd64-only. On Apple Silicon, enable Rosetta
  emulation in Docker Desktop or switch the `db` image to `mcr.microsoft.com/azure-sql-edge`.
- In production-like deployments supply real secrets via environment variables
  instead of `.env` files.

### References
* [Docker's .NET guide](https://docs.docker.com/language/dotnet/)
* The [dotnet-docker](https://github.com/dotnet/dotnet-docker/tree/main/samples)
  repository has many relevant samples and docs.
