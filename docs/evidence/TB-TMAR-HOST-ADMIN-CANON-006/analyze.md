# TB-TMAR-HOST-ADMIN-CANON-006 — Analyze

## Defect

`src/backend/Host/Tooba.Host/Admin/HostOrderAdminEffectiveAccessReader.cs` directly
imported and consumed a foreign module's internals:

- `Tooba.AccessControl.Application` / `.Application.Models` / `.Application.Permissions`
- `Tooba.AccessControl.Domain`
- `IAccessControlDirectory`, `AccessOwnerScope`, `AccessOwnerScopeKind`

Host must not depend on another module's Application/Domain layer.

## Mandatory audit (before creating anything)

### 1. Existing AccessControl projects

```text
src/backend/Modules/AccessControl/Tooba.AccessControl.Domain
src/backend/Modules/AccessControl/Tooba.AccessControl.Application
src/backend/Modules/AccessControl/Tooba.AccessControl.Infrastructure
src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints
```

There is **no** `Tooba.AccessControl.Contracts` project.

### 2. Existing legal cross-module seam (REUSE)

A canonical neutral seam already exists and is already the repository convention:

```text
src/backend/BuildingBlocks/Tooba.BuildingBlocks/Security/IPlatformAccessSeams.cs
```

Exposes exactly the required data with zero module leakage:

| Element | Purpose |
| --- | --- |
| `IPlatformEffectiveAccessReader.GetEffectivePermissionsAsync(Guid userId, PlatformAccessOwnerKind ownerKind, Guid? ownerScopeId, CancellationToken)` | effective-access read |
| `PlatformAccessOwnerKind` (`Platform` / `Seller`) | module-neutral owner scope kind |
| `PlatformPermissionGrant(string PermissionId, PlatformAccessScopeKind ScopeKind, Guid? ScopeResourceId, bool DeniedByCeiling)` | grant DTO with ceiling flag |

Note: unlike the raw directory DTO, the neutral seam returns the **complete** grant
set; `DeniedByCeiling` is carried as data and the consumer filters it. The Order
policy (`AdminOrderOperationsOrchestrator.Has`) already filters
`!p.DeniedByCeiling`, so authorization semantics are identical.

### 3. AccessControl DI ownership

```text
Host/Program.cs:
  IPlatformEffectiveAccessReader -> AccessControl.Infrastructure.Adapters.Security.PlatformEffectiveAccessReader

AccessControlModule.cs:
  IAccessControlDirectory -> AccessControlDirectory
```

Implementation stays inside AccessControl; Host only consumes the neutral interface.

### 4. Shape parity of `AccessControlDirectory.GetEffectiveAccessAsync`

The Platform-scope projection is grouped by `(PermissionId, ScopeKind, ScopeResourceId)`
and the resulting list is `OrderBy(PermissionId)`. Ordering within the Order consumer is
irrelevant: `Has`/`HasAny`/`Prefer` perform lookups and equality, no positional reliance.

## Decision

REUSE the existing `IPlatformEffectiveAccessReader` seam. No new project, no new
AccessControl.Contracts project, no AccessControl business logic moved into Host.
