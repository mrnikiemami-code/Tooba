# Analyze — TB-TMAR-HOST-ADMIN-AMC-001-W2

Candidates:
1. Admin/QuantitySettingsEndpoints.cs — HTTP GET/PUT quantity-rounding; GET via ICatalogLookupGateway; PUT via SaveStoreQuantitySettingsCommand; auth via static AdminPanelAccess; RAW Results.Json + PlatformHttpException.
2. Admin/StoreAppearanceSettingsEndpoints.cs — HTTP GET/PUT appearance; delegates to StoreAppearanceSettingsComposer.
3. Admin/StoreAppearanceSettingsComposer.cs — uses StoreAppearanceProjector (Tooba.Host.Storefront) for GetEffective/Invalidate + ISender SaveStoreAppearanceSettingsCommand.

Foreign coupling findings:
- Quantity GET illegally used Application lookup gateway from Host endpoint (rehome to Catalog Query).
- StoreAppearanceComposer depends on Host.Storefront StoreAppearanceProjector — MUST NOT move into Catalog.
