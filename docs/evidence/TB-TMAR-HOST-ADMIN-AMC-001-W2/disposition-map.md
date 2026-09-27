# Disposition map — W2

| File | Disposition | Reason |
|---|---|---|
| QuantitySettingsEndpoints.cs | MOVE_TO_Catalog.Endpoints | Catalog-owned settings; CQRS-ready; no Host.Storefront |
| StoreAppearanceSettingsEndpoints.cs | NOT_MOVED/DEFERRED | Depends on Host composer with Storefront projector |
| StoreAppearanceSettingsComposer.cs | NOT_MOVED/DEFERRED | Host.Storefront StoreAppearanceProjector coupling; needs Catalog port/Contracts redesign |

Host/Admin count: 59 → 58 (−1 QuantitySettingsEndpoints).
