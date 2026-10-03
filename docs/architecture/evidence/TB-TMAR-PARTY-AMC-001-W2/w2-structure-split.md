# TB-TMAR-PARTY-AMC-001-W2 — Layer structure split

## Mode

`ARCHITECT_DIRECT_AMSC` — Migrate/Structure physical tree (behavior-preserving).

## Changes

| Layer | Before | After |
|---|---|---|
| Domain | root `PartyDomain.cs` god-file | `Aggregates/`, `Enums/`, `Events/` — path↔namespace exact |
| Application | root `PartyContracts.cs` | `Ports/`, `Models/` (+ existing Admin/Seller capability trees) |
| Contracts | root port dump | `Ports/` (`IPartyLookup`, sellers grid, seed, seller settings) |
| Infrastructure | root Directory/Outbox/Seed/Projection | `Directories/`, `Persistence/PartyOutboxRegistration.cs`, `Development/`, `Projections/` (+ existing Admin/Grid/Seller/Adapters) |

## Consumer usings

Cross-module consumers updated to `Tooba.Party.Contracts.Ports` (and Application/Domain sub-namespaces where Host.Tests touch internals). No foreign Application/Infrastructure/Domain project references introduced.

## Guards

- `PartyModuleAmcW2StructureGuardTests` — no root dumps; expected capability folders present
- `PartyFoundationTests` Authzed scan updated for split sources
- HostSellerAmcR3 + Party W1/W2/Foundation: green

## Microservice note

Contracts-only seams retained (`IPartyLookup` et al.). Physical cohesion now matches extractable module layout; typed faults/error catalog remain W3.
