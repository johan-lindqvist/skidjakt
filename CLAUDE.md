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

## Commands
- Backend build: `cd backend && dotnet build`
- Backend run: `cd backend && dotnet run --project src/Skidjakt.Api`
- Backend test: `cd backend && dotnet test`
- Frontend dev: `cd frontend && npm run dev`
- Frontend build: `cd frontend && npm run build`
- Format C#: `cd backend && dotnet csharpier format .`
- Format TS: `cd frontend && npx prettier --write src/`
- Docker: `docker compose up --build`

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
