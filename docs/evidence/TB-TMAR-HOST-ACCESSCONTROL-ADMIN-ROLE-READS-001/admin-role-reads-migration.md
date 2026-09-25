# TB-TMAR-HOST-ACCESSCONTROL-ADMIN-ROLE-READS-001 — Admin platform role read migration

Mode: evacuate only the two Admin platform role READ routes. No role mutation route touched.

## Before / after route ownership

| Route | Before | After |
| --- | --- | --- |
| `GET /v1/admin/access-control/roles` | Host `AdminListRolesAsync` | `AccessControl.Endpoints/Admin/AccessControlAdminEndpoints.ListRolesAsync` |
| `GET /v1/admin/access-control/roles/{roleId:guid}` | Host `AdminGetRoleAsync` | `AccessControl.Endpoints/Admin/AccessControlAdminEndpoints.GetRoleAsync` |

## CQRS files

- `src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Queries/ListRoles/ListRolesQuery.cs`
  - `ListRolesQuery(AccessOwnerScopeKind OwnerScopeKind, Guid? OwnerScopeId, string? TenantId, bool IncludeArchived) : IRequest<IReadOnlyList<AccessRoleDto>>` + real `IRequestHandler`.
- `src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Queries/GetRole/GetRoleQuery.cs`
  - `GetRoleQuery(Guid RoleId, AccessOwnerScopeKind OwnerScopeKind, Guid? OwnerScopeId, string? TenantId) : IRequest<AccessRoleDto?>` + real `IRequestHandler`.

Both handlers construct `AccessOwnerScope(kind, id, tenantId)` — identical to the Host `PlatformScope(tenant) = new(AccessOwnerScopeKind.Platform, null, tenant.Current?.TenantId.Value)` semantics — and call `IAccessControlDirectory.ListRolesAsync` / `GetRoleAsync`.

## Authorization parity

Endpoint order preserved: `IAdminPanelAccess.RequireAuthorizedAsync` → `AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", authz, tenant, ct)` → `sender.Send(...)` → `Results.Json(...)`. `includeArchived` remains an optional query parameter on the list route.

## Response / 404 / error parity

- List: `Results.Json(IReadOnlyList<AccessRoleDto>)`; ordering/`includeArchived` semantics unchanged.
- Get: `role is null ? Results.NotFound() : Results.Json(role)` — identical.
- Get error handling: `AccessControlException` mapped to `{ title, code }` with status 403 when the code contains `escalation`/`ceiling`, else 400 — a narrow endpoint-local mapper reproducing the Host `MapError` branch for this exception type verbatim.

## Validator classification

- `ListRolesQuery`: `NO_VALIDATOR_REQUIRED` — `IncludeArchived` is a `bool?`-free `bool` flag with no invalid state.
- `GetRoleQuery`: `NO_VALIDATOR_REQUIRED` — route constraint already enforces `guid`; `Guid.Empty` is not explicitly invalid under existing accepted patterns.

## Single-route-ownership proof

- Host `AdminListRolesAsync` / `AdminGetRoleAsync`: **ZERO**.
- Host `admin.MapGet("/roles", ...)` / `admin.MapGet("/roles/{roleId:guid}", ...)`: **ZERO** (remaining Host `/roles` match is `roles/{roleId:guid}/permissions`, a different family).
- Module `group.MapGet("/roles", ...)`: **EXACTLY ONE** (`AccessControlAdminEndpoints.cs:29`).
- Module `group.MapGet("/roles/{roleId:guid}", ...)`: **EXACTLY ONE** (`AccessControlAdminEndpoints.cs:30`).
- Requests sent via `ISender`: yes. Real `IRequestHandler`: yes.
- `AccessControl.Endpoints` direct `IAccessControlDirectory` usage: **ZERO**.
- `AccessControl.Application` → `Tooba.Host.*`: ZERO. → `Tooba.AccessControl.Endpoints`: ZERO. → `Microsoft.AspNetCore.*`: ZERO.

## Focused builds

- `dotnet build Tooba.AccessControl.Application.csproj --no-restore`: succeeded, 0 errors.
- `dotnet build Tooba.AccessControl.Endpoints.csproj --no-restore`: succeeded, 0 errors.
- `dotnet build Tooba.Host.csproj --no-restore`: succeeded, 0 errors.

Focused tests: none — no existing focused test directly covers these two routes.

## Residual Host AccessControl role routes (still in Host)

- `POST /v1/admin/access-control/roles`
- `PUT /v1/admin/access-control/roles/{roleId:guid}`
- `POST /v1/admin/access-control/roles/{roleId:guid}/clone`
- `DELETE /v1/admin/access-control/roles/{roleId:guid}`
- `GET/PUT /v1/admin/access-control/roles/{roleId:guid}/permissions`
- all admin-seller role routes and all seller role routes

## Recovery honesty

AccessControl remains IN_PROGRESS. Host AccessControl residue remains non-zero. Not marked COMPLETE_REFERENCE_PATTERN; not structure-certified; role family not claimed complete.

## Protected state

Checkout untouched; frontend frozen; Fulfillment untouched; no schema/migrations; `Program.cs` mapping cleanup not performed; `AccessControlDevelopmentSeed.cs` / `AccessControlDemoSnapshot.cs` untouched.
