# TB-TMAR-HOST-ACCESSCONTROL-ADMIN-ROLE-WRITES-001 — Admin role create/update evacuation

## Scope

Evacuate exactly two Admin platform role mutation routes from `Tooba.Host` into
module-owned CQRS + Endpoints:

- `POST /v1/admin/access-control/roles`
- `PUT /v1/admin/access-control/roles/{roleId:guid}`

Clone (`POST /roles/{roleId}/clone`) and Archive (`DELETE /roles/{roleId}`) were
explicitly out of scope.

## Before / after route ownership

| Route | Before | After |
| --- | --- | --- |
| `POST /v1/admin/access-control/roles` | `Host/AccessControl/AccessControlEndpoints.cs` → `AdminCreateRoleAsync` | `Tooba.AccessControl.Endpoints.Admin.AccessControlAdminEndpoints.CreateRoleAsync` |
| `PUT /v1/admin/access-control/roles/{roleId:guid}` | `Host/AccessControl/AccessControlEndpoints.cs` → `AdminUpdateRoleAsync` | `Tooba.AccessControl.Endpoints.Admin.AccessControlAdminEndpoints.UpdateRoleAsync` |

## CQRS files

- `src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Commands/CreateRole/CreateRoleCommand.cs`
  - `CreateRoleCommand : IRequest<AccessRoleDto>`
  - `CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, AccessRoleDto>` (depends on `IAccessControlDirectory`)
- `src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Commands/UpdateRole/UpdateRoleCommand.cs`
  - `UpdateRoleCommand : IRequest<AccessRoleDto>`
  - `UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, AccessRoleDto>` (depends on `IAccessControlDirectory`)

Commands carry only application data: trusted actor id, tenant id, body values,
and trace id. No `HttpRequest`, `HttpContext`, `IResult`, `Results.*`, or Host /
Endpoints references.

Handlers construct `AccessOwnerScope(AccessOwnerScopeKind.Platform, null, tenantId)`
— identical to the previous Host `PlatformScope(tenant)` helper.

## Validator coverage / classification

| Command | Validator | Classification |
| --- | --- | --- |
| `CreateRoleCommand` | none added | **Not required** — the application does not accept invalid transport state; `IAccessControlDirectory.CreateRoleAsync` already rejects it with stable typed codes |
| `UpdateRoleCommand` | none added | **Not required** — same reasoning; the directory re-validates name and enforces mutability |

Existing rule-carrying validation remains authoritative and is deliberately NOT
duplicated:

- `RequireText(name, 128)` → `AccessControlException("access.validation.text")`
- `RequireCode(code)` → `AccessControlException("access.validation.code")` (2..64, lowercased, `[a-z0-9_-]`)
- `ValidateOwner(owner)` → `access.owner.invalid`
- `EnsureMutable(role)` on update

Adding a FluentValidation validator here would pre-empt those stable domain error
codes with generic `ValidationException` output, i.e. it would **change** the
current typed/stable error behavior that this task requires preserving. Because
the transport shape (non-empty / max length / code charset) is already fully
enforced inside the directory with dedicated codes, no ceremonial validator was
added. Classification: `validator = N/A (domain-enforced)`.

## Authorization parity

Both routes keep:

1. `IAdminPanelAccess.RequireAuthorizedAsync(request, ct)` — same panel
   authorization seam substituted for the Host `AdminPanelAccess` static helper.
2. `AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.manage", authz, tenant, ct)`
   — same branch ordering (Allow → view fail-open → Unavailable 503 → manage →
   403 denied).

## Trace parity

`X-Request-Id` is read via the endpoint-local `Trace(HttpRequest)` helper
(identical implementation to the Host helper) and forwarded into the command as
`TraceId`, which the handler passes unchanged to
`IAccessControlDirectory.CreateRoleAsync` / `UpdateRoleAsync`.

## Response parity

- Success: `Results.Json(<AccessRoleDto>)` — same DTO type returned by the
  directory, so the serialized payload is byte-identical.
- Create: `PlatformHttpException` → `Results.Json(new { title, code }, statusCode: ph.StatusCode)`.
- Update: `PlatformHttpException` → generic `MapAccessError` (the pre-existing
  Host asymmetry is preserved verbatim).

## Error parity

`MapAccessError` reproduces the Host `MapError` logic exactly:

- `AccessControlException` → `400`, or `403` when `Code` contains `escalation`
  or `ceiling`.
- Any other exception → `500` with `{ title = "access.error", code = "access.error" }`.

No `ex.Message` classification and no message parsing were introduced.

## Host removals

Removed from `src/backend/Host/Tooba.Host/AccessControl/AccessControlEndpoints.cs`:

- `admin.MapPost("/roles", AdminCreateRoleAsync)`
- `admin.MapPut("/roles/{roleId:guid}", AdminUpdateRoleAsync)`
- `AdminCreateRoleAsync` handler body
- `AdminUpdateRoleAsync` handler body

Shared helpers (`MapError`, `Trace`, `PlatformScope`, `SellerScope`) are
untouched and still used by the remaining residual role routes.

## Single-route ownership

- `AdminCreateRoleAsync` = ZERO
- `AdminUpdateRoleAsync` = ZERO
- module `POST /roles` mapping = EXACTLY ONE (admin group)
- module `PUT /roles/{roleId:guid}` mapping = EXACTLY ONE (admin group)
- Host mappings for these two admin routes = ZERO

Note: `adminSeller` and `seller` route groups retain their own `/roles` routes
(`AdminSellerCreateRoleAsync`, `SellerCreateRoleAsync`, …); these are separate
route groups and out of scope.

## Residual host role routes (still in Host)

- `POST /roles/{roleId:guid}/clone`
- `DELETE /roles/{roleId}`
- `GET /roles/{roleId}/permissions`
- `PUT /roles/{roleId}/permissions`
- `adminSeller` / `seller` role routes

## Focused builds

| Project | Result |
| --- | --- |
| `Tooba.AccessControl.Application` | succeeded, 0 errors |
| `Tooba.AccessControl.Endpoints` | succeeded, 0 errors |
| `Tooba.Host` | succeeded, 0 errors (pre-existing warnings only) |

## Focused tests

No new focused tests were added; no existing validator/handler tests target
these commands. Domain behavior is unchanged, so the existing directory-level
validation path is covered by the pre-existing suite.

## Residual defects

- Update-route `PlatformHttpException` handling remains asymmetric with the
  Create route (generic `MapAccessError` instead of the explicit
  `{ title, code }` mapping). This is pre-existing Host behavior and was
  preserved deliberately; not repaired in this task.

## Recovery honesty

AccessControl remains **IN_PROGRESS**. Host `AccessControl` residue is still
non-zero. Role family is NOT complete and NOT structure-certified.
