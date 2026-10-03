# TB-TMAR-USERPREFERENCE-AMC-001-W4 — Structure gate

## Structure-State

`READY_FOR_CERTIFY`

## Folder-Granularity-State

`PROFESSIONAL_SHALLOW`

- Application: `LocalePreferences/{Commands,Queries,Validators}` + `UiPreferences/{Commands,Queries,Validators}` + `Composition` / `Models` / `Ports`
- Domain: `Aggregates`
- Infrastructure: `Directories` / `Persistence` (+ Migrations, Outbox) / `Development`
- Endpoints: root composition + `Customer/` + `Admin/`
- Contracts: `Errors` / `Resources`

## Solution-Explorer-State

`CANONICAL` — `/Modules/UserPreference/` in `Tooba.slnx` (5 projects including Contracts + Endpoints).

## Path-Namespace-State

`EXACT`

## Physical-Copy-State

`CLEAN`

## Root-Allowlist-State

`ENFORCED` — see manifest UserPreference entry.
