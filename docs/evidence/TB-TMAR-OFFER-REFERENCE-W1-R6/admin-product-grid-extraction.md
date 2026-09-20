# admin-product-grid-extraction

`AdminProductGridQueryEngine` uses `IOfferQueryGateway`.

- Offer counts: `CountAllOffersGroupedByCatalogVariantAsync` (all statuses; preserves prior GroupBy semantics)
- Offer→variant index: `MapAllOfferIdsToCatalogVariantIdsAsync` for sellable units / location / amount metrics
- In-memory metric sort/filter preserved; no N+1 per row
