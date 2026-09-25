# TB-TMAR-HOST-ACCESSCONTROL-SELLER-ROLE-FAMILY-001 — Seller role family evacuation

## Scope

Evacuate the complete Seller role + role-permissions family from `Tooba.Host`
(group `/v1/seller/access-control`):

1. `GET /roles`
2. `POST /roles`
3. `GET /roles/{roleId:guid}`
4. `PUT /roles/{roleId:guid}`
5. `POST /roles/{roleId:guid}/clone`
6. `DELETE /roles/{roleId:guid}`
7. `GET /roles/{roleId:guid}/permissions`
8. `PUT /roles/{roleId:guid}/permissions`

Seller ceiling/assignments/users/effective/scope-resources and all Admin/AdminSeller
families were out of scope.

## Before / after route ownership

| # | Route (module group `/v1/seller/access-control`) | Before | After |
| --- | --- | --- | --- |
| 1 | `GET /roles` | `SellerListRolesAsync` | `AccessControlSellerEndpoints.ListRolesAsync` |
| 2 | `POST /roles` | `SellerCreateRoleAsync` | `AccessControlSellerEndpoints.CreateRoleAsync` |
| 3 | `GET /roles/{roleId:guid}` | `SellerGetRoleAsync` | `AccessControlSellerEndpoints.GetRoleAsync` |
| 4 | `PUT /roles/{roleId:guid}` | `SellerUpdateRoleAsync` | `AccessControlSellerEndpoints.UpdateRoleAsync` |
| 5 | `POST /roles/{roleId:guid}/clone` | `SellerCloneRoleAsync` | `AccessControlSellerEndpoints.CloneRoleAsync` |
| 6 | `DELETE /roles/{roleId:guid}` | `SellerArchiveRoleAsync` | `AccessControlSellerEndpoints.ArchiveRoleAsync` |
| 7 | `GET /roles/{roleId:guid}/permissions` | `SellerGetRolePermissionsAsync` | `AccessControlSellerEndpoints.GetRolePermissionsAsync` |
| 8 | `PUT /roles/{roleId:guid}/permissions` | `SellerSetRolePermissionsAsync` | `AccessControlSellerEndpoints.SetRolePermissionsAsync` |

The existing `/v1/seller/access-control` module group is reused; no second root
group was added. `AccessControlSellerEndpoints` already owned that group and now
also owns the eight role/permission routes.

## CQRS reuse statement

**Pure reuse — no new commands/queries and no generalization.** The requests
generalized in the parent task already accept `AccessOwnerScopeKind`,
`OwnerScopeId`, and `TenantId`, so Seller endpoints pass
`AccessOwnerScopeKind.Seller` + authorized `sellerId`:

- `ListRolesQuery`, `GetRoleQuery`, `CreateRoleCommand`, `UpdateRoleCommand`,
  `CloneRoleCommand`, `ArchiveRoleCommand`, `GetRolePermissionsQuery`,
  `SetRolePermissionsCommand`

No Seller-specific duplicates were created. No generic dispatcher. Handlers remain
the accepted ones; `IAccessControlDirectory` was not moved into Endpoints.

## Seller auth parity

Replaced Host `SellerPanelAccess.RequireAuthorizedAsync(request, session, guard, env, ct)`
(through the `RequireSellerAsync` local wrapper) with the module seam
`ISellerPanelAccess.RequireAuthorizedAsync(request, cancellationToken)`, which
returns `(actor, sellerId)`.

Capability gates:

| Route | Capability |
| --- | --- |
| `GET /roles` | `accesscontrol.view` |
| `POST /roles` | `accesscontrol.manage` |
| `GET /roles/{roleId}` | `accesscontrol.view` |
| `PUT /roles/{roleId}` | `accesscontrol.manage` |
| `POST /roles/{roleId}/clone` | `accesscontrol.manage` |
| `DELETE /roles/{roleId}` | `accesscontrol.manage` |
| `GET /roles/{roleId}/permissions` | `accesscontrol.view` |
| `PUT /roles/{roleId}/permissions` | `accesscontrol.manage` |

`RequireSellerAsync` and `SellerPanelAccess` were NOT reintroduced into the module.

## Owner-scope parity

| Family | Host | Module |
| --- | --- | --- |
| Seller | `SellerScope(sellerId, tenant)` = `new(AccessOwnerScopeKind.Seller, sellerId, tenant.Current?.TenantId.Value)` | `new AccessOwnerScope(AccessOwnerScopeKind.Seller, sellerId, tenant.Current?.TenantId.Value)` |
| AdminSeller | `SellerScope(route sellerId, tenant)` | `AccessOwnerScopeKind.Seller` + route `sellerId` |
| Admin platform | `PlatformScope(tenant)` | `AccessOwnerScopeKind.Platform` + `null` |

Verified by inspection: 9 `AccessOwnerScopeKind.Platform` sites in
`AccessControlAdminEndpoints.cs`, 7 `Seller` sites in
`AccessControlAdminSellerEndpoints.cs`, 9 `Seller` sites in
`AccessControlSellerEndpoints.cs`.

## Trace parity

`X-Request-Id` read by the endpoint-local `Trace(HttpRequest)` helper and forwarded
as `TraceId` for create/update/clone/archive/set-permissions. `GET /roles`,
`GET /roles/{roleId}` and `GET .../permissions` carry no trace, matching Host.

## Response / error parity

- `GET /roles` → `Results.Json(IReadOnlyList<AccessRoleDto>)`
- `POST /roles` → `Results.Json(AccessRoleDto)`
- `GET /roles/{roleId}` → `null ? Results.NotFound() : Results.Json(role)`
- `PUT /roles/{roleId}` → `Results.Json(AccessRoleDto)`
- `POST .../clone` → `Results.Json(AccessRoleDto)`
- `DELETE /roles/{roleId}` → `Results.NoContent()`
- `GET .../permissions` → `Results.Json(IReadOnlyList<RolePermissionGrant>)`
- `PUT .../permissions` → `Results.NoContent()`

Error: Host caught `AccessControlException` only for all eight; module endpoints
reproduce this with `catch (AccessControlException ace)` → `MapAccessError` (400, or
403 when `Code` contains `escalation`/`ceiling`). `PlatformHttpException` from the
capability gate remains uncaught, matching Host. No `ex.Message` classification, no
message parsing, no broad error-policy normalization.

## Validator classification

| Route | Classification |
| --- | --- |
| `GET /roles` (ListRoles) | `NO_VALIDATOR_REQUIRED` — no body; sellerId from authorized context |
| `GET /roles/{roleId}` (GetRole) | `NO_VALIDATOR_REQUIRED` — guid route constraint; `RequireRoleAsync` authoritative |
| `POST /roles` (Create) | `DOMAIN/APPLICATION-ENFORCED` — `RequireText`, `RequireCode`, `ValidateOwner` |
| `PUT /roles/{roleId}` (Update) | `DOMAIN/APPLICATION-ENFORCED` — `RequireText`, `EnsureMutable` |
| `POST .../clone` (Clone) | `DOMAIN/APPLICATION-ENFORCED` — `RequireText`, `RequireCode`, `EnsureMutable` on source |
| `DELETE /roles/{roleId}` (Archive) | `NO_VALIDATOR_REQUIRED` / `DOMAIN-ENFORCED` — `EnsureMutable` + `IsSystem` guard |
| `GET .../permissions` | `NO_VALIDATOR_REQUIRED` — `RequireRoleAsync` authoritative |
| `PUT .../permissions` (SetPermissions) | `DOMAIN/APPLICATION-ENFORCED` — `RequireRoleAsync`, `EnsureMutable`, `ValidateGrantsForOwnerAsync`, `PermissionCatalog.Require` |

No validators added or modified; this mapping introduced no new invalid transport
state.

## Admin / AdminSeller regression statement

The Admin platform and AdminSeller endpoints were NOT modified in this task. Both
files compile unchanged, their mappings are unchanged, and their owner-scope
intent is unchanged (`Platform`/`null` and `Seller`/route `sellerId`
respectively). `Tooba.Host` builds with 0 errors, confirming no shared-CQRS
breakage.

## Host removals

Removed from `src/backend/Host/Tooba.Host/AccessControl/AccessControlEndpoints.cs`:

- the eight `seller.Map*` role/permission route registrations
- the eight handler bodies (`SellerListRolesAsync`, `SellerCreateRoleAsync`,
  `SellerGetRoleAsync`, `SellerUpdateRoleAsync`, `SellerCloneRoleAsync`,
  `SellerArchiveRoleAsync`, `SellerGetRolePermissionsAsync`,
  `SellerSetRolePermissionsAsync`)

`RequireSellerAsync` was retained because residual Seller routes
(ceiling/assignments/users/effective/scope-resources) still consume it. Shared
helpers (`MapError`, `Trace`, `PlatformScope`, `SellerScope`) also remain.

## Single ownership proof

- All eight old Host handler names = ZERO repo-wide (only historical docs/evidence reference them)
- Module `/v1/seller/access-control` role/permission mappings = EXACTLY ONE each
- Host mappings for these eight routes = ZERO
- `/v1/seller/access-control` group mapped EXACTLY ONCE (module root)
- Endpoints direct `IAccessControlDirectory` use = ZERO

## Residual Host AccessControl families

- Seller ceiling, assignments, users, effective, scope-resources
- AdminSeller ceiling, assignments, users/effective
- Admin platform assignments, users, effective, demo-preview, scope resources
- Admin platform bootstrap/me-capabilities/permissions catalog already module-owned

## Focused validation results

| Project | Result |
| --- | --- |
| `Tooba.AccessControl.Application` | succeeded, 0 errors |
| `Tooba.AccessControl.Endpoints` | succeeded, 0 errors |
| `Tooba.Host` | succeeded, 0 errors (pre-existing warnings only) |

No solution build, no broad suite, no retries.

## Residual defects

- Seller role routes do not catch `PlatformHttpException` — pre-existing Host
  behaviour for this family, preserved deliberately. Admin/AdminSeller asymmetries
  also left untouched.

## Recovery honesty

AccessControl remains **IN_PROGRESS**. Host `AccessControl` residue is still
non-zero. After this task: Admin platform Role family, AdminSeller Role family, and
Seller Role family are all module-owned. Remaining Host AccessControl work is
non-role families. Not COMPLETE_REFERENCE_PATTERN and not structure-certified.
