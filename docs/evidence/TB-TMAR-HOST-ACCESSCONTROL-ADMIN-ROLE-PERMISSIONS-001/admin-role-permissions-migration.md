# TB-TMAR-HOST-ACCESSCONTROL-ADMIN-ROLE-PERMISSIONS-001 — Admin role-permissions evacuation

## Scope

Evacuate exactly two Admin platform role-permissions routes from `Tooba.Host` into
module-owned CQRS + Endpoints:

- `GET /v1/admin/access-control/roles/{roleId:guid}/permissions`
- `PUT /v1/admin/access-control/roles/{roleId:guid}/permissions`

AdminSeller and Seller role/permission families were explicitly out of scope.

## Before / after ownership

| Route | Before | After |
| --- | --- | --- |
| `GET /v1/admin/access-control/roles/{roleId:guid}/permissions` | `Host/AccessControl/AccessControlEndpoints.cs` → `AdminGetRolePermissionsAsync` | `Tooba.AccessControl.Endpoints.Admin.AccessControlAdminEndpoints.GetRolePermissionsAsync` |
| `PUT /v1/admin/access-control/roles/{roleId:guid}/permissions` | `Host/AccessControl/AccessControlEndpoints.cs` → `AdminSetRolePermissionsAsync` | `Tooba.AccessControl.Endpoints.Admin.AccessControlAdminEndpoints.SetRolePermissionsAsync` |

## CQRS files

- `src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Queries/GetRolePermissions/GetRolePermissionsQuery.cs`
  - `GetRolePermissionsQuery : IRequest<IReadOnlyList<RolePermissionGrant>>`
  - `GetRolePermissionsQueryHandler : IRequestHandler<GetRolePermissionsQuery, IReadOnlyList<RolePermissionGrant>>`
- `src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Commands/SetRolePermissions/SetRolePermissionsCommand.cs`
  - `SetRolePermissionsCommand : IRequest<Unit>`
  - `SetRolePermissionsCommandHandler : IRequestHandler<SetRolePermissionsCommand, Unit>`

Both handlers depend only on `IAccessControlDirectory` and construct
`AccessOwnerScope(AccessOwnerScopeKind.Platform, null, tenantId)` — identical to
Host `PlatformScope(tenant)`. Requests carry only roleId, actor id, tenant id,
grants, and trace id. No `HttpRequest`, `HttpContext`, `IResult`, `Results.*`,
Host, or Endpoints references.

## Validator classification

| Route | Validator | Classification |
| --- | --- | --- |
| GET role permissions | none added | **Not required** — `roleId` is `guid`-constrained by the route; the directory's `RequireRoleAsync(roleId, owner, ct)` is the authoritative invalid-state check |
| PUT role permissions (grants) | none added | **DOMAIN/APPLICATION-ENFORCED** — `SetRolePermissionsAsync` already enforces `RequireRoleAsync` (`access.role.not_found`), `EnsureMutable(role)` (`access.role.immutable`), `ValidateGrantsForOwnerAsync` (ceiling/escalation), and `PermissionCatalog.Require(grant.PermissionId)` (`access.permission.unknown`) |

Adding FluentValidation for these routes would pre-empt stable typed
`AccessControlException` codes with generic `ValidationException` output, changing
the error semantics this task requires preserving. No transport-invalid state was
found that is not already authoritatively enforced downstream, so no validators
were added. Classification: `validator = N/A (domain/application-enforced)`.

`Guid.Empty` is NOT an established invalid-state invariant elsewhere in the
existing behavior (an empty id simply fails the directory lookup), so no
ceremonial validator was introduced for it either.

## Authorization parity

- GET: `IAdminPanelAccess.RequireAuthorizedAsync` → `AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", authz, tenant, ct)`.
- PUT: `IAdminPanelAccess.RequireAuthorizedAsync` → `AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.manage", authz, tenant, ct)`.

Branch ordering unchanged.

## Trace parity (PUT)

`X-Request-Id` read by the endpoint-local `Trace(HttpRequest)` helper (identical to
the Host helper) and forwarded as `TraceId` into `SetRolePermissionsCommand` and on
to the directory. GET carries no trace, matching Host.

## Response / error parity

- GET success: `Results.Json(IReadOnlyList<RolePermissionGrant>)` — same type and
  ordering (`OrderBy(PermissionId)`) as the Host route.
- PUT success: `Results.NoContent()`.
- Error: Host caught only `AccessControlException`; the module endpoints reproduce
  this with `catch (AccessControlException ace)` → `MapAccessError`, mirroring Host
  `MapError` (400, or 403 when `Code` contains `escalation`/`ceiling`).
- `PlatformHttpException` from the capability gate still propagates uncaught,
  matching pre-existing Host behavior. Asymmetries deliberately NOT repaired.
- No `ex.Message` classification, no message parsing, no new generic translation.

## Host removals

Removed from `src/backend/Host/Tooba.Host/AccessControl/AccessControlEndpoints.cs`:

- `admin.MapGet("/roles/{roleId:guid}/permissions", AdminGetRolePermissionsAsync)`
- `admin.MapPut("/roles/{roleId:guid}/permissions", AdminSetRolePermissionsAsync)`
- `AdminGetRolePermissionsAsync` handler body
- `AdminSetRolePermissionsAsync` handler body

Shared helpers (`MapError`, `Trace`, `PlatformScope`, `SellerScope`,
`AdminPanelAccess`, `AccessControlCapabilityGate`) remain untouched and are still
used by residual routes.

## Single-route ownership proof

- `AdminGetRolePermissionsAsync` = ZERO repo-wide
- `AdminSetRolePermissionsAsync` = ZERO repo-wide
- module `GET /roles/{roleId:guid}/permissions` in admin group = EXACTLY ONE
- module `PUT /roles/{roleId:guid}/permissions` in admin group = EXACTLY ONE
- Host admin mappings for these two routes = ZERO
- Endpoints direct `IAccessControlDirectory` use for these routes = ZERO (all via `ISender`)

## Residual Host role families

- `adminSeller` route group: `AdminSellerGetPermsAsync` / `AdminSellerSetPermsAsync`
  plus all other AdminSeller role routes
- `seller` route group: `SellerGetPermsAsync` / `SellerSetPermsAsync` plus all other
  Seller role routes

Also still in Host for the admin group (non-role families): assignments, users,
effective access, demo-preview, scope resources.

## Focused validation results

| Project | Result |
| --- | --- |
| `Tooba.AccessControl.Application` | succeeded, 0 errors, 0 warnings |
| `Tooba.AccessControl.Endpoints` | succeeded, 0 errors, 0 warnings |
| `Tooba.Host` | succeeded, 0 errors (pre-existing warnings only) |

No new focused tests were added; no existing tests target these requests/handlers.
No solution build, no broad suite, no retries.

## Residual defects

- GET/PUT role-permissions do not catch `PlatformHttpException` while the create/
  update role routes do — pre-existing Host asymmetry, preserved deliberately.

## Recovery honesty

AccessControl remains **IN_PROGRESS**. Host `AccessControl` residue is still
non-zero. Admin platform base role CRUD + role permissions are now module-owned,
but AdminSeller/Seller role and permission families still remain in Host. Not
COMPLETE_REFERENCE_PATTERN and not structure-certified.
