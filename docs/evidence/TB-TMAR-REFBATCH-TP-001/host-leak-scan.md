# Host leak scan — TB-TMAR-REFBATCH-TP-001

Production Host (`Tooba.Host`) was searched for `TaxDbContext` and `PricingDbContext`.

Before:
- `ProductWorkspaceComposer`, `AdminProductGridQueryEngine`, `StorefrontComposer` read both contexts.
- `StorefrontDemoCatalogBootstrap`, `AccessControlDevelopmentSeed` read tax categories and rules.
- `MerchandisingCampaignDevelopmentSeed` read pricing rows.
- `ProductWorkspaceDevelopmentBootstrap` migrated both contexts.
- `ModuleMigrationRegistry` named both context types.

After:
- Host production contains no `TaxDbContext` or `PricingDbContext`.
- Reads go through `ITaxQueryGateway` and `IPriceQueryGateway`.
- Bootstrap migration goes through `ITaxSchemaMigrator` and `IPricingSchemaMigrator`.
- MigrationRunner uses `TaxModuleMigration` / `PricingModuleMigration`, matching Offer.

Host tests may still construct the contexts. That is test setup, not production authority.

Other modules' DbContexts in Host were left in place. This task does not absorb unrelated Host debt.

No Host tax or pricing aggregate mutation remains outside the module directories.
