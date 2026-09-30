# Certify — Host/Wishlist AMC-001

## Verdict

**HOST_ZERO PASS** for `Host/Wishlist` folder evacuation. Module HTTP/CQRS/Contracts boundaries established. Full Offer-clone ARCH-COMPLETE-002 structure certification is **PARTIAL** (foundation improved; residual root Infrastructure layout acceptable for this Host-evacuation wave).

## Checklist

| Goal | State |
|---|---|
| Host/Wishlist production files | 0 (directory ABSENT) |
| Module HTTP ownership | `Wishlist.Endpoints` — 4 routes |
| CQRS / ISender | PASS |
| Catalog cross-module | Contracts only (`ICatalogReviewProductLookup`, `ICatalogStorefrontProductCardLookup`, sampler) |
| Cross-module DbContext | ZERO from Wishlist |
| Actor | `ICurrentAuthenticatedUser` + `StorefrontGuestActor` |
| Session 401 | `ApiResponseFactory` + foundation `customer.session.required` (not re-registered) |
| Product unavailable | `WishlistErrorCodes` + catalog contributor + resx |
| Schema change | NONE |
| Frontend | UNCHANGED (card DTO property names aligned) |
| Durable guards | `HostWishlistAmcGuardTests` + updated foundation/storefront R3 |
| Focused validation | PASS (26 passed, 1 skipped postgres) |

## Blockers for full ARCH-COMPLETE-002 (not blocking Host ZERO)

- Wishlist.Infrastructure still has root `WishlistModule.cs` / `WishlistDirectory.cs` (not DependencyInjection/Adapters layout)
- No dedicated Wishlist.Tests architecture/validator-coverage project
- Domain `WishlistItem` remains at Domain root (not Aggregates/)

## Durable guard

`Tooba.Host.Tests.Architecture.HostWishlistAmcGuardTests`
