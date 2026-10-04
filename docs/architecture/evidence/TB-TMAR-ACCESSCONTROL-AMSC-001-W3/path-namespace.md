# Path ↔ namespace + alias/shim proof — AccessControl (W3)

## Method

For every production `.cs` under `src/backend/Modules/AccessControl/**` (excluding `obj/`, `bin/`),
the declared namespace was compared against the path-derived namespace `<ProjectName>[.<folder>…]`.

## Result

```text
production .cs scanned   = 79
namespace mismatches     = 0
Path-Namespace-State     = EXACT
```

Independent re-derivation is enforced by
`AccessControlManifestDiskReconciliationGuardTests.AccessControl_production_paths_match_namespaces_exactly`
and by the shared `TmarCompleteReferenceStructureGateTests.AssertNamespaceAlignment`.

Representative rows:

| File | Declared namespace |
| --- | --- |
| `Application/Models/AccessOwnerScope.cs` | `Tooba.AccessControl.Application.Models` |
| `Application/Roles/Models/CreateRoleRequest.cs` | `Tooba.AccessControl.Application.Roles.Models` |
| `Application/Permissions/Models/RolePermissionGrant.cs` | `Tooba.AccessControl.Application.Permissions.Models` |
| `Application/Assignments/Models/UserRoleAssignmentDto.cs` | `Tooba.AccessControl.Application.Assignments.Models` |
| `Application/Ceiling/Models/SellerCeilingEntryDto.cs` | `Tooba.AccessControl.Application.Ceiling.Models` |
| `Application/Access/Models/EffectiveAccessDto.cs` | `Tooba.AccessControl.Application.Access.Models` |
| `Application/Validation/AccessControlException.cs` | `Tooba.AccessControl.Application.Validation` |
| `Contracts/Errors/AccessControlErrorCodes.cs` | `Tooba.AccessControl.Contracts.Errors` |
| `Domain/Aggregates/AccessRole.cs` | `Tooba.AccessControl.Domain.Aggregates` |
| `Endpoints/Admin/AccessControlAdminEndpoints.cs` | `Tooba.AccessControl.Endpoints.Admin` |
| `Endpoints/Seller/Development/SellerDevContextEndpoints.cs` | `Tooba.AccessControl.Endpoints.Seller.Development` |
| `Infrastructure/Adapters/Security/PlatformEffectiveAccessReader.cs` | `Tooba.AccessControl.Infrastructure.Adapters.Security` |
| `Infrastructure/Persistence/Migrations/20260827140753_InitialAccessControl.cs` | `Tooba.AccessControl.Infrastructure.Persistence.Migrations` |
| `Endpoints/AccessControlEndpointModule.cs` (root) | `Tooba.AccessControl.Endpoints` |
| `Infrastructure/AccessControlModule.cs` (root) | `Tooba.AccessControl.Infrastructure` |

## Alias / shim proof

| Workaround | Count |
| --- | --- |
| `using X = Y;` namespace alias hiding folder debt | `ZERO` |
| `global using` alias workaround | `ZERO` |
| `TypeForwardedTo` | `ZERO` |
| Duplicate compatibility type | `ZERO` |
| Foreign-module global alias | `ZERO` |
| `GlobalUsings.cs` in the module | `ZERO` (not present; no namespace exemption exercised) |

## Locked exemptions

Only the repository's existing EF exemptions apply (`Persistence/Migrations/*`,
`*ModelSnapshot.cs`), and even those are already path-exact.
