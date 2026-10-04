# Capability map — AccessControl (W0)

Capabilities are discovered from the module's existing capability roots, endpoint audiences and
Contracts/Domain aggregates. No capability name was invented mechanically from a Command type name.

| Capability | Application root | Endpoints audience | Contracts / Domain surface |
| --- | --- | --- | --- |
| `Roles` | `Application/Roles/{Commands,Queries,Validators}` | Admin + Seller (`/roles*`) | `Domain/Aggregates/AccessRole.cs` |
| `Assignments` | `Application/Assignments/{Commands,Queries,Validators}` | Admin + Seller (`/assignments*`) | `Domain/Aggregates/UserRoleAssignment.cs` |
| `Permissions` | `Application/Permissions/{Commands,Queries,Validators}` + `PermissionCatalog.cs` | Admin + Seller (`/permissions`, `/roles/{id}/permissions`) | `Domain/Aggregates/RolePermission.cs` |
| `Ceiling` | `Application/Ceiling/{Commands,Queries,Validators}` | Admin-seller + Seller (`/ceiling`) | `Domain/Aggregates/PlatformSellerCeiling.cs` |
| `Access` | `Application/Access/Queries` | Admin + Seller (`/me/capabilities`, `/users`, `/scope-resources/*`) | `Contracts/Access/AccessControlEffectiveAccessContracts.cs` |
| `Bootstrap` | `Application/Bootstrap/Commands` | Admin (`/bootstrap`) | `Domain/Aggregates/{AccessRole,RolePermission,UserRoleAssignment}` |
| `Development/Seller` | `Application/Development/Seller` | Seller (`/v1/seller/dev-contexts`) | `Contracts/Development/AccessControlDevelopmentSeedContracts.cs` |
| `Authorization` (cross-cutting) | `Application/Authorization/AccessControlCapabilityGate.cs` | all audiences | `Contracts/Readiness`, `Infrastructure/Authorization` |
| `Audit` (cross-cutting) | — | — | `Domain/Aggregates/AccessAuditEvent.cs` |

## Audience → route groups

| Group | Prefix | Mapped by |
| --- | --- | --- |
| Admin | `/v1/admin/access-control` | `Admin/AccessControlAdminEndpoints.cs` |
| Admin-for-seller | `/v1/admin/sellers/{sellerId:guid}/access-control` | `Admin/AccessControlAdminSellerEndpoints.cs` |
| Seller | `/v1/seller/access-control` | `Seller/AccessControlSellerEndpoints.cs` |
| Seller (Development) | `/v1/seller` | `Seller/Development/SellerDevContextEndpoints.cs` |

Route count (total `Map*` registrations): **57** (`AccessControlAdminEndpoints` 22,
`AccessControlAdminSellerEndpoints` 13, `AccessControlSellerEndpoints` 21,
`SellerDevContextEndpoints` 1).

## Endpoint-reachable request inventory

20 MediatR requests dispatched through `ISender` from module endpoints:

`CreateRoleCommand`, `UpdateRoleCommand`, `CloneRoleCommand`, `ArchiveRoleCommand`,
`AssignRoleCommand`, `RemoveAssignmentCommand`, `SetRolePermissionsCommand`,
`SetSellerCeilingCommand`, `EnsureAccessControlBootstrapCommand`, `GetRoleQuery`, `ListRolesQuery`,
`GetRolePermissionsQuery`, `ListPermissionCatalogQuery`, `ListSellerPermissionCatalogQuery`,
`ListAssignmentsQuery`, `GetSellerCeilingQuery`, `GetEffectiveAccessQuery`,
`SearchAccessUsersQuery`, `ListScopeResourcesQuery`, `GetSellerDevContextsQuery`.

## Validator classification matrix

| Request | Classification |
| --- | --- |
| `CreateRoleCommand` | `VALIDATOR_REQUIRED` — `CreateRoleCommandValidator` |
| `UpdateRoleCommand` | `VALIDATOR_REQUIRED` — `UpdateRoleCommandValidator` |
| `CloneRoleCommand` | `VALIDATOR_REQUIRED` — `CloneRoleCommandValidator` |
| `AssignRoleCommand` | `VALIDATOR_REQUIRED` — `AssignRoleCommandValidator` |
| `SetRolePermissionsCommand` | `VALIDATOR_REQUIRED` — `SetRolePermissionsCommandValidator` |
| `SetSellerCeilingCommand` | `VALIDATOR_REQUIRED` — `SetSellerCeilingCommandValidator` |
| `ArchiveRoleCommand` | `NO_VALIDATOR_REQUIRED` — no transport payload beyond route id |
| `RemoveAssignmentCommand` | `NO_VALIDATOR_REQUIRED` — route id only |
| `EnsureAccessControlBootstrapCommand` | `NO_VALIDATOR_REQUIRED` — actor derived from auth context |
| `GetEffectiveAccessQuery` | `NO_VALIDATOR_REQUIRED` — route id only |
| `GetRoleQuery` | `NO_VALIDATOR_REQUIRED` — route id only |
| `GetRolePermissionsQuery` | `NO_VALIDATOR_REQUIRED` — route id only |
| `GetSellerCeilingQuery` | `NO_VALIDATOR_REQUIRED` — route id only |
| `ListAssignmentsQuery` | `NO_VALIDATOR_REQUIRED` — optional filter |
| `ListRolesQuery` | `NO_VALIDATOR_REQUIRED` — optional filter |
| `ListPermissionCatalogQuery` | `NO_VALIDATOR_REQUIRED` — no payload |
| `ListSellerPermissionCatalogQuery` | `NO_VALIDATOR_REQUIRED` — no payload |
| `SearchAccessUsersQuery` | `NO_VALIDATOR_REQUIRED` — optional search text |
| `ListScopeResourcesQuery` | `NO_VALIDATOR_REQUIRED` — optional search text |
| `GetSellerDevContextsQuery` | `NO_VALIDATOR_REQUIRED` — no payload |

Coverage: **6 / 20 `VALIDATOR_REQUIRED`, 6 present, 0 missing**; 14 `NO_VALIDATOR_REQUIRED` with
durable explicit reasons. Guarded by `Tooba.Host.Tests/AccessControlValidatorTests.cs`.

Validators emit stable machine codes from `AccessControlValidationCodes`
(`accesscontrol.validation.*`) and never localized text.
