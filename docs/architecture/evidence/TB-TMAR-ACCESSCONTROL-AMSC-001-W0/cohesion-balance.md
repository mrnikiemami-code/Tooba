# Cohesion balance — AccessControl (W0)

## Largest production files

| LOC | File | Verdict |
| --- | --- | --- |
| 877 | `Infrastructure/Directories/AccessControlDirectory.cs` | `OVERSIZED_ONLY` (baselined `OVERSIZED_LEGACY` @957) |
| 401 | `Endpoints/Seller/AccessControlSellerEndpoints.cs` | `COHESIVE` |
| 401 | `Endpoints/Admin/AccessControlAdminEndpoints.cs` | `COHESIVE` |
| 344 | `Infrastructure/Authorization/SpiceDbAuthorizationAdapter.cs` | `COHESIVE` |
| 341 | `Endpoints/Admin/AccessControlAdminSellerEndpoints.cs` | `COHESIVE` |
| 193 | `Infrastructure/Authorization/AuthorizationAdapters.cs` | `COHESIVE` |
| 177 | `Infrastructure/Authorization/SpiceDbAuthorizationBootstrapper.cs` | `COHESIVE` |
| 147 | `Infrastructure/Development/Seller/SellerDevContextBootstrap.cs` | `COHESIVE` |
| 126 | `Application/Access/Queries/SearchAccessUsersQuery.cs` | `COHESIVE` |
| 119 | `Infrastructure/Authorization/SpiceDbAuthorizationOptions.cs` | `COHESIVE` |
| 116 | `Application/Permissions/PermissionCatalog.cs` | `COHESIVE` |
| 113 | `Infrastructure/Persistence/AccessControlDbContext.cs` | `COHESIVE` |
| 95 | `Application/Validators/AccessControlFluentRules.cs` | `COHESIVE` |
| 76 | `Application/Models/AccessControlDtos.cs` | **`MULTI_RESPONSIBILITY_COHESION_VIOLATION`** |
| ≤ 73 | all remaining production files | `COHESIVE` |

## F1 — `Application/Models/AccessControlDtos.cs` mixed bundle

Eight unrelated top-level types with distinct reasons to change:

| Type | Consumer boundary | Correct capability home |
| --- | --- | --- |
| `AccessOwnerScope` | Application-internal directory port value | `Application/Models/` (shared, legitimate) |
| `CreateAccessRoleCommand` | HTTP body posted by Admin/AdminSeller/Seller endpoints | `Application/Roles/Models/` |
| `UpdateAccessRoleCommand` | HTTP body posted by the same endpoints | `Application/Roles/Models/` |
| `CloneAccessRoleCommand` | HTTP body posted by the same endpoints | `Application/Roles/Models/` |
| `RolePermissionGrant` | grant envelope for `SetRolePermissionsCommand` + directory port | `Application/Permissions/Models/` |
| `AccessRoleDto` | role read model returned by 7 handlers | `Application/Roles/Models/` |
| `UserRoleAssignmentDto` | assignment read model | `Application/Assignments/Models/` |
| `SellerCeilingEntryDto` | ceiling read model | `Application/Ceiling/Models/` |
| `EffectivePermissionDto` | effective-access read model | `Application/Access/Models/` |
| `EffectiveAccessDto` | effective-access read model | `Application/Access/Models/` |
| `AccessUserHitDto` | user-search read model | `Application/Access/Models/` |

Aggravating factor: the three `*AccessRoleCommand` records are **command-shaped** types living in a
shared `Models` dump while their authoritative MediatR requests (`CreateRoleCommand`,
`UpdateRoleCommand`, `CloneRoleCommand`) live in `Roles/Commands/`. They are genuine HTTP body
contracts (not duplicate CQRS requests), but their naming collides with the CQRS vocabulary and
their placement violates capability-first cohesion. W1 renames them to explicit transport-body
semantics and co-locates them with `Roles`.

Verified consumer inventory for the three bodies (must all be updated in W1):

- `Endpoints/Admin/AccessControlAdminEndpoints.cs` — `CreateRoleAsync`, `UpdateRoleAsync`,
  `CloneRoleAsync` parameters.
- `Endpoints/Admin/AccessControlAdminSellerEndpoints.cs` — same three parameters.
- `Endpoints/Seller/AccessControlSellerEndpoints.cs` — same three parameters.
- `Application/Ports/IAccessControlDirectory.cs` — `CreateRoleAsync`, `UpdateRoleAsync`,
  `CloneRoleAsync` port signatures.
- `Infrastructure/Directories/AccessControlDirectory.cs` — port implementation.
- `Application/Roles/Commands/{Create,Update,Clone}RoleCommand.cs` — handler → directory mapping.
- `Application/Permissions/PermissionCatalog.cs` — imports `Application.Models` for
  `AccessOwnerScope` only.
- `Application/Authorization/AccessControlCapabilityGate.cs` — imports `Application.Models` for
  `AccessOwnerScope` only.
- `Infrastructure/Adapters/AccessControlEffectiveAccessReader.cs` — aliases
  `AppScope = Application.Models.AccessOwnerScope`.
- Tests: `Tooba.Host.Tests/{AccessControlFoundationTests,AccessControlRuntimeScopeTests,AccessControlValidatorTests,FakeAccessControlDirectory}.cs`.

## F2 — raw error-code literals in `AccessControlDirectory.cs`

20 occurrences of `new AccessControlException("access.…")` / `new AccessControlException("seller.…")`
that should reference `AccessControlErrorCodes.*`. Codes emitted as literals:

`access.role.code_conflict` (95), `access.role.system_immutable` (164, 790),
`access.user.invalid` (280), `access.role.archived` (286), `access.assignment.exists` (294),
`access.assignment.not_found` (326), `access.ceiling.not_delegable` (385),
`access.scope.unsupported` (390, 820), `access.scope.unknown_resource` (398, 827, 833),
`access.role.not_found` (783), `access.owner.invalid` (798),
`access.escalation.platform_permission` (841), `access.escalation.ceiling` (846),
`access.validation.text` (945), `access.validation.code` (956).

Additional defect in the same file: `using Tooba.AccessControl.Contracts.Enums;` is duplicated on
lines 8 and 9.

## F3 — `Application/Exceptions/` single-file folder

`AccessControlException.cs` (14 LOC) is the module typed-fault vocabulary. It is a cross-capability
Application primitive and should live beside the other shared Application primitives rather than in
a folder of one file. W1 merges `Validators/` + `Exceptions/` into `Validation/`:

- `Validation/AccessControlValidationCodes.cs`
- `Validation/AccessControlFluentRules.cs`
- `Validation/AccessControlException.cs`

## Not a cohesion defect

`Endpoints/Errors/AccessControlHttpErrors.cs` (18 LOC) is currently unreferenced by production code
(the endpoints rely on `Result` + `ApiResponseFactory`). It is not a second mapping path in use; it
is recorded for W3 review, not for deletion inside a behaviour-preserving run without confirming the
`AddAccessControlEndpointPresentation` composition expectation.

## Explicit non-goals

- No split of `AccessControlDirectory.cs`. It is `OVERSIZED_ONLY` with one cohesive responsibility;
  splitting it would be cosmetic and would create artificial parallel decomposition.
- No adoption of `IClock`/`IIdGenerator` in the directory (behaviour change).
- No cosmetic splitting anywhere else.
