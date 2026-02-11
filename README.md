# Skidjakt

Ski deal aggregator that scrapes last-minute ski travel deals from four Swedish agencies (Nortlander, Slopestar, STS Alpresor, Skilink) and displays them in a unified, filterable view.

## Prerequisites

### Local development

| Tool | Version | Install |
|------|---------|---------|
| .NET SDK | 10.0+ | https://dot.net |
| Node.js | 22+ | https://nodejs.org |
| npm | 10+ | Comes with Node.js |

### Deployment

| Tool | Install |
|------|---------|
| SSH client | Built into Windows 10+ |
| Docker + Compose | Installed on the droplet via setup script |

## Quick start (local)

### Option A: PowerShell script (recommended)

```powershell
# Start both backend and frontend
.\dev.ps1

# Or just one:
.\dev.ps1 -BackendOnly
.\dev.ps1 -FrontendOnly

# Or run via Docker:
.\dev.ps1 -Docker
```

The backend starts on `http://localhost:5000` and the frontend on `http://localhost:5173`.
Vite proxies `/api/*` requests to the backend automatically.

### Option B: Manual

**Terminal 1 - Backend:**
```powershell
cd backend
dotnet run --project src/Skidjakt.Api --urls http://localhost:5000
```

**Terminal 2 - Frontend:**
```powershell
cd frontend
npm install   # first time only
npm run dev
```

### Option C: Docker Compose

```powershell
docker compose up --build
```

This starts the full stack: backend on port 5000, frontend (nginx) on port 80.

## Useful commands

```powershell
# Build
cd backend && dotnet build
cd frontend && npm run build

# Test
cd backend && dotnet test

# Format
cd backend && dotnet csharpier format .
cd frontend && npx prettier --write src/

# Check API health
curl http://localhost:5000/api/health

# Trigger a manual scrape
curl -X POST http://localhost:5000/api/scrape/trigger
```

## Deploy to DigitalOcean

> **Using a custom domain?** See [DOMAIN-SETUP.md](DOMAIN-SETUP.md) for configuring HTTPS with `skidjakt.linkasaurus.se`.

### First-time setup

1. Create a droplet (Ubuntu 22.04+, 1 GB RAM minimum).

2. Push this repo to GitHub/GitLab so the droplet can clone it.

3. Run the setup from your Windows machine:
   ```powershell
   .\deploy.ps1 -Host YOUR_DROPLET_IP -Setup -RepoUrl git@github.com:youruser/skidjakt.git
   ```
   This SSHs into the droplet and installs Docker, clones the repo, and starts the services.

4. The app is now running at `http://YOUR_DROPLET_IP`.

### Subsequent deploys

Push your changes to `main`, then:

```powershell
.\deploy.ps1 -Host YOUR_DROPLET_IP
```

This pulls the latest code, rebuilds images, and restarts containers.

### Manual deploy (on the droplet)

If you're already SSH'd into the droplet:

```bash
bash /opt/skidjakt/deploy.sh
```

### Deploy options

```powershell
# Default (root user, default SSH key)
.\deploy.ps1 -Host 123.45.67.89

# Custom user and key
.\deploy.ps1 -Host 123.45.67.89 -User deploy -KeyFile ~/.ssh/id_ed25519

# First-time setup
.\deploy.ps1 -Host 123.45.67.89 -Setup -RepoUrl git@github.com:user/skidjakt.git
```

### Droplet management

```bash
# View logs
ssh root@YOUR_DROPLET_IP "cd /opt/skidjakt && docker compose logs -f"

# View status
ssh root@YOUR_DROPLET_IP "cd /opt/skidjakt && docker compose ps"

# Restart services
ssh root@YOUR_DROPLET_IP "cd /opt/skidjakt && docker compose restart"

# Backup database
ssh root@YOUR_DROPLET_IP "docker compose cp backend:/app/data/skidjakt.db ./skidjakt-backup.db"
```

### Custom domain setup (skidjakt.linkasaurus.se)

See [DOMAIN-SETUP.md](DOMAIN-SETUP.md) for full instructions.

**Quick version:**
1. Create DNS A record: `skidjakt.linkasaurus.se` → your droplet IP
2. Push the setup files: `git push origin main`
3. Run setup script on droplet: `sudo bash /opt/skidjakt/setup-domain.sh`

This configures nginx with HTTPS (Let's Encrypt) as a reverse proxy.

## Architecture

```
Windows (dev)                          DigitalOcean (prod)
--------------                         -------------------
dotnet run  ─┐                        ┌─ Docker: backend
             ├─ http://localhost:5000  │   .NET 10, port 8080
npm run dev ─┘                        │   SQLite at /app/data/
  proxies /api/* ──────►              │
  http://localhost:5173               ├─ Docker: frontend
                                      │   nginx, port 80
                                      │   serves static files
                                      │   proxies /api/* → backend
                                      └─ Volume: db-data (SQLite)
```

## API

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/deals` | List deals (filter, sort, paginate) |
| GET | `/api/deals/{id}` | Single deal |
| GET | `/api/deals/filters` | Available filter options |
| GET | `/api/deals/stats` | Aggregate statistics |
| GET | `/api/deals/stream` | SSE real-time updates |
| GET | `/api/health` | Health check |
| POST | `/api/scrape/trigger` | Trigger manual scrape |

### Query parameters for `/api/deals`

| Param | Type | Example |
|-------|------|---------|
| `search` | string | `?search=Livigno` |
| `agencies` | comma-separated | `?agencies=skilink,alpresor` |
| `countries` | comma-separated | `?countries=Italien,Frankrike` |
| `minPrice` | int | `?minPrice=3000` |
| `maxPrice` | int | `?maxPrice=8000` |
| `fromDate` | DateOnly | `?fromDate=2026-03-01` |
| `toDate` | DateOnly | `?toDate=2026-04-01` |
| `transportTypes` | comma-separated | `?transportTypes=Flyg,Buss` |
| `sortBy` | string | `?sortBy=price` / `price_desc` / `date` / `discount` / `newest` |
| `page` | int | `?page=2` |
| `pageSize` | int (max 100) | `?pageSize=30` |

## Project structure

```
skidjakt/
├── backend/
│   ├── Skidjakt.slnx
│   ├── Dockerfile
│   ├── src/
│   │   ├── Skidjakt.Api/              # Minimal API endpoints
│   │   ├── Skidjakt.Core/             # Domain entities, DTOs, interfaces
│   │   ├── Skidjakt.Infrastructure/   # EF Core, SQLite, repository
│   │   └── Skidjakt.Scraper/          # 4 scrapers, background service, SSE
│   └── tests/
│       └── Skidjakt.Tests/            # 15 unit tests
├── frontend/
│   ├── Dockerfile
│   ├── nginx.conf
│   └── src/
│       ├── components/                # DealCard, FilterBar, etc.
│       ├── hooks/                     # useDeals, useFilters, useDealStream
│       ├── services/                  # API client
│       └── types/                     # TypeScript interfaces
├── docker-compose.yml
├── deploy.ps1                         # Deploy from Windows via SSH
├── deploy.sh                          # Deploy script (runs on droplet)
├── dev.ps1                            # Local development script
├── CLAUDE.md                          # AI assistant instructions
├── PLAN.md                            # Implementation plan + status
└── README.md                          # This file
```
