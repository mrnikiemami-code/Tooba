# Evidence — TB-TMAR-HOST-ACCESSCONTROL-ADMINSELLER-NONROLE-FAMILY-001

Track: HOST_FIRST_ACCESSCONTROL
Parent: TB-TMAR-HOST-ACCESSCONTROL-SELLER-CEILING-ASSIGNMENTS-001 (commit `46c3777ca7f97e2c73a4e4c4f8b59f5982db4883`)
Scope: complete AdminSeller non-role family (6 routes)

## 1. Route ownership — before / after

| Route | Before (Host) | After (module) |
| --- | --- | --- |
| `GET /v1/admin/sellers/{sellerId:guid}/access-control/ceiling` | `Tooba.Host/AccessControl/AccessControlEndpoints.cs` → `AdminGetCeilingAsync` | `Tooba.AccessControl.Endpoints/Admin/AccessControlAdminSellerEndpoints.cs` → `GetCeilingAsync` |
| `PUT /v1/admin/sellers/{sellerId:guid}/access-control/ceiling` | Host → `AdminSetCeilingAsync` | Endpoints → `SetCeilingAsync` |
| `GET /v1/admin/sellers/{sellerId:guid}/access-control/assignments` | Host → `AdminSellerListAssignmentsAsync` | Endpoints → `ListAssignmentsAsync` |
| `POST /v1/admin/sellers/{sellerId:guid}/access-control/assignments` | Host → `AdminSellerAssignAsync` | Endpoints → `AssignAsync` |
| `DELETE /v1/admin/sellers/{sellerId:guid}/access-control/assignments/{assignmentId:guid}` | Host → `AdminSellerRemoveAssignmentAsync` | Endpoints → `RemoveAssignmentAsync` |
| `GET /v1/admin/sellers/{sellerId:guid}/access-control/users/{userId:guid}/effective` | Host → `AdminSellerEffectiveAsync` | Endpoints → `EffectiveAsync` |

Additions were made to the existing AdminSeller group in `AccessControlAdminSellerEndpoints.Map`; no new root group was created.

## 2. CQRS reuse / new command

Reused (no duplication):

- `GetSellerCeilingQuery` → GET ceiling
- `ListAssignmentsQuery` → GET assignments
- `AssignRoleCommand` → POST assignments
- `RemoveAssignmentCommand` → DELETE assignment
- `GetEffectiveAccessQuery` → GET effective

New (only what was missing):

- `Commands/SetSellerCeiling/SetSellerCeilingCommand.cs` — real MediatR `IRequest<Unit>` / `IRequestHandler<SetSellerCeilingCommand, Unit>`
  - carries `SellerPartyId`, `IReadOnlyList<SellerCeilingEntryInput>`, `ActorUserId`, `TraceId`
  - Application-layer `SellerCeilingEntryInput(PermissionId, Enabled, ScopeKind, ScopeResourceId)` prevents leaking HTTP transport types
  - handler maps inputs to the existing `IAccessControlDirectory.SetSellerCeilingAsync` tuple list and invokes it unchanged

## 3. Authorization parity

| Route | Host | Module |
| --- | --- | --- |
| GET ceiling | `AdminPanelAccess.RequireAuthorizedAsync` + `view` | `IAdminPanelAccess.RequireAuthorizedAsync` + `view` |
| PUT ceiling | same + `manage` | same + `manage` |
| GET assignments | same + `view` | same + `view` |
| POST assignments | same + `manage` | same + `manage` |
| DELETE assignment | same + `manage` | same + `manage` |
| GET effective | same + `view` | same + `view` |

## 4. Owner-scope parity

Assignments / effective: `AccessOwnerScopeKind.Seller`, `OwnerScopeId = sellerId` **from route** (not the seller panel
context), `TenantId = tenant.Current?.TenantId.Value` — identical to Host `SellerScope(sellerId, tenant)`.

Ceiling: `GetSellerCeilingQuery(sellerId)` and `SetSellerCeilingCommand(sellerId, …)` — ceiling is keyed by
`sellerPartyId`, not by owner scope, matching Host.

Regression intent verified: Seller routes still derive `sellerId` from the authorized seller context with
`AccessOwnerScopeKind.Seller`; Admin platform Platform/null usage unchanged; their code was not modified.

## 5. Trace parity

`Trace(HttpRequest)` (reads `X-Request-Id`) is passed as `TraceId` for PUT ceiling, POST assignments, and DELETE
assignment. GET ceiling / GET assignments / GET effective carry no trace, matching Host.

## 6. Response / error parity

- GET ceiling → `Results.Json(...)`
- PUT ceiling → `Results.NoContent()`
- GET assignments → `Results.Json(...)`
- POST assignments → `Results.Json(...)`
- DELETE assignment → `Results.NoContent()`
- GET effective → `Results.Json(...)`
- PUT/POST/DELETE retain `AccessControlException`-only catch with the Host mapping semantics
  (`title = ace.Message`, `code = ace.Code`, `403` when the code contains `escalation` or `ceiling`, else `400`).
- Read routes remain uncaught, matching Host. No `ex.Message` classification, no message parsing,
  no `PlatformHttpException` normalization.

## 7. Ceiling JSON contract parity

Endpoint-local records reproduce the public contract exactly:

```json
{ "entries": [ { "permissionId": "...", "enabled": true, "scopeKind": "GlobalWithinOwner", "scopeResourceId": null } ] }
```

`scopeKind` defaults to `AccessScopeKind.GlobalWithinOwner` and `scopeResourceId` is nullable, matching the removed
Host `CeilingBody` / `CeilingEntry`. The private Host `CeilingBody`/`CeilingEntry` records were removed because no
Host consumer remains.

## 8. Validator classification

- GET ceiling: `NO_VALIDATOR_REQUIRED`
- PUT ceiling: `NO_VALIDATOR_REQUIRED` — `SetSellerCeilingAsync` already performs the authoritative validation and
  throws stable `AccessControlException` codes (escalation/ceiling policy); no genuinely uncovered transport-invalid
  state was found, so adding a validator would have replaced stable error semantics.
- GET assignments: `NO_VALIDATOR_REQUIRED`
- POST assignments: `NO_VALIDATOR_REQUIRED` — reuses accepted `AssignRoleCommand` classification unchanged
- DELETE assignment: `NO_VALIDATOR_REQUIRED` — reuses accepted `RemoveAssignmentCommand`; guid route constraint
- GET effective: `NO_VALIDATOR_REQUIRED` — reuses accepted `GetEffectiveAccessQuery`

## 9. Host removals

Removed from `src/backend/Host/Tooba.Host/AccessControl/AccessControlEndpoints.cs`:

- mappings: the six `adminSeller.Map*` lines for this family (group variable retained because the group is still
  created by the module registration pattern; no Host AdminSeller route remains)
- handlers: `AdminGetCeilingAsync`, `AdminSetCeilingAsync`, `AdminSellerListAssignmentsAsync`,
  `AdminSellerAssignAsync`, `AdminSellerRemoveAssignmentAsync`, `AdminSellerEffectiveAsync`
- private records: `CeilingBody`, `CeilingEntry` (no Host consumer remained)

Retained: `AssignBody` (still used by `AdminAssignAsync`), `RequireSellerAsync`, `MapError`, `PlatformScope`,
`SellerScope`, and all out-of-scope Admin platform / Seller residue handlers.

## 10. Single-route ownership proof

- All six AdminSeller non-role routes = EXACTLY ONE module mapping each in
  `AccessControlAdminSellerEndpoints`; the only other occurrence of the AdminSeller root path is the module's own
  `AccessControlEndpointModule` group registration.
- Host mappings for those six = ZERO; Host handler names = ZERO repo-wide.
- `AdminSeller *` handler identifiers appear nowhere except historical task/evidence records.

## 11. Boundary audit

- Application → Host: ZERO
- Application → Endpoints: ZERO
- No `HttpRequest`/`HttpContext`/`IResult`/`Results.*` in Application
- Endpoints direct `IAccessControlDirectory`: ZERO
- Endpoints Infrastructure/DbContext: ZERO
- Host types/namespaces in Endpoints: ZERO
- All application dispatch goes through `ISender`.

## 12. Focused builds

- `Tooba.AccessControl.Application.csproj --no-restore` → succeeded, 0 errors
- `Tooba.AccessControl.Endpoints.csproj --no-restore` → succeeded, 0 errors
- `Tooba.Host.csproj --no-restore` → succeeded, 0 errors

Focused tests: `src/backend/Host/Tooba.Host.Tests/AccessControlFoundationTests.cs` asserts the AdminSeller group is
registered and the module boundary is clean; no route/handler-specific test exists for this family, and none was
required since no validator was added.

## 13. Residual Host AccessControl families

Admin platform assignments/users/effective, Seller users/effective, Admin + Seller scope-resources, demo-preview,
DevelopmentSeed/DemoSnapshot, and shared helpers remain Host-owned. AccessControl remains **IN_PROGRESS**;
Host AccessControl residue is non-zero. No COMPLETE_REFERENCE_PATTERN, no structure certification.
