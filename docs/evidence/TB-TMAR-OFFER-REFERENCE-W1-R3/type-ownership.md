# Type ownership

- Domain enums live in `Tooba.Offer.Domain.ValueObjects`.
- Contract enums live in `Tooba.Offer.Contracts.Dtos` with matching numeric values.
- `SellerOffer` uses Domain enums; `OfferReference` uses Contract enums.
- `OfferContractMapping` performs explicit boundary conversion.
- Domain no longer references Contracts.
- `TypeForwarders.cs` was removed.
- Domain and Contracts retain identical stable error-code values.
