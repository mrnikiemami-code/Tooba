# seed-bootstrap-extraction

| Site | Change |
|------|--------|
| ProductWorkspaceDevelopmentBootstrap | `IOfferSchemaMigrator.MigrateAsync` |
| AccessControlDevelopmentSeed | `IOfferQueryGateway.FindLatestBySellerAndVariantAsync` |
| CatalogAttributeSchemaDevelopmentBootstrap | `ExistsBySellerSkuAsync` |
| StorefrontDemoCatalogBootstrap | `ListOfferIdsBySellerSkuPrefixAsync("DEMO-")` |
| MerchandisingCampaignDevelopmentSeed | query gateway + `IOfferDevelopmentSeedGateway.EnsureActiveCloneFromAnyActiveAsync` |
| MigrationRunner | `OfferModuleMigration` factory (Adapters); no `OfferDbContext` type name |

Host production seeds no longer type `OfferDbContext`.
