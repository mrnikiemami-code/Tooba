# offer-persistence-guards

Added in `OfferArchitectureGuardTests`:

1. `Host_production_sources_do_not_reference_offer_persistence`
2. `Foreign_modules_do_not_reference_offer_dbcontext`
3. `Extracted_host_surfaces_use_offer_query_gateway_not_persistence` (explicit composer/grid/storefront/merch/reservation paths)

Exceptions: Offer.Infrastructure, Offer/Host integration tests, MigrationRunner tooling via `OfferModuleMigration`.
