# TB-TMAR-LOCALIZATION-AMC-001-W4 — Structure gate

## Structure-State

`READY_FOR_CERTIFY`

## Folder-Granularity-State

`PROFESSIONAL_SHALLOW`

- Application capability-first: `Languages/{Commands,Queries,Validators}` + shared `Composition` / `Models` / `Ports`
- No Application root dump; no top-level technical `Commands`/`Queries`
- Domain: `Aggregates` + `Enums`
- Infrastructure: `Languages` / `Adapters` / `Bootstrap` / `Persistence` (+ Migrations under Persistence)
- Endpoints: root composition + `Admin/`
- Contracts: `Ports` / `Errors` / `Resources`

## Solution-Explorer-State

`CANONICAL` — `/Modules/Localization/` in `Tooba.slnx` with Application, Contracts, Domain, Endpoints, Infrastructure.

## Path-Namespace-State

`EXACT` — every production `.cs` (excluding Migrations) matches path-derived namespace.

## Physical-Copy-State

`CLEAN` — no stale root Language* dumps; catalog moved from Endpoints to Contracts.

## Root-Allowlist-State

`ENFORCED` — see `tmar-module-structure-manifests.json` Localization entry.
