# TB-TMAR-HOST-ACCESSCONTROL-ADMINSELLER-ROLE-FAMILY-001 — AdminSeller role family evacuation

## Scope

Evacuate the complete AdminSeller role + role-permissions family from `Tooba.Host`:

1. `GET /v1/admin/sellers/{sellerId:guid}/access-control/roles`
2. `POST /v1/admin/sellers/{sellerId:guid}/access-control/roles`
3. `PUT /v1/admin/sellers/{sellerId:guid}/access-control/roles/{roleId:guid}`
4. `POST /v1/admin/sellers/{sellerId:guid}/access-control/roles/{roleId:guid}/clone`
5. `DELETE /v1/admin/sellers/{sellerId:guid}/access-control/roles/{roleId:guid}`
6. `GET /v1/admin/sellers/{sellerId:guid}/access-control/roles/{roleId:guid}/permissions`
7. `PUT /v1/admin/sellers/{sellerId:guid}/access-control/roles/{roleId:guid}/permissions`

AdminSeller ceiling/assignments/effective, the whole Seller role family, and the
Admin platform assignments/users/effective families were out of scope.

## Before / after route ownership

| # | Route (module group `/v1/admin/sellers/{sellerId:guid}/access-control`) | Before | After |
| --- | --- | --- | --- |
| 1 | `GET /roles` | `AdminSellerListRolesAsync` | `AccessControlAdminSellerEndpoints.ListRolesAsync` |
| 2 | `POST /roles` | `AdminSellerCreateRoleAsync` | `AccessControlAdminSellerEndpoints.CreateRoleAsync` |
| 3 | `PUT /roles/{roleId:guid}` | `AdminSellerUpdateRoleAsync` | `AccessControlAdminSellerEndpoints.UpdateRoleAsync` |
| 4 | `POST /roles/{roleId:guid}/clone` | `AdminSellerCloneRoleAsync` | `AccessControlAdminSellerEndpoints.CloneRoleAsync` |
| 5 | `DELETE /roles/{roleId:guid}` | `AdminSellerArchiveRoleAsync` | `AccessControlAdminSellerEndpoints.ArchiveRoleAsync` |
| 6 | `GET /roles/{roleId:guid}/permissions` | `AdminSellerGetPermsAsync` | `AccessControlAdminSellerEndpoints.GetRolePermissionsAsync` |
| 7 | `PUT /roles/{roleId:guid}/permissions` | `AdminSellerSetPermsAsync` | `AccessControlAdminSellerEndpoints.SetRolePermissionsAsync` |

## CQRS reuse vs generalization

The accepted Role CQRS was **generalized**, not duplicated. Generalized requests:

- `CreateRoleCommand` — now carries `OwnerScopeKind`, `OwnerScopeId`, `ActorUserId`, `TenantId`, body values, `TraceId`
- `UpdateRoleCommand` — now carries `RoleId`, `OwnerScopeKind`, `OwnerScopeId`, `ActorUserId`, `TenantId`, body values, `TraceId`
- `CloneRoleCommand` — now carries `RoleId`, `OwnerScopeKind`, `OwnerScopeId`, `ActorUserId`, `TenantId`, body values, `TraceId`
- `ArchiveRoleCommand` — now carries `RoleId`, `OwnerScopeKind`, `OwnerScopeId`, `ActorUserId`, `TenantId`, `TraceId`
- `GetRolePermissionsQuery` — now carries `RoleId`, `OwnerScopeKind`, `OwnerScopeId`, `TenantId`
- `SetRolePermissionsCommand` — now carries `RoleId`, `OwnerScopeKind`, `OwnerScopeId`, `ActorUserId`, `TenantId`, `Grants`, `TraceId`

Each handler constructs `new AccessOwnerScope(request.OwnerScopeKind, request.OwnerScopeId, request.TenantId)`.
`ListRolesQuery` and `GetRoleQuery` already supported owner kind/id and were left unchanged.

All handlers still depend only on `IAccessControlDirectory`. No `HttpRequest`,
`HttpContext`, `IResult`, `Results.*`, Host, or Endpoints references.

New endpoint surface: `src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Admin/AccessControlAdminSellerEndpoints.cs`,
registered once in `AccessControlEndpointModule` via
`app.MapGroup("/v1/admin/sellers/{sellerId:guid}/access-control")`.

## Admin platform regression / parity statement

The accepted Admin platform routes were NOT semantically modified; only the request
construction was updated to pass the owner scope explicitly:

- `GET /roles`, `GET /roles/{roleId:guid}` — unchanged (`ListRolesQuery`/`GetRoleQuery` already passed `Platform`/`null`)
- `POST /roles`, `PUT /roles/{roleId}`, `POST /roles/{roleId}/clone`, `DELETE /roles/{roleId}` — now pass `AccessOwnerScopeKind.Platform, null` explicitly
- `GET/PUT /roles/{roleId}/permissions` — now pass `AccessOwnerScopeKind.Platform, null` explicitly

Verified: every admin-platform endpoint call site passes `AccessOwnerScopeKind.Platform` + `null` owner scope id (9 occurrences in `AccessControlAdminEndpoints.cs`), so behaviour is byte-identical to the previously hardcoded Platform owner.

## Owner-scope parity

| Family | Host `SellerScope(sellerId, tenant)` | Module |
| --- | --- | --- |
| AdminSeller | `new(AccessOwnerScopeKind.Seller, sellerId, tenant.Current?.TenantId.Value)` | `new AccessOwnerScope(AccessOwnerScopeKind.Seller, sellerId, tenant.Current?.TenantId.Value)` |

Identical. Admin platform remains `Platform` + `null`.

## Authorization / capability parity

| Route | Capability |
| --- | --- |
| GET list roles | `accesscontrol.view` |
| POST create | `accesscontrol.manage` |
| PUT update | `accesscontrol.manage` |
| POST clone | `accesscontrol.manage` |
| DELETE archive | `accesscontrol.manage` |
| GET permissions | `accesscontrol.view` |
| PUT permissions | `accesscontrol.manage` |

All after `IAdminPanelAccess.RequireAuthorizedAsync(request, cancellationToken)`.
Branch ordering of `AccessControlCapabilityGate.EnsureAsync` unchanged.

## Trace parity

`X-Request-Id` is read by the endpoint-local `Trace(HttpRequest)` helper and
forwarded as `TraceId` for create/update/clone/archive/set-permissions — matching
Host. GET list and GET permissions carry no trace, matching Host.

## Response / error parity

- List → `Results.Json(IReadOnlyList<AccessRoleDto>)`
- Create/Update/Clone → `Results.Json(AccessRoleDto)`
- Archive → `Results.NoContent()`
- Get permissions → `Results.Json(IReadOnlyList<RolePermissionGrant>)`
- Set permissions → `Results.NoContent()`
- Error: Host caught `AccessControlException` only for all seven; the module
  endpoints reproduce this with `catch (AccessControlException ace)` →
  `MapAccessError` (400, or 403 when `Code` contains `escalation`/`ceiling`).
- `PlatformHttpException` from the capability gate still propagates uncaught,
  matching Host. No `ex.Message` classification, no message parsing, no new generic
  translation, and the unrelated PlatformHttpException asymmetry was NOT normalized.

## Validator classification

No validators added or modified. Classification: `NO_VALIDATOR_REQUIRED` for all
seven. Generalizing the requests introduced no new invalid transport state — the
owner scope values come from trusted server-side route/tenant data, and all
user-controlled body validation remains authoritatively enforced in
`IAccessControlDirectory` (`RequireText`, `RequireCode`, `ValidateOwner`,
`EnsureMutable`, `ValidateGrantsForOwnerAsync`, `PermissionCatalog.Require`) with
stable typed `AccessControlException` codes. Adding FluentValidation would replace
those codes with generic `ValidationException`.

## Host removals

Removed from `src/backend/Host/Tooba.Host/AccessControl/AccessControlEndpoints.cs`:

- the seven `adminSeller.Map*` route registrations for the role/permission family
- the seven handler bodies (`AdminSellerListRolesAsync`, `AdminSellerCreateRoleAsync`,
  `AdminSellerUpdateRoleAsync`, `AdminSellerCloneRoleAsync`, `AdminSellerArchiveRoleAsync`,
  `AdminSellerGetPermsAsync`, `AdminSellerSetPermsAsync`)

Retained untouched in the same `adminSeller` group: `/ceiling` (GET/PUT, both
`AdminGetCeilingAsync`/`AdminSetCeilingAsync`), `/assignments` (list/create/delete),
`/users/{userId:guid}/effective`. Shared helpers (`MapError`, `Trace`,
`PlatformScope`, `SellerScope`) remain and are still used by residual routes.

## Single ownership proof

- All seven old Host handler names = ZERO repo-wide (only historical docs/evidence mention them)
- Module `adminSeller` group mappings for these seven routes = EXACTLY ONE each
- Host mappings for these seven routes = ZERO
- `/v1/admin/sellers/{sellerId:guid}/access-control` group is mapped EXACTLY ONCE (module root)
- Endpoints direct `IAccessControlDirectory` use for these routes = ZERO (all via `ISender`)

## Residual Host AccessControl families

- Seller role family: `/v1/seller/access-control/roles*` (list/create/get/update/clone/archive/get perms/set perms)
- Seller ceiling, assignments, users/effective
- Admin platform assignments, users, effective, demo-preview, scope resources
- AdminSeller ceiling, assignments, users/effective

## Focused validation results

| Project | Result |
| --- | --- |
| `Tooba.AccessControl.Application` | succeeded, 0 errors |
| `Tooba.AccessControl.Endpoints` | succeeded, 0 errors |
| `Tooba.Host` | succeeded, 0 errors (pre-existing warnings only) |

No solution build, no broad suite, no retries.

## Residual defects

- AdminSeller role routes do not catch `PlatformHttpException` — pre-existing Host
  behaviour for this family, preserved deliberately. Admin platform create/update
  route asymmetry also left untouched.

## Recovery honesty

AccessControl remains **IN_PROGRESS**. Host `AccessControl` residue is still
non-zero. After this task: Admin platform Role family = module-owned; AdminSeller
Role family = module-owned; Seller Role family still remains in Host. Not
COMPLETE_REFERENCE_PATTERN and not structure-certified.
