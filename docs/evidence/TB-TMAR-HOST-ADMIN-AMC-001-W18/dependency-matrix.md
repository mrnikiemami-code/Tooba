# W18 — Dependency matrix

## Allowed project references (as built)

| Project | May reference | Actual W18 refs |
|---|---|---|
| Contracts | neutral BuildingBlocks only if required | **none** |
| Domain | own domain only | **none** |
| Application | Contracts + Domain + BuildingBlocks; foreign `*.Contracts` only when needed | BuildingBlocks + Contracts + Domain |
| Infrastructure | Application + Contracts + Domain; foreign Contracts only; ModuleContracts | ModuleContracts + Application + Contracts + Domain |
| Endpoints | Application + Contracts + BuildingBlocks presentation | Application + Contracts + BuildingBlocks |

## Forbidden edges proven ZERO

| Edge | State |
|---|---|
| Application → foreign Application | ZERO |
| Application → foreign Infrastructure | ZERO |
| Application → Party.Application | ZERO |
| Infrastructure → foreign Application / Infrastructure / Domain | ZERO |
| Infrastructure → foreign DbContext / Persistence | ZERO |
| Endpoints → ProductWorkspace.Infrastructure | ZERO |
| Endpoints → foreign Infrastructure | ZERO |
| ProductWorkspace.* → Host | ZERO |

## Host → ProductWorkspace

Host references **Infrastructure only** for neutral `IToobaModule` registration. No endpoint map call. No DI of business services.

## Foreign Contracts not yet referenced

W18 Application/Infrastructure intentionally do **not** yet reference Catalog/Offer/Pricing/Inventory/Tax/Party Contracts — those arrive when live composition ports are adopted (W19+). Documented in `catalog-read-boundary-map.md` / `party-boundary.md`.
