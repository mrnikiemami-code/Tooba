# TB-TMAR-BULKINQUIRY-AMC-001-W4 — Structure gate

## Structure-State

`READY_FOR_CERTIFY`

## Folder-Granularity-State

`PROFESSIONAL_SHALLOW`

- Application: `Storefront/{Commands,Validators}` + `Composition` / `Models` / `Ports`
- Domain: `Aggregates` / `Enums`
- Infrastructure: `Directories` / `Persistence` (+ Migrations, Outbox)
- Endpoints: root composition + `Storefront/`
- Contracts: `Errors` / `Resources`

## Solution-Explorer-State

`CANONICAL` — `/Modules/BulkInquiry/` in `Tooba.slnx` (5 projects including Contracts + Endpoints).

## Path-Namespace-State

`EXACT`

## Physical-Copy-State

`CLEAN`

## Root-Allowlist-State

`ENFORCED` — see manifest BulkInquiry entry.
