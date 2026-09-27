# W22-STORE-MENU closure

## Outcome
Host Admin StoreMenu* evacuated into Catalog (AMC Migrate = W21 StoreLanding pattern).

## Proof
- Host Admin files deleted: Endpoints, Composer, DevelopmentSeed
- Catalog owns `MapCatalogStoreMenuAdminEndpoints` + `MapCatalogStoreMenuStorefrontEndpoints`
- `IStoreMenuWorkspace` / `StoreMenuWorkspace` registered in CatalogModule
- Program: no `MapStoreMenuEndpoints` / no `StoreMenuComposer` DI
- Host Admin `*.cs` count: **36**
- Guard: `HostAdminAmcStoreMenuGuardTests`
- Focused tests: `StoreMenuComposerT013Tests` + guard (via Workspace factory)

## SoT
- `docs/architecture/tmar-current-state.json` → `hostAdminAmcStoreMenu`
- `docs/evidence/TB-TMAR-HOST-ADMIN-AMC-EMPTY/progress.md` updated
