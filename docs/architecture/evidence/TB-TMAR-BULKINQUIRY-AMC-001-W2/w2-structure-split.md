# TB-TMAR-BULKINQUIRY-AMC-001-W2 — Layer structure split

## Mode

`ARCHITECT_DIRECT_AMSC` — Migrate/Structure physical tree (behavior-preserving).

## Changes

| Layer | Before | After |
|---|---|---|
| Domain | root `BulkPurchaseInquiry.cs` | `Aggregates/` + `Enums/` |
| Application | root contracts + technical `Commands/` | `Ports/` + `Models/` + `Storefront/{Commands,Validators}` |
| Infrastructure | root Directory + root Migrations + Outbox in Module | `Directories/` + `Persistence/` (+ Migrations, Outbox) |
| Contracts | `Errors/` (unchanged this wave) | `Errors/` |

## Consumer usings

Host Program CQRS marker, Endpoints, Host.Tests, and HostProductQnA Directory path updated. Catalog.Contracts-only seam unchanged.

## Guards

- `BulkInquiryModuleAmcW2StructureGuardTests`
- HostProductQnA path update for Directory

## Microservice note

Physical cohesion now matches extractable module layout; Result/error catalog remain W3.
