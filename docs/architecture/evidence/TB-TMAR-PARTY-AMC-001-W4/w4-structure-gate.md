# TB-TMAR-PARTY-AMC-001-W4 — Structure gate

## Structure-State

`READY_FOR_CERTIFY`

## Folder-Granularity-State

`PROFESSIONAL_SHALLOW`

- Application: `Admin/Sellers/{Queries,Validators}` + `Seller/{Commands,Queries,Models,Validators}` + `Composition` / `Models` / `Ports`
- Domain: `Aggregates` / `Enums` / `Events`
- Infrastructure: `Directories` / `Admin` / `Grid` / `Seller` / `Adapters` / `Development` / `Events` / `Projections` / `Persistence` (+ Migrations)
- Endpoints: root composition + `Admin/Sellers` + `Seller`
- Contracts: `Ports` / `Errors` / `Resources`

## Solution-Explorer-State

`CANONICAL` — `/Modules/Party/` in `Tooba.slnx` (5 projects including Endpoints).

## Path-Namespace-State

`EXACT`

## Physical-Copy-State

`CLEAN`

## Root-Allowlist-State

`ENFORCED` — see manifest Party entry.
