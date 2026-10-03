# TB-TMAR-WISHLIST-AMC-001-W4 — Structure gate

## Structure-State

`READY_FOR_CERTIFY`

## Folder-Granularity-State

`PROFESSIONAL_SHALLOW`

- Application: `Customer/{Commands,Queries,Validators}` + `Composition` / `Models` / `Ports`
- Domain: `Aggregates`
- Infrastructure: `Directories` / `Persistence` (+ Migrations, Outbox) / `Development`
- Endpoints: root composition + `Customer/`
- Contracts: `Errors` / `Ports` / `Resources`

## Solution-Explorer-State

`CANONICAL` — `/Modules/Wishlist/` in `Tooba.slnx` (5 projects including Contracts + Endpoints).

## Path-Namespace-State

`EXACT`

## Physical-Copy-State

`CLEAN`

## Root-Allowlist-State

`ENFORCED` — see manifest Wishlist entry.
