# Evidence — TB-TMAR-HOST-ACCESSCONTROL-SELLER-CEILING-ASSIGNMENTS-001

Track: HOST_FIRST_ACCESSCONTROL
Parent: TB-TMAR-HOST-ACCESSCONTROL-SELLER-ROLE-FAMILY-001 (commit `284a3eb09212d696288d71c1739f7a9c419acd8e`)
Scope: Seller ceiling + assignments family only (4 routes)

## 1. Route ownership — before / after

| Route | Before (Host) | After (module) |
| --- | --- | --- |
| `GET /v1/seller/access-control/ceiling` | `Tooba.Host/AccessControl/AccessControlEndpoints.cs` → `SellerGetCeilingAsync` | `Tooba.AccessControl.Endpoints/Seller/AccessControlSellerEndpoints.cs` → `GetCeilingAsync` |
| `GET /v1/seller/access-control/assignments` | Host → `SellerListAssignmentsAsync` | Endpoints → `ListAssignmentsAsync` |
| `POST /v1/seller/access-control/assignments` | Host → `SellerAssignAsync` | Endpoints → `AssignAsync` |
| `DELETE /v1/seller/access-control/assignments/{assignmentId:guid}` | Host → `SellerRemoveAssignmentAsync` | Endpoints → `RemoveAssignmentAsync` |

## 2. Application CQRS files (new)

- `Queries/GetSellerCeiling/GetSellerCeilingQuery.cs` — `IRequest<IReadOnlyList<SellerCeilingEntryDto>>`
- `Queries/ListAssignments/ListAssignmentsQuery.cs` — owner-scope aware `IRequest<IReadOnlyList<UserRoleAssignmentDto>>`
- `Commands/AssignRole/AssignRoleCommand.cs` — `IRequest<UserRoleAssignmentDto>`
- `Commands/RemoveAssignment/RemoveAssignmentCommand.cs` — `IRequest<Unit>`

All use real MediatR `IRequest` / `IRequestHandler`. No generic dispatcher. Handlers depend only on
`IAccessControlDirectory`. Requests carry trusted `AccessOwnerScopeKind` / `OwnerScopeId` / `TenantId`,
trusted `ActorUserId` for mutations, body/route values, and `TraceId` for mutations.

## 3. Authorization parity

| Route | Host gate | Module gate |
| --- | --- | --- |
| GET ceiling | `RequireSellerAsync` + `accesscontrol.view` | `ISellerPanelAccess.RequireAuthorizedAsync` + `accesscontrol.view` |
| GET assignments | `RequireSellerAsync` + `accesscontrol.view` | `ISellerPanelAccess.RequireAuthorizedAsync` + `accesscontrol.view` |
| POST assignments | `RequireSellerAsync` + `accesscontrol.manage` | `ISellerPanelAccess.RequireAuthorizedAsync` + `accesscontrol.manage` |
| DELETE assignment | `RequireSellerAsync` + `accesscontrol.manage` | `ISellerPanelAccess.RequireAuthorizedAsync` + `accesscontrol.manage` |

Host `RequireSellerAsync` is **not** used by the module. Residual Seller routes (users/effective/scope-resources)
still use it, so it was retained in Host.

## 4. Owner-scope parity

Host `SellerScope(sellerId, tenant)` == `new AccessOwnerScope(AccessOwnerScopeKind.Seller, sellerId, tenant.Current?.TenantId.Value)`.
Module requests carry `AccessOwnerScopeKind.Seller`, `OwnerScopeId = sellerId` from the authorized seller context,
and the same current tenant id, so scope construction is identical.

## 5. Trace parity

`Trace(HttpRequest)` reads `X-Request-Id` and is passed as `TraceId` for POST assign and DELETE remove.
GET ceiling and GET assignments carry no trace, matching Host.

## 6. Response / error parity

- GET ceiling → `Results.Json(ceiling)`
- GET assignments → `Results.Json(assignments)`
- POST assignments → `Results.Json(assignment)`
- DELETE assignment → `Results.NoContent()`
- POST/DELETE retain `AccessControlException`-only catch and the Host `MapError` mapping semantics
  (`title = ace.Message`, `code = ace.Code`, status `403` when the code contains `escalation` or `ceiling`, else `400`).
  No `ex.Message` classification, no message-text parsing, no `PlatformHttpException` normalization.

## 7. Validator classification

- GET ceiling: `NO_VALIDATOR_REQUIRED` (read-only; no transport-invalid state).
- GET assignments: `NO_VALIDATOR_REQUIRED` (read-only; no transport-invalid state).
- POST assignments: `NO_VALIDATOR_REQUIRED` — `UserId`/`RoleId` are transport inputs already authoritatively
  rejected downstream with stable `AccessControlException` codes; no distinct transport-invalid state exists, so no
  FluentValidation validator was added (adding one would replace stable error semantics).
- DELETE assignment: `NO_VALIDATOR_REQUIRED` — route carries an `assignmentId:guid` constraint; `Guid.Empty`
  not an established invalid invariant.

## 8. Host removals

Removed from `src/backend/Host/Tooba.Host/AccessControl/AccessControlEndpoints.cs`:

- mappings: `seller.MapGet("/ceiling", …)`, `seller.MapGet("/assignments", …)`,
  `seller.MapPost("/assignments", …)`, `seller.MapDelete("/assignments/{assignmentId:guid}", …)`
- handlers: `SellerGetCeilingAsync`, `SellerListAssignmentsAsync`, `SellerAssignAsync`, `SellerRemoveAssignmentAsync`

Retained in Host (out of scope / still used): `RequireSellerAsync`, `MapError`, `SellerScope`, residual Seller
users/effective/scope-resources handlers, and all AdminSeller/Admin platform handlers.

## 9. Single-route ownership proof

- `GET /v1/seller/access-control/ceiling` — EXACTLY ONE module mapping; Host = ZERO.
- `GET /v1/seller/access-control/assignments` — EXACTLY ONE module mapping; Host = ZERO (AdminSeller's
  `/v1/admin/sellers/{sellerId:guid}/access-control/assignments` remains, separate route).
- `POST /v1/seller/access-control/assignments` — EXACTLY ONE module mapping; Host = ZERO.
- `DELETE /v1/seller/access-control/assignments/{assignmentId:guid}` — EXACTLY ONE module mapping; Host = ZERO.
- Old four Handler names = ZERO repo-wide except this canonical task/evidence and historical records.

## 10. Boundary audit

- Application → Host: ZERO
- Application → Endpoints: ZERO
- Endpoints direct `IAccessControlDirectory`: ZERO
- Endpoints Infrastructure/DbContext: ZERO
- Host types in Endpoints: ZERO
- All application calls go through `ISender`.

## 11. Focused builds

- `Tooba.AccessControl.Application.csproj --no-restore` → succeeded, 0 errors
- `Tooba.AccessControl.Endpoints.csproj --no-restore` → succeeded, 0 errors
- `Tooba.Host.csproj --no-restore` → succeeded, 0 errors

## 12. Residual Host AccessControl families

Still Host-owned after this slice: AdminSeller ceiling/assignments/effective, Admin platform assignments/users/effective,
Seller users/effective, Seller scope-resources, demo-preview, and shared helpers. AccessControl remains
**IN_PROGRESS**; Host AccessControl residue is non-zero. No COMPLETE_REFERENCE_PATTERN, no structure certification.
