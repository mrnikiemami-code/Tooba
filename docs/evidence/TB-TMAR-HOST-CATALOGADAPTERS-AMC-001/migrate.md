# Migrate — Host/CatalogAdapters AMC-001

## Waves

1. **Promotion.Contracts seam**: `MerchandisingCampaignContracts.cs` (`IMerchandisingCampaignQuery`, runtime models, type codes, Development ids, limits)
2. **Delete Application runtime ports**: removed `MerchandisingCampaignRuntimePorts.cs` / `MerchandisingDevelopmentIds.cs` from Promotion.Application
3. **Catalog adapters**: moved gate/merchandising/shell into `Catalog.Infrastructure/StoreLanding/`
4. **DI**: `CatalogModule` registers StoreLanding adapters; Host `Program.cs` CatalogAdapters regs ZERO
5. **Host ZERO**: deleted `Host/CatalogAdapters/`
6. **Callers/tests**: Promotion Infrastructure/Endpoints/Admin usings retargeted; Host tests retargeted to Contracts
7. **Guards**: `HostCatalogAdaptersAmcGuardTests` + retargeted W33 / Storefront R3 guards + durable SoT

## Behavior preserved

- Same campaign membership gate for store landing pages
- Same amazing-type merchandising member resolve + store-alpha Development id mapping
- Same shell product/category/brand/article/review composition via Catalog storefront composer
- HTTP routes / CQRS / schema unchanged; frontend unchanged
