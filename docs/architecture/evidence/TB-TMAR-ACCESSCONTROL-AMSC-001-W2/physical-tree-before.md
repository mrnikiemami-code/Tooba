# Physical tree — before (AccessControl W2)

Captured at W2 start, i.e. immediately after the W1 `Migrate` commit
`c9e009f8` (`refactor(tmar): AccessControl AMSC-001 W1 migrate …`).

## Classification

| Axis | State |
| --- | --- |
| Folder-Granularity-State | `PROFESSIONAL_SHALLOW` |
| Solution-Explorer-State | `CANONICAL` |
| Path-Namespace-State | `EXACT` |
| Physical-Copy-State | `CLEAN` |
| Root-Allowlist-State | `ENFORCED` |
| File-Cohesion-State | `COHESIVE` (one `OVERSIZED_ONLY` watch) |
| Structure-State | `READY_FOR_CERTIFY` (candidate) |

## Production project directories on disk

```text
src/backend/Modules/AccessControl/
  Tooba.AccessControl.Application/
  Tooba.AccessControl.Contracts/
  Tooba.AccessControl.Domain/
  Tooba.AccessControl.Endpoints/
  Tooba.AccessControl.Infrastructure/
```

Exactly five production projects. No `*.Tests` project exists (W0 finding `F5`, out of scope).

## Application tree (post-W1)

```text
Tooba.AccessControl.Application/
  Access/           Models/ (3)  Queries/ (3)
  Assignments/      Commands/ (2)  Models/ (1)  Queries/ (1)  Validators/ (1)
  Authorization/    AccessControlCapabilityGate.cs
  Bootstrap/        Commands/ (1)
  Ceiling/          Commands/ (1)  Models/ (1)  Queries/ (1)  Validators/ (1)
  Composition/      AccessControlOperation.cs
  Development/      Seller/ (3)
  Models/           AccessOwnerScope.cs
  Permissions/      Commands/ (1)  Models/ (1)  Queries/ (3)  Validators/ (1)  PermissionCatalog.cs
  Ports/            IAccessControlDirectory.cs
  Roles/            Commands/ (4)  Models/ (4)  Queries/ (2)  Validators/ (3)
  Validation/       (3)
```

No root `.cs`. No `Application/Commands/**` or `Application/Queries/**` technical-axis root.
No `Exceptions/` or `Validators/` top-level folder (W1 consolidation).

## Root production `.cs` per project

| Project | Root `.cs` on disk |
| --- | --- |
| `Tooba.AccessControl.Application` | *(none)* |
| `Tooba.AccessControl.Contracts` | *(none)* |
| `Tooba.AccessControl.Domain` | *(none)* |
| `Tooba.AccessControl.Endpoints` | `AccessControlEndpointModule.cs` |
| `Tooba.AccessControl.Infrastructure` | `AccessControlModule.cs` |
