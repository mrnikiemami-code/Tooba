# Migrate — Host/Wishlist AMC-001

## Waves executed

1. **Catalog Contracts seams**
   - `ICatalogStorefrontProductCardLookup` + `CatalogStorefrontProductCardDto`
   - `ICatalogDevelopmentPublishedProductSampler`
   - Adapters in Catalog.Infrastructure; registered in CatalogModule

2. **Wishlist foundation**
   - New `Tooba.Wishlist.Endpoints` (actor resolver, HTTP map, error catalog/resx)
   - Application CQRS: Add/Remove commands, List/Membership queries, validators
   - `WishlistPresentationComposer` via Catalog Contracts (no IStorefrontComposer)
   - `WishlistDirectory` → `ICatalogReviewProductLookup` (no Catalog.Application/Domain)
   - Seed → `Wishlist.Infrastructure/Development` via Catalog sampler + StorefrontGuestActor

3. **Host ZERO**
   - Deleted `Host/Tooba.Host/Wishlist/`
   - Program: `AddWishlistEndpointPresentation` + `MapWishlistModuleEndpoints` + CQRS assembly
   - DevelopmentSchemaMigrator calls module seed only

## Behavior preserved

- Routes: GET/POST/DELETE `/v1/customer/wishlist`, POST `/membership`
- Idempotent add 200/201; remove 204; unavailable reason `product-unavailable`
- Seed: guest actor, ≤3 distinct-category Published products, fixed CreatedAt

## Not done (out of wave)

- Full ARCH-COMPLETE-002 Offer-clone foldering (Infrastructure root Module/Directory remain; Domain not Aggregates/)
- Validator coverage durable guard class (validators present; no separate Wishlist.Tests project yet)
