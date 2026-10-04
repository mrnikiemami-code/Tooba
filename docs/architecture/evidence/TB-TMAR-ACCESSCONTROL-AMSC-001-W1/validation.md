# Validation evidence — AccessControl (AMSC-001 W1 Migrate)

Base: `a3ba1a4f` (W0) → W1 working tree.

## Focused builds

| Command | Result |
| --- | --- |
| `dotnet build src/backend/Tooba.slnx` | **PASS — 0 errors** (60 pre-existing warnings, none new from AccessControl) |
| `dotnet build src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj` | **PASS — 0 errors** |

## Focused tests

| Filter | Result |
| --- | --- |
| `FullyQualifiedName~AccessControl` (Tooba.Host.Tests) | **PASS — 35 passed, 4 skipped, 0 failed** |

Before W1 the same filter was `Failed: 1, Passed: 29, Skipped: 4` — the single failure was W0 finding F4
(stale solution-guard parser). W1 repaired the parser and the guard now passes against the canonical
`/Modules/AccessControl/` grouping.

The 4 skips are Docker/Testcontainers-gated AccessControl persistence tests (environmental, unchanged).

| Filter | Result |
| --- | --- |
| `~AccessControlFoundationTests` / `~AccessControlRuntimeScopeTests` / `~AccessControlValidatorTests` | PASS |
| `~AccessControlModuleAmcW1SolutionGuardTests` | PASS (repaired parser) |
| `~AccessControlModuleAmcW2SemanticGuardTests` | PASS (retargeted to `Validation/`) |
| `~AccessControlModuleAmcW3ResultGuardTests` | PASS |
| `~AccessControlModuleAmcW4StructureGuardTests` | PASS (retargeted) |
| `~AccessControlModuleAmcW5CertGuardTests` | PASS |
| `~AccessControlModuleAmc002W2CertGuardTests` | PASS |
| `~AccessControlStructureRepair001GuardTests` | PASS (Validation consolidation) |
| `~AccessControlStructureRecert001GuardTests` | PASS |
| `~AccessControlModuleAmsc001W1MigrateGuardTests` (new) | PASS |
| `~SettingsFoundationTests` / `~ContentPermissionEnforcementTests` / `~CatalogAttributeSchemaTests` / `~CatalogCategoryFacetTests` / `~SupportFoundationTests` / `~WalletFoundationTests` | PASS |

## Post-change re-scan (all ZERO)

| Check | Result |
| --- | --- |
| `"access.<code>"` literals in module outside `Contracts/Errors` + `.resx` | **0** |
| `using Tooba.AccessControl.Domain` in Endpoints | **0** |
| `using Tooba.AccessControl.Infrastructure` in Endpoints | **0** |
| `Results.(Json\|BadRequest\|Problem)` in Endpoints | **0** |
| `catch (AccessControlException` in Endpoints | **0** |
| foreign `*.Application` / `*.Infrastructure` / `*.Domain` usings in module | **0** |
| `Console.WriteLine` / `Debug.WriteLine` in module | **0** |
| leftover `CreateAccessRoleCommand` / `UpdateAccessRoleCommand` / `CloneAccessRoleCommand` | **0** (only inside the two guards that assert their absence) |
| leftover `AccessControlDtos` | **0** (only inside guards asserting its absence) |
| leftover `Application.Exceptions` / `Application.Validators` namespace refs | **0** |
| duplicate `using` directives in `AccessControlDirectory.cs` | **0** |

## Path ↔ namespace

All 11 new model files and 3 relocated `Validation/` files carry path-derived namespaces exactly:

```text
Application/Models/AccessOwnerScope.cs                        -> Tooba.AccessControl.Application.Models
Application/Roles/Models/AccessRoleDto.cs                     -> Tooba.AccessControl.Application.Roles.Models
Application/Roles/Models/CreateRoleRequest.cs                 -> Tooba.AccessControl.Application.Roles.Models
Application/Roles/Models/UpdateRoleRequest.cs                 -> Tooba.AccessControl.Application.Roles.Models
Application/Roles/Models/CloneRoleRequest.cs                  -> Tooba.AccessControl.Application.Roles.Models
Application/Permissions/Models/RolePermissionGrant.cs         -> Tooba.AccessControl.Application.Permissions.Models
Application/Assignments/Models/UserRoleAssignmentDto.cs       -> Tooba.AccessControl.Application.Assignments.Models
Application/Ceiling/Models/SellerCeilingEntryDto.cs           -> Tooba.AccessControl.Application.Ceiling.Models
Application/Access/Models/EffectiveAccessDto.cs               -> Tooba.AccessControl.Application.Access.Models
Application/Access/Models/EffectivePermissionDto.cs           -> Tooba.AccessControl.Application.Access.Models
Application/Access/Models/AccessUserHitDto.cs                 -> Tooba.AccessControl.Application.Access.Models
Application/Validation/AccessControlException.cs              -> Tooba.AccessControl.Application.Validation
Application/Validation/AccessControlFluentRules.cs            -> Tooba.AccessControl.Application.Validation
Application/Validation/AccessControlValidationCodes.cs        -> Tooba.AccessControl.Application.Validation
```

Enforced machine-side by `TmarCompleteReferenceStructureGateTests.AssertNamespaceAlignment` (global) and
`AccessControlModuleAmsc001W1MigrateGuardTests` (module-scoped).

## Known pre-existing, out-of-scope RED (unchanged at this HEAD)

| Test | Cause | Relation to W1 |
| --- | --- | --- |
| `TmarCompleteReferenceStructureGateTests.Manifest_is_well_formed_and_only_declared_modules_are_certified` | manifest declares `Catalog` certified while SoT `structureLock.certifiedModules` does not list it | unrelated module (Catalog) |
| `TmarCompleteReferenceStructureGateTests.Uncertified_modules_are_explicitly_not_claimed` | same Catalog drift | unrelated |
| `TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_namespace_alignment` | `Tooba.Catalog.Contracts.Cart/*` namespace vs declared project name | unrelated |
| `TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable` | same Catalog/SoT divergence | unrelated |
| `TmarSourceSizeAndInfraAppTests` (3 failures) | stale sibling `.tmp-baseline` worktree scanned (W0 F6) | environment |

None of these failures mentions AccessControl, and none is caused by the W1 diff. They were RED at the
W0 baseline as well. Repairing them would require touching another module's certification state, which
this bounded task does not authorize.

## Completion state

`READY_FOR_CERTIFICATION` (migration) with `Structure-Handoff-State = REQUIRED` — W2 (Structure) owns the
final physical/VS gate before W3 Certify.
