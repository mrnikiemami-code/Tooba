# Evidence — TB-TMAR-HOST-ACCESSCONTROL-ADMINPLATFORM-ASSIGNMENTS-EFFECTIVE-001

Track: HOST_FIRST_ACCESSCONTROL
Parent: TB-TMAR-HOST-ACCESSCONTROL-ADMINSELLER-NONROLE-FAMILY-001 (commit `b9dda0d7b29906a63378b5f6eae6cdbba190d66b`)
Scope: Admin platform assignments + effective (4 routes)

## 1. Route ownership — before / after

| Route | Before (Host) | After (module) |
| --- | --- | --- |
| `GET /v1/admin/access-control/assignments` | `Tooba.Host/AccessControl/AccessControlEndpoints.cs` → `AdminListAssignmentsAsync` | `Tooba.AccessControl.Endpoints/Admin/AccessControlAdminEndpoints.cs` → `ListAssignmentsAsync` |
| `POST /v1/admin/access-control/assignments` | Host → `AdminAssignAsync` | Endpoints → `AssignAsync` |
| `DELETE /v1/admin/access-control/assignments/{assignmentId:guid}` | Host → `AdminRemoveAssignmentAsync` | Endpoints → `RemoveAssignmentAsync` |
| `GET /v1/admin/access-control/users/{userId:guid}/effective` | Host → `AdminEffectiveAsync` | Endpoints → `EffectiveAsync` |

Added to the existing Admin group in `AccessControlAdminEndpoints.Map`; no new root group created.

## 2. CQRS reuse

No new requests or handlers were created. Reused exactly:

- `ListAssignmentsQuery` → GET assignments
- `AssignRoleCommand` → POST assignments
- `RemoveAssignmentCommand` → DELETE assignment
- `GetEffectiveAccessQuery` → GET effective

## 3. Authorization parity

| Route | Host | Module |
| --- | --- | --- |
| GET assignments | `AdminPanelAccess.RequireAuthorizedAsync` + `accesscontrol.view` | `IAdminPanelAccess.RequireAuthorizedAsync` + `accesscontrol.view` |
| POST assignments | same + `accesscontrol.manage` | same + `accesscontrol.manage` |
| DELETE assignment | same + `accesscontrol.manage` | same + `accesscontrol.manage` |
| GET effective | same + `accesscontrol.view` | same + `accesscontrol.view` |

## 4. Platform owner-scope parity

All four requests pass `AccessOwnerScopeKind.Platform`, `OwnerScopeId = null`,
`TenantId = tenant.Current?.TenantId.Value` — identical to the removed Host `PlatformScope(tenant)` helper.

Regression check (shared CQRS unchanged elsewhere): Admin platform = Platform/null; AdminSeller = Seller with
`sellerId` from route; Seller = Seller with authorized `sellerId`. Those endpoint files were not modified.

## 5. Trace parity

`Trace(HttpRequest)` (reads `X-Request-Id`) is passed as `TraceId` for POST assignments and DELETE assignment.
GET assignments and GET effective carry no trace, matching Host.

## 6. Response / error parity

- GET assignments → `Results.Json(...)`
- POST assignments → `Results.Json(...)`
- DELETE assignment → `Results.NoContent()`
- GET effective → `Results.Json(...)`
- POST and DELETE keep `AccessControlException`-only catch with the Host mapping semantics
  (`403` when the code contains `escalation` or `ceiling`, else `400`).
- Read routes stay uncaught, matching Host. No `ex.Message` classification, no message parsing,
  no `PlatformHttpException` normalization.

## 7. Validator classification

- GET assignments: `NO_VALIDATOR_REQUIRED`
- POST assignments: `NO_VALIDATOR_REQUIRED` — reuses accepted `AssignRoleCommand` classification
- DELETE assignment: `NO_VALIDATOR_REQUIRED` — reuses accepted `RemoveAssignmentCommand`
- GET effective: `NO_VALIDATOR_REQUIRED` — reuses accepted `GetEffectiveAccessQuery`

## 8. Host removals

Removed from `src/backend/Host/Tooba.Host/AccessControl/AccessControlEndpoints.cs`:

- mappings: the four `admin.Map*` lines for assignments/assignments/{id}/users/{id}/effective
- handlers: `AdminListAssignmentsAsync`, `AdminAssignAsync`, `AdminRemoveAssignmentAsync`, `AdminEffectiveAsync`
- private record `AssignBody` — removed because no Host consumer remained

Retained: `AdminSearchUsersAsync` (+ map), `AdminDemoPreviewAsync`, `PlatformScope`, `SellerScope`, `Trace`,
`MapError`, `RequireSellerAsync`, and all out-of-scope scope-resource / Seller residue handlers.

## 9. Single-route ownership proof

- All four routes = EXACTLY ONE module mapping each; the only other occurrence of the Admin root path is the
  module's own `AccessControlEndpointModule` group registration.
- Host mappings for those four = ZERO; the four Host handler names = ZERO repo-wide.
- `GET /v1/admin/access-control/users` remains exactly one Host mapping (intentional hold).

## 10. AdminSearchUsersAsync hold confirmation

`AdminSearchUsersAsync` is **untouched**: map still at
`src/backend/Host/Tooba.Host/AccessControl/AccessControlEndpoints.cs:23` and handler at `:59`. It still uses
`IIdentityContactLookup` / `IOperatorProfileDirectory` / `IIdentityAuthenticationService` enrichment and the
`EnrichUserHitsAsync` helper — no cross-module Identity/OperatorProfile dependency was moved into
AccessControl.Application or Endpoints. This remains pending the dedicated enrichment-seam task.

## 11. Application / endpoint boundary

- No new Application dependencies.
- Application → Host = ZERO; Application → Endpoints = ZERO.
- Endpoints: `ISender` only, no direct `IAccessControlDirectory`, no Infrastructure/DbContext, no Host types.

## 12. Focused builds

- `Tooba.AccessControl.Application.csproj --no-restore` → succeeded, 0 errors
- `Tooba.AccessControl.Endpoints.csproj --no-restore` → succeeded, 0 errors
- `Tooba.Host.csproj --no-restore` → succeeded, 0 errors

Focused tests: no existing test targets these shared requests specifically; no test added (no new behaviour).

## 13. Residual Host AccessControl families

`GET /v1/admin/access-control/users` (held), Seller users/effective, Admin + Seller scope-resources, demo-preview,
DevelopmentSeed/DemoSnapshot, and shared Host helpers remain Host-owned. AccessControl remains **IN_PROGRESS**;
Host residue non-zero. No COMPLETE_REFERENCE_PATTERN, no structure certification.
