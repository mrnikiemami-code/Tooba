# Root allowlist — AccessControl (W0)

`Root-Allowlist-State = ENFORCED`

Source of truth: `docs/architecture/tmar-module-structure-manifests.json` → `module: AccessControl`
(`structureCertified: true`, `lockVersion: ARCH-COMPLETE-002`).

| Project | `rootAllowlist` | Actual root `.cs` | Verdict |
| --- | --- | --- | --- |
| `Tooba.AccessControl.Application` | `[]` | none | OK |
| `Tooba.AccessControl.Contracts` | `[]` | none | OK |
| `Tooba.AccessControl.Domain` | `[]` | none | OK |
| `Tooba.AccessControl.Endpoints` | `["AccessControlEndpointModule.cs"]` | `AccessControlEndpointModule.cs` | OK |
| `Tooba.AccessControl.Infrastructure` | `["AccessControlModule.cs"]` | `AccessControlModule.cs` | OK |

## `forbiddenRootFiles` (current manifest)

| Project | Forbidden root files |
| --- | --- |
| Application | `AccessControlContracts.cs`, `AccessControlDtos.cs`, `PermissionCatalog.cs`, `AccessControlException.cs`, `IAccessControlDirectory.cs`, `AccessControlOperation.cs` |
| Domain | `AccessControlDomain.cs` |
| Endpoints | `AccessControlAdminEndpoints.cs`, `AccessControlAdminSellerEndpoints.cs`, `AccessControlSellerEndpoints.cs` |
| Infrastructure | `AccessControlDirectory.cs`, `AccessControlInstrumentation.cs`, `AccessControlOutboxRegistration.cs` |
| Contracts | `AccessControlContracts.cs`, `AccessControlEffectiveAccessContracts.cs`, `AccessOwnerScopeKind.cs`, `AccessScopeKind.cs`, `AccessControlErrorCodes.cs` |

`forbiddenTopLevelFolders` is `[]` for every project.

## Required manifest updates for W1

The W1 cohesion split introduces new Application files. The Application `forbiddenRootFiles` list must
be extended so the new filenames can never return to the project root:

- `AccessOwnerScope.cs`
- `AccessRoleDto.cs`
- `CreateAccessRoleBody.cs`
- `UpdateAccessRoleBody.cs`
- `CloneAccessRoleBody.cs`
- `UserRoleAssignmentDto.cs`
- `RolePermissionGrant.cs`
- `SellerCeilingEntryDto.cs`
- `EffectiveAccessDto.cs`
- `EffectivePermissionDto.cs`
- `AccessUserHitDto.cs`

`rootAllowlist` remains `[]` for Application. The `forbiddenRootFiles` entry `AccessControlDtos.cs`
is retained as a permanent prohibition on the removed dump file.

W1 also removes the `Exceptions/` top-level folder in favour of `Validation/`; the manifest's
`forbiddenTopLevelFolders` for Application may optionally record `Exceptions` as a prohibited
reintroduction. That decision is made in W2 (Structure) so the physical allowlist and the disk stay
honest.
