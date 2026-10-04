# Validator coverage — AccessControl (W3)

## Classification

| Aspect | State |
| --- | --- |
| Coverage classification | `EXHAUSTIVE` |
| `VALIDATOR_REQUIRED` | 6 |
| `NO_VALIDATOR_REQUIRED` | 14 (13 production + 1 development-only) |
| Unclassified endpoint request | `ZERO` |
| Durable coverage guard | `AccessControlValidatorTests` |

## Required validators (6)

| Request | Validator | File | Namespace |
| --- | --- | --- | --- |
| `CreateRoleCommand` | `CreateRoleCommandValidator` | `Roles/Validators/CreateRoleCommandValidator.cs` | `Tooba.AccessControl.Application.Roles.Validators` |
| `UpdateRoleCommand` | `UpdateRoleCommandValidator` | `Roles/Validators/UpdateRoleCommandValidator.cs` | `Tooba.AccessControl.Application.Roles.Validators` |
| `CloneRoleCommand` | `CloneRoleCommandValidator` | `Roles/Validators/CloneRoleCommandValidator.cs` | `Tooba.AccessControl.Application.Roles.Validators` |
| `AssignRoleCommand` | `AssignRoleCommandValidator` | `Assignments/Validators/AssignRoleCommandValidator.cs` | `Tooba.AccessControl.Application.Assignments.Validators` |
| `SetRolePermissionsCommand` | `SetRolePermissionsCommandValidator` | `Permissions/Validators/SetRolePermissionsCommandValidator.cs` | `Tooba.AccessControl.Application.Permissions.Validators` |
| `SetSellerCeilingCommand` | `SetSellerCeilingCommandValidator` | `Ceiling/Validators/SetSellerCeilingCommandValidator.cs` | `Tooba.AccessControl.Application.Ceiling.Validators` |

Each validator is discoverable through `AddToobaCqrsFoundation` DI — asserted by
`AccessControlValidatorTests.Six_required_validators_resolve_via_foundation_DI_and_13_requests_have_none`.

## `NO_VALIDATOR_REQUIRED` (14) with durable reason

| Request | Reason |
| --- | --- |
| `ArchiveRoleCommand` | route-bound `{roleId:guid}` only; existence/mutability enforced in Application |
| `RemoveAssignmentCommand` | route-bound `{assignmentId:guid}` only |
| `EnsureAccessControlBootstrapCommand` | admin-only, no transport input beyond ambient tenant/actor |
| `GetEffectiveAccessQuery` | read-only, route/query-bound identifiers |
| `GetRoleQuery` | read-only, route-bound identifier |
| `GetRolePermissionsQuery` | read-only, route-bound identifier |
| `GetSellerCeilingQuery` | read-only, route-bound seller id |
| `ListAssignmentsQuery` | read-only, optional filter |
| `ListRolesQuery` | read-only, boolean flag |
| `ListPermissionCatalogQuery` | read-only, no input |
| `ListSellerPermissionCatalogQuery` | read-only, no input |
| `SearchAccessUsersQuery` | read-only, optional search text bounded by Application |
| `ListScopeResourcesQuery` | read-only, optional search text bounded by Application |
| `GetSellerDevContextsQuery` | development-only surface, no transport input |

Business rules (role existence/mutability, code conflict, ceiling, escalation, catalog membership,
category existence, assignment uniqueness) remain Application/Domain owned and are intentionally not
duplicated in transport validators — asserted by
`AccessControlValidatorTests.Validators_only_declare_transport_shape_rules`.

## Stable machine codes (not localized text)

Validators emit stable codes, not prose:

```text
accesscontrol.validation.role_name_required
accesscontrol.validation.role_code_shape
```

Asserted by `AccessControlValidatorTests.Validators_use_stable_machine_readable_codes`, which also
asserts the codes are never blank and never the generic `validation.failed`.
`Application/Validation/AccessControlValidationCodes.cs` owns the vocabulary and
`Application/Validation/AccessControlFluentRules.cs` holds the reusable fragments.

## Structural placement

Validator files sit under capability `Validators/` folders with matching namespaces, never under
`Commands/` or `Queries/` — asserted by
`AccessControlValidatorTests.Validator_files_sit_under_capability_Validators_folders_with_matching_namespaces`.

## Guard strength

`AccessControlValidatorTests` fails if a new request bypasses classification:

- the manifest length must be exactly 19 with 6 required / 13 not-required;
- request type names must be unique;
- every `VALIDATOR_REQUIRED` request must resolve a validator through foundation DI;
- every `NO_VALIDATOR_REQUIRED` request must resolve **no** validator.
