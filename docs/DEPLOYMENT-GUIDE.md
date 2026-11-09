# BengalTex ERP – Production Deployment & Operations Guide

**Document Version:** 1.0.0 – Production Deployment Manual  
**Release:** BengalTex ERP v1.0.0-GA  
**Target Environment:** Linux Server (Ubuntu 22.04 LTS / 24.04 LTS) / Docker Compose v2  

---

## 1. System Requirements & Hardware Sizing

### Recommended Server Sizing (50–200 Concurrent Users)
- **CPU:** 4 to 8 vCPU cores (Intel Xeon or AMD EPYC @ 2.5GHz+)
- **Memory (RAM):** 16 GB minimum (32 GB recommended for high manufacturing throughput)
- **Disk Storage:** 100 GB+ NVMe SSD (separate mount for SQL Server data and nightly `.bak` backups)
- **Operating System:** Ubuntu Server 22.04 LTS or 24.04 LTS x86_64
- **Container Engine:** Docker Engine 24.0+ & Docker Compose v2.20+

---

## 2. Architecture Overview

The production deployment runs as 3 coordinated containers communicating over an isolated internal Docker bridge network (`erp-net`):

| Container Name | Service Image / Dockerfile | Internal Port | Host Port | Role |
| :--- | :--- | :---: | :---: | :--- |
| **`bengaltex-db`** | `mcr.microsoft.com/mssql/server:2022-latest` | 1433 | N/A (Internal) | Microsoft SQL Server Database |
| **`bengaltex-api`** | `src/BengalTex.ERP.Api/Dockerfile` | 8080 | N/A (Internal) | ASP.NET Core .NET 8 Web API & Hangfire |
| **`bengaltex-web`** | `client/Dockerfile` (Nginx Alpine) | 80 | **8088 / 443** | Angular SPA + Nginx Reverse Proxy |

> **Same-Origin Architecture:** The user's browser only interacts with the `web` container. Nginx serves the compiled Angular SPA and transparently reverse-proxies `/api` and `/hubs` to the `api` container.

---

## 3. Step-by-Step Installation & Deployment

### Step 3.1: Clone the Repository & Configure Directory
```bash
git clone https://github.com/BengalTex/BengalTex.ERP.git /opt/bengaltex-erp
cd /opt/bengaltex-erp
```

### Step 3.2: Configure Environment Secrets (`.env`)
Create a production `.env` file from the example template:
```bash
cp .env.example .env
chmod 600 .env
```

Edit `.env` and configure strong cryptographic secrets:
```ini
# --- SQL Server Configuration ---
SA_PASSWORD=YourSuperStrongSaPassword!2026   # 16+ chars, upper/lower/digits/symbols
ACCEPT_EULA=Y

# --- Cryptographic & Token Secrets ---
# Generate using: openssl rand -base64 64
JWT_SECRET=zK9qF4wM8vP2xR1yL7nJ5tH0bC3sV6aD9gE2iU4oQ7wE1rT3yU5iO7pA9sD1fG3hJ5kL7zX9cV1bN3mQ5wE7rT9yU
# Generate using: openssl rand -base64 32
FINGERPRINT_SALT=X3mP9qR2vL5nK8tY1wB4zC7sD0fG2hJ4

# --- Network & Domain Binding ---
PUBLIC_ORIGIN=https://erp.bengaltex.com
ALLOWED_HOSTS=erp.bengaltex.com,localhost
WEB_PORT=8088

# --- Production Mode ---
ASPNETCORE_ENVIRONMENT=Production
```

### Step 3.3: Build & Launch the Containers
```bash
docker compose -f docker-compose.yml up -d --build
```

### Step 3.4: Verify Container Health & Startup Logs
Monitor the automated database migration and seeding sequence:
```bash
docker compose ps
docker compose logs -f api
```

Expected startup log output:
```text
info: BengalTex.ERP.Api.Program[0]
      Applying database migrations and ensuring database exists...
info: Microsoft.EntityFrameworkCore.Migrations[20405]
      Applying migration '20261109000000_InitialSchema'.
info: BengalTex.ERP.Infrastructure.Persistence.DataSeeder[0]
      Default seed data applied successfully. SuperAdmin account verified.
info: Hangfire.BackgroundJobServer[0]
      Starting Hangfire Server using SQL Server storage.
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://[::]:8080
```

Verify the health check from the host server:
```bash
curl -i http://localhost:8088/api/../health
# HTTP/1.1 200 OK
# {"status":"healthy","time":"2026-11-09T...Z"}
```

---

## 4. First-Time Administrator Login & Initial Setup

1. Open your web browser and navigate to `https://erp.bengaltex.com` (or `http://<SERVER_IP>:8088`).
2. Log in using the default SuperAdmin credentials:
   - **Username / Email:** `superadmin@bengaltex.com`
   - **Default Seed Password:** Configured during seed execution (see `DataSeeder.cs`).
3. **MANDATORY SECURITY ACTION:** Immediately navigate to **Profile $\rightarrow$ Change Password** and set a high-entropy administrator password.
4. Navigate to **Master Data $\rightarrow$ Company Settings**, verify the BIN/TIN registration, and upload the official company logo.

---

## 5. TLS / SSL Termination & Reverse Proxy

For production internet exposure, terminate HTTPS at a host reverse proxy (Nginx, Traefik, or Caddy) in front of container port `8088`:

### Sample Host Nginx Configuration (`/etc/nginx/sites-available/bengaltex.conf`):
```nginx
server {
    listen 80;
    server_name erp.bengaltex.com;
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl http2;
    server_name erp.bengaltex.com;

    ssl_certificate /etc/letsencrypt/live/erp.bengaltex.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/erp.bengaltex.com/privkey.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;

    client_max_body_size 50M;

    location / {
        proxy_pass http://127.0.0.1:8088;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto https;
    }
}
```

---

## 6. Automated Backup & Disaster Recovery Runbook

### Step 6.1: Setup Nightly Automated SQL Backup Cron Job
Create the backup script `/usr/local/bin/backup-bengaltex.sh`:
```bash
#!/usr/bin/env bash
set -euo pipefail

BACKUP_DIR="/var/backups/bengaltex"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
mkdir -p "${BACKUP_DIR}"

source /opt/bengaltex-erp/.env

# Execute compressed backup inside SQL Server container
docker compose -f /opt/bengaltex-erp/docker-compose.yml exec -T db \
  /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "${SA_PASSWORD}" -C \
  -Q "BACKUP DATABASE [BengalTexERP] TO DISK='/var/opt/mssql/backup/BengalTexERP_${TIMESTAMP}.bak' WITH INIT, COMPRESSION"

# Copy backup to host storage
docker compose -f /opt/bengaltex-erp/docker-compose.yml cp \
  db:/var/opt/mssql/backup/BengalTexERP_${TIMESTAMP}.bak "${BACKUP_DIR}/"

# Retain backups for 30 days
find "${BACKUP_DIR}" -type f -name "*.bak" -mtime +30 -delete

echo "Backup completed: BengalTexERP_${TIMESTAMP}.bak"
```

Make executable and add to crontab:
```bash
chmod +x /usr/local/bin/backup-bengaltex.sh
crontab -e
# Add: 0 2 * * * /usr/local/bin/backup-bengaltex.sh >> /var/log/bengaltex-backup.log 2>&1
```

### Step 6.2: Disaster Recovery Restore Procedure
To restore from a backup file `BengalTexERP_YYYYMMDD.bak`:
1. Stop API to sever active database connections:
   ```bash
   docker compose stop api
   ```
2. Copy backup into database container:
   ```bash
   docker compose cp /var/backups/bengaltex/BengalTexERP_YYYYMMDD.bak db:/var/opt/mssql/backup/restore.bak
   ```
3. Execute SQL Restore with replace:
   ```bash
   docker compose exec -T db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "${SA_PASSWORD}" -C \
     -Q "ALTER DATABASE [BengalTexERP] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
         RESTORE DATABASE [BengalTexERP] FROM DISK='/var/opt/mssql/backup/restore.bak' WITH REPLACE;
         ALTER DATABASE [BengalTexERP] SET MULTI_USER;"
   ```
4. Restart all services:
   ```bash
   docker compose start api
   ```

---

## 7. Zero-Downtime Rolling Update Procedure

When upgrading to a new software release:
```bash
cd /opt/bengaltex-erp
git pull origin main
docker compose build api web
docker compose up -d --no-deps api web
```
The API container automatically applies any incremental EF Core migrations during startup before serving traffic.

