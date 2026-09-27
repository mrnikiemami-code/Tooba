# W24-RESERVATION-POLICY analyze

## Source (Host Admin ONLY)
- `ReservationPolicyAdminEndpoints.cs` — HTTP_ENDPOINT (admin + seller)
- `ReservationPolicyAdminComposer.cs` — PRESENTATION_COMPOSITION + PERSISTENCE via CatalogDbContext
- `ReservationPolicyAdminModels.cs` — response/write DTOs

## Destination
| Responsibility | Class | Destination |
|---|---|---|
| Admin/Seller HTTP routes | HTTP_ENDPOINT | Order.Endpoints Admin/Settings + Seller/Settings |
| Preview composition ForStore/Category/Offer | PRESENTATION_COMPOSITION | Order.Application ReservationPolicyComposer |
| CQRS get/save/deny | APPLICATION_USE_CASE | Order.Application Admin/Settings/ReservationPolicy |
| Store/category/offer override + audit persistence | PERSISTENCE | Catalog.Contracts `IStoreReservationPolicySettingsPort` + Catalog.Infrastructure |
| Offer → primary category | CONTRACT | Offer.Contracts `IOfferQueryGateway` + Catalog.Contracts `ICatalogVariantLookup` |
| Policy preview resolver | already Order | `IReservationCyclePolicyResolver` (unchanged) |

## Out of scope
HoldPolicy* (BLOCK multi-owner; minimal compile adaptation only), StoreAppearance*, Merchandising*, ProductWorkspace*, CheckoutIdentity, template seeds.

## Foundation
Order + Catalog: FOUNDATION_READY.
