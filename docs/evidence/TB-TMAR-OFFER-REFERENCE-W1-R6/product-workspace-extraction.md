# product-workspace-extraction

`ProductWorkspaceComposer` uses `IOfferQueryGateway`.

- List/detail offer mapping via `ListOffersByCatalogVariantIdsAsync`
- Delete reference check via `AnyOffersForCatalogVariantIdsAsync`
- Grid engine receives gateway (not DbContext)
- Pricing/Inventory enrichment unchanged and owner-local
