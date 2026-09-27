# W34-TEMPLATE-SEEDS closure

## Outcome
Admin template catalog seeds + attribute-schema Development bootstrap evacuated to Catalog.Infrastructure/Development. Admin `*.cs` count **23 → 18**.

## Proof
- Catalog Development owns: FashionTemplateCatalogSeed, IndustryBatchA/B/CTemplateCatalogSeed, CatalogAttributeSchemaDevelopmentSeed
- Host Development thin wrappers: `*TemplateCatalogSeedHost`, CatalogAttributeSchemaDevelopmentSeedHost
- Host Development enricher: CatalogAttributeSchemaSellableEnricher → `ICatalogAttributeSchemaSellableEnricher`
- Storefront Fashion/Industry template constants import Catalog.Infrastructure.Development (not Host.Admin)
- Program call sites updated; DI registers sellable enricher
- Deleted Admin: FashionTemplateCatalogSeed.cs, IndustryBatchA/B/CTemplateCatalogSeed.cs, CatalogAttributeSchemaDevelopmentBootstrap.cs
- Guard: HostAdminAmcW34TemplateSeedsGuardTests
- HoldPolicy / StoreAppearance: untouched

## SoT
- docs/architecture/tmar-current-state.json → hostAdminAmcTemplateSeeds
