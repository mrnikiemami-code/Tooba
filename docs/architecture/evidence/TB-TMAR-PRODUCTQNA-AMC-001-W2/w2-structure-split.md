# TB-TMAR-PRODUCTQNA-AMC-001-W2 — Layer structure split

## Mode

`ARCHITECT_DIRECT_AMSC` — Migrate/Structure physical tree (behavior-preserving).

## Changes

| Layer | Before | After |
|---|---|---|
| Domain | root `ProductQuestion.cs` | `Aggregates/` + `Enums/` |
| Application | root contracts + technical `Commands/`/`Queries/` | `Ports/` + `Models/` + `Customer/` + `Storefront/` |
| Infrastructure | root Directory/Seed + root Migrations | `Directories/` + `Development/` + `Persistence/` (+ Migrations) |
| Contracts | `Errors/` (unchanged this wave) | `Errors/` |

## Consumer usings

Host Program CQRS marker and Host.Tests/Endpoints updated to new namespaces. Catalog Contracts-only seam unchanged.

## Guards

- `ProductQnAModuleAmcW2StructureGuardTests`
- HostProductQnA path update for Directory
