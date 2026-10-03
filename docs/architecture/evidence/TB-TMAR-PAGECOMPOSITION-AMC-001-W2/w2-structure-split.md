# TB-TMAR-PAGECOMPOSITION-AMC-001-W2 — Layer structure split

## Mode

`ARCHITECT_DIRECT_AMSC` — Migrate/Structure physical tree (behavior-preserving).

## Changes

| Layer | Before | After |
|---|---|---|
| Domain | root `PageCompositionEntities.cs` god-file | `Aggregates/`, `Catalog/`, `Constants/` — path↔namespace exact |
| Application | root contracts/failure mapper + technical `Commands/`/`Queries/`/`Validators/`/`Presentation/` | `Ports/` + `Models/` + `Admin/` + `Storefront/` + `Composition/` |
| Infrastructure | root Directory/Seed + root Migrations | `Directories/` + `Development/` + `Persistence/` (+ Migrations) |
| Contracts | `Errors/` (unchanged this wave) | `Errors/` |

## Consumer usings

Host Program CQRS marker, Development seed seams, Endpoints, and Host.Tests updated to new namespaces. Foreign module coupling remains ZERO.

## Guards

- `PageCompositionModuleAmcW2StructureGuardTests`
- `HostPageCompositionAmcGuardTests` Presentation→Composition path update

## Microservice note

Physical cohesion now matches extractable module layout; typed faults/Result/error catalog remain W3.
