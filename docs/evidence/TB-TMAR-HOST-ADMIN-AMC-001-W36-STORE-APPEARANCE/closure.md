# W36-STORE-APPEARANCE closure

## Outcome
Host Admin StoreAppearanceSettingsEndpoints + Composer evacuated to Catalog. Projector moved from Host.Storefront to Catalog.Infrastructure. Admin `*.cs` count **17 → 15** (KEEP platform floor).

## Proof
- Catalog.Endpoints: StoreAppearanceSettingsEndpoints GET/PUT `/v1/admin/settings/appearance`
- Catalog.Application: GetStoreAppearanceAdminSettingsQuery + StoreAppearanceSettingsComposer + IStoreAppearanceProjector
- SaveStoreAppearanceSettingsHandler invalidates projector cache after write
- Catalog.Infrastructure/StoreAppearance/StoreAppearanceProjector registered in CatalogModule
- Program: MapStoreAppearanceSettingsEndpoints + Host projector/composer DI removed; CatalogEndpointModule registers route
- Host.Storefront consumers use `Tooba.Catalog.Infrastructure.StoreAppearance`
- Guard: HostAdminAmcW36StoreAppearanceGuardTests (Admin=15 KEEP floor)
- Remaining Host Admin: platform KEEP only

## SoT
- docs/architecture/tmar-current-state.json → hostAdminAmcStoreAppearance
