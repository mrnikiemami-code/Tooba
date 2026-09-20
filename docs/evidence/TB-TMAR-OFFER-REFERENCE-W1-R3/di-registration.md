# DI registration

- Host CQRS foundation scans the Offer Application assembly.
- OfferModule registers `IOfferStore` as `OfferStore`.
- `IOfferLookupGateway` resolves to the same `OfferStore`.
- Catalog and Party modules register their new contract lookup interfaces.
- No service locator is used by Offer handlers.
