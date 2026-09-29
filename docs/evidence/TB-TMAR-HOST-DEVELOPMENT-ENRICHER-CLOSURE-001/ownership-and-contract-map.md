# TB-TMAR-HOST-DEVELOPMENT-ENRICHER-CLOSURE-001 — Ownership & Contract Map

## Ownership decision

The schema demo-product sellable workflow is **Catalog-owned**. Catalog already owns
`CatalogAttributeSchemaDevelopmentSeed`; the enricher was its missing tail. Host must not own
cross-module business/development orchestration.

- Owner after this task: `Tooba.Catalog.Infrastructure/Development/CatalogAttributeSchemaSellableEnricher.cs`
- Host: composition-only. `Program.cs` no longer registers the implementation.
- `DevelopmentTenantCommerceContext.cs` remains the single accepted tenant-commerce development seam.

## Capability → owner → boundary map

| Capability in the workflow | True owner | Boundary used by Catalog |
| --- | --- | --- |
| Demo product/category/variant/media/SEO/publish | Catalog | own `ICatalogDirectory` + `ProductPublishPrep` + `CatalogCategoryTreeRules` |
| Seller Party resolve/create | Party | `IPartyDevelopmentSeedGateway` (new, Party.Contracts) |
| Offer create + activate (idempotent by seller SKU) | Offer | `IOfferDevelopmentSeedGateway.EnsureActiveSellerOfferAsync` (new method) |
| Offer existence by seller SKU | Offer | `IOfferQueryGateway.ExistsBySellerSkuAsync` (existing) |
| Price create + activate | Pricing | `IPricingDevelopmentSeedGateway.EnsureDevelopmentBasePriceAsync` (new) |
| Inventory location + position + stock increase | Inventory | `IInventoryDevelopmentSeedGateway` (new) |
| Tax category + offer classification | Tax | `ITaxDevelopmentSeedGateway` (new) |

## Contract reuse vs creation

Reused (no new code): `IOfferQueryGateway`, `IOfferDevelopmentSeedGateway.EnsureActiveAsync`,
`IInventoryQueryGateway`, `IOfferLookupGateway`.

Created (minimum required): four narrow development-support ports + one additional method on the
existing Offer development port. Each port name is capability-first and development-scoped, not
named after the retired Host class.

## REFERENCE-MODULE EXCEPTION (Offer)

Offer is normally read-only. For this task the Architect authorized the minimum Offer
Contracts + Infrastructure change required by this seed:

- `Tooba.Offer.Contracts/Ports/IOfferDevelopmentSeedGateway.cs` — added
  `EnsureActiveSellerOfferAsync(Guid catalogVariantId, Guid sellerPartyId, string sellerSku, CancellationToken)`.
  Existing `EnsureActiveAsync` / `EnsureActiveCloneFromAnyActiveAsync` are unchanged so the
  Promotion `MerchandisingCampaignDevelopmentSeed` consumer is unaffected.
- `Tooba.Offer.Infrastructure/Adapters/OfferDevelopmentSeedGateway.cs` — implements the new method
  and factors the shared "activate if needed" step into a private helper (no behavior change to the
  existing methods).

Offer was **not** re-audited, reorganized, re-certified, or otherwise refactored.

## Persistence ownership

Every write is executed inside the owning module's Infrastructure adapter.
Catalog touches its own `CatalogDbContext` only. Zero foreign DbContext/DbSet/Application/
Infrastructure/Domain references remain in the moved file (`Foreign-*-Reference-State = ZERO`).

## No cross-module join

The workflow reads each module through its own port and orchestrates by identifier. There is no
`join` across module persistence sources, no `FromSql` over module-owned tables, and no shared
mutable entity.

## CQRS / HTTP position

This is development-seed infrastructure, not an HTTP use case. No endpoint, route, CQRS ceremony,
validator, error catalog entry, or ProblemDetails mapping was added — consistent with the task's
explicit instruction. The retired Host implementation used `MediatR.ISender`; the replacement uses
the Offer module's own development port instead, which is narrower and removes the Host→foreign
Application edge.

## Registration

| Module | Registration added |
| --- | --- |
| Catalog | `ICatalogAttributeSchemaSellableEnricher -> CatalogAttributeSchemaSellableEnricher` (Infrastructure owns its own implementation) |
| Party | `IPartyDevelopmentSeedGateway -> PartyDevelopmentSeedGateway` |
| Pricing | `IPricingDevelopmentSeedGateway -> PricingDevelopmentSeedGateway` |
| Inventory | `IInventoryDevelopmentSeedGateway -> InventoryDevelopmentSeedGateway` |
| Tax | `ITaxDevelopmentSeedGateway -> TaxDevelopmentSeedGateway` |
| Host | removed `ICatalogAttributeSchemaSellableEnricher` registration |

No circular project reference was introduced. `Tooba.Catalog.Infrastructure` gained
`Party.Contracts` and `Tax.Contracts` (Contracts-only) references; Offer/Pricing/Inventory
Contracts were already referenced.
