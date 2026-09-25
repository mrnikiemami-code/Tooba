# TB-TMAR-HOST-ACCESSCONTROL-ADMIN-ROLE-CLONE-ARCHIVE-001 — Admin role clone/archive evacuation

## Scope

Evacuate exactly two Admin platform role routes from `Tooba.Host` into module-owned
CQRS + Endpoints:

- `POST /v1/admin/access-control/roles/{roleId:guid}/clone`
- `DELETE /v1/admin/access-control/roles/{roleId:guid}`

Role permissions and AdminSeller/Seller role families were explicitly out of scope.

## Before / after route ownership

| Route | Before | After |
| --- | --- | --- |
| `POST /v1/admin/access-control/roles/{roleId:guid}/clone` | `Host/AccessControl/AccessControlEndpoints.cs` → `AdminCloneRoleAsync` | `Tooba.AccessControl.Endpoints.Admin.AccessControlAdminEndpoints.CloneRoleAsync` |
| `DELETE /v1/admin/access-control/roles/{roleId:guid}` | `Host/AccessControl/AccessControlEndpoints.cs` → `AdminArchiveRoleAsync` | `Tooba.AccessControl.Endpoints.Admin.AccessControlAdminEndpoints.ArchiveRoleAsync` |

## CQRS files

- `src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Commands/CloneRole/CloneRoleCommand.cs`
  - `CloneRoleCommand : IRequest<AccessRoleDto>`
  - `CloneRoleCommandHandler : IRequestHandler<CloneRoleCommand, AccessRoleDto>`
- `src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Commands/ArchiveRole/ArchiveRoleCommand.cs`
  - `ArchiveRoleCommand : IRequest<Unit>`
  - `ArchiveRoleCommandHandler : IRequestHandler<ArchiveRoleCommand, Unit>`

Both handlers depend only on `IAccessControlDirectory` and construct
`AccessOwnerScope(AccessOwnerScopeKind.Platform, null, tenantId)` — identical to the
previous Host `PlatformScope(tenant)` helper. Commands carry only roleId, trusted
actor id, tenant id, clone body values, and trace id. No `HttpRequest`,
`HttpContext`, `IResult`, `Results.*`, Host, or Endpoints references.

## Validator classification

| Command | Validator | Classification |
| --- | --- | --- |
| `CloneRoleCommand` | none added | **Not required** — the clone body is already authoritatively validated inside `CloneRoleAsync` / `CreateRoleAsync` (`RequireText(name,128)`, `RequireCode(code)`, `EnsureMutable(source)`), and the source role description fallback is applied there |
| `ArchiveRoleCommand` | none added | **Not required** — no body; `roleId` is `guid`-constrained by the route; `EnsureMutable(role)` + `IsSystem` guard live in the directory |

Adding FluentValidation validators would pre-empt stable typed
`AccessControlException` codes (`access.validation.text`, `access.validation.code`,
`access.role.*`) with generic `ValidationException` output, changing the error
contract this task requires preserving. Classification: `validator = N/A (domain-enforced)`.

## Authorization parity

Both routes keep:

1. `IAdminPanelAccess.RequireAuthorizedAsync(request, cancellationToken)`.
2. `AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.manage", authz, tenant, ct)`
   with unchanged branch ordering.

## Trace parity

`X-Request-Id` read by the endpoint-local `Trace(HttpRequest)` helper (identical
implementation to the Host helper) and forwarded as `TraceId` to the directory.

## Response parity

- Clone success: `Results.Json(AccessRoleDto)` — same DTO returned by the directory.
- Archive success: `Results.NoContent()`.
- Capability-gate `PlatformHttpException` is NOT caught (matching the Host behavior
  for these two handlers, which only caught `AccessControlException`), so gate
  failures propagate exactly as before.

## Exception parity

Host catch clause was `catch (Exception ex) when (ex is AccessControlException)`.
The module endpoints reproduce this with `catch (AccessControlException ace)` and
route to `MapAccessError`, which mirrors Host `MapError`:

- `AccessControlException` → `400`, or `403` when `Code` contains `escalation` or
  `ceiling`.
- No `ex.Message` classification, no message parsing, no new generic translation.

`PlatformHttpException` from the capability gate intentionally still propagates
uncaught, preserving the pre-existing asymmetry.

## Host removals

Removed from `src/backend/Host/Tooba.Host/AccessControl/AccessControlEndpoints.cs`:

- `admin.MapPost("/roles/{roleId:guid}/clone", AdminCloneRoleAsync)`
- `admin.MapDelete("/roles/{roleId:guid}", AdminArchiveRoleAsync)`
- `AdminCloneRoleAsync` handler body
- `AdminArchiveRoleAsync` handler body

Shared helpers (`MapError`, `Trace`, `PlatformScope`, `SellerScope`,
`AdminPanelAccess`, `AccessControlCapabilityGate`) remain untouched and are still
used by residual routes.

## Single-route ownership

- `AdminCloneRoleAsync` = ZERO repo-wide
- `AdminArchiveRoleAsync` = ZERO repo-wide
- module `POST /roles/{roleId:guid}/clone` = EXACTLY ONE
- module `DELETE /roles/{roleId:guid}` = EXACTLY ONE
- Host mappings for these two admin routes = ZERO

## Residual host role routes (still in Host)

- `GET /roles/{roleId:guid}/permissions`
- `PUT /roles/{roleId:guid}/permissions`
- `adminSeller` role routes (`/v1/admin/sellers/{sellerId:guid}/access-control/roles*`)
- `seller` role routes (`/v1/seller/access-control/roles*`)

`adminSeller` and `seller` route groups still contain their own clone/archive
handlers (`AdminSellerCloneRoleAsync`, `SellerCloneRoleAsync`, …), which are
separate route groups and out of scope.

## Focused validation results

| Project | Result |
| --- | --- |
| `Tooba.AccessControl.Application` | succeeded, 0 errors, 0 warnings |
| `Tooba.AccessControl.Endpoints` | succeeded, 0 errors, 0 warnings |
| `Tooba.Host` | succeeded, 0 errors (pre-existing warnings only) |

No new focused tests were added; no existing validator/handler tests target these
commands. No solution build, no broad suite, no retries.

## Residual defects

- Clone/Archive do not catch `PlatformHttpException` while Create/Update do —
  pre-existing Host asymmetry, preserved deliberately, not repaired in this task.

## Recovery honesty

AccessControl remains **IN_PROGRESS**. Host `AccessControl` residue is still
non-zero. Role family is NOT complete and NOT structure-certified. Admin platform
base role CRUD is now module-owned, but Role Permissions and AdminSeller/Seller
role families still remain in Host.
