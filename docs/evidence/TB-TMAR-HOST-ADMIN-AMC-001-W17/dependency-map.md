# W17 — ProductWorkspaceComposer dependency map

Source: `ProductWorkspaceComposer` constructor + `AdminProductGridQueryEngine`.
Program DI: `builder.Services.AddScoped<ProductWorkspaceComposer>()`; routes via `app.MapProductWorkspaceEndpoints()`.

## Injected dependencies

| Dependency | Declared location | Classification | Used by | Notes |
|---|---|---|---|---|
| `CatalogDbContext` | Catalog.Infrastructure.Persistence | **direct module persistence violation** (Host reach-through) | List/Get/mutations/brand-options/quantity/units/delete | Must be replaced by Catalog Contracts/read ports + Catalog write commands before Host evacuation completes |
| `ICatalogDirectory` | Catalog.Application | Host→foreign Application (legacy Host residue; shrink) | Create, category, lifecycle, variants, readiness wrapper, history append/list | Future: Catalog Commands/Queries via MediatR; composition module must not take Catalog.Application |
| `IOfferQueryGateway` | Offer.Contracts.Ports | **lawful Contracts dependency** | List enrich, Get offers, Delete reference check, Grid metrics | Keep |
| `IPriceQueryGateway` | Pricing.Contracts | **lawful Contracts dependency** | List enrich, Get prices, Grid metrics | Keep |
| `IInventoryQueryGateway` | Inventory.Contracts.Availability | **lawful Contracts dependency** | List enrich, Get stock/locations, Grid metrics | Keep |
| `ITaxQueryGateway` | Tax.Contracts | **lawful Contracts dependency** | Get tax classifications/categories | Keep on aggregate Get only |
| `IPartyLookupGateway` | **Party.Application** (not Contracts) | **Host-only / boundary debt** | Get seller display names on Offers | Canonical Contracts port exists: `Tooba.Party.Contracts.IPartyLookup`. Future composition owner must use Contracts, not Party.Application |

## Special surfaces

### ListAsync

- Catalog persistence for product ids + names + categories + media
- Offer/Price/Inventory Contracts for offer count, amount range, sellable units, locations
- Classification: **Host composition residue** that must move to a dedicated composition owner depending only on Contracts (+ Catalog read Contracts)

### QueryGridAsync

- Instantiates `AdminProductGridQueryEngine(_catalog, _offers, _prices, _inventory)`
- Engine: CatalogDbContext filters/sorts + Offer/Price/Inventory metric filters
- Policy: `AdminProductGridQueryPolicy` (Host Grid) — module-specific grid policy must leave Host (evacuation protocol)

### GetAsync

- CatalogDbContext: Products, Variants, VariantAttributeValues, AttributeDefinitions, ProductAttributeValues, MediaReferences, ProductCategories, Categories, LocalizedTexts, UnitsOfMeasure (+ translations)
- Contracts: Offer, Pricing, Inventory, Tax
- Party.Application lookup gateway
- Catalog.Application: `GetProductPublishReadinessAsync`, `ListProductHistoryAsync` (Activity/Audit shell)
- Response: full `ProductWorkspaceView` — **cross-module aggregate**

### Mutation methods calling RequireWorkspaceAsync / GetAsync after write

| Composer method | Post-write composition |
|---|---|
| `CreateSimpleProductAsync` | `GetAsync` |
| `UpdateProductCoreAsync` | `GetAsync` |
| `AssignProductCategoryAsync` | `GetAsync` |
| `AddAdditionalCategoryAsync` | `GetAsync` |
| `RemoveAdditionalCategoryAsync` | `GetAsync` |
| `AssignProductBrandAsync` | `GetAsync` |
| `UpdateCatalogTitleAsync` | `GetAsync` |
| `UpdateQuantityPolicyAsync` | `GetAsync` |
| `PublishAsync` / `UnpublishAsync` / `ArchiveAsync` / `RestoreAsync` | `RequireWorkspaceAsync` → `GetAsync` |
| `CreateVariantAsync` / `PatchVariantAsync` | `RequireWorkspaceAsync` → `GetAsync` |
| `DeleteOrSoftArchiveAsync` | none (NoContent / throw) |

## Candidate ports for future composition owner

1. **Catalog.Contracts** product workspace read port(s): product aggregate slice, list ids, brand options, localized texts, category paths, units — replace direct `CatalogDbContext`
2. **Catalog.Contracts / MediatR commands**: create/update/lifecycle/variant/delete — replace Host write paths
3. Keep Offer/Pricing/Inventory/Tax Contracts gateways as-is
4. Switch Party to `IPartyLookup` (Contracts)
5. Grid policy/engine move into composition module Application/Infrastructure (not Catalog)

## Forbidden proposals (explicit)

- Catalog.Application / Catalog.Infrastructure composing Offer/Pricing/Inventory/Tax/Party for `ProductWorkspaceView`
- New composition module Application → foreign Application (must use Contracts only)
- Expanding Host DbContext join across schemas (already avoided; must stay avoided)
