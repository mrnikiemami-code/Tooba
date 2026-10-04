# Stale / duplicate physical copy — AccessControl (W2)

## Classification

| Axis | State |
| --- | --- |
| Physical-Copy-State | `CLEAN` |
| Stale copy | `ZERO` |
| Duplicate live home | `ZERO` |

## W1 move audit (every moved/renamed path re-verified on disk)

| W1 action | Old path | New path | Old path still present? |
| --- | --- | --- | --- |
| split | `Application/Models/AccessControlDtos.cs` | 11 capability model files | no (deleted) |
| rename | `Application/Exceptions/AccessControlException.cs` | `Application/Validation/AccessControlException.cs` | no |
| rename | `Application/Validators/AccessControlFluentRules.cs` | `Application/Validation/AccessControlFluentRules.cs` | no |
| rename | `Application/Validators/AccessControlValidationCodes.cs` | `Application/Validation/AccessControlValidationCodes.cs` | no |

Verified on disk after W1:

```text
Application/Models/AccessControlDtos.cs   -> absent
Application/Exceptions/                   -> absent
Application/Validators/                   -> absent
Application/Validation/                   -> present (3 files)
```

## Duplicate-responsibility scan

| Responsibility | Live homes found | Verdict |
| --- | --- | --- |
| Application model DTOs | one capability `Models/` folder per capability + shared `Models/AccessOwnerScope.cs` | no duplicate |
| Validation primitives | exactly one `Application/Validation/` | no duplicate |
| Permission catalog | exactly one `Application/Permissions/PermissionCatalog.cs` | no duplicate |
| Directory port + implementation | port in `Application/Ports/`, implementation in `Infrastructure/Directories/` | intentional port/adapter pair |
| Effective-access reader | `Infrastructure/Adapters/AccessControlEffectiveAccessReader.cs` + `Infrastructure/Adapters/Security/PlatformEffectiveAccessReader.cs` | distinct responsibilities (module reader vs platform-security reader) |
| Instrumentation | `Infrastructure/Observability/AccessControlInstrumentation.cs` + `Infrastructure/Authorization/AuthorizationInstrumentation.cs` | distinct scopes (module meter vs authorization meter) |
| Endpoint composition | exactly one `Endpoints/AccessControlEndpointModule.cs` | no duplicate |
| Module composition | exactly one `Infrastructure/AccessControlModule.cs` | no duplicate |

## Solution entry integrity

All five `/Modules/AccessControl/` `.slnx` entries resolve to existing `.csproj` files. No
solution entry points at a deleted or renamed path.

## Cross-project duplicate types

No type name is declared in two AccessControl projects. In particular the W1 transport rename
(`CreateAccessRoleCommand`/`UpdateAccessRoleCommand`/`CloneAccessRoleCommand` →
`CreateRoleRequest`/`UpdateRoleRequest`/`CloneRoleRequest`) removed the only case where a
transport body shared the CQRS command vocabulary; the authoritative MediatR requests remain the
sole owners of `*Command` in `Application/Roles/Commands/`.
