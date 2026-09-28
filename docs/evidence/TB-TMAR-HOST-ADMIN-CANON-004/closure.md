# TB-TMAR-HOST-ADMIN-CANON-004 — Closure

## Outcome

PASS.

`HostOrderAdminAuthorizer` is now a thin Host platform adapter: panel gate through
`IAdminPanelAccess`, permission evaluation through the neutral authorization abstraction, fail-closed
503 on authorization unavailability, and preserved 403 explicit denial. All `AccessControl`
Application/Domain and directory coupling is gone from the file.

## Changes

### Host (in scope)

- `Admin/HostOrderAdminAuthorizer.cs` — rewritten:
  - constructor `(IAdminPanelAccess adminAccess, IAuthorizationService authz, ICurrentTenant tenant)`;
  - `RequireAdminAsync` → `adminAccess.RequireAuthorizedAsync(context.Request, ct)`;
  - `RequirePermissionAsync` → panel actor, then
    `AuthorizationCheck { Subject = ForUser(actor), Resource = Permission/{permissionId},
    Permission = Check, CallContext = { Edition = SingleStore, TenantId = tenant.Current?.TenantId.Value ?? "unknown" } }`;
  - `Allow` → actor; `Deny` → 403 `order.operation.denied`; `Unavailable` → 503
    `order.authorization.unavailable`;
  - removed imports `Tooba.AccessControl.Application`, `Tooba.AccessControl.Domain`,
    `Tooba.AccessControl.Application.Models`, `Tooba.AccessControl.Application.Permissions`;
  - removed `CurrentAuthenticatedSession` / `IAuthorizationGuard` / `IHostEnvironment` /
    `IAccessControlDirectory` dependencies and all direct `AdminPanelAccess.RequireAuthorizedAsync`
    calls.
- `Host/Admin` count remains exactly **15**.

### Modules (error-code ownership + localization)

- `Tooba.Order.Endpoints/Errors/OrderErrorCodes.cs` — **new**; constant-owns
  `OperationDenied = "order.operation.denied"` (existing canonical denial code, no behavior change)
  and `AuthorizationUnavailable = "order.authorization.unavailable"`.
- `Tooba.Order.Endpoints/Errors/OrderErrorCatalogContributor.cs` — registered
  `OrderErrorCodes.AuthorizationUnavailable` as `ErrorClassification.Platform`,
  `503 ServiceUnavailable`.
- `Tooba.Order.Endpoints/Resources/OrderErrors.resx` — added `order.authorization.unavailable`
  (`Order authorization service is unavailable.`).
- `Tooba.Order.Endpoints/Resources/OrderErrors.fa.resx` — added the Persian value
  (`سرویس مجوز سفارش در دسترس نیست.`).

### Tests

- Added `Host/Tooba.Host.Tests/Architecture/HostAdminCanon004GuardTests.cs` — structural guard plus
  Allow / Deny / Unavailable / `RequireAdminAsync` / panel-failure / blank-permission behavior tests.
- `Tooba.Order.Tests/Endpoints/OrderEndpointPresentationTests.cs` — `AllCodes` now includes the new
  Order-owned admin-panel code, keeping the explicit-descriptor + en/fa resource invariants.

## Security posture after change

| Check | Result |
| --- | --- |
| Order authorizer fail-open branches | ZERO |
| Order authorizer `RequestServices` service locator | ZERO |
| Order authorizer `AdminPanelAccess.RequireAuthorizedAsync` | ZERO |
| Order authorizer `Tooba.AccessControl.Application` | ZERO |
| Order authorizer `Tooba.AccessControl.Domain` | ZERO |
| Order authorizer `IAccessControlDirectory` | ZERO |
| Panel gate via `IAdminPanelAccess` | YES |
| Permission gate via neutral `IAuthorizationService` | YES |
| 403 for `Deny` preserved (`order.operation.denied`) | YES |
| Distinct 503 for `Unavailable` (Order-owned) | YES |
| 403 code reused for 503 | NO |

## Preserved

`IOrderAdminAuthorizer` interface and signatures, returned actor IDs/`Guid` contracts, requested
`permissionId` passthrough, SingleStore edition + tenant `CallContext`, admin route ownership,
`Program.cs` registration, `Host/Admin` = 15, CANON-002/CANON-003 guard surfaces.

## Precise limitation (documented, not broadened)

The capability `CallContext` still reports `TenantId = "unknown"` for a Development/Marketplace
synthetic-tenant admin, because no canonical platform seam exposes a Marketplace capability tenant.
This matches the pre-task behavior (including the CANON-003 Support/Wallet path); inventing
Marketplace capability semantics was explicitly out of scope.

## Out of scope untouched

`HostOrderAdminEffectiveAccessReader.cs`, `HostSupportAdminAuthorizer.cs`,
`HostWalletAdminAuthorizer.cs`, `HostPaymentAdminAuthorizer.cs`, `HostPromotionAdminAuthorizer.cs`,
`HostReturnAdminAuthorizer.cs`, `HostSettlementAdminAuthorizer.cs`, `AdminPanelAccess.cs`,
`HostAdminPanelAccess.cs`, `AdminPanelComposer.cs`, foldering, DevActor cleanup, Order business
logic, AccessControl module structure, frontend/schema.

## SoT

`docs/architecture/tmar-current-state.json` → `hostAdminCanon004`.

## Stop

`workflowStop = USER_REVIEW_HOST_ADMIN_CANON_004`. No further work started.
