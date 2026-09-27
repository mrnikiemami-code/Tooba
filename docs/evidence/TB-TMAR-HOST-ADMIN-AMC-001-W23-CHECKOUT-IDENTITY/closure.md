# W23-CHECKOUT-IDENTITY closure

## Outcome
Host Admin CheckoutIdentity settings evacuated into Catalog (AMC Migrate = CheckoutAbuse settings mirror).

## Proof
- Host Admin file deleted: `CheckoutIdentitySettingsEndpoints.cs`
- Catalog owns `MapCheckoutIdentitySettingsEndpoints` via `CatalogEndpointModule`
- `IStoreCheckoutIdentitySettingsDirectory` / `StoreCheckoutIdentitySettingsDirectory` registered in CatalogModule
- Program: no `MapCheckoutIdentitySettingsEndpoints`
- Host `Storefront/CheckoutIdentityGate.cs` retained
- Host Admin `*.cs` count: **35**
- Guard: `HostAdminAmcCheckoutIdentityGuardTests`
- Older Admin-count guards (CheckoutAbuse/StoreLanding/StoreMenu) updated to 35

## SoT
- `docs/architecture/tmar-current-state.json` → `hostAdminAmcCheckoutIdentity`
- `docs/evidence/TB-TMAR-HOST-ADMIN-AMC-EMPTY/progress.md` updated
