# TOOBA REFERENCE MODULE PATTERN

Canonical golden module: **Offer** (physically revalidated by `TB-TMAR-OFFER-REFERENCE-W1-R1` after `TB-TMAR-OFFER-REFERENCE-W1` was reopened).

## Module top-level projects

```text
src/backend/Modules/<Name>/
  Tooba.<Name>.Domain/
  Tooba.<Name>.Application/
  Tooba.<Name>.Contracts/
  Tooba.<Name>.Infrastructure/
  Tooba.<Name>.Endpoints/
  Tooba.<Name>.Tests/
```

These projects must exist as **real on-disk `.csproj` trees**, not logical/documentation-only groupings.

## Layer purpose

| Project | Purpose |
| --- | --- |
| Domain | Aggregates, domain events, invariants. No Infrastructure/Endpoints/Host. |
| Application | Ports, use-case guards, application directories. No DbContext/Host. |
| Contracts | Stable cross-module DTOs/ports/enums. No EF types. |
| Infrastructure | Persistence, adapters, outbox, DI module registration. |
| Endpoints | HTTP transport mapping only. No business persistence/decisions. |
| Tests | Module-owned Domain/Application/Contracts/Infrastructure/Endpoints/Architecture tests. |

## Folder conventions (Offer proven on disk)

- Domain: `Aggregates/`, `Events/`
- Application: `Ports/` (create `UseCases/` only when real use-case files exist)
- Contracts: `Ports/`, `Dtos/`
- Infrastructure: `Persistence/` (+ `Configurations/`, `Migrations/`), `Adapters/`, `Outbox/`, `Events/`, `DependencyInjection/`
- Endpoints: feature folders (e.g. `Seller/`) + `*EndpointModule.cs`
- Tests: mirrors layers under `Domain/`, `Contracts/`, `Infrastructure/`, `Endpoints/`, `Architecture/`

Do not create empty ceremonial folders.

## Physical structure lock

`COMPLETE_REFERENCE_PATTERN` requires **Physical-Structure-State: VERIFIED_ON_DISK**.

- Production `.cs` files must live under approved responsibility folders.
- Namespaces must align with those folders where semantically appropriate.
- Project-reference / Host-route guards alone are insufficient (`ARCH-MODULE-PHYSICAL-001`).
- Tax/Pricing must be independently rechecked against this physical rule before claiming PROVEN_ON_N from a repair task.

## Endpoint ownership

- Module HTTP routes live in `Tooba.<Name>.Endpoints`.
- Host calls `Map<Name>Module()` (composition only).
- Host must not map module-owned business routes (guard `HOST-MODULE-ENDPOINT-001`).
- Auth/policy adapters may be Host-provided behind Endpoints ports.

## Host composition rule

Host may:

- register the module (`IToobaModule`)
- map endpoints
- provide cross-cutting platform services / temporary BFF enrichment adapters

Host must not own module route maps or grow new Offer/module business endpoints.

Offer seller read enrichment is owned by Offer Application read-model composition through Catalog/Pricing/Inventory Contracts gates (R4). IOfferSellerPanel removed.

## Contracts boundary

Inbound consumers depend on `Tooba.<Name>.Contracts`, not Application/Domain implementations.

## Persistence ownership

- One module DbContext / schema
- Migrations owned by the module
- No cross-module FK
- ARCH-DATA-001 remains active

## Source-size / file cohesion

- No new multi-responsibility god-files (`ARCH-MODULE-FILE-001`)
- No Offer/reference production file >800 LOC (`ARCH-SIZE-001`)
- Prefer one aggregate / one EF configuration / one cohesive endpoint class per file

## Dependency rules

```text
Domain
  ↑
Application  →  Contracts  ← external consumers
  ↑
Endpoints

Infrastructure → implements Application/Contracts ports
Host → composition + thin adapters only
```

No cycles. Endpoints must not reference Infrastructure internals.

## Test ownership

Architecture guards live in the module Tests project and, where Host-facing, in Host.Tests.
Physical-folder guards (`ARCH-MODULE-PHYSICAL-001`) are mandatory for reference-complete claims.

## Migration checklist for the next module

1. Inventory baseline (projects, LOC, Host endpoints, foreign deps)
2. Confirm suitability or BLOCK
3. Create Endpoints + Tests projects if missing
4. Normalize Domain/Application/Contracts/Infrastructure folders **on disk**
5. Align namespaces to folders; update consumers
6. Extract Host endpoints → module Endpoints; Host Map*Module only
   - If no Host HTTP routes exist for the module, still create Endpoints + `Map*Module()` composition point (proven by Tax)
7. Add architecture + source-size + **physical-structure** guards
8. Persist before/after filesystem trees as evidence
9. Preserve routes/behavior; NEW_FAILURES=0
10. Update reference docs + recovery SoT
11. Stop; do not start the next module in the same task

Reference-pattern reuse across Tax/Pricing remains historically shipped, but physical revalidation after Offer R1 is Architect-gated (`Reference-Pattern-State: REVALIDATED_WITH_PHYSICAL_STRUCTURE` on Offer; do not claim PROVEN_ON_2/3 from the Offer repair alone).

## Anti-patterns

- Claiming COMPLETE from namespace/docs without on-disk folders
- Leaving duplicate module routes in Host
- Endpoints calling foreign DbContexts directly
- Application referencing Host
- Shared mega-DbContext / cross-module FK
- Empty folder ceremony
- Growing Host BFF residuals without documenting them
- Continuing Checkout W6 (or any other paused stream) inside a reference-module task
