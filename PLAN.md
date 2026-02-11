# Skidjakt - Implementation Plan

## Status Overview

| Phase | Status | Description |
|-------|--------|-------------|
| Phase 1 | **DONE** | Project Scaffolding |
| Phase 2 | **DONE** | First Scraper + API (Skilink) |
| Phase 3 | **DONE** | Frontend MVP |
| Phase 4 | **IN PROGRESS** | Remaining Scrapers + Unit Tests |
| Phase 5 | **DONE** | Polish & Deploy |

---

## Phase 1: Project Scaffolding

- [x] Create .NET 10 solution with 4 projects (Api, Core, Infrastructure, Scraper)
- [x] Create React + Vite + TypeScript + TailwindCSS frontend
- [x] Create docker-compose.yml, Dockerfiles
- [x] Create CLAUDE.md with project conventions
- [x] Set up EF Core with SQLite (auto-created on startup)
- [x] Wire up minimal API with health endpoint
- [x] Verify everything builds and runs

## Phase 2: First Scraper + API (Skilink vertical slice)

- [x] Implement `SkilinkScraper` (HTTP + AngleSharp HTML parsing)
- [x] Implement `ScrapingBackgroundService` with PeriodicTimer scheduling
- [x] Implement deal upsert logic in `DealRepository`
- [x] Implement `GET /api/deals` with full filtering, sorting, pagination
- [x] Implement `GET /api/deals/filters` (distinct filter options)
- [x] Implement `GET /api/deals/{id}`, `/stats`, `/stream`, `/health`
- [x] Implement `POST /api/scrape/trigger`
- [x] Verify: trigger scrape -> deals in DB -> deals from API

## Phase 3: Frontend MVP

- [x] Build `DealCard` component (agency badge, price, inclusions, discount)
- [x] Build `DealGrid` with responsive layout (1/2/3 columns)
- [x] Build `FilterBar` with agency + country + transport type filters
- [x] Build `SearchInput` with debounced search
- [x] Build `SortSelector` (price, date, discount, newest)
- [x] Build `FilterChips` (active filter display with removal)
- [x] Build layout components (Header, Footer)
- [x] Wire up `useDeals` hook with TanStack Query
- [x] Wire up `useFilters` hook with URL sync
- [x] Wire up `useDealStream` SSE hook
- [x] Wire up `App.tsx` with all components
- [x] Verify: full flow from scrape -> API -> UI

## Phase 4: Remaining Scrapers

- [x] Implement `SkilinkScraper` — rewritten for actual HTML structure (`table#tourlist-table tr.item-row`, pagination)
- [x] Implement `SlopestarScraper` — rewritten to use AJAX endpoint (`ajax_show-earlybookings.php`, `div.st-list-discount-container`)
- [x] Disable `AlpresorScraper` — requires Playwright (client-side React app)
- [x] Disable `NortlanderScraper` — requires Playwright (Next.js client-side search)
- [x] HTML test fixtures for Skilink and Slopestar
- [x] Unit tests: 53 total (38 scraper + 15 repository)
- [x] Remove Alpresor/Nortlander stubs from DI (prevent 0-deal upsert nuking data)
- [x] Zero-deal guard in ScrapingBackgroundService and scrape trigger
- [x] Skilink scraper robustness (Accept header, status code logging/guard)
- [x] New fields: RoomType, PersonCount, DistanceToLiftMeters/Slope/Centre
- [x] Slopestar: parse room type, person count, distances from HTML
- [x] API filters: maxPersons, includesTransfer
- [x] Frontend: new fields in DealCard, person/transfer filters
- [x] Corrupt data cleanup (Oesterrike → Österrike)
- [x] Parser tests for new Slopestar fields (room type, person count, distances)
- [ ] Implement `AlpresorScraper` with Playwright (site requires JS rendering)
- [ ] Implement `NortlanderScraper` with Playwright (site requires JS rendering)

## Phase 5: Polish & Deploy

- [x] Add deal stats summary component (DealStats)
- [x] Add proper pagination component (Pagination)
- [x] SSE real-time updates (useDealStream hook)
- [x] Responsive/mobile optimization (grid cols 1/2/3)
- [x] Docker multi-stage builds (backend + frontend Dockerfiles)
- [x] Deploy script (deploy.sh)
- [x] Unit tests for DealRepository (15 tests passing)
- [x] Updated index.html (Swedish title, meta, dark bg)
- [ ] Price range slider (basic filtering via URL params works)
- [ ] Date range picker UI component

---

## Technology Stack

### Backend
- .NET 10, ASP.NET Core Minimal API
- Entity Framework Core 10 + SQLite
- AngleSharp (HTML parsing), Microsoft.Playwright (JS-heavy sites)
- IHostedService + PeriodicTimer for scheduling
- SSE for real-time updates
- Serilog for structured logging
- CSharpier for code formatting

### Frontend
- React 19 + TypeScript
- Vite 6
- TailwindCSS 4 (dark alpine theme)
- TanStack Query (server state)
- Lucide React (icons)
- Prettier for code formatting
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
│   ├── Skidjakt.slnx
│   ├── Dockerfile
│   ├── src/
│   │   ├── Skidjakt.Api/              # Minimal API host, endpoints
│   │   ├── Skidjakt.Core/             # Domain entities, interfaces, DTOs
│   │   ├── Skidjakt.Infrastructure/   # EF Core DbContext, DealRepository
│   │   └── Skidjakt.Scraper/          # Per-agency scrapers, background service
│   └── tests/
│       └── Skidjakt.Tests/
├── frontend/
│   ├── Dockerfile
│   ├── nginx.conf
│   ├── src/
│   │   ├── components/
│   │   │   ├── deals/                 # DealCard, DealGrid
│   │   │   ├── filters/              # FilterBar, FilterChips, SortSelector
│   │   │   ├── search/               # SearchInput
│   │   │   └── layout/               # Header, Footer
│   │   ├── hooks/                     # useDeals, useFilters, useDealStream
│   │   ├── types/                     # Deal, Filter types
│   │   ├── services/                  # API client
│   │   ├── App.tsx
│   │   └── main.tsx
│   ├── index.html
│   ├── package.json
│   ├── vite.config.ts
│   └── .prettierrc
├── docker-compose.yml
├── deploy.sh
├── CLAUDE.md
└── PLAN.md                            # This file - always kept up to date
```

---

## Key Architecture Decisions

- **SQLite** over PostgreSQL: Single file, no separate container, easy backup, sufficient for single-droplet deployment
- **SSE** over SignalR: Simpler for one-way server-to-client notifications
- **TanStack Query** over manual state: Automatic caching, refetching, and stale data management
- **AngleSharp + Playwright**: AngleSharp for simple HTML, Playwright for JS-rendered pages
- **URL-synced filters**: Shareable filter state via query parameters
- **CSharpier/Prettier**: Consistent formatting enforced by tools
