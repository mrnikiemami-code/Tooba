# offer-read-contract

## Port

`IOfferQueryGateway : IOfferLookupGateway` in `Tooba.Offer.Contracts.Ports`.

One cohesive read port (not one interface per Host file). Existing lookup methods retained; query/metrics/storefront/seed reads added.

## DTOs (`Tooba.Offer.Contracts.Dtos`)

- `OfferSellerStatusRow`
- `OfferListItem` (includes `UpdatedAt` for merch candidate ordering)
- Existing `OfferReference` / `OfferStatus` / `SalesChannel` reused

## Capabilities

| Method | Host consumer |
|--------|---------------|
| CountActiveOffersAsync | AdminPanel dashboard |
| ListDistinctSellerPartyIdsAsync | AdminPanel / AdminSellersGrid |
| ListSellerStatusRowsAsync | AdminPanel seller list |
| CountActiveOffersBySellerAsync | AdminSellersGrid metrics |
| CountAllOffersGroupedByCatalogVariantAsync | AdminProductGrid offerCount |
| MapAllOfferIdsToCatalogVariantIdsAsync | AdminProductGrid sellable/location/price metrics |
| ListOffersByCatalogVariantIdsAsync | ProductWorkspace |
| ListActiveOffersByCatalogVariantIdsAsync | Storefront |
| ListActiveOffersAsync | Storefront public sellers |
| AnyOffersForCatalogVariantIdsAsync | ProductWorkspace delete guard |
| ListRecentActiveOffersAsync | Merchandising candidates |
| FindOffer / FindOffersBatch | Merch + Reservation (via inheritance) |
| FindLatestBySellerAndVariant / ExistsBySellerSku / ListOfferIdsBySellerSkuPrefix / FindBySellerSku | Development seeds |

## Rules honored

- No EF types
- No Pricing/Inventory fields
- Batch APIs where Host previously looped or loaded dictionaries
- English comments on new Offer Contracts/Infrastructure APIs

Also:

- `IOfferSchemaMigrator`
- `IOfferDevelopmentSeedGateway`
