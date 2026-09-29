# Contract map — TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-CATALOG-SEED-REHOME-001

## Reused (unchanged) contracts

| Contract | Owner | Used by |
| --- | --- | --- |
| `Tooba.Offer.Contracts.Ports.IOfferDevelopmentSeedGateway.EnsureActiveSellerOfferAsync` | Offer | `WorkspaceDemoMarketplaceSeed` |
| `Tooba.Pricing.Contracts.IPricingDevelopmentSeedGateway.EnsureDevelopmentBasePriceAsync` | Pricing | `WorkspaceDemoMarketplaceSeed` |
| `Tooba.Party.Contracts.IPartyDevelopmentSeedGateway.ResolveDevelopmentSellerPartyAsync` | Party | (existing consumer) |

Offer remained untouched — `EnsureActiveSellerOfferAsync` was sufficient, so no Offer file changed.

## Narrow additive contracts (parity-required)

| Contract | Owner | Added member | Why |
| --- | --- | --- | --- |
| `IPartyDevelopmentSeedGateway` | Party | `EnsureDevelopmentOrganizationAsync`, `EnsureDevelopmentOrganizationDisplayNamesAsync`, `DevelopmentOrganizationRename` | Demo seller resolve/create and legacy display-name refresh without Host `PartyDbContext` access |
| `ITaxDevelopmentSeedGateway` | Tax | `EnsureDevelopmentRuleAsync`, `DevelopmentTaxRuleKind`, `DevelopmentTaxOverridePolicy` | Create + activate the exact development tax rule without Host `ITaxDirectory`/`Tax.Domain` |
| `IInventoryDevelopmentSeedGateway` | Inventory | `ReserveDevelopmentHoldAsync`, `SeedDevelopmentStockHold` | Exact development reservation-hold (`workspace-live-hold`) without Host `IInventoryDirectory` |

Each addition is one narrow development method plus the minimum request/parameter surface.

### Boundary-type discipline

`DevelopmentTaxRuleKind` and `DevelopmentTaxOverridePolicy` exist so `Tooba.Tax.Contracts` (which has
no reference to `Tooba.Tax.Domain`) can express the rule shape. `TaxDevelopmentSeedGateway` maps them
to `TaxRuleKind`/`TaxOverridePolicy` internally. This mirrors the existing pattern already used by the
Development seed surface (`Tooba.Inventory.Contracts.Availability.IInventoryDevelopmentSeedGateway`
exposing `SeedDevelopmentStock` while `InventoryDevelopmentSeedGateway` maps to
`StockAdjustmentKind` internally, and `CatalogAttributeSchemaSellableEnricher` referencing
`Tooba.Offer.Contracts.Dtos.SalesChannel`).

## Forbidden surfaces not exposed

No contract addition exposes `DbContext`, `DbSet`, module entities, Application services, Domain types,
`IServiceProvider`, or generic business CRUD.

## Catalog side

| Member | Kind |
| --- | --- |
| `IWorkspaceDemoSeed` | Application-owned Development port (composition seam) |
| `WorkspaceDemoSeed` | Infrastructure implementation, Di-registered `Scoped` |
| `WorkspaceDemoProductSeed` | Infrastructure Development seed; its only persistence is its own `CatalogDbContext` |
| `WorkspaceDemoMarketplaceSeed` | Infrastructure Development orchestration; foreign access is Contracts-only |

`Tooba.Catalog.Infrastructure.csproj` already referenced Party/Offer/Pricing/Inventory/Tax `.Contracts`
projects — no new project reference was added, and no foreign `.Application`/`.Infrastructure`/`.Domain`
reference exists.
