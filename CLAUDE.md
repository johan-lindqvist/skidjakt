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
- The `deploy.ps1` script runs from Windows and SSHs to the droplet
- The `deploy.sh` script runs directly on the droplet

## Gotchas & Learnings
- **Docker Compose version**: The droplet may have `docker-compose` (v1, hyphen) instead of `docker compose` (v2, space). All scripts auto-detect which is available.
- **PowerShell SSH string escaping**: Use single-quote here-strings (`@'...'@`) when passing complex shell commands via SSH from PowerShell. Double-quote here-strings (`@"..."@`) will mangle quotes and variables.
- **Alpine Docker images**: Use alpine-based .NET images for smaller size. Requires `icu-libs` for Swedish locale support (`apk add --no-cache icu-libs`).
- **Initial scraping**: The scraping service checks the database on startup and only runs an initial scrape if no recent data exists. No need to manually trigger after fresh deploy.
- **PowerShell module loading**: Use dot-sourcing (`. "path\script.ps1"`) not `Import-Module` for `.ps1` files. `Export-ModuleMember` only works in `.psm1` module files.
