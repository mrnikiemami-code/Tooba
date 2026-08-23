# Tooba — Module Composition & Boundary Enforcement

Status:

```text
IN_PROGRESS — TB-P01-T009 awaiting Architect ACCEPT
```

Task:

```text
TB-P01-T009
```

```text
Architecture boundaries are executable rules, not documentation only.
```

This document locks the P01 composition root and dependency guards. P00 ownership rules remain in `docs/architecture/03-data-ownership-and-module-contracts.md`.

## Module registration model

Tooba-owned contract: `IToobaModule` in `Tooba.ModuleContracts`.

A module registers its own services, infrastructure, and optional background workers through `AddServices`. Host composes an explicit list. There is no reflection-based auto-discovery.

Optional HTTP endpoints are not part of `IToobaModule` so the contract does not become a god-interface and ModuleContracts does not take an ASP.NET FrameworkReference. A future endpoint contributor can be a separate Host-facing seam that still lives behind module-owned mapping called from composition — not from business Domain.

`Tooba.ModuleContracts` is the stable surface for commands/queries/integration/gateway contracts later. It is not a dumping ground. No business contracts, persistence types, or DbContext belong there now.

## Composition root

`Tooba.Host.ToobaModuleComposition` is the single explicit composition location.

```text
Host
→ AddToobaModules
→ PlatformProbeModule (disposable sample)
```

Host may reference module Infrastructure to compose. Host must not contain business logic or own module tables.

PlatformProbe remains a disposable architecture sample. It proves registration and boundary tests. It is not a business capability.

## Dependency direction

Preferred:

```text
Domain
↑
Application
↑
Infrastructure
↑
Host composition
```

Forbidden:

```text
Domain → Infrastructure
Application → Host
Module A Infrastructure → Module B Infrastructure
Module A → Module B persistence internals
ModuleContracts → Host / module Infrastructure / Tooba.Persistence
BuildingBlocks → Host / modules
```

Shared technical persistence helpers (`Tooba.Persistence`) are not a module database. They must not become a mega `ToobaDbContext` / `AppDbContext`.

Cross-module interaction is only via contracts, interfaces, events, projections, and gateways. No foreign-module repository, DbContext, EF navigation, or SQL JOIN.

```text
Backend module boundary != UI boundary
```

Consumer business logic must not depend on in-process implementation details so a module can later be extracted as a service.

## Architecture tests

`ArchitectureBoundaryTests` fail the build when prohibited project references or a global `ToobaDbContext` / `AppDbContext` appear.

Limitation: only `Tooba.PlatformProbe.Infrastructure` exists as a module project. Domain/Application layer rules are encoded and will bind as soon as those projects appear; they are vacuously true today.

## Future module template

```text
src/backend/Modules/{Name}/Tooba.{Name}.Domain
src/backend/Modules/{Name}/Tooba.{Name}.Application
src/backend/Modules/{Name}/Tooba.{Name}.Infrastructure
cross-module types → Tooba.ModuleContracts
Host lists the module explicitly in ToobaModuleComposition
```

Do not add Identity, Catalog, or other business modules without a new Architect envelope.
