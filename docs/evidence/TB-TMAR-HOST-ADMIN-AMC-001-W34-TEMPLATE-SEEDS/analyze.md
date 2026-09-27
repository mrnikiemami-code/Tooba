# Analyze — W34 Template / Attribute Seeds

| Member | Disposition |
|---|---|
| FashionTemplateCatalogSeed (+ MediaPaths) | MOVE → Catalog.Infrastructure/Development |
| FashionTemplateCatalogSeedHost | KEEP thin Host/Development (commerce assigner) |
| IndustryBatchA/B/CTemplateCatalogSeed | MOVE → Catalog.Infrastructure/Development |
| IndustryBatchA/B/CTemplateCatalogSeedHost | KEEP thin Host/Development (commerce assigner) |
| CatalogAttributeSchemaDevelopmentBootstrap | MUST_SPLIT |
| → schema seed (category/attrs/product/variants) | MOVE → Catalog.Infrastructure/Development |
| → sellable enricher (Offer/Pricing/Inventory/Tax/Party) | KEEP Host.Development implementing `ICatalogAttributeSchemaSellableEnricher` |
| → commerce assigner | KEEP thin Host.Development SeedHost |

Owner: Catalog (template catalog + attribute schema seeds). Host retains ControlPlane/commerce wrappers and cross-module sellable enricher only.

Do NOT touch: HoldPolicy, StoreAppearance.
