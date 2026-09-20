# host-offer-files

## Module-owned Offer HTTP

- `src/backend/Modules/Offer/Tooba.Offer.Endpoints/OfferEndpointModule.cs` — `MapOfferModule()`
- `src/backend/Modules/Offer/Tooba.Offer.Endpoints/Seller/OfferSellerEndpoints.cs`
- `src/backend/Modules/Offer/Tooba.Offer.Endpoints/Seller/IOfferSellerAuthorizer.cs`

## Host thin composition only

- `Program.cs` — `MapOfferModule()` + DI registration for `IOfferSellerAuthorizer` / `IOfferSellerPanel`
- `Seller/HostOfferSellerAuthorizer.cs` — auth adapter implementing Endpoints.Seller port
- `Seller/SellerPanelComposer.cs` — residual BFF enrichment implementing `IOfferSellerPanel` (documented; no Offer route maps)
- `Seller/SellerPanelEndpoints.cs` — comment only; no `/offers` Map*

Verified: Host SellerPanelEndpoints does not MapGet/MapPost/MapPatch `/offers`.
