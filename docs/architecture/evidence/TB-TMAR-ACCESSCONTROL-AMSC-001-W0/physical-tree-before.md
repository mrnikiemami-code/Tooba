# Physical tree before — AccessControl (HEAD `eea29fb8`)

```text
src/backend/Modules/AccessControl/
├── Tooba.AccessControl.Application/
│   ├── Access/Queries/GetEffectiveAccessQuery.cs
│   ├── Access/Queries/ListScopeResourcesQuery.cs
│   ├── Access/Queries/SearchAccessUsersQuery.cs
│   ├── Assignments/Commands/AssignRoleCommand.cs
│   ├── Assignments/Commands/RemoveAssignmentCommand.cs
│   ├── Assignments/Queries/ListAssignmentsQuery.cs
│   ├── Assignments/Validators/AssignRoleCommandValidator.cs
│   ├── Authorization/AccessControlCapabilityGate.cs
│   ├── Bootstrap/Commands/EnsureAccessControlBootstrapCommand.cs
│   ├── Ceiling/Commands/SetSellerCeilingCommand.cs
│   ├── Ceiling/Queries/GetSellerCeilingQuery.cs
│   ├── Ceiling/Validators/SetSellerCeilingCommandValidator.cs
│   ├── Composition/AccessControlOperation.cs
│   ├── Development/Seller/GetSellerDevContextsQuery.cs
│   ├── Development/Seller/ISellerDevContextStore.cs
│   ├── Development/Seller/SellerDevContextModels.cs
│   ├── Exceptions/AccessControlException.cs
│   ├── Models/AccessControlDtos.cs
│   ├── Permissions/Commands/SetRolePermissionsCommand.cs
│   ├── Permissions/PermissionCatalog.cs
│   ├── Permissions/Queries/GetRolePermissionsQuery.cs
│   ├── Permissions/Queries/ListPermissionCatalogQuery.cs
│   ├── Permissions/Queries/ListSellerPermissionCatalogQuery.cs
│   ├── Permissions/Validators/SetRolePermissionsCommandValidator.cs
│   ├── Ports/IAccessControlDirectory.cs
│   ├── Roles/Commands/ArchiveRoleCommand.cs
│   ├── Roles/Commands/CloneRoleCommand.cs
│   ├── Roles/Commands/CreateRoleCommand.cs
│   ├── Roles/Commands/UpdateRoleCommand.cs
│   ├── Roles/Queries/GetRoleQuery.cs
│   ├── Roles/Queries/ListRolesQuery.cs
│   ├── Roles/Validators/CloneRoleCommandValidator.cs
│   ├── Roles/Validators/CreateRoleCommandValidator.cs
│   ├── Roles/Validators/UpdateRoleCommandValidator.cs
│   ├── Validators/AccessControlFluentRules.cs
│   └── Validators/AccessControlValidationCodes.cs
├── Tooba.AccessControl.Contracts/
│   ├── Access/AccessControlEffectiveAccessContracts.cs
│   ├── Development/AccessControlDevelopmentSeedContracts.cs
│   ├── Enums/AccessOwnerScopeKind.cs
│   ├── Enums/AccessScopeKind.cs
│   ├── Errors/AccessControlErrorCodes.cs
│   └── Readiness/AuthorizationReadinessContracts.cs
├── Tooba.AccessControl.Domain/
│   └── Aggregates/{AccessAuditEvent,AccessRole,PlatformSellerCeiling,RolePermission,UserRoleAssignment}.cs
├── Tooba.AccessControl.Endpoints/
│   ├── AccessControlEndpointModule.cs
│   ├── Admin/AccessControlAdminEndpoints.cs
│   ├── Admin/AccessControlAdminSellerEndpoints.cs
│   ├── Errors/AccessControlErrorCatalogContributor.cs
│   ├── Errors/AccessControlHttpErrors.cs
│   ├── Resources/AccessControlErrorResources.cs
│   ├── Resources/AccessControlErrors.resx
│   ├── Resources/AccessControlErrors.fa.resx
│   ├── Seller/AccessControlSellerEndpoints.cs
│   └── Seller/Development/SellerDevContextEndpoints.cs
└── Tooba.AccessControl.Infrastructure/
    ├── AccessControlModule.cs
    ├── Adapters/AccessControlEffectiveAccessReader.cs
    ├── Adapters/AuthorizationReadinessProbe.cs
    ├── Adapters/Security/PlatformEffectiveAccessReader.cs
    ├── Authorization/{AuthorizationAdapters,AuthorizationInstrumentation,AuthorizationRegistration,SpiceDbAuthorizationAdapter,SpiceDbAuthorizationBootstrapper,SpiceDbAuthorizationOptions}.cs
    ├── Development/AccessControlDevelopmentSeedPrelude.cs
    ├── Development/Seller/SellerDevContextBootstrap.cs
    ├── Directories/AccessControlDirectory.cs
    ├── Messaging/AccessControlOutboxRegistration.cs
    ├── Observability/AccessControlInstrumentation.cs
    └── Persistence/AccessControlDbContext.cs
        └── Migrations/{20260827140753_InitialAccessControl(.Designer),20260827181000_AddSellerCeilingScope,AccessControlDbContextModelSnapshot}.cs
```

Production `.cs` count: **77** (5 + 6 + 5 + 10 + 22 + 29 counting per project as above).
Root `.cs` files: `AccessControlEndpointModule.cs`, `AccessControlModule.cs` only (allowlisted).

## Solution Explorer (`src/backend/Tooba.slnx`)

```text
<Folder Name="/Modules/AccessControl/">
  Tooba.AccessControl.Domain/Tooba.AccessControl.Domain.csproj
  Tooba.AccessControl.Contracts/Tooba.AccessControl.Contracts.csproj
  Tooba.AccessControl.Application/Tooba.AccessControl.Application.csproj
  Tooba.AccessControl.Infrastructure/Tooba.AccessControl.Infrastructure.csproj
  Tooba.AccessControl.Endpoints/Tooba.AccessControl.Endpoints.csproj
</Folder>
```

All five projects are grouped exactly once under the dedicated `/Modules/AccessControl/` folder.
No loose AccessControl entry remains under a flat `/Modules/` folder.
