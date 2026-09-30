# Analyze — Host/Wishlist AMC-001

## Target

`src/backend/Host/Tooba.Host/Wishlist/` (3 files) + Wishlist module foundation + Catalog coupling in `WishlistDirectory`.

## True ownership

| Responsibility | Owner |
|---|---|
| Wishlist aggregate + schema `wishlist` | Wishlist.Domain / Infrastructure |
| Add/remove/list/membership use cases | Wishlist.Application (CQRS) |
| HTTP `/v1/customer/wishlist` | Wishlist.Endpoints |
| Product published check | Catalog.Contracts (`ICatalogReviewProductLookup`) |
| Storefront product cards for list compose | Catalog.Contracts (`ICatalogStorefrontProductCardLookup`) |
| Dev seed product sampling (distinct categories) | Catalog.Contracts sampler + Wishlist.Infrastructure seed |
| Actor resolution | Wishlist.Endpoints (ICurrentAuthenticatedUser + StorefrontGuestActor) |
| Host | composition only (`MapWishlistModuleEndpoints`, seed call order) |

## Coupling / blockers

1. Host endpoints → `IWishlistDirectory` + Host `WishlistComposer` (no MediatR).
2. Host composer → `Catalog.Application` `IStorefrontComposer` / `StorefrontProductCard`.
3. Host seed → `CatalogDbContext` + `Catalog.Domain` + `WishlistDbContext` cross-persistence.
4. `WishlistDirectory` → `ICatalogLookupGateway` (Catalog.Application) + `CatalogPublicationStatus` (Catalog.Domain).
5. Actor guest id from `Order.Application.StorefrontCheckoutService` (must be `Order.Contracts.Fulfillment.StorefrontGuestActor`).
6. Module missing Endpoints project; Application is flat `WishlistContracts.cs` (FOUNDATION_PARTIAL).

## Canonical gaps

- No ApiResponseFactory / SemanticError for 401 (ad-hoc JSON).
- No FluentValidation on membership body / productId.
- No module error catalog / resx for wishlist product codes.

## Migration plan (behavior-preserving)

1. Catalog Contracts: product-card lookup + development published-product sampler.
2. Wishlist foundation: Endpoints + CQRS Commands/Queries + PresentationComposer + seed move + Directory→Contracts.
3. Host ZERO: delete `Host/Wishlist`, rewire Program + DevelopmentSchemaMigrator.
4. Guards + SoT + focused validate.

## Behavior locks

- Routes unchanged: GET/POST/DELETE `/v1/customer/wishlist`, POST `/membership`.
- Idempotent add (200 vs 201), remove NoContent, unavailable reason `product-unavailable`.
- Seed: guest actor, up to 3 published products from distinct categories, fixed CreatedAt.
