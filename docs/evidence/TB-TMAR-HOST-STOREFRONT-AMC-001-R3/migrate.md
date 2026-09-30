# TB-TMAR-HOST-STOREFRONT-AMC-001-R3 — Migrate

## Scope

Evacuate Host residual storefront **browse BFF** (composer + models + remaining GET routes) into Catalog; move geography to Order; Contracts-only foreign enrichment.

## Disposition executed

| Item | Before | After |
| --- | --- | --- |
| `StorefrontComposer.cs` | Host/Storefront (~1600 LOC, foreign Application) | Catalog.Infrastructure/Storefront behind `IStorefrontComposer` |
| `StorefrontModels.cs` | Host/Storefront | Catalog.Application/Storefront/Models |
| Browse routes (home/categories/brands/sellers/merchandising/products/category-plp) | Host StorefrontEndpoints | Catalog.Endpoints `CatalogStorefrontBrowseEndpoints` via CQRS `ISender` |
| `GET /geography/provinces` | Host StorefrontEndpoints | Order.Endpoints StorefrontOrderEndpoints |
| Party enrichment | `IPartyLookupGateway` (Party.Application) | `IPartyLookup` (Party.Contracts) |
| Promotion enrichment | `IPromotionEvaluator` (Promotion.Application) | `ICheckoutPromotionPort` (Promotion.Contracts) |
| Reviews enrichment | `IReviewDirectory` (Reviews.Application) | **new** `IReviewsStorefrontLookup` (Reviews.Contracts) + Infra adapter |
| Content enrichment | `IContentDirectory` + Content.Domain locale | **new** `IContentStorefrontArticlesPort` (Content.Contracts) + Infra adapter |
| WishlistComposer / StoreLandingShellAdapter | Host `StorefrontComposer` | Catalog `IStorefrontComposer` |

## Host/Storefront residual after R3

- `StorefrontAccountIdentity.cs` (Host auth platform — R4/later relocate under Authentication if needed)
- `StorefrontDemoCatalogBootstrap.cs` / `StorefrontDemoCatalogMatrix.cs` (**R4**)
- `StorefrontEndpoints.cs` (empty stub for R1/R2 guard file presence)

## Behavior parity

- Paths under `/v1/storefront/*` unchanged
- JSON DTO shapes unchanged (models relocated, same record shapes)
- errorCodes: `storefront.brand.missing`, `storefront.seller.missing`, `storefront.product.missing`, `storefront.category.missing`
- Raw `Results.Json` 404 envelope preserved

## Guards

- `HostStorefrontAmcR3GuardTests`
- Updated: `HostCartResidualGuardTests` allowlist (removed deleted composer/models)

## Explicit non-goals (R4)

- DemoCatalogBootstrap/Matrix → module Development
- Host/Storefront ABSENT + final Host ZERO certify
