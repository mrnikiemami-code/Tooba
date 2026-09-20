# query-shape

| Surface | Before | After |
|---------|--------|-------|
| Admin product grid | Load all offer GroupBy / OfferId maps via DbContext | Same batch shape via gateway methods |
| Admin sellers grid | Distinct sellers + Active GroupBy | Same via gateway |
| Storefront cards | Per-product `_offers.Offers` query inside Compose | One batch `ListActiveOffersByCatalogVariantIdsAsync` for card set |
| Merch candidates | Take(200) Active ordered UpdatedAt | `ListRecentActiveOffersAsync(200)` |
| Merch enrich / Reservation batch | Contains id list | `FindOffersBatchAsync` |

No Task.Run / sync-over-async introduced.
