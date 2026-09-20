# admin-panel-extraction

`AdminPanelComposer` now injects `IOfferQueryGateway`.

- Active count via `CountActiveOffersAsync`
- Seller universe via `ListDistinctSellerPartyIdsAsync` / `ListSellerStatusRowsAsync`
- `AdminSellersGridQueryEngine` constructed with the same gateway
- No `OfferDbContext`, no `_offers.Offers`, no Persistence using
