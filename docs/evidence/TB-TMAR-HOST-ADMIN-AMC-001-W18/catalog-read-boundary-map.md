# W18 — Catalog read boundary map (design lock, not full migration)

W19 requires ProductWorkspace composition to read Catalog data **without** `CatalogDbContext` or `Catalog.Application`.

## Existing Catalog.Contracts inventory (reuse first)

| Contract | Path | Relevance to W19 aggregate reads |
|---|---|---|
| `ICatalogOfferReadGateway` / `CatalogOfferPresentation` | `CatalogOfferReadContracts.cs` | Offer presentation titles/brand/unit — partial list enrichment |
| `ICatalogVariantLookup` / `CatalogVariantLookupResult` | `ICatalogVariantLookup.cs` | Variant↔product identity; titles; primary category ids |
| `ICatalogCartPresentationLookup` | `Cart/ICatalogCartPresentationLookup.cs` | Cart presentation — not Admin workspace primary |
| Cart quantity contracts | `Cart/CatalogCartQuantityContracts.cs` | Cart-scoped quantity — not Admin workspace primary |
| Reservation / checkout abuse settings | `Reservation/*`, `Checkout/*` | Out of W19 Admin composition scope |
| Access-control scope resources | `AccessControlScopeResourceContracts.cs` | AC resource ids — not composition reads |
| `CatalogErrorCodes` | `Errors/CatalogErrorCodes.cs` | Catalog-owned errors (do not duplicate in ProductWorkspace) |

## Preferred W19 Catalog read needs vs current Contracts

| W19 need | Current Contracts coverage | W18 action |
|---|---|---|
| Product identity / list basics | Partial via variant lookup + offer presentation | **Document only** — need dedicated Admin product list/get read ports |
| Variant identifiers / status / catalog seam | Partial (`FindVariantAsync`, titles) | **Document only** — status/axes missing |
| Product attributes + variant-axis values | **Missing** | Defer to W19 Catalog.Contracts additions |
| Media references | **Missing** (Host uses CatalogDbContext / media directory) | Defer; do not expose EF entities |
| Category assignments / leaf names / paths | **Missing** as workspace aggregate | Defer |
| Brand identity / display | Partial via offer presentation BrandName | Defer dedicated brand options (Catalog-only route candidate) |
| Localized product fields | Partial titles only | Defer |
| Quantity policy + unit options | **Missing** for Admin workspace | Defer |
| Publish readiness | Already Catalog Endpoints (`ProductPublishing`) | Reuse Catalog ownership; composition may call Contracts query if exposed |
| History shell | Already Catalog Endpoints (`ProductHistory`) | Same |

## W18 decision

**Stop at documented boundary map.** No new Catalog.Contracts types added in W18 (avoid guessing a giant “workspace snapshot” that mirrors persistence). PASS does not require all W19 gateways implemented.

## Forbidden

- Exposing EF / Domain entities through Contracts
- Adding write commands to Catalog.Contracts in W18
- ProductWorkspace.Infrastructure referencing `CatalogDbContext`
