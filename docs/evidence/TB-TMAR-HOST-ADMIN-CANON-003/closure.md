# TB-TMAR-HOST-ADMIN-CANON-003 — Closure

## Outcome

PASS.

Support and Wallet Host Admin authorizers no longer fail open, no longer use the
`HttpContext.RequestServices` service locator, and delegate the panel gate through the Host
platform seam `IAdminPanelAccess`.

## Changes

### Host (in scope)

- `Admin/HostSupportAdminAuthorizer.cs` — constructor-injected
  `(IAdminPanelAccess, IAuthorizationService, ICurrentTenant)`;
  capability `Allow` → actor; `Deny` → 403 `admin.authorization.denied`;
  `Unavailable` → 503 `support.authorization.unavailable`.
- `Admin/HostWalletAdminAuthorizer.cs` — same shape; `Unavailable` → 503
  `wallet.authorization.unavailable`.
- `Host/Admin` count remains exactly **15**.

### Modules (error catalog ownership + contract docs)

- `Tooba.Support.Application/Errors/SupportErrorCodes.cs` — added
  `AuthorizationUnavailable = "support.authorization.unavailable"`.
- `Tooba.Support.Endpoints/Errors/SupportErrorCatalogContributor.cs` — registered the code as
  `ErrorClassification.Platform`, `503`.
- `Tooba.Support.Endpoints/Admin/ISupportAdminAuthorizer.cs` — contract doc now states
  fail-closed (503) instead of "Preserves Unavailable fail-open compatibility".
- `Tooba.Wallet.Application/Errors/WalletErrorCodes.cs` — added
  `AuthorizationUnavailable = "wallet.authorization.unavailable"`.
- `Tooba.Wallet.Endpoints/Errors/WalletErrorCatalogContributor.cs` — registered the code as
  `ErrorClassification.Platform`, `503`.
- `Tooba.Wallet.Endpoints/Admin/IWalletAdminAuthorizer.cs` — contract doc updated likewise.

### Tests

- Added `Host/Tooba.Host.Tests/Architecture/HostAdminCanon003GuardTests.cs` — structural guard
  plus Allow/Deny/Unavailable/panel-failure behavior tests for both modules.
- `Tooba.Support.Tests/Architecture/SupportArchitectureGuardTests.cs` — host-admin assertions
  moved from the removed direct call to `IAdminPanelAccess` + stable unavailable code + no
  service locator.
- `Tooba.Wallet.Tests/Architecture/WalletArchitectureGuardTests.cs` — same.
- `Tooba.Wallet.Tests/Behavior/WalletCqrsAndHttpContractTests.cs` — contract-doc test now
  asserts fail-closed wording.

## Security posture after change

| Check | Result |
| --- | --- |
| Support fail-open branches | ZERO |
| Wallet fail-open branches | ZERO |
| Support `RequestServices` | ZERO |
| Wallet `RequestServices` | ZERO |
| Support → `IAdminPanelAccess` | YES |
| Wallet → `IAdminPanelAccess` | YES |
| 403 for `Deny` preserved | YES |
| Distinct 503 for `Unavailable` | YES (module-owned codes) |
| 403 code reused for 503 | NO |

## Preserved

Endpoint interfaces and signatures, returned actor IDs, HTTP route ownership, SingleStore
capability context, panel-gate error semantics, `Host/Admin` = 15, CANON-002 guard surface.

## Precise limitation (documented, not broadened)

Marketplace/Development synthetic-tenant capability still evaluates with
`TenantId = "unknown"` because no canonical platform seam provides a marketplace capability
tenant. This matches the pre-task behavior; inventing Marketplace capability semantics was
explicitly out of scope.

## Out of scope untouched

`HostOrderAdminAuthorizer`, `HostOrderAdminEffectiveAccessReader`, `HostPaymentAdminAuthorizer`,
`HostPromotionAdminAuthorizer`, `HostReturnAdminAuthorizer`, `HostSettlementAdminAuthorizer`,
`AdminPanelAccess`, `HostAdminPanelAccess`, `AdminPanelComposer`, foldering, DevActor,
module business logic.

## SoT

`docs/architecture/tmar-current-state.json` → `hostAdminCanon003`.

## Stop

`workflowStop = USER_REVIEW_HOST_ADMIN_CANON_003`. No further work started.
