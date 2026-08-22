# Tooba — Platform Foundation Bootstrap

Status:

```text
P01 bootstrap — candidate layout; not an ADR; not a P01 Gate
```

Task:

```text
TB-P01-T001
```

## Chosen repository / application layout

```text
src/backend/Tooba.slnx
src/backend/Host/Tooba.Host/
src/backend/BuildingBlocks/Tooba.BuildingBlocks/
src/backend/Modules/Tooba.ModuleContracts/
src/frontend/          Next.js App Router (TypeScript, Tailwind)
```

Existing local `shopeiva/` remains an **uncommitted external reference**, not this skeleton.

## Backend project / folder purpose

| Path | Purpose |
| --- | --- |
| `Tooba.Host` | ASP.NET Core composition root. Starts the process. Health/readiness only. No business APIs. |
| `Tooba.BuildingBlocks` | Shared technical primitives later (clock, errors). No module tables. |
| `Tooba.ModuleContracts` | Cross-module contract assembly. Persistence stays inside future module Infrastructure projects. |
| `Modules/` | Future domain modules (Catalog, Order, …) as separate projects. Not generated in this task. |

Solution format is `Tooba.slnx` (current `dotnet new sln` output).

## Frontend project / folder purpose

| Path | Purpose |
| --- | --- |
| `src/frontend/app/` | App Router. Server Components by default. |
| `src/frontend/app/layout.tsx` | Root layout (RSC). |
| `src/frontend/app/page.tsx` | Minimal non-commercial home. |
| Tailwind | Utility CSS baseline only. Not a Design System. |

No Shopeiva copy. No Data Grid. No commercial workspaces.

## Module-boundary convention

- Host may reference BuildingBlocks and ModuleContracts.
- Future modules: Domain / Application / Infrastructure / Contracts as needed.
- No module may reference another module’s Infrastructure/persistence.
- Collaboration later: contracts, gateways, events, projections.

## Configuration convention

Host `appsettings.json` keys under `Tooba:` for Edition, PostgreSQL connection string (empty in repo), Observability, Cache, ExternalProviders.

No secrets. No tenant resolution. Empty connection string is a seam, not a working database.

## Build / run commands

Backend:

```bash
dotnet restore src/backend/Tooba.slnx
dotnet build src/backend/Tooba.slnx
dotnet run --project src/backend/Host/Tooba.Host/Tooba.Host.csproj
```

Health: `GET /health` and `GET /ready` on the host URL (default `http://localhost:5088`).

Frontend:

```bash
cd src/frontend
npm install
npm run typecheck
npm run lint
npm run build
npm run dev
```

## Intentionally NOT implemented

Identity, tenant routing, PostgreSQL usage, OpenTelemetry pipeline, Redis, SpiceDB, outbox, business modules, Admin/Seller/Customer UI, Data Grid, Design System, Shopeiva import/study.

No business tests yet because no business implementation exists.

## How this preserves P00 architecture

Modular monolith Host; module contracts isolated from persistence; PostgreSQL named as the only DB seam (no SQL Server/SQLite as architecture truth); UI shell is not a backend CRUD mirror; Server Component First; future Data Grid and Deep Shopeiva Study remain ROADMAP items.
