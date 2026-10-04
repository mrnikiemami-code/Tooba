# Final physical tree — AccessControl (W3)

## Module production surface

```text
src/backend/Modules/AccessControl/
├── Tooba.AccessControl.Contracts/            (5 files)
│   ├── Access/       AccessControlEffectiveAccessContracts.cs
│   ├── Development/  AccessControlDevelopmentSeedContracts.cs
│   ├── Enums/        AccessOwnerScopeKind.cs  AccessScopeKind.cs
│   ├── Errors/       AccessControlErrorCodes.cs
│   └── Readiness/    AuthorizationReadinessContracts.cs
├── Tooba.AccessControl.Domain/               (5 files)
│   └── Aggregates/   AccessRole.cs  RolePermission.cs  UserRoleAssignment.cs
│                     PlatformSellerCeiling.cs  AccessAuditEvent.cs
├── Tooba.AccessControl.Application/          (43 files)
│   ├── Access/          Models/ (3)  Queries/ (3)
│   ├── Assignments/     Commands/ (2)  Models/ (1)  Queries/ (1)  Validators/ (1)
│   ├── Authorization/   AccessControlCapabilityGate.cs
│   ├── Bootstrap/       Commands/ (1)
│   ├── Ceiling/         Commands/ (1)  Models/ (1)  Queries/ (1)  Validators/ (1)
│   ├── Composition/     AccessControlOperation.cs
│   ├── Development/     Seller/ (3)
│   ├── Models/          AccessOwnerScope.cs
│   ├── Permissions/     Commands/ (1)  Models/ (1)  Queries/ (3)  Validators/ (1)
│   │                    PermissionCatalog.cs
│   ├── Ports/           IAccessControlDirectory.cs
│   ├── Roles/           Commands/ (4)  Models/ (4)  Queries/ (2)  Validators/ (3)
│   └── Validation/      AccessControlException.cs  AccessControlFluentRules.cs
│                        AccessControlValidationCodes.cs
├── Tooba.AccessControl.Infrastructure/       (18 files)
│   ├── AccessControlModule.cs
│   ├── Adapters/            AccessControlEffectiveAccessReader.cs
│   │                        AuthorizationReadinessProbe.cs
│   ├── Adapters/Security/   PlatformEffectiveAccessReader.cs
│   ├── Authorization/       AuthorizationAdapters.cs  AuthorizationInstrumentation.cs
│   │                        AuthorizationRegistration.cs  SpiceDbAuthorizationAdapter.cs
│   │                        SpiceDbAuthorizationBootstrapper.cs  SpiceDbAuthorizationOptions.cs
│   ├── Development/         AccessControlDevelopmentSeedPrelude.cs
│   ├── Development/Seller/  SellerDevContextBootstrap.cs
│   ├── Directories/         AccessControlDirectory.cs
│   ├── Messaging/           AccessControlOutboxRegistration.cs
│   ├── Observability/       AccessControlInstrumentation.cs
│   ├── Persistence/         AccessControlDbContext.cs
│   └── Persistence/Migrations/  20260827140753_InitialAccessControl.cs
│                                20260827140753_InitialAccessControl.Designer.cs
│                                20260827181000_AddSellerCeilingScope.cs
│                                AccessControlDbContextModelSnapshot.cs
└── Tooba.AccessControl.Endpoints/            (8 files)
    ├── AccessControlEndpointModule.cs
    ├── Admin/       AccessControlAdminEndpoints.cs  AccessControlAdminSellerEndpoints.cs
    ├── Errors/      AccessControlErrorCatalogContributor.cs  AccessControlHttpErrors.cs
    ├── Resources/   AccessControlErrorResources.cs
    │                AccessControlErrors.resx  AccessControlErrors.fa.resx
    ├── Seller/      AccessControlSellerEndpoints.cs
    └── Seller/Development/  SellerDevContextEndpoints.cs
```

## Totals

| Project | Production `.cs` | Root `.cs` |
| --- | --- | --- |
| `Tooba.AccessControl.Contracts` | 5 | 0 |
| `Tooba.AccessControl.Domain` | 5 | 0 |
| `Tooba.AccessControl.Application` | 43 | 0 |
| `Tooba.AccessControl.Infrastructure` | 18 | 1 (`AccessControlModule.cs`) |
| `Tooba.AccessControl.Endpoints` | 8 | 1 (`AccessControlEndpointModule.cs`) |
| **Total** | **79 `.cs`** | **2** |

Plus 2 `.resx` localization resources and 1 `.csproj` per project.

## Generated / non-production exclusions

- `Persistence/Migrations/*.Designer.cs` and `*ModelSnapshot.cs` are EF-generated and excluded from
  size/cohesion classification per the repository locks.
- No `Tooba.AccessControl.Tests` project exists (W0 finding `F5`, out of scope).
