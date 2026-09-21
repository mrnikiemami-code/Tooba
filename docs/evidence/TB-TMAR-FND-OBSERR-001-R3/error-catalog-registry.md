# Error catalog registry

- Foundation: `FoundationErrorCatalogContributor`
- Offer: `OfferErrorCatalogContributor` via `AddOfferEndpointPresentation`
- Compose: `ErrorDefinitionCatalog` from `IEnumerable<IErrorCatalogContributor>`
- Duplicate codes fail fast at catalog construction
