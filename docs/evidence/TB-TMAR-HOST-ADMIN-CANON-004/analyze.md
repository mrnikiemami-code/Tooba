# TB-TMAR-HOST-ADMIN-CANON-004 — Analyze

## Goal

Repair ONLY `src/backend/Host/Tooba.Host/Admin/HostOrderAdminAuthorizer.cs` so the Host Order admin
endpoint authorizer delegates the panel gate through `IAdminPanelAccess` and evaluates the requested
permission through the neutral BuildingBlocks authorization abstraction — with no
`AccessControl` Application/Domain coupling, no direct `AdminPanelAccess` static call, and no
fail-open on authorization unavailability.

## Defect (pre-state)

`HostOrderAdminAuthorizer`:

1. constructor-injected `CurrentAuthenticatedSession`, `ICurrentTenant`, `IAuthorizationGuard`,
   `IHostEnvironment`, `IAccessControlDirectory` (AccessControl coupling);
2. called the static `AdminPanelAccess.RequireAuthorizedAsync(...)` directly for both
   `RequireAdminAsync` and `RequirePermissionAsync`;
3. built `AccessOwnerScope(AccessOwnerScopeKind.Platform, null, tenant)` and read
   `IAccessControlDirectory.GetEffectiveAccessAsync(...)` (`AccessControl.Application.Models`);
4. denied with a hard-coded literal `"order.operation.denied"` on any ungranted permission, with no
   distinction for authorization-engine unavailability.

Pre-state imports (defect evidence):

```csharp
using Tooba.AccessControl.Application;
using Tooba.AccessControl.Domain;
using Tooba.AccessControl.Application.Models;
using Tooba.AccessControl.Application.Permissions;
```

## Mandatory audit results

- **`IOrderAdminAuthorizer`** (`Tooba.Order.Endpoints/OrderEndpointModule.cs`) declares
  `RequirePermissionAsync(HttpContext, string permissionId, CancellationToken) : Task<Guid>` and
  `RequireAdminAsync(HttpContext, CancellationToken) : Task<Guid>`. Both are preserved verbatim.
- **Order error catalog**: `OrderErrorCatalogContributor` owned the codes for the admin
  completeness surface, but neither `order.operation.denied` nor any Order 503 authorization code
  was registered/constant-owned. `seller.authorization.unavailable` already existed as an
  Order-catalog-registered code — the precedent for a module-owned 503 authorization descriptor.
- **Order permission IDs / resource convention**: admin routes pass `order.view`, `order.handle`
  (`AdminOrderCompletenessEndpoints`), and the ops family grants are read through
  `IOrderAdminEffectiveAccessReader`. The neutral ReBAC convention for a permission check
  (`AuthorizationObjectTypes.Permission` + `AuthorizationRelations.Check`) is exactly the one used
  by `AccessControlCapabilityGate` and by CANON-003 `HostSupportAdminAuthorizer`.
- **Existing admin order permission tests**: `AdminOrderOperationsTests` /
  `AdminOrderFulfillmentOperationsTests` assert the *stable code* `order.operation.denied` from the
  Order operations orchestrator (module-internal gate), not from the Host authorizer — so the Host
  adapter's denial code must stay `order.operation.denied` to remain consistent.
- **Neutral authorization examples in Host**: `HostSupportAdminAuthorizer` / `HostWalletAdminAuthorizer`
  (CANON-003) are the canonical constructor-injected `(IAdminPanelAccess, IAuthorizationService,
  ICurrentTenant)` shape with a fail-closed `Unavailable` branch. `AccessControlCapabilityGate`
  shows the identical neutral check construction.

## Target shape

```csharp
internal sealed class HostOrderAdminAuthorizer(
    IAdminPanelAccess adminAccess,
    IAuthorizationService authz,
    ICurrentTenant tenant) : IOrderAdminAuthorizer
```

| Decision kind | Result |
| --- | --- |
| `Allow` | return the actor (endpoint contract preserved) |
| `Deny` | 403 + `order.operation.denied` (existing canonical code preserved) |
| `Unavailable` | 503 + `order.authorization.unavailable` (new, Order-owned, fail-closed) |

`RequireAdminAsync` delegates directly to `adminAccess.RequireAuthorizedAsync(context.Request, ct)`
without any capability check (matching the pre-state panel-boundary-only semantics).

## Error-code ownership decision

`order.operation.denied` was already the canonical explicit-denial code consumed by the Order
operations orchestrator and the frontend (`admin-error-map.ts`, `admin-order-operations.ts`,
`admin-order-completeness.ts`). It is now constant-owned in the Order endpoints layer
(`OrderErrorCodes.OperationDenied`) so the Host adapter references a stable module-owned contract
instead of a literal.

No stable 503 authorization code existed for Order. The smallest correct addition is a code owned by
the Order module (which the Host adapter consumes), registered as `ErrorClassification.Platform`,
`503`, in `OrderErrorCatalogContributor` and localized in both `OrderErrors.resx` and
`OrderErrors.fa.resx`. No 403 code was reused for 503.

## Scope / out-of-scope

- In scope: `Host/Admin/HostOrderAdminAuthorizer.cs`, the Order-owned error code + catalog + resx
  entries it needs, and the durable guard.
- Untouched: `HostOrderAdminEffectiveAccessReader.cs` (still legitimately AccessControl-backed),
  every other authorizer, `AdminPanelAccess`, `HostAdminPanelAccess`, `AdminPanelComposer`,
  foldering, DevActor, Order business logic, AccessControl module structure, frontend/schema.
- `Host/Admin` file count before = 15, after = 15.
