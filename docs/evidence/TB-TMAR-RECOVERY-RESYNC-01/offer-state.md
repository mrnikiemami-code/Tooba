# Offer State

## TB-TMAR-OFFER-REFERENCE-W1

Implemented Offer golden module: Domain Aggregates/Events; Application Ports; Contracts Ports/Dtos; Infrastructure Persistence/Configurations/Adapters/Outbox/DI/Events; **Tooba.Offer.Endpoints** + **Tooba.Offer.Tests**; Host `MapOfferModule()`; seller Offer HTTP extracted from Host; ARCH-MODULE-FILE-001 + HOST-MODULE-ENDPOINT-001.

## TB-TMAR-OFFER-REFERENCE-W1-R1

User visual reopen of COMPLETE. Repair aligned namespaces to folders, removed empty ceremonial dirs, moved Domain TypeForwarders into Aggregates, added **ARCH-MODULE-PHYSICAL-001** (`OfferPhysicalStructureGuardTests`), Physical-Structure-State **VERIFIED_ON_DISK**, Module-Recovery-State **COMPLETE_REFERENCE_PATTERN**, Reference-Pattern-State **REVALIDATED_WITH_PHYSICAL_STRUCTURE**.

## Current physical structure (on disk)

Projects: Domain, Application, Contracts, Infrastructure, Endpoints, Tests — all with `.csproj`.

Folders (non-empty): Domain `Aggregates/` `Events/`; Application `Ports/`; Contracts `Ports/` `Dtos/`; Infrastructure `Persistence/(Configurations|Migrations)` `Adapters/` `Outbox/` `Events/` `DependencyInjection/`; Endpoints `Seller/` + `OfferEndpointModule.cs`; Tests Architecture/Contracts/Domain/Endpoints/Infrastructure.

## Endpoints / Host

- `Tooba.Offer.Endpoints` **exists**
- Host `Program.cs` calls `MapOfferModule()`; registers thin `IOfferSellerAuthorizer` adapter
- `SellerPanelEndpoints` does **not** map `/offers` (comment only)
- Residual: `SellerPanelComposer` still implements `IOfferSellerPanel` (documented BFF enrichment)

## Persistence

- `OfferDbContext` + Configurations + Migrations under Offer.Infrastructure.Persistence

## Source-size / guards

- Offer production >800 LOC: 0 (R1 evidence)
- Architecture + physical guards in Offer.Tests

## Dependency residuals (documented debt; not blocking COMPLETE claim)

- Offer.Infrastructure → Catalog.Application + Party.Application (grandfathered lookups)
- Host BFF `IOfferSellerPanel` enrichment

## Offer-Module-Recovery-State

**COMPLETE_REFERENCE_PATTERN**
