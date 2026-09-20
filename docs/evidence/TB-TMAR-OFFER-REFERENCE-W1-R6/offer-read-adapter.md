# offer-read-adapter

Implemented on `OfferStore` (`IOfferStore` + `IOfferQueryGateway`) in Offer.Infrastructure.

- `AsNoTracking` on pure reads
- GroupBy/projection before materialization for counts
- Batch Contains filters for id/variant sets
- No cross-module joins / no Pricing/Inventory DbContext
- Registered in `OfferModule`: `IOfferLookupGateway`, `IOfferQueryGateway`, `IOfferSchemaMigrator`, `IOfferDevelopmentSeedGateway`

Supporting adapters:

- `OfferSchemaMigrator`
- `OfferDevelopmentSeedGateway`
- `OfferModuleMigration` (tooling factory for MigrationRunner; Host never types `OfferDbContext`)
