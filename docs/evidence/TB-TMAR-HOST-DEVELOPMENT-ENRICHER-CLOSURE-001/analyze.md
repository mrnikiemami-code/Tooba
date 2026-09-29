# TB-TMAR-HOST-DEVELOPMENT-ENRICHER-CLOSURE-001 — Analyze

## Active source (single debt #1)

`src/backend/Host/Tooba.Host/Development/CatalogAttributeSchemaSellableEnricher.cs`

- 154 LOC, `namespace Tooba.Host.Development`.
- Implements `Tooba.Catalog.Application.Development.ICatalogAttributeSchemaSellableEnricher`.
- Registered only in `Host/Program.cs` line 173.
- Invoked from `CatalogAttributeSchemaDevelopmentSeed.InvokeEnricherAsync` (Catalog.Infrastructure
  `Development/`) via `provider.GetService<ICatalogAttributeSchemaSellableEnricher>()`.
- Depends on `IServiceProvider` and resolves everything by `GetRequiredService`.

## Current classification

`CROSS_MODULE_DEVELOPMENT_ORCHESTRATION`
`STRUCTURAL_DEBT_ONLY`
`NEEDS_ARCHITECT_DECISION` (AMC-002) — reached Catalog + Offer + Party + Pricing + Inventory + Tax
and directly consumed Application/Infrastructure/Persistence surfaces.

## Illegal dependencies found (all removed by this task)

| Dependency | Kind | Status |
| --- | --- | --- |
| `Tooba.Catalog.Infrastructure.Persistence.CatalogDbContext` | own module (legal) | retained inside Catalog |
| `Tooba.Catalog.Application.ICatalogDirectory` | own module (legal) | retained |
| `Tooba.Party.Infrastructure.Persistence.PartyDbContext` (`.Parties`) | foreign DbContext + DbSet | REMOVED |
| `Tooba.Party.Application.IPartyDirectory` | foreign Application | REMOVED |
| `Tooba.Inventory.Application.Ports.IInventoryDirectory` | foreign Application | REMOVED |
| `Tooba.Pricing.Application.IPriceDirectory` | foreign Application | REMOVED |
| `Tooba.Tax.Application.ITaxDirectory` | foreign Application | REMOVED |
| `Tooba.Offer.Application.Commands.CreateOffer/ActivateOffer` + `MediatR.ISender` | foreign Application + CQRS bypass | REMOVED |
| `Tooba.Offer.Application.Ports.IOfferQueryGateway` (impl) | foreign Application | replaced by Offer **Contracts** port |
| `Tooba.Inventory.Application.Ports.IInventoryQueryGateway` (impl) | foreign Application | replaced by Inventory **Contracts** port |
| `Tooba.Inventory.Domain.ValueObjects.StockAdjustmentKind` | foreign Domain | REMOVED |
| `Tooba.Offer.Contracts.Dtos.SalesChannel` | Contracts | retained (legal) |

No cross-module EF/SQL join exists in this file (each module is read through its own port), so
`Cross-Module-Join-State = NONE` both before and after.

## Forbidden coupling summary

- Foreign DbContext: 2 (`CatalogDbContext` is own; `PartyDbContext` foreign).
- Foreign DbSet: 2 (`Parties` foreign; Catalog DbSets own).
- Foreign `Application`: Offer, Party, Pricing, Inventory, Tax (5).
- Foreign `Infrastructure`: Catalog only (own).
- Foreign `Domain`: Inventory (`StockAdjustmentKind`).
- Foreign persistence reach-through used to pick a seller and read variants/product/categories.

## Existing lawful Contracts reused

| Existing port | Module | Used for |
| --- | --- | --- |
| `IOfferQueryGateway` | Offer.Contracts | `ExistsBySellerSkuAsync` idempotency check |
| `IOfferDevelopmentSeedGateway` | Offer.Contracts | `EnsureActiveAsync` (already existed) |
| `IInventoryQueryGateway` | Inventory.Contracts | `FindLocationByCodeAsync` |
| `IOfferLookupGateway` (via Pricing impl) | Offer.Contracts | Offer existence/channel for Pricing |

## New Contracts ports judged necessary (minimum capability surface)

| New port | Where | Why a narrow port is required |
| --- | --- | --- |
| `IPartyDevelopmentSeedGateway` | `Tooba.Party.Contracts` | Party had no Contracts port that selects/creates an Organization; the exact capability was only reachable through `PartyDbContext` or foreign Application. |
| `IPricingDevelopmentSeedGateway` | `Tooba.Pricing.Contracts/Ports` | `IPriceDirectory` is Application; an authored+activated base price had no Contracts boundary. |
| `IInventoryDevelopmentSeedGateway` | `Tooba.Inventory.Contracts/Availability` | location/position/stock write had no Contracts boundary; Domain `StockAdjustmentKind` must not leak. |
| `ITaxDevelopmentSeedGateway` | `Tooba.Tax.Contracts/Ports` | category create + offer classification had no Contracts boundary. |
| `IOfferDevelopmentSeedGateway.EnsureActiveSellerOfferAsync` | `Tooba.Offer.Contracts/Ports` | additional method on the existing Offer development port (REFERENCE-MODULE EXCEPTION). |

All new contracts expose IDs / primitives / `Result` only. None expose EF types, DbContext,
entities, `IServiceProvider`, or foreign Application types.

## Port placement decision

Module-boundary ports live in each owning module's `Contracts`. They are **not** placed in
Catalog, because then the Party/Pricing/Inventory/Tax Infrastructure projects would depend on
`Tooba.Catalog.Contracts`, which is a foreign-module dependency and would violate
`Non_host_projects_do_not_reference_foreign_module_infrastructure` /
`Module_infrastructure_does_not_reference_foreign_module_infrastructure_or_persistence`
read the other way round.

## Closed-folder / destination integrity

| Destination | Classification | Outcome |
| --- | --- | --- |
| `Host/Tooba.Host/Development` | LOCKED_BY_ACCEPTED_DISPOSITION (AMC-002) | 6 -> **5** production files (shrink only, no sink) |
| `Catalog.Infrastructure/Development` | OPEN_FOR_CURRENT_TASK (owns the capability) | +1 file |
| `Party/Pricing/Inventory/Tax/Ofer Contracts` | OPEN_FOR_CURRENT_TASK (in-task capability surface) | +1 file each |
| `Party/Pricing/Inventory/Tax Infrastructure` | OPEN_FOR_CURRENT_TASK (in-task capability surface) | +1 adapter each |

No new Host folder, no Host folder growth: `SINK_FOLDER_REGRESSION = NONE`.

## Bounded disposition

`READY_TO_MIGRATE` — the required narrow Contracts surfaces were introducible without a broader
product/domain/schema/public-contract decision. `ProductWorkspaceDevelopmentBootstrap.cs` was
read-only inspected and deliberately NOT touched.
