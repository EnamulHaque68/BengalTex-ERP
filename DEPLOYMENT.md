# Bengal TEX ERP — Deployment Runbook

Production deployment guide for a Bangladesh garments-accessories factory.
The supported path is **Docker Compose** (SQL Server + .NET API + Angular/nginx, single server).

---

## 1. Server requirements

| Item | Minimum | Recommended |
|---|---|---|
| OS | Ubuntu 22.04 / Windows Server 2019+ with Docker | Ubuntu 24.04 LTS |
| CPU / RAM | 2 vCPU / 8 GB (SQL Server alone wants 2+ GB) | 4 vCPU / 16 GB |
| Disk | 60 GB SSD | 120 GB SSD + separate backup target |
| Software | Docker Engine + Compose v2 | — |
| Network | Fixed LAN IP (e.g. `192.168.1.50`); port 8088 (or 80/443 with proxy) reachable from office/factory floor | — |

SQL Server runs as **Express** in the compose file (free, production-licensed, 10 GB data cap).
A garments-accessories SME stays under 10 GB for years; switch `MSSQL_PID` to `Standard` (licensed) when you outgrow it.

## 2. Secrets — generate BEFORE first boot ⚠️

> The JWT secret that was once committed to git history is **permanently compromised**.
> Never reuse it. Production secrets live only in `.env` (git-ignored).

```bash
git clone <repo> bengaltex-erp && cd bengaltex-erp
cp .env.example .env

# Generate real values (Linux/macOS/Git-Bash):
openssl rand -base64 64 | tr -d '\n'    # → JWT_SECRET
openssl rand -base64 32 | tr -d '\n'    # → FINGERPRINT_SALT
```

Edit `.env` and set:

| Variable | Set to |
|---|---|
| `SA_PASSWORD` | Strong SQL password (8+ chars, upper+lower+digit+symbol) |
| `JWT_SECRET` | Fresh `openssl rand -base64 64` output |
| `FINGERPRINT_SALT` | Fresh `openssl rand -base64 32` output |
| `SEED_ADMIN_PASSWORD` | Strong first-boot SuperAdmin password (used only if the account doesn't exist yet) |
| `PUBLIC_ORIGIN` | Exactly what the browser will use, e.g. `http://192.168.1.50:8088` or `https://erp.factory.com` |
| `ALLOWED_HOSTS` | The hostname(s) users browse to, e.g. `192.168.1.50;erp.factory.com` (`*` only for trials) |
| `WEB_PORT` | Published UI port (default `8088`; use `80` if no reverse proxy) |

**Rotating a secret later**: change it in `.env` → `docker compose up -d api`.
Rotating `JWT_SECRET` logs every user out (tokens invalidated) — do it off-hours. `SA_PASSWORD` rotation additionally requires an `ALTER LOGIN` inside the db container first.

## 3. First boot

```bash
docker compose up -d --build
docker compose logs -f api     # watch until "Starting Bengal TEX ERP API"
```

First start (≈1–3 min): SQL container initializes → API waits for the healthcheck → applies **all EF migrations** (`Database__InitializeOnStartup=true`) → runs the idempotent seeder (roles, permissions, company/factory/warehouse, BDT currency, UoMs, numbering series, chart of accounts, leave types, SuperAdmin).

Then verify:

1. `http://<server>:8088` opens the login page.
2. Log in: **`superadmin`** / your `SEED_ADMIN_PASSWORD`.
3. Even though the seed password came from `.env`, change it once via the UI and store it in the company password manager.
4. Company profile (name, address, BIN, logo) → it appears on all printed documents.
5. Create real users + assign roles; keep `superadmin` as break-glass only.

## 4. TLS / reverse proxy (recommended beyond LAN-only trials)

The stack serves plain HTTP. For HTTPS put **Caddy** (easiest — automatic Let's Encrypt) in front:

```bash
# /etc/caddy/Caddyfile  (Caddy installed on the host; WEB_PORT=8088 stays internal)
erp.factory.com {
    reverse_proxy localhost:8088
}
```

Then in `.env`: `PUBLIC_ORIGIN=https://erp.factory.com`, `ALLOWED_HOSTS=erp.factory.com` → `docker compose up -d api`.

The API already honors `X-Forwarded-Proto/For` (`ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` in compose), so HSTS and https detection work behind the proxy. nginx + certbot works the same way (`proxy_set_header X-Forwarded-Proto $scheme;`).

LAN-only with no domain? HTTP on a trusted factory LAN is acceptable for a trial; add TLS before exposing beyond the LAN — **never port-forward the HTTP port to the internet**.

## 5. Backups 💾

Automatic (built-in):

- **01:30 nightly** — full `BACKUP DATABASE` + `RESTORE VERIFYONLY`, written to the shared `db-backups` volume; backups older than 14 days auto-deleted (`DatabaseBackup` config section).
- **02:30 nightly** — audit-log retention trim (365 days).

On demand (before migrations/upgrades): `POST /api/maintenance/backup-now` (admin JWT with `Settings.Edit`).

**Offsite copy — your job, not the system's.** A backup on the same disk does not survive disk failure. Sync the volume daily (cron) to a NAS/cloud:

```bash
# Volume path on the host:
docker volume inspect bengaltex-erp_db-backups --format '{{ .Mountpoint }}'
# e.g. rclone copy <mountpoint> gdrive:bengaltex-backups   (or rsync to a NAS)
```

### Restore drill — practice BEFORE you need it

```bash
docker compose stop api
docker compose exec db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -C -Q \
  "RESTORE DATABASE [BengalTexERP] FROM DISK = N'/var/opt/mssql/backups/BengalTexERP-<timestamp>.bak' WITH REPLACE, RECOVERY"
docker compose start api
```

New/replacement server: install Docker → clone repo → restore the **same `.env`** (keep a secure copy of it with the backups — without the same `JWT_SECRET`/`FINGERPRINT_SALT` all sessions/device bindings reset; without `SA_PASSWORD` the restored DB is awkward to reach) → `docker compose up -d --build` → stop api → restore latest `.bak` as above → start api.

## 6. Upgrades (new code version)

```bash
curl -X POST http://<server>:8088/api/maintenance/backup-now -H "Authorization: Bearer <admin token>"   # 1. backup first
git pull                                # 2. fetch the new version
docker compose up -d --build            # 3. rebuild + restart; migrations auto-apply on boot
docker compose logs -f api              # 4. watch for a clean start
```

Rollback = `git checkout <previous tag>` + rebuild, **plus restore the pre-upgrade backup** if the new version had already migrated the schema.

## 7. Monitoring / operations

| What | Where |
|---|---|
| Health | `GET /health` → `{"status":"healthy"}` — wire to Uptime Kuma / cron mail |
| API logs | `docker compose logs api` + rolling files in the `api-logs` volume (30 days) |
| Failed logins / lockouts | Serilog warnings (per-account lockout 5×/15 min + per-IP rate limit 10/min on auth endpoints) |
| Background jobs | Serilog (`OutboxProcessor`, `DatabaseBackup`, `AuditLogRetention`); Hangfire dashboard is **Development-only** |
| Email/SMS | `Email__Provider=Smtp` + SMTP env vars to actually send (default DevLogger only logs); audit at `/emails` in the UI |
| Disk space | Watch the host — SQL data + backups + logs all grow. Alert at 80 %. |

## 8. Troubleshooting

| Symptom | Likely cause / fix |
|---|---|
| API restarts in a loop on first boot | DB healthcheck not green yet — check `docker compose logs db`; most often a `SA_PASSWORD` that fails SQL complexity rules |
| API exits: "Jwt:Secret is missing or too short" | `JWT_SECRET` empty / < 32 bytes in `.env` |
| Browser CORS errors | `PUBLIC_ORIGIN` must match the browser URL **exactly** (scheme + host + port) |
| 400 "Host header" errors | Add the hostname/IP users browse to into `ALLOWED_HOSTS` |
| Everyone logged out after deploy | `JWT_SECRET` changed — expected on rotation |
| Backup job: "cleanup directory not visible" warning | Backup itself succeeded; the `db-backups` volume isn't mounted in the api container — check compose volumes |
| Backup fails with COMPRESSION error | SQL Express doesn't support it — keep `DatabaseBackup__Compress=false` |
| Email never arrives | Default provider is DevLogger (logs only) — set `Email__Provider=Smtp` + `Email__Smtp__*` vars; check `/emails` audit for the error message |

## 9. Opening balances (the careful part) ⚖️

Loading a live factory into a fresh system means entering, **as of one cut-off date**, everything that already exists: stock on the floor, money customers owe you, money you owe suppliers, and cash/bank. Do this *after* master data and *before* any day-1 transactions.

### 9.1 Cut-off discipline
- Pick a **go-live date** — ideally the first day of a month (e.g. close the old books on 30-Jun, open here on **01-Jul**).
- Take a **physical stock count** and pull the **outstanding customer/supplier lists** as of that date from the old system.
- Freeze the old system for new entries once you start; date every opening document with the cut-off date.

### 9.2 The "Opening Balance Equity" technique
Every document in this ERP auto-posts its own journal, so the trick is to let those journals build the ledger, with one **Opening Balance Equity** account absorbing the other side. Create it once: **Accounting → Chart of Accounts → New** → code `3150`, name `Opening Balance Equity`, type **Equity**, parent `3000`. (Or reuse `3100 Owner's Capital`.) When you finish, this account's balance equals the business's net worth, and the Trial Balance balances.

### 9.3 Cash & bank — one journal entry
**Accounting → Journal Entries → New**, dated the cut-off:
```
Dr  1110 Cash in Hand        <cash on hand>
Dr  1120 Bank Accounts       <each bank's cleared balance>
    Cr  3150 Opening Balance Equity   <total>
```

### 9.4 Opening receivables (per customer)
For each customer with a balance, **Customer Invoices → New** (one summary invoice per customer, dated cut-off, no VAT) for the amount due, then **Issue** it. That posts `Dr AR / Cr Sales Revenue` and the invoice shows in **AR Ageing** and the customer statement. Because real revenue wasn't earned now, sweep it to equity with **one** journal for the grand total:
```
Dr  4100 Sales Revenue                 <total opening AR>
    Cr  3150 Opening Balance Equity     <total opening AR>
```
Net effect: `Dr AR / Cr Opening Balance Equity`, and the sub-ledger (ageing/statements) is populated. *(Tip: set each opening invoice's due date to the original due date so the ageing buckets are right.)*

### 9.5 Opening payables (per supplier)
Mirror image — for each supplier you owe, **Supplier Invoices → New** + Approve (`Cr AP / Dr expense-or-inventory`), then one netting journal for the total:
```
Dr  3150 Opening Balance Equity        <total opening AP>
    Cr  <the expense/inventory account the invoices debited>   <total opening AP>
```

### 9.6 Opening stock — raw materials AND finished goods
Use the dedicated **Inventory → Opening Stock** screen (the right tool for this — it sets both quantity *and* weighted-average cost and posts the correct equity journal). One entry per warehouse:
1. **Opening Stock → New Opening Stock** → pick the warehouse, set the entry date (cut-off).
2. Add a line per item — **raw material OR finished product** (the item picker lists both, `RM ·` / `FG ·`), its on-hand quantity, and its known unit cost.
3. **Post** it. For every line it seeds the item's WAC from the unit cost, writes an `OpeningStock` stock movement, and posts ONE balanced journal: `Dr Raw Material Inventory + Dr Finished Goods Inventory / Cr 3150 Opening Balance Equity`.

This is the only path that seeds **finished-goods** stock outside a Production run, and it seeds **value** (not just quantity). *(Don't use a plain Stock Adjustment for opening balances — it's raw-material-only, values at the current WAC=0, and posts to the count-correction account.)*

### 9.8 Fixed assets & loans
Enter assets via **Fixed Assets → New** with the original cost and accumulated depreciation to date (or a journal `Dr asset / Cr accum. dep. / Cr Opening Balance Equity`). Enter outstanding bank loans as `Cr loan liability / Dr Opening Balance Equity`.

### 9.9 Reconcile before you start trading
- **Trial Balance** (Accounting) balances (total Dr = total Cr). ✅
- **Stock Summary** total value = your physical-count valuation. ✅
- **AR Ageing** total = the outstanding-customer list you started from; **AP Ageing** total = the supplier list. ✅
- `3150 Opening Balance Equity` ≈ the business's real net worth. A surprise here means something is double-counted or missing.
- Take a backup (`POST /api/maintenance/backup-now`) and tag it "post-opening-balances".

## 10. Go-live checklist

- [ ] `.env` filled with fresh secrets (§2) — never the committed/example values
- [ ] First boot OK; logged in; SuperAdmin password changed + stored in password manager (§3)
- [ ] Company profile + logo set; real users created with proper roles
- [ ] Master data loaded: customers, suppliers, products (+HS codes for exporters), raw materials, BOMs, warehouses, bank accounts
- [ ] Opening balances entered per §9 (cash/bank, AR, AP, RM+FG stock via **Inventory → Opening Stock**, fixed assets) and **reconciled** (§9.9): Trial Balance balances, stock/AR/AP totals match the source lists
- [ ] "post-opening-balances" backup taken
- [ ] Numbering series prefixes reviewed (Settings) — codes appear on printed documents
- [ ] SMTP configured + test email sent (`/emails` shows Sent)
- [ ] Approval threshold (`Approvals__PurchaseOrderThreshold`) matches company policy
- [ ] Nightly backup ran at least once; `.bak` visible; **restore drill done** (§5)
- [ ] Offsite backup sync scheduled (§5)
- [ ] TLS in place if reachable beyond the factory LAN (§4)
- [ ] `/health` monitored
- [ ] UAT signed off by accounts + store + production + HR users
