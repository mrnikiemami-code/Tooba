# TB-TMAR-PRODUCTQNA-AMC-001-W4 — Structure gate

## Structure-State

`READY_FOR_CERTIFY`

## Folder-Granularity-State

`PROFESSIONAL_SHALLOW`

- Application: `Customer/{Commands,Validators}` + `Storefront/{Queries,Validators}` + `Composition` / `Models` / `Ports`
- Domain: `Aggregates` / `Enums`
- Infrastructure: `Directories` / `Development` / `Persistence` (+ Migrations)
- Endpoints: root composition + `Customer/` + `Storefront/`
- Contracts: `Errors` / `Resources`

## Solution-Explorer-State

`CANONICAL` — `/Modules/ProductQnA/` in `Tooba.slnx` (5 projects including Endpoints).

## Path-Namespace-State

`EXACT`

## Physical-Copy-State

`CLEAN`

## Root-Allowlist-State

`ENFORCED` — see manifest ProductQnA entry.
