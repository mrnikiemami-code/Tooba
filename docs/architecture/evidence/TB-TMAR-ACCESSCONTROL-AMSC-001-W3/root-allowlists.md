# Root allowlists — AccessControl (W3)

| Project | `rootAllowlist` | Disk root `.cs` | Match |
| --- | --- | --- | --- |
| `Tooba.AccessControl.Application` | `[]` | — | ✅ |
| `Tooba.AccessControl.Contracts` | `[]` | — | ✅ |
| `Tooba.AccessControl.Domain` | `[]` | — | ✅ |
| `Tooba.AccessControl.Endpoints` | `["AccessControlEndpointModule.cs"]` | `AccessControlEndpointModule.cs` | ✅ |
| `Tooba.AccessControl.Infrastructure` | `["AccessControlModule.cs"]` | `AccessControlModule.cs` | ✅ |

Both allowed root files are **composition entries only**:

- `AccessControlEndpointModule.cs` — HTTP endpoint composition (`MapAccessControlEndpoints`).
- `AccessControlModule.cs` — DI/infrastructure composition.

Neither contains business logic, persistence access or route handlers.

## Forbidden root files — all absent

`Application` (17): `AccessControlContracts.cs`, `AccessControlDtos.cs`, `PermissionCatalog.cs`,
`AccessControlException.cs`, `IAccessControlDirectory.cs`, `AccessControlOperation.cs`,
`AccessOwnerScope.cs`, `AccessRoleDto.cs`, `CreateRoleRequest.cs`, `UpdateRoleRequest.cs`,
`CloneRoleRequest.cs`, `RolePermissionGrant.cs`, `UserRoleAssignmentDto.cs`,
`SellerCeilingEntryDto.cs`, `EffectiveAccessDto.cs`, `EffectivePermissionDto.cs`,
`AccessUserHitDto.cs` → **none present**.

`Contracts` (5): `AccessControlContracts.cs`, `AccessControlEffectiveAccessContracts.cs`,
`AccessOwnerScopeKind.cs`, `AccessScopeKind.cs`, `AccessControlErrorCodes.cs` → **none present**.

`Domain` (1): `AccessControlDomain.cs` → **absent**.

`Endpoints` (3): `AccessControlAdminEndpoints.cs`, `AccessControlAdminSellerEndpoints.cs`,
`AccessControlSellerEndpoints.cs` → **none present** (all live under `Admin/` and `Seller/`).

`Infrastructure` (3): `AccessControlDirectory.cs`, `AccessControlInstrumentation.cs`,
`AccessControlOutboxRegistration.cs` → **none present**.

## Forbidden top-level folders — all absent

`Application`: `Exceptions`, `Validators` → **neither exists** (W1 consolidation into `Validation/`).
All other AccessControl projects declare no forbidden top-level folder.

## Enforcement

`TmarCompleteReferenceStructureGateTests` (shared) and
`AccessControlManifestDiskReconciliationGuardTests` (module-scoped, W2) both re-derive the disk
root `.cs` set and compare it to the manifest on every run. `AccessControlModuleAmsc001W1MigrateGuardTests`
additionally forbids `Exceptions/`/`Validators/` resurrection.
