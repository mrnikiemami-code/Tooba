# Physical tree — after (AccessControl W2)

## Change summary

**No production file, folder or project was moved, renamed, created or deleted in W2.**

W1 already produced the canonical physical tree. W2's job was to verify it independently and to
make the verification durable. The physical tree after W2 is byte-for-byte identical to the tree
captured in `physical-tree-before.md`.

```text
production tree delta (W2) = ZERO
production .cs delta (W2)  = ZERO
.csproj delta (W2)         = ZERO
Tooba.slnx delta (W2)      = ZERO
```

## Artifacts added by W2 (non-production)

| Path | Purpose |
| --- | --- |
| `src/backend/Host/Tooba.Host.Tests/Architecture/AccessControlManifestDiskReconciliationGuardTests.cs` | durable manifest↔disk + structure guard (test project) |
| `docs/architecture/evidence/TB-TMAR-ACCESSCONTROL-AMSC-001-W2/*` | W2 structure evidence |
| `docs/architecture/tmar-current-state.json` | SoT record `accessControlModuleAmsc001W2` |

## Final production tree

```text
src/backend/Modules/AccessControl/
  Tooba.AccessControl.Contracts/
    Access/       AccessControlEffectiveAccessContracts.cs
    Development/  AccessControlDevelopmentSeedContracts.cs
    Enums/        AccessOwnerScopeKind.cs  AccessScopeKind.cs
    Errors/       AccessControlErrorCodes.cs
    Readiness/    AuthorizationReadinessContracts.cs
  Tooba.AccessControl.Domain/
    Aggregates/   AccessRole.cs  RolePermission.cs  UserRoleAssignment.cs
                  PlatformSellerCeiling.cs  AccessAuditEvent.cs
  Tooba.AccessControl.Application/
    Access/        Models/  Queries/
    Assignments/   Commands/  Models/  Queries/  Validators/
    Authorization/ AccessControlCapabilityGate.cs
    Bootstrap/     Commands/
    Ceiling/       Commands/  Models/  Queries/  Validators/
    Composition/   AccessControlOperation.cs
    Development/   Seller/
    Models/        AccessOwnerScope.cs
    Permissions/   Commands/  Models/  Queries/  Validators/  PermissionCatalog.cs
    Ports/         IAccessControlDirectory.cs
    Roles/         Commands/  Models/  Queries/  Validators/
    Validation/    AccessControlException.cs  AccessControlFluentRules.cs
                   AccessControlValidationCodes.cs
  Tooba.AccessControl.Infrastructure/
    AccessControlModule.cs
    Adapters/      AccessControlEffectiveAccessReader.cs  AuthorizationReadinessProbe.cs
    Adapters/Security/  PlatformEffectiveAccessReader.cs
    Authorization/ AuthorizationAdapters.cs  AuthorizationInstrumentation.cs
                   AuthorizationRegistration.cs  SpiceDbAuthorizationAdapter.cs
                   SpiceDbAuthorizationBootstrapper.cs  SpiceDbAuthorizationOptions.cs
    Development/   AccessControlDevelopmentSeedPrelude.cs
    Development/Seller/  SellerDevContextBootstrap.cs
    Directories/   AccessControlDirectory.cs
    Messaging/     AccessControlOutboxRegistration.cs
    Observability/ AccessControlInstrumentation.cs
    Persistence/   AccessControlDbContext.cs
    Persistence/Migrations/  4 EF files
  Tooba.AccessControl.Endpoints/
    AccessControlEndpointModule.cs
    Admin/         AccessControlAdminEndpoints.cs  AccessControlAdminSellerEndpoints.cs
    Errors/        AccessControlErrorCatalogContributor.cs  AccessControlHttpErrors.cs
    Resources/     AccessControlErrorResources.cs
    Seller/        AccessControlSellerEndpoints.cs
    Seller/Development/  SellerDevContextEndpoints.cs
```

86 production `.cs` files across 5 projects. No root dump. No technical-axis-first request root.
No single-file request/use-case leaf folder. No stale or duplicate copy.
