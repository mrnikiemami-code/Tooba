# Root allowlist — AccessControl (W2)

## Classification

| Axis | State |
| --- | --- |
| Root-Allowlist-State | `ENFORCED` |
| Root dump | `ZERO` |
| Allowlist widening to hide debt | `NONE` |

## Declared vs disk

| Project | Manifest `rootAllowlist` | Root `.cs` on disk | Verdict |
| --- | --- | --- | --- |
| `Tooba.AccessControl.Application` | `[]` | *(none)* | exact |
| `Tooba.AccessControl.Contracts` | `[]` | *(none)* | exact |
| `Tooba.AccessControl.Domain` | `[]` | *(none)* | exact |
| `Tooba.AccessControl.Endpoints` | `["AccessControlEndpointModule.cs"]` | `AccessControlEndpointModule.cs` | exact |
| `Tooba.AccessControl.Infrastructure` | `["AccessControlModule.cs"]` | `AccessControlModule.cs` | exact |

The two allowed root files are composition entries only
(`AccessControlEndpointModule` = HTTP composition, `AccessControlModule` = DI composition).

## Forbidden root files (all absent)

| Project | Declared `forbiddenRootFiles` | Present on disk |
| --- | --- | --- |
| `Tooba.AccessControl.Application` | `AccessControlContracts.cs`, `AccessControlDtos.cs`, `PermissionCatalog.cs`, `AccessControlException.cs`, `IAccessControlDirectory.cs`, `AccessControlOperation.cs`, `AccessOwnerScope.cs`, `AccessRoleDto.cs`, `CreateRoleRequest.cs`, `UpdateRoleRequest.cs`, `CloneRoleRequest.cs`, `RolePermissionGrant.cs`, `UserRoleAssignmentDto.cs`, `SellerCeilingEntryDto.cs`, `EffectiveAccessDto.cs`, `EffectivePermissionDto.cs`, `AccessUserHitDto.cs` | none |
| `Tooba.AccessControl.Domain` | `AccessControlDomain.cs` | none |
| `Tooba.AccessControl.Endpoints` | `AccessControlAdminEndpoints.cs`, `AccessControlAdminSellerEndpoints.cs`, `AccessControlSellerEndpoints.cs` | none |
| `Tooba.AccessControl.Infrastructure` | `AccessControlDirectory.cs`, `AccessControlInstrumentation.cs`, `AccessControlOutboxRegistration.cs` | none |
| `Tooba.AccessControl.Contracts` | `AccessControlContracts.cs`, `AccessControlEffectiveAccessContracts.cs`, `AccessOwnerScopeKind.cs`, `AccessScopeKind.cs`, `AccessControlErrorCodes.cs` | none |

## Forbidden top-level folders (all absent)

| Project | Declared `forbiddenTopLevelFolders` | Present on disk |
| --- | --- | --- |
| `Tooba.AccessControl.Application` | `Exceptions`, `Validators` | neither exists (consolidated into `Validation/` in W1) |
| all other AccessControl projects | *(empty)* | — |

## Note on `artifacts/`

`Application/artifacts`, `Domain/artifacts` and `Infrastructure/artifacts` exist on disk as
**untracked** build/test output directories (the same pattern exists in 23 sibling modules).
They contain no production source, are not part of the repository tree, and are therefore not a
root-allowlist violation. No allowlist was widened to accommodate them.
