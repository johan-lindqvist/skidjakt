# Skidjakt - Project Instructions

## Overview
Ski deal aggregator that scrapes last-minute ski travel deals from Swedish agencies.

## Tech Stack
- **Backend**: .NET 10, ASP.NET Core Minimal API, EF Core + SQLite, AngleSharp, Playwright
- **Frontend**: React 19, TypeScript, Vite 6, TailwindCSS 4, TanStack Query
- **Infrastructure**: Docker, nginx, DigitalOcean

## Project Structure
- `backend/` - .NET solution (Skidjakt.slnx)
  - `src/Skidjakt.Api/` - Minimal API host
  - `src/Skidjakt.Core/` - Domain entities, interfaces, DTOs
  - `src/Skidjakt.Infrastructure/` - EF Core, DbContext, DealRepository
  - `src/Skidjakt.Scraper/` - Per-agency scrapers, background service
  - `tests/Skidjakt.Tests/` - Unit tests
- `frontend/` - React app
  - `src/components/` - React components (deals, filters, search, layout)
  - `src/hooks/` - Custom hooks (useDeals, useFilters, useDealStream)
  - `src/types/` - TypeScript interfaces
  - `src/services/` - API client

## Conventions
- **C# formatting**: CSharpier (`cd backend && dotnet csharpier format .`)
- **TypeScript formatting**: Prettier (`cd frontend && npx prettier --write src/`)
- **Language**: Swedish UI labels, code in English
- **Solution format**: .slnx (not .sln)
- **PLAN.md**: Always keep up to date with current progress. Update checkboxes and phase statuses after completing work.
- **README.md**: Keep in sync when adding new commands, scripts, or changing architecture.
- **CLAUDE.md**: Update this file with new learnings, conventions, or gotchas discovered during development. This file should be a living document that prevents repeating mistakes.
- **Git workflow**: Always commit changes after completing a task. Do NOT push — the user will push manually when ready.
- **Git commits**: Use [Conventional Commits](https://www.conventionalcommits.org/) format:
  - `feat: add new feature`
  - `fix: bug fix`
  - `docs: documentation changes`
  - `refactor: code refactoring`
  - `test: add or update tests`
  - `chore: maintenance tasks`
  - `perf: performance improvements`
  - Always include `Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>` (or current model) in commit body
- **No secrets in code**: Never hardcode IP addresses, hostnames, passwords, or credentials in committed files. Use the `.env` file for all environment-specific configuration (see `.env.example` for template).
- **Environment configuration**: All deployment scripts read from `.env` (gitignored). Committed files should only contain `.env.example` as a template with placeholder values.
- **Testing**: Always write tests for new parsing/scraping code and ensure `dotnet test` passes before a task is considered complete.
- **Build verification**: Always verify `dotnet build` succeeds before committing.

## Commands

### Local development
- Quick start: `.\dev.ps1` (starts both backend and frontend)
- Backend only: `.\dev.ps1 -BackendOnly`
- Frontend only: `.\dev.ps1 -FrontendOnly`
- Via Docker: `.\dev.ps1 -Docker` or `docker compose up --build`

### Build and test
- Backend build: `cd backend && dotnet build`
- Backend run: `cd backend && dotnet run --project src/Skidjakt.Api --urls http://localhost:5000`
- Backend test: `cd backend && dotnet test`
- Frontend dev: `cd frontend && npm run dev`
- Frontend build: `cd frontend && npm run build`

### Formatting
- Format C#: `cd backend && dotnet csharpier format .`
- Format TS: `cd frontend && npx prettier --write src/`

### Deployment
- Deploy from Windows: `.\deploy.ps1` (reads host from .env)
- First-time setup: `.\deploy.ps1 -Setup` (reads repo URL from .env)
- On the droplet: `bash /opt/skidjakt/deploy.sh`
- Check logs: `.\check-logs.ps1`, `.\check-logs.ps1 -Follow`, `.\check-logs.ps1 -Errors`
- Setup HTTPS: `.\setup-https.ps1`
- Diagnose HTTPS: `.\diagnose-https.ps1`

## API Endpoints
- `GET /api/deals` - List deals with filtering (search, agencies, countries, price range, dates, sort, pagination)
- `GET /api/deals/{id}` - Single deal
- `GET /api/deals/filters` - Available filter options
- `GET /api/deals/stats` - Statistics (total, per agency, avg price)
- `GET /api/deals/stream` - SSE real-time updates
- `GET /api/health` - Health check
- `POST /api/scrape/trigger` - Manual scrape trigger

## Important Notes
- Always run `dotnet csharpier format .` in backend/ after modifying C# files
- Always run `npx prettier --write src/` in frontend/ after modifying TypeScript files
- Always update PLAN.md when completing tasks or phases
- The SQLite database is auto-created on first run at `data/skidjakt.db`
- The scraping background service runs every 30 minutes (5 in dev)
- Development is done on Windows; production runs on Linux (DigitalOcean droplet)
- All deployment scripts read configuration from `.env` file (see `.env.example`)
- The `deploy.ps1` script builds images locally, transfers via `scp`, and restarts on the droplet (no remote builds)
- The `deploy.sh` script runs directly on the droplet (expects images to be pre-loaded via `docker load`)
- Production compose (`docker-compose.prod.yml`) uses `image:` not `build:` — images are pre-built locally

## Scraper Data Sources

### Slopestar (active)
- **AJAX endpoint**: `https://www.slopestar.se/includes/ajax_show-earlybookings.php?...&track_page={page}`
- **HTML structure**: `div.st-list-discount-container` contains deal rows
- **Available fields**: date, duration, destination, country (flag icon `des-flag` + h3 fallback), transport, accommodation, room type (span title), price, original price, discount, distances (`.distance-containers`), price-includes, booking URL
- **Encoding**: ISO-8859-1 (Latin1)

### Skilink (active)
- **Listing URL**: `https://www.skilink.se/sista-minuten/`
- **HTML structure**: `table#tourlist-table tr.item-row` with 6+ `<td>` columns
- **Available fields**: date (meta itemprop), transport, destination, country (from URL path), price (meta itemprop or span.theprice), booking URL
- **Encoding**: ISO-8859-1 (Latin1), requires `Accept: text/html` header
- **Pagination**: `?pagenumber={n}`, total from `.pager` text "Sida X av Y"

### Alpresor (disabled — needs Playwright)
- Site is a client-side React app, requires JavaScript rendering

### Nortlander (disabled — needs Playwright)
- Site uses Next.js with client-side search, requires JavaScript rendering

### Data field availability per source
| Field | Slopestar | Skilink |
|-------|-----------|---------|
| Destination | Yes | Yes |
| Country | Yes (flag/h3) | Yes (URL path) |
| Date | Yes (DD.MM.YYYY) | Yes (meta/DD/M) |
| Duration | Yes | No (default 7) |
| Transport | Yes | Yes |
| Price | Yes | Yes |
| Original price | Yes | No |
| Accommodation | Yes | No |
| Room type | Yes (span title) | No |
| Person count | Yes (from room type) | No |
| Distances | Yes (.distance-containers) | No |
| Lift pass | Yes (price-includes) | No |
| Transfer | Yes (price-includes) | No |
| Meals | Yes (price-includes) | No |

## Gotchas & Learnings
- **Docker Compose version**: The droplet may have `docker-compose` (v1, hyphen) instead of `docker compose` (v2, space). All scripts auto-detect which is available.
- **PowerShell SSH string escaping**: Use single-quote here-strings (`@'...'@`) when passing complex shell commands via SSH from PowerShell. Double-quote here-strings (`@"..."@`) will mangle quotes and variables.
- **Alpine Docker images**: Use alpine-based .NET images for smaller size. Requires `icu-libs` for Swedish locale support (`apk add --no-cache icu-libs`).
- **Initial scraping**: The scraping service checks the database on startup and only runs an initial scrape if no recent data exists. No need to manually trigger after fresh deploy.
- **PowerShell module loading**: Use dot-sourcing (`. "path\script.ps1"`) not `Import-Module` for `.ps1` files. `Export-ModuleMember` only works in `.psm1` module files.
- **Docker Compose override files**: NEVER use `docker-compose.override.yml` to change ports. Compose merges sequence fields (like `ports`), causing duplicate port bindings. Use `-f docker-compose.prod.yml` explicitly instead.
- **Docker builds on droplet**: The small DigitalOcean droplet can't handle .NET SDK multi-stage builds (100% CPU). Always build images locally and transfer via `docker save` + `scp` + `docker load`.
- **Zero-deal guard**: `UpsertDealsAsync` marks all existing deals for an agency as inactive when the incoming list doesn't contain them. If a scraper returns 0 deals (e.g. network error), this nukes all data. Always guard: skip upsert if the scraper returns 0 deals.
