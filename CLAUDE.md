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
  - `src/Skidjakt.Infrastructure/` - EF Core, DbContext
  - `src/Skidjakt.Scraper/` - Per-agency scrapers, background service
  - `tests/Skidjakt.Tests/` - Unit tests
- `frontend/` - React app

## Conventions
- **C# formatting**: CSharpier (`dotnet csharpier .` in backend/)
- **TypeScript formatting**: Prettier (`npx prettier --write .` in frontend/)
- **Language**: Swedish UI labels, code in English
- **Solution format**: .slnx (not .sln)

## Commands
- Backend build: `cd backend && dotnet build`
- Backend run: `cd backend && dotnet run --project src/Skidjakt.Api`
- Frontend dev: `cd frontend && npm run dev`
- Frontend build: `cd frontend && npm run build`
- Format C#: `cd backend && dotnet csharpier .`
- Format TS: `cd frontend && npx prettier --write src/`
- Docker: `docker compose up --build`

## API Endpoints
- `GET /api/deals` - List deals with filtering
- `GET /api/deals/{id}` - Single deal
- `GET /api/deals/filters` - Filter options
- `GET /api/deals/stats` - Statistics
- `GET /api/deals/stream` - SSE real-time updates
- `GET /api/health` - Health check
- `POST /api/scrape/trigger` - Manual scrape trigger
