# storefront-extraction

`StorefrontComposer` uses `IOfferQueryGateway`.

Classified uses:
- Public seller discovery: `ListActiveOffersAsync`
- Product compose: `ListActiveOffersByCatalogVariantIdsAsync`
- Card batch path: `BuildProductCardsAsync` preloads variants + one batch active-offer query, then passes `preloadedActiveOffers` into `ComposeProductAsync` (removes per-product Offer EF loop)

Selection/preferred-seller/price/inventory policy remains in Host; Offer port returns Offer-owned refs only.
