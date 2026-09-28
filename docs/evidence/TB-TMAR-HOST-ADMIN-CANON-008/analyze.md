# TB-TMAR-HOST-ADMIN-CANON-008 — Analyze

## Scope (single concern)

Physical structure + namespace alignment of the remaining 15 Host/Admin platform files.
No ownership migration, no behavior change, no policy change, no new seams/contracts, no module work.

## Pre-state audit (disk, before change)

`src/backend/Host/Tooba.Host/Admin/*.cs` — 15 files, all **flat** in the `Admin` root,
all declaring `namespace Tooba.Host.Admin;`:

```
Admin/AdminDevActorBootstrap.cs
Admin/AdminGridQueryEndpoint.cs
Admin/AdminPanelAccess.cs
Admin/AdminPanelComposer.cs
Admin/AdminPanelEndpoints.cs
Admin/AdminPanelModels.cs
Admin/HostAdminPanelAccess.cs
Admin/HostOrderAdminAuthorizer.cs
Admin/HostOrderAdminEffectiveAccessReader.cs
Admin/HostPaymentAdminAuthorizer.cs
Admin/HostPromotionAdminAuthorizer.cs
Admin/HostReturnAdminAuthorizer.cs
Admin/HostSettlementAdminAuthorizer.cs
Admin/HostSupportAdminAuthorizer.cs
Admin/HostWalletAdminAuthorizer.cs
```

The set exactly matches the task's `CURRENT FILE SET` (15/15). No `INCOMPLETE` stop required.

## Target capability mapping

| File | Capability | Target |
|---|---|---|
| AdminPanelAccess.cs | panel access gate | Access |
| HostAdminPanelAccess.cs | `IAdminPanelAccess` adapter | Access |
| HostOrderAdminAuthorizer.cs | Order admin authorizer | Access/Authorizers |
| HostOrderAdminEffectiveAccessReader.cs | Order effective access (CANON-005/006 seam) | Access/Authorizers |
| HostPaymentAdminAuthorizer.cs | Payment admin authorizer | Access/Authorizers |
| HostPromotionAdminAuthorizer.cs | Promotion admin authorizer | Access/Authorizers |
| HostReturnAdminAuthorizer.cs | Returns admin authorizer | Access/Authorizers |
| HostSettlementAdminAuthorizer.cs | Settlement admin authorizer | Access/Authorizers |
| HostSupportAdminAuthorizer.cs | Support admin authorizer | Access/Authorizers |
| HostWalletAdminAuthorizer.cs | Wallet admin authorizer | Access/Authorizers |
| AdminPanelComposer.cs | admin dashboard/seller BFF composer | Panel |
| AdminPanelEndpoints.cs | admin panel HTTP surface | Panel |
| AdminPanelModels.cs | admin panel read models | Panel |
| AdminGridQueryEndpoint.cs | shared server-side grid endpoint helper | Grid |
| AdminDevActorBootstrap.cs | Development-only admin actor bootstrap | Development |

## Namespace targets

```
Tooba.Host.Admin.Access
Tooba.Host.Admin.Access.Authorizers
Tooba.Host.Admin.Panel
Tooba.Host.Admin.Grid
Tooba.Host.Admin.Development
```

## Consumers that require reference repair (compile-only)

Production/consumers updated to the specific new namespaces (no `global using`, no shim):

- `Program.cs` — usings + registration FQNs.
- `Reviews/ReviewEndpoints.cs` — `AdminPanelAccess` (Access) + `AdminGridQueryEndpoint` (Grid).
- `Media`, `Localization`, `Preferences (x2)`, `OperatorProfile`, `PageComposition`,
  `Story/StoryEndpoints`, `Development/MarketplaceAdminDevBootstrap` — `AdminPanelAccess` (Access).
- `Wallet/WalletDevelopmentSeedHost`, `Support/SupportDevelopmentSeedHost`,
  `Settings/SettingsFoundationDevelopmentSeed`, `Development/ProductWorkspaceDevelopmentBootstrap`
  — `AdminDevActorBootstrap` (Development).
- `Grid/AdminListGridPolicies`, `Grid/AdminSellersGridQueryEngine` — panel read models (Panel).

Removed no-longer-required `using Tooba.Host.Admin;` from files whose Admin symbols moved out of that
root (`Story/StoryPanelComposer`, `PageComposition/PageCompositionPanelComposer`,
`CatalogAdapters/StoreLandingMerchandisingAdapter`, and the Order-domain-only test files).

## Deliberately untouched

- All 15 type names, visibility, DI lifetimes, routes, error codes/statuses.
- `AdminPanelComposer` behavior, admin grid behavior, DevActor behavior.
- CANON-001..007 seams and Contracts.
- Every `Modules/**` and `src/frontend/**` file (no module edit was required).
