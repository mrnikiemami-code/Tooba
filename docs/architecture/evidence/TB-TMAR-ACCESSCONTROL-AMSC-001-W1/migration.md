# TB-TMAR-ACCESSCONTROL-AMSC-001 — W1 MIGRATE

- Task: `TB-TMAR-ACCESSCONTROL-AMSC-001-W1`
- Module: `Modules/AccessControl`
- Skill: `tooba-architecture-migrate` (second of four: Analyze → **Migrate** → Structure → Certify)
- Starting base: `a3ba1a4f` (W0 analyze, `HEAD == origin/main`)
- Behavior-Preservation-Risk: `LOW` (split + rename + constant substitution; no route/status/DTO/schema change)
- Structure handoff: `Structure-Handoff-State = REQUIRED` (final physical gate owned by W2)

## 1. What this wave changed

| # | Change | Kind |
| --- | --- | --- |
| 1 | `Application/Models/AccessControlDtos.cs` (8 mixed types) split into capability `Models/` files | cohesion split |
| 2 | Three command-shaped transport bodies renamed to `*Request` and co-located with `Roles` | rename + relocation |
| 3 | `Application/Exceptions/` + `Application/Validators/` merged into a single `Application/Validation/` | folder consolidation |
| 4 | 20 raw `access.*` literals in `AccessControlDirectory.cs` replaced by `AccessControlErrorCodes.*`; duplicate `using` removed | duplicate-SoT removal |
| 5 | Consumers updated (Endpoints / Infrastructure / Host.Tests) | consumer repair |
| 6 | Stale/obsolete durable guards repaired to their documented intent | guard correctness |
| 7 | New durable cohesion guard + manifest allowlist/forbidden-folder update | durable enforcement |

## 2. Cohesion split — `AccessControlDtos.cs`

The 76-LOC file declared eight unrelated top-level types with different reasons to change. It is gone;
each type now lives in its true capability folder:

| Type | New path | Namespace |
| --- | --- | --- |
| `AccessOwnerScope` | `Models/AccessOwnerScope.cs` | `Tooba.AccessControl.Application.Models` |
| `AccessRoleDto` | `Roles/Models/AccessRoleDto.cs` | `Tooba.AccessControl.Application.Roles.Models` |
| `CreateAccessRoleCommand` → **`CreateRoleRequest`** | `Roles/Models/CreateRoleRequest.cs` | `Tooba.AccessControl.Application.Roles.Models` |
| `UpdateAccessRoleCommand` → **`UpdateRoleRequest`** | `Roles/Models/UpdateRoleRequest.cs` | `…Roles.Models` |
| `CloneAccessRoleCommand` → **`CloneRoleRequest`** | `Roles/Models/CloneRoleRequest.cs` | `…Roles.Models` |
| `RolePermissionGrant` | `Permissions/Models/RolePermissionGrant.cs` | `…Permissions.Models` |
| `UserRoleAssignmentDto` | `Assignments/Models/UserRoleAssignmentDto.cs` | `…Assignments.Models` |
| `SellerCeilingEntryDto` | `Ceiling/Models/SellerCeilingEntryDto.cs` | `…Ceiling.Models` |
| `EffectiveAccessDto` | `Access/Models/EffectiveAccessDto.cs` | `…Access.Models` |
| `EffectivePermissionDto` | `Access/Models/EffectivePermissionDto.cs` | `…Access.Models` |
| `AccessUserHitDto` | `Access/Models/AccessUserHitDto.cs` | `…Access.Models` |

`AccessOwnerScope` is the only cross-capability Application-internal primitive and legitimately keeps the
shared `Models/` root; every capability-owned DTO now sits under its capability.

### Why the transport bodies were renamed (not moved unchanged)

`CreateAccessRoleCommand` / `UpdateAccessRoleCommand` / `CloneAccessRoleCommand` were **HTTP request
bodies**, not MediatR requests, yet they shared the CQRS `*Command` vocabulary with the authoritative
requests in `Roles/Commands/`. That collision is exactly the "duplicate command/query shape in
Application Models beside the authoritative MediatR request" anti-pattern. They are now explicitly
transport-shaped (`*Request`) and live in `Roles/Models/`, so the `*Command` namespace is owned
exclusively by the real `IRequest<T>` types.

Field shapes are byte-for-byte identical (same names, same types, same nullability, same defaults), so
HTTP binding and JSON contracts are unchanged.

## 3. Validation consolidation

| Before | After |
| --- | --- |
| `Application/Exceptions/AccessControlException.cs` (single-file folder) | `Application/Validation/AccessControlException.cs` |
| `Application/Validators/AccessControlFluentRules.cs` | `Application/Validation/AccessControlFluentRules.cs` |
| `Application/Validators/AccessControlValidationCodes.cs` | `Application/Validation/AccessControlValidationCodes.cs` |

`Application/Exceptions/` and `Application/Validators/` no longer exist. Capability-scoped validators
(`Roles/Validators/`, `Assignments/Validators/`, `Permissions/Validators/`, `Ceiling/Validators/`) were
**not** touched: they stay next to the request they validate (capability-first), which the existing
`AccessControlStructureRepair001GuardTests` and `AccessControlValidatorTests` already enforce.

`Tooba.AccessControl.Application.Validation` is the module-local analogue of the certified `Offer` /
`Order` `Application/Validation/` precedent — no new pattern was invented.

## 4. Duplicate source of truth removed

`Infrastructure/Directories/AccessControlDirectory.cs` emitted 20 stable codes as raw string literals
(`"access.role.code_conflict"`, `"access.scope.unsupported"`, …) while the module already owned the
typed constants in `Contracts/Errors/AccessControlErrorCodes.cs`. All 19 `throw` sites now reference
`AccessControlErrorCodes.*` (the 20th occurrence was the duplicate `using` line).

```csharp
throw new AccessControlException(AccessControlErrorCodes.RoleCodeConflict);
```

The duplicate `using Tooba.AccessControl.Contracts.Enums;` (lines 8 and 9) was removed, and the whole
using block was rebuilt canonically.

## 5. Move table (executed)

| From | To |
| --- | --- |
| `Application/Models/AccessControlDtos.cs` | **deleted** (split into 11 capability files) |
| `Application/Exceptions/AccessControlException.cs` | `Application/Validation/AccessControlException.cs` |
| `Application/Validators/AccessControlFluentRules.cs` | `Application/Validation/AccessControlFluentRules.cs` |
| `Application/Validators/AccessControlValidationCodes.cs` | `Application/Validation/AccessControlValidationCodes.cs` |

Deleted (now empty): `Application/Exceptions`, `Application/Validators`.

## 6. Consumers updated

| Consumer | Change |
| --- | --- |
| `Endpoints/Admin/AccessControlAdminEndpoints.cs` | `+ Roles.Models`; bodies renamed |
| `Endpoints/Admin/AccessControlAdminSellerEndpoints.cs` | `+ Roles.Models`; bodies renamed |
| `Endpoints/Seller/AccessControlSellerEndpoints.cs` | `+ Roles.Models`; bodies renamed |
| `Endpoints/Errors/AccessControlHttpErrors.cs` | `Exceptions` → `Validation` |
| `Application/Ports/IAccessControlDirectory.cs` | capability `Models` usings; port signatures renamed |
| `Application/Composition/AccessControlOperation.cs` | `Exceptions` → `Validation` |
| `Application/Permissions/PermissionCatalog.cs` | `Exceptions` → `Validation` |
| `Application/**` (20 request/handler files) | capability `Models` usings |
| `Infrastructure/Directories/AccessControlDirectory.cs` | capability `Models` + `Validation` + `Contracts.Errors`; literals → constants |
| `Infrastructure/**` (adapters, module, migrations, DbContext) | capability `Models` usings |
| `Host.Tests/{AccessControlFoundationTests,AccessControlRuntimeScopeTests,AccessControlValidatorTests,FakeAccessControlDirectory}` | capability `Models`/`Validation` usings, renamed bodies |
| `Host.Tests/{Settings,Content,Support,Wallet,Catalog*}FoundationTests` | `Models` → `Roles.Models` where required |

## 7. Behavior preservation

Unchanged by this wave:

- all routes, HTTP verbs, status codes, `Created` locations and audience grouping;
- all 20 stable error-code **values** and their `ErrorDescriptor` catalog entries / localization keys;
- all 22 validation codes and their messages;
- request/response DTO shapes (`AccessRoleDto`, `UserRoleAssignmentDto`, `SellerCeilingEntryDto`,
  `EffectiveAccessDto`, `EffectivePermissionDto`, `AccessUserHitDto`, `RolePermissionGrant`);
- the 20-request CQRS inventory, 6-required / 14-no-validator-required classification, `ISender`-only dispatch;
- `AccessControlException` semantics (stable code only, no localized message);
- `access_control` schema, EF model and the two existing migrations;
- telemetry event names, correlation/trace continuity, logging foundation.

## 8. Guard correctness (not guard weakening)

| Guard | Defect | Repair |
| --- | --- | --- |
| `AccessControlModuleAmcW1SolutionGuardTests` | assumed a flat `<Folder Name="/Modules/">` window that no longer exists → `ArgumentOutOfRangeException` | parses the dedicated `/Modules/AccessControl/` folder, asserts exactly 5 project entries, and asserts the flat dump folder is gone |
| `AccessControlModuleAmcW4StructureGuardTests` | asserted `AccessControlDtos.cs` and `Exceptions/AccessControlException.cs` exist | asserts the dump file is gone, `Models/AccessOwnerScope.cs` exists, `Validation/AccessControlException.cs` exists |
| `AccessControlModuleAmcW2SemanticGuardTests` | read `Exceptions/AccessControlException.cs` | reads `Validation/AccessControlException.cs` |
| `AccessControlStructureRepair001GuardTests` | required `Exceptions/` + `Validators/` to exist | requires `Validation/`, asserts `Exceptions/` and `Validators/` are absent |

New durable guard: `AccessControlModuleAmsc001W1MigrateGuardTests` —
capability-cohesive Models, single shared `Validation/` folder, owned error constants with zero raw
`access.*` literals, no duplicate `using`, no transport type shadowing the CQRS vocabulary, and a
non-stale solution parser.

## 9. Coupling after W1

| Edge | State |
| --- | --- |
| AccessControl → foreign Domain / Application / Infrastructure | `ZERO` |
| AccessControl → foreign `*.Contracts` | legal only (`Identity.Contracts`, `OperatorProfile.Contracts`, `Catalog.Contracts`, `Party.Contracts`) |
| foreign → AccessControl | `*.Contracts` only |
| Cross-module joins | `NONE` |
| Host reference inside AccessControl | `NONE` |
| Host final closure (`HOST_ROOT_FINAL_CERTIFIED`) | `PRESERVED` — no Host production file added or modified |

Microservice extractability is preserved: the module lifts with `Tooba.BuildingBlocks`, `Tooba.Persistence`
platform seams and its declared foreign `*.Contracts` edges only.

## 10. Known pre-existing, out-of-scope failures (unchanged by W1)

- `TmarCompleteReferenceStructureGateTests` / `TmarDurableGuardTests` fail on `Catalog` certification
  drift (manifest declares `Catalog` certified; `Tooba.Catalog.Contracts.Cart/…` namespace mismatch and
  SoT `certifiedModules` list divergence). Catalog is outside this task's module scope.
- `TmarSourceSizeAndInfraAppTests` scans the stale sibling `.tmp-baseline` worktree (W0 finding F6).

Both were already RED at the W0 baseline and are reported, not repaired, to keep the task bounded.
