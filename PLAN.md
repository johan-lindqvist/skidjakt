# Skidjakt - Implementation Plan

## Status Overview

| Phase | Status | Description |
|-------|--------|-------------|
| Phase 1 | **IN PROGRESS** | Project Scaffolding |
| Phase 2 | Pending | First Scraper + API (Skilink) |
| Phase 3 | Pending | Frontend MVP |
| Phase 4 | Pending | Remaining Scrapers |
| Phase 5 | Pending | Polish & Deploy |

---

## Phase 1: Project Scaffolding

- [ ] Create .NET 10 solution with 4 projects (Api, Core, Infrastructure, Scraper)
- [ ] Create React + Vite + TypeScript + TailwindCSS frontend
- [ ] Create docker-compose.yml, Dockerfiles
- [ ] Create CLAUDE.md with project conventions
- [ ] Set up EF Core with SQLite, create initial migration
- [ ] Wire up minimal API with health endpoint
- [ ] Verify everything builds and runs

## Phase 2: First Scraper + API (Skilink vertical slice)

- [ ] Implement `SkilinkScraper` (AJAX endpoint, direct HTTP)
- [ ] Implement `ScrapingBackgroundService` with scheduling
- [ ] Implement deal upsert logic in repository
- [ ] Implement `GET /api/deals` with basic filtering
- [ ] Implement `GET /api/deals/filters`
- [ ] Implement `GET /api/deals/{id}`, `/stats`, `/stream`, `/health`
- [ ] Implement `POST /api/scrape/trigger`
- [ ] Verify: trigger scrape -> deals in DB -> deals from API

## Phase 3: Frontend MVP

- [ ] Build `DealCard` component
- [ ] Build `DealGrid` with responsive layout
- [ ] Build `FilterBar` with agency + country filters
- [ ] Build `SearchBar`
- [ ] Build layout components (Header, Footer, Container)
- [ ] Wire up `useDeals` hook with TanStack Query
- [ ] Wire up `useFilters` hook with URL sync
- [ ] Verify: full flow from scrape -> API -> UI

## Phase 4: Remaining Scrapers

- [ ] Implement `AlpresorScraper` (alpresor.se/sista-minuten/)
- [ ] Implement `NortlanderScraper` (nortlander.se)
- [ ] Implement `SlopestarScraper` (slopestar.se - Playwright for AJAX)
- [ ] Test each scraper individually, verify data normalization

## Phase 5: Polish & Deploy

- [ ] Add price range slider, date filter, sort options
- [ ] Add SSE real-time updates
- [ ] Add deal stats summary
- [ ] Add pagination
- [ ] Responsive/mobile optimization
- [ ] Docker multi-stage builds
- [ ] Deploy script (deploy.sh)

---

## Technology Stack

### Backend
- .NET 10, ASP.NET Core Minimal API
- Entity Framework Core 10 + SQLite
- AngleSharp (HTML parsing), Microsoft.Playwright (JS-heavy sites)
- IHostedService + PeriodicTimer for scheduling
- SSE for real-time updates
- Serilog for structured logging

### Frontend
- React 19 + TypeScript
- Vite 6
- TailwindCSS 4 (dark alpine theme)
- TanStack Query (server state)
- Lucide React (icons)
- Swedish-only UI

### Infrastructure
- Docker + docker-compose
- SQLite (file-based, persisted volume)
- nginx (frontend static serving + API proxy)
- DigitalOcean droplet

---

## Project Structure

```
C:\code\skidjakt\
├── backend/
│   ├── Skidjakt.sln
│   ├── Dockerfile
│   ├── src/
│   │   ├── Skidjakt.Api/
│   │   ├── Skidjakt.Core/
│   │   ├── Skidjakt.Infrastructure/
│   │   └── Skidjakt.Scraper/
│   └── tests/
│       └── Skidjakt.Tests/
├── frontend/
│   ├── Dockerfile
│   ├── src/
│   │   ├── components/
│   │   ├── hooks/
│   │   ├── types/
│   │   ├── services/
│   │   ├── App.tsx
│   │   └── main.tsx
│   ├── index.html
│   ├── package.json
│   ├── tsconfig.json
│   ├── vite.config.ts
│   └── tailwind.config.ts
├── docker-compose.yml
├── deploy.sh
├── CLAUDE.md
└── PLAN.md
```
