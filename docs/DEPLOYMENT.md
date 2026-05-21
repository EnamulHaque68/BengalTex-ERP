# Bengal TEX ERP — Deployment Guide

The full stack runs as three containers via Docker Compose:

| Service | Image | Role |
|---------|-------|------|
| `db`  | `mcr.microsoft.com/mssql/server:2022` | SQL Server (app + Hangfire share one database) |
| `api` | built from `src/BengalTex.ERP.Api/Dockerfile` | .NET 8 API (Kestrel on :8080) |
| `web` | built from `client/Dockerfile` | Angular SPA served by nginx, reverse-proxies `/api` + `/hubs` to `api` |

The browser talks only to **`web`** (default `http://<server>:8088`). nginx serves the SPA and proxies API/SignalR calls to the API container, so everything is same-origin.

## 1. Prerequisites
- Docker Engine + Docker Compose v2 on the server.

## 2. Configure secrets
```bash
cp .env.example .env
```
Edit `.env` and set **real** values:
- `SA_PASSWORD` — SQL Server SA password (8+ chars; upper + lower + digit + symbol).
- `JWT_SECRET` — `openssl rand -base64 64` (**generate fresh** — the old committed key is compromised).
- `FINGERPRINT_SALT` — `openssl rand -base64 32`.
- `PUBLIC_ORIGIN` — the URL users browse to, e.g. `http://192.168.1.50:8088`.
- `ALLOWED_HOSTS` — the server host/IP (or leave `*` on a trusted LAN).
- `WEB_PORT` — published web port (default `8088`).

`.env` is git-ignored — never commit it.

## 3. Build & run
```bash
docker compose up -d --build
```
On first boot the API **applies EF migrations and seeds** the database automatically
(`Database__InitializeOnStartup=true`): SuperAdmin, roles, permissions, base currency (BDT),
and numbering series. This is idempotent — safe on every restart.

Check health: `curl http://localhost:8088/api/../health` (or the API container's `/health`),
and `docker compose ps` / `docker compose logs -f api`.

## 4. First login
Use the seeded SuperAdmin account (see `DataSeeder`), then **change its password immediately**
and create real user accounts. The default seed credentials must not survive into real use.

## 5. TLS (production)
Containers run HTTP internally. For internet-facing deployments, terminate TLS at a reverse
proxy (nginx/Caddy/Traefik) in front of the `web` container, or add a `443` server block with
certs to `client/nginx.conf`. Then set `PUBLIC_ORIGIN` to the `https://` URL.

## 6. Backups
The database lives in the `mssql-data` volume and uploaded files in `api-uploads`.
- **DB backup** (nightly cron):
  ```bash
  docker compose exec db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -C \
    -Q "BACKUP DATABASE [BengalTexERP] TO DISK='/var/opt/mssql/backup/BengalTexERP_$(date +%F).bak' WITH INIT, COMPRESSION"
  ```
  Copy the `.bak` off-box (and back up the `api-uploads` volume too).

## Local development (unchanged)
Local dev does NOT use Docker. Run SQL Server locally, keep secrets in the git-ignored
`appsettings.Development.json`, apply migrations manually (`dotnet ef database update`), and
run `dotnet run --project src/BengalTex.ERP.Api` + `ng serve` in `client/`.
