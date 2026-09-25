# TB-TMAR-ACCESSCONTROL-PRECERT-VALIDATORS-001 — Transport-validator coverage evidence

Parent: `TB-TMAR-ACCESSCONTROL-PRECERT-STRUCTURE-REPAIR-001` (commit `681e3639998e1090c7489e2db69c77680502ca26`)
Track: `ACCESSCONTROL_PRECERT`
Status: PASS (validator closure only — NOT certification)

## 1. Endpoint-reachable request inventory (exactly 19)

| # | Request | Surface | Classification |
|---|---------|---------|----------------|
| 1 | `CreateRoleCommand` | Admin / AdminSeller / Seller | VALIDATOR_REQUIRED — PRESENT |
| 2 | `UpdateRoleCommand` | Admin / AdminSeller / Seller | VALIDATOR_REQUIRED — PRESENT |
| 3 | `CloneRoleCommand` | Admin / AdminSeller / Seller | VALIDATOR_REQUIRED — PRESENT |
| 4 | `AssignRoleCommand` | Admin / AdminSeller / Seller | VALIDATOR_REQUIRED — PRESENT |
| 5 | `SetRolePermissionsCommand` | Admin / AdminSeller / Seller | VALIDATOR_REQUIRED — PRESENT |
| 6 | `SetSellerCeilingCommand` | AdminSeller | VALIDATOR_REQUIRED — PRESENT |
| 7 | `ArchiveRoleCommand` | Admin / Seller | NO_VALIDATOR_REQUIRED |
| 8 | `RemoveAssignmentCommand` | Admin / Seller | NO_VALIDATOR_REQUIRED |
| 9 | `EnsureAccessControlBootstrapCommand` | bootstrap/composition | NO_VALIDATOR_REQUIRED |
| 10 | `GetEffectiveAccessQuery` | Seller | NO_VALIDATOR_REQUIRED |
| 11 | `GetRoleQuery` | Admin / Seller | NO_VALIDATOR_REQUIRED |
| 12 | `GetRolePermissionsQuery` | Admin / Seller | NO_VALIDATOR_REQUIRED |
| 13 | `GetSellerCeilingQuery` | AdminSeller | NO_VALIDATOR_REQUIRED |
| 14 | `ListAssignmentsQuery` | Admin / AdminSeller / Seller | NO_VALIDATOR_REQUIRED |
| 15 | `ListRolesQuery` | Admin / Seller | NO_VALIDATOR_REQUIRED |
| 16 | `ListPermissionCatalogQuery` | Admin / Seller | NO_VALIDATOR_REQUIRED |
| 17 | `ListSellerPermissionCatalogQuery` | Seller | NO_VALIDATOR_REQUIRED |
| 18 | `SearchAccessUsersQuery` | Admin | NO_VALIDATOR_REQUIRED |
| 19 | `ListScopeResourcesQuery` | Admin / Seller | NO_VALIDATOR_REQUIRED |

Totals — endpoint-reachable = **19**, VALIDATOR_REQUIRED = **6**, Validators-Present = **6**,
NO_VALIDATOR_REQUIRED = **13**, gap = **0**.

No endpoint inventory drift was observed versus the accepted parent task artifact.
All endpoint dispatches still use `ISender`; no request type was added or removed.

## 2. Exact validator file paths

| Validator | Physical path (relative to repo root) | Namespace |
|-----------|--------------------------------------|-----------|
| `CreateRoleCommandValidator` | `src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Validators/Role/CreateRoleCommandValidator.cs` | `Tooba.AccessControl.Application.Validators.Role` |
| `UpdateRoleCommandValidator` | `.../Validators/Role/UpdateRoleCommandValidator.cs` | `Tooba.AccessControl.Application.Validators.Role` |
| `CloneRoleCommandValidator` | `.../Validators/Role/CloneRoleCommandValidator.cs` | `Tooba.AccessControl.Application.Validators.Role` |
| `AssignRoleCommandValidator` | `.../Validators/Assignment/AssignRoleCommandValidator.cs` | `Tooba.AccessControl.Application.Validators.Assignment` |
| `SetRolePermissionsCommandValidator` | `.../Validators/Permissions/SetRolePermissionsCommandValidator.cs` | `Tooba.AccessControl.Application.Validators.Permissions` |
| `SetSellerCeilingCommandValidator` | `.../Validators/Ceiling/SetSellerCeilingCommandValidator.cs` | `Tooba.AccessControl.Application.Validators.Ceiling` |

Shared helpers:

- `.../Validators/AccessControlFluentRules.cs` — `Tooba.AccessControl.Application.Validators`
- `.../Validators/AccessControlValidationCodes.cs` — `Tooba.AccessControl.Application.Validators`

Paths and namespaces match exactly (verified by test
`Validator_files_sit_under_Validators_folder_with_matching_namespaces`).

## 3. Rule classification per validator (transport shape only)

| Validator | Transport rules | Deliberately NOT validated here |
|-----------|-----------------|---------------------------------|
| `CreateRoleCommandValidator` | `Name` non-blank; `Name` trimmed length ≤ 128; `Code` non-blank; `Code` lexical/length shape (`2..64`, letters/digits/`-`/`_`); `Description` optional but ≤ 512 when supplied | owner resolution, role-code uniqueness, permission ceiling/delegability, ownership |
| `UpdateRoleCommandValidator` | `Name` non-blank; `Name` ≤ 128; `Description` optional ≤ 512 | role existence, role mutability, system-role protection |
| `CloneRoleCommandValidator` | `Name` non-blank; `Name` ≤ 128; `Code` non-blank + lexical/length shape; `Description` optional ≤ 512 | source role existence, source role mutability, clone code uniqueness |
| `AssignRoleCommandValidator` | `UserId` non-empty; `RoleId` non-empty | role existence, archived state, assignment uniqueness, ownership, actor authorization |
| `SetRolePermissionsCommandValidator` | `Grants` envelope non-null; `RoleId` non-empty; each grant `PermissionId` non-blank; each grant `PermissionId` ≤ 128 | `PermissionCatalog` membership, delegability, ceiling, category existence, scope business compatibility |
| `SetSellerCeilingCommandValidator` | `Entries` envelope non-null; `SellerPartyId` non-empty; each entry `PermissionId` non-blank; each entry `PermissionId` ≤ 128 | `PermissionCatalog` membership, delegability, category existence, ceiling semantics |

`ActorUserId`, `TenantId` and `TraceId` are **excluded** from validation because they are supplied by
trusted server context (`IAdminPanelAccess` / `ISellerPanelAccess` / `ICurrentTenant` / `Trace(request)`),
not by the request body, and are therefore not user-payload validation targets.

Length limits are the pre-existing persisted transport maxima, not new business contract:
`access_roles.name` → 128, `access_roles.code` → 64, `access_roles.description` → 512, `permission_id` → 128
(`AccessControlDbContext` property declarations). The lexical code rule (`2..64`, `[A-Za-z0-9_-]`) mirrors the
already-accepted `AccessControlDirectory.RequireCode` contract and is preserved adversarially, not invented.

## 4. Business rules deliberately left in Application/Domain

All of the following remain owned by `AccessControlDirectory` / `AccessControlException` semantics and were
**not** duplicated into FluentValidation:

- role existence (`RequireRoleAsync`)
- role mutability / system-role protection (`EnsureMutable`)
- duplicate role code (`access.role.code_conflict`)
- seller ceiling (`access.escalation.ceiling`)
- permission delegation (`access.escalation.platform_permission`)
- permission catalog membership
- category existence (`IAccessControlScopeResourceLookup.CategoryExistsAsync`)
- escalation
- assignment uniqueness
- domain ownership/state (`ValidateOwner`, `RequireRoleAsync` scope filtering)

The pre-existing `access.validation.text` and `access.validation.code` domain-side shape codes remain in
`AccessControlDirectory` unchanged and are still reachable for values that pass the transport layer.

## 5. Validator discovery mechanism

No custom registration was added. Discovery uses the already-certified platform pattern:

- `ToobaCqrsRegistration.AddToobaCqrsFoundation(...)` in `Tooba.BuildingBlocks/TmarFoundation.cs`
  calls `services.AddValidatorsFromAssembly(assembly)` for every `additionalHandlerAssemblies` entry
  (line 237) and registers `ValidationBehavior<,>` **exactly once** (line 240).
- `Tooba.Host/Program.cs` already passes
  `typeof(Tooba.AccessControl.Application.Commands.EnsureBootstrap.EnsureAccessControlBootstrapCommand).Assembly`
  to `AddToobaCqrsFoundation` (line 156), so the six new validators are auto-discovered from the
  AccessControl Application assembly with zero extra wiring.
- No second validation pipeline, no manual `IValidator` invocation in endpoints or handlers,
  no endpoint/handler reference to FluentValidation.

This is asserted by tests `Six_required_validators_resolve_via_foundation_DI_and_13_requests_have_none`
(real `AddToobaCqrsFoundation` DI resolution for all six, and `null` for all thirteen) and
`Validators_only_declare_transport_shape_rules`.

## 6. Stable validation/error-contract approach

- Transport failures are raised by the existing foundation `ValidationBehavior<,>` as
  `FluentValidation.ValidationException`, unchanged.
- `SafeErrorMapper.MapValidation` maps them to the pre-existing platform envelope
  (`validation.failed` + per-property grouped `ValidationErrors`).
- Each rule supplies a **stable machine-readable error code** from `AccessControlValidationCodes`
  (e.g. `accesscontrol.validation.role_name_required`, `accesscontrol.validation.role_code_shape`).
  No localized text classification, no `ex.Message` parsing.
- Business failures still surface as `AccessControlException` with unchanged stable codes
  (`access.role.code_conflict`, `access.escalation.*`, `access.validation.text`, `access.validation.code`),
  so the previously accepted HTTP error contract is preserved for non-transport-shape failures.

## 7. Host ZERO confirmation

- `src/backend/Host/Tooba.Host/AccessControl` → **absent** (`Test-Path` = False).
- `namespace Tooba.Host.AccessControl` → **ZERO** production occurrences.
- `MapAccessControlEndpoints` → **ZERO** local production occurrences (the only textual hit is the
  negative assertion inside `AccessControlFoundationTests.cs`).
- `Program.cs` contains `app.MapAccessControlModuleEndpoints();` **exactly once** (line 513).

## 8. Structure / root confirmation

| Location | Result |
|----------|--------|
| Application root `.cs` files | **0** (only `.csproj` + `Validators/` + existing capability subfolders) |
| Endpoints root | `AccessControlEndpointModule.cs` only |
| Infrastructure root | `AccessControlModule.cs` only |
| New validators root | `Validators/` with `Role/`, `Assignment/`, `Permissions/`, `Ceiling/` subfolders |

## 9. Boundary audit

| Edge | Result |
|------|--------|
| `AccessControl.Application` → Host | ZERO |
| `AccessControl.Application` → foreign Application/Domain | ZERO (only `Catalog.Contracts`, `Identity.Contracts`, `OperatorProfile.Contracts` — explicitly allowed) |
| `AccessControl.Infrastructure` → `Catalog.Application` | ZERO |
| `AccessControl.Infrastructure` → `Catalog.Domain` | ZERO |
| `AccessControl.Infrastructure` → `Catalog.Contracts` | ALLOWED |
| `AccessControl.Endpoints` → Host | ZERO |
| `AccessControl.Endpoints` → `IAccessControlDirectory` / `AccessControlDirectory` | ZERO |
| `Validators/*` → Domain/Directory/PermissionCatalog/Catalog lookup | ZERO |

## 10. Focused builds / tests

| Command | Result |
|---------|--------|
| `dotnet build .../Tooba.AccessControl.Application.csproj --no-restore` | Build succeeded, 0 errors, 0 warnings |
| `dotnet build .../Tooba.AccessControl.Infrastructure.csproj --no-restore` | Build succeeded, 0 errors (3 pre-existing Catalog XML-comment warnings) |
| `dotnet build .../Tooba.AccessControl.Endpoints.csproj --no-restore` | Build succeeded, 0 errors, 0 warnings |
| `dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore` | Build succeeded, 0 errors (8 pre-existing warnings) |
| `dotnet test Tooba.Host.Tests --filter FullyQualifiedName~AccessControlValidatorTests\|FullyQualifiedName~AccessControl_module_boundary_static_checks` | Passed **12**, Failed 0, Skipped 0 |

No solution build, no broad integration suite, no broad architecture suite, no retry loop.

Test location rationale: AccessControl has no dedicated `.Tests` project; the narrowest available
low-cost location is `Tooba.Host.Tests`, which already references the AccessControl module chain and
hosts the existing `AccessControlFoundationTests` boundary guard. The added tests are pure in-memory
validator/DI tests with no database or web host, so they add negligible infrastructure cost.

## 11. Repository-wide baseline debt (pre-existing — NOT fixed here)

`Tooba.Host.Tests/Architecture/TmarSourceSizeAndInfraAppTests.cs` remains red at this revision because
`Baselines/tmar-source-size-baseline.json`, `Baselines/tmar-infra-to-foreign-application.json` and
`docs/evidence/TB-TMAR-BOUNDARY-V1-R1/source-size-inventory.json` are stale from previously accepted
module evacuations. Per task instruction these three artifacts were **not modified**. Recorded as
pre-existing repository-wide debt only.

## 12. Certification readiness

`Validator-Gap-State = CLOSED_6_OF_6`.

AccessControl remains **NOT** `COMPLETE_REFERENCE_PATTERN` and **NOT**
`ARCH-COMPLETE-002 STRUCTURE_CERTIFIED`; `certifiedModules` was not touched and Recovery SoT closure
was not performed here.

Remaining blockers for the next certification task:

1. Pre-existing stale TMAR baseline JSON + source-size inventory evidence debt (repository-wide).
2. AccessControl-specific stale baseline entries produced by the earlier Host evacuation and
   catalogue-boundary repair (to be absorbed in the certification task's authorized baseline refresh).

Expected next task: `TB-TMAR-ACCESSCONTROL-FINAL-CERTIFICATION-AND-SOT-CLOSURE-001`.
