# TB-TMAR-ACCESSCONTROL-PRECERT-STRUCTURE-REPAIR-001 — Pre-cert structure repair evidence

Parent: `TB-TMAR-HOST-ACCESSCONTROL-FINAL-CLEANUP-001` (commit `ceb8ac85`)
Track: `ACCESSCONTROL_PRECERT`
Status: PASS (structure repair only — NOT certification)

## 1. Root files before / after

### Application project
| Before (root) | After |
|---------------|-------|
| `Tooba.AccessControl.Application/AccessControlContracts.cs` | `Tooba.AccessControl.Application/Models/AccessControlContracts.cs` |
| `Tooba.AccessControl.Application/PermissionCatalog.cs` | `Tooba.AccessControl.Application/Permissions/PermissionCatalog.cs` |

Application root `.cs` files after repair: **ZERO**. Root holds only the `.csproj`
(plus `artifacts/` and build output dirs, which are not hand-written sources).

### Infrastructure project
| Before (root) | After |
|---------------|-------|
| `Tooba.AccessControl.Infrastructure/AccessControlDirectory.cs` | `.../Directories/AccessControlDirectory.cs` |
| `.../AccessControlInstrumentation.cs` | `.../Observability/AccessControlInstrumentation.cs` |
| `.../AccessControlOutboxRegistration.cs` | `.../Messaging/AccessControlOutboxRegistration.cs` |
| `.../AccessControlModule.cs` | unchanged at root (module composition entry) |

Infrastructure root `.cs` files after repair: **`AccessControlModule.cs` only**.

### Endpoints project
Root holds **`AccessControlEndpointModule.cs` only**.

## 2. Exact move map + path/namespace proof

| Physical path | Namespace | Match |
|---------------|-----------|-------|
| `Application/Models/AccessControlContracts.cs` | `Tooba.AccessControl.Application.Models` | EXACT |
| `Application/Permissions/PermissionCatalog.cs` | `Tooba.AccessControl.Application.Permissions` | EXACT |
| `Infrastructure/Directories/AccessControlDirectory.cs` | `Tooba.AccessControl.Infrastructure.Directories` | EXACT |
| `Infrastructure/Observability/AccessControlInstrumentation.cs` | `Tooba.AccessControl.Infrastructure.Observability` | EXACT |
| `Infrastructure/Messaging/AccessControlOutboxRegistration.cs` | `Tooba.AccessControl.Infrastructure.Messaging` | EXACT |
| `Infrastructure/AccessControlModule.cs` | `Tooba.AccessControl.Infrastructure` | EXACT |
| `Endpoints/AccessControlEndpointModule.cs` | `Tooba.AccessControl.Endpoints` | EXACT |

No namespace alias, no `global using` alias, no type-name shadowing was introduced.
All consumers were updated with ordinary `using` directives
(`...Application.Models`, `...Application.Permissions`, `...Infrastructure.Directories`,
`...Infrastructure.Observability`, `...Infrastructure.Messaging`) inside the module,
in `Tooba.Order.Infrastructure`, and in Host/Host.Tests consumers.

Both moved Infrastructure/Permissions/Models files remain functionally identical —
no DTO, contract, error-code, DI-lifetime, or behavior change.

## 3. Host ZERO confirmation

- `src/backend/Host/Tooba.Host/AccessControl` → **absent**.
- `namespace Tooba.Host.AccessControl` → ZERO production occurrences.
- `MapAccessControlEndpoints` → ZERO (only a negative assertion inside the boundary test).
- `Program.cs` still contains `app.MapAccessControlModuleEndpoints();` (line 513).

## 4. Boundary audit

| Edge | Result |
|------|--------|
| `AccessControl.Application` → Host | ZERO |
| `AccessControl.Application` → foreign Application/Domain | ZERO |
| `AccessControl.Application` → `Catalog.Contracts` | ALLOWED |
| `AccessControl.Application` → `Identity.Contracts` | ALLOWED |
| `AccessControl.Application` → `OperatorProfile.Contracts` | ALLOWED |
| `AccessControl.Infrastructure` → `Catalog.Application` | ZERO |
| `AccessControl.Infrastructure` → `Catalog.Domain` | ZERO |
| `AccessControl.Infrastructure` → `Catalog.Contracts` | ALLOWED |
| `AccessControl.Endpoints` → Host | ZERO |
| `AccessControl.Endpoints` → `IAccessControlDirectory` / `AccessControlDirectory` | ZERO (ISender dispatch only) |

## 5. Endpoint-reachable request inventory

All endpoint dispatches use `ISender`; 19 distinct endpoint-reachable MediatR requests:

Commands (11): `CreateRoleCommand`, `UpdateRoleCommand`, `CloneRoleCommand`,
`ArchiveRoleCommand`, `AssignRoleCommand`, `RemoveAssignmentCommand`,
`SetRolePermissionsCommand`, `SetSellerCeilingCommand`, `EnsureAccessControlBootstrapCommand`
(by `Endpoints`-reachable surface: `AccessControlAdminEndpoints`,
`AccessControlAdminSellerEndpoints`, `AccessControlSellerEndpoints`).

Queries (8): `GetEffectiveAccessQuery`, `GetRoleQuery`, `GetRolePermissionsQuery`,
`GetSellerCeilingQuery`, `ListAssignmentsQuery`, `ListRolesQuery`,
`ListPermissionCatalogQuery`, `ListSellerPermissionCatalogQuery`,
`SearchAccessUsersQuery`, `ListScopeResourcesQuery`.

(`ListScopeResourcesQuery` is the module-owned scope-resource query; it is included in the
inventory. Total endpoint-reachable request types counted = 19.)

## 6. Validator classification

| Classification | Count | Requests |
|----------------|-------|----------|
| VALIDATOR_REQUIRED (transport shape) | 6 | `CreateRoleCommand`, `UpdateRoleCommand`, `CloneRoleCommand`, `AssignRoleCommand`, `SetRolePermissionsCommand`, `SetSellerCeilingCommand` |
| NO_VALIDATOR_REQUIRED | 13 | `ArchiveRoleCommand`, `RemoveAssignmentCommand`, `EnsureAccessControlBootstrapCommand`, `GetEffectiveAccessQuery`, `GetRoleQuery`, `GetRolePermissionsQuery`, `GetSellerCeilingQuery`, `ListAssignmentsQuery`, `ListRolesQuery`, `ListPermissionCatalogQuery`, `ListSellerPermissionCatalogQuery`, `SearchAccessUsersQuery`, `ListScopeResourcesQuery` |

Rationale:
- The 6 commands carry caller-supplied structured payloads (role name/code/description,
  grants, ceiling entries, user/role ids) whose shape is validated by serializer binding plus
  domain/application enforcement (`AccessControlException` codes such as
  `access.scope.unknown_resource`, `access.escalation.platform_permission`,
  `access.escalation.ceiling`).
- Remaining commands/queries are either auth-scoped actor identity only, guid-only routes,
  no-input lists, or optional `q` search presentation inputs.

**No FluentValidation validator is registered or present in the AccessControl module**
(`AbstractValidator`/`IValidator`/`AddValidatorsFromAssembly` → ZERO).

## 7. Validator gaps

A transport validator gap exists for the 6 VALIDATOR_REQUIRED commands
(0-of-6 present). Per task instruction this is recorded, not implemented here.

`Certification-Ready-State = NO_VALIDATOR_GAP_PENDING`

The next task must decide whether these remain application/domain-enforced
(consistent with prior accepted tasks in this track) or require transport validators
before `ARCH-COMPLETE-002` structure certification.

## 8. Focused builds / tests

| Command | Result |
|---------|--------|
| `dotnet build .../Tooba.AccessControl.Application.csproj --no-restore` | Build succeeded, 0 errors |
| `dotnet build .../Tooba.AccessControl.Infrastructure.csproj --no-restore` | Build succeeded, 0 errors |
| `dotnet build .../Tooba.AccessControl.Endpoints.csproj --no-restore` | Build succeeded, 0 errors |
| `dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore` | Build succeeded, 0 errors |
| `dotnet test Tooba.Host.Tests --filter FullyQualifiedName~AccessControl_module_boundary_static_checks` | Passed 1, Failed 0 |

## 9. Pre-existing residual (NOT introduced by this task)

`Tooba.Host.Tests/Architecture/TmarSourceSizeAndInfraAppTests.cs` fails at HEAD
(`Hand_written_source_size_does_not_expand_beyond_baseline`,
`Infrastructure_to_foreign_Application_edges_do_not_expand_beyond_baseline`,
`Source_size_inventory_evidence_exists_and_matches_scan_count`). The baselines
`Baselines/tmar-source-size-baseline.json` and
`Baselines/tmar-infra-to-foreign-application.json` are stale for multiple modules
that were already evacuated in prior accepted tasks (e.g. `FulfillmentDomain.cs`,
`SettlementDomain.cs`, `AdminOrderCompletenessComposer.cs`, `Settlement.Infrastructure`
edges), plus the inventory evidence file `docs/evidence/TB-TMAR-BOUNDARY-V1-R1/source-size-inventory.json`
is outdated. This is pre-existing repository-wide baseline debt that also affects
evacuated modules; the baselines were reverted in this task to avoid unauthorized
broad out-of-scope edits. The AccessControl-specific stale entries are recorded here
as residual for the certification task.

## 10. Certification readiness

Structure repair = COMPLETE for the enumerated root debt.
AccessControl remains **NOT** `COMPLETE_REFERENCE_PATTERN` and **NOT**
`ARCH-COMPLETE-002 STRUCTURE_CERTIFIED`; `certifiedModules` was not touched;
Recovery SoT closure was not performed here.

Blockers for the next certification task:
1. `NO_VALIDATOR_GAP_PENDING` (6 required transport validators absent) — needs an explicit
   Architect decision.
2. Pre-existing stale TMAR baseline JSON + source-size inventory evidence debt.

Expected next task: `TB-TMAR-ACCESSCONTROL-FINAL-CERTIFICATION-AND-SOT-CLOSURE-001`.
