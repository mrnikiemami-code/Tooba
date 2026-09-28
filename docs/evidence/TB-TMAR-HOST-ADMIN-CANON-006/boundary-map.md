# TB-TMAR-HOST-ADMIN-CANON-006 — Boundary Map

## Before

```text
Host/Admin/HostOrderAdminEffectiveAccessReader.cs
  -> Tooba.AccessControl.Application (IAccessControlDirectory)
  -> Tooba.AccessControl.Application.Models (AccessOwnerScope, AccessOwnerScopeKind)
  -> Tooba.AccessControl.Domain
  -> Tooba.Order.Contracts.Admin.Operations
```

Forbidden edge: `Host -> AccessControl.Application/Domain`.

## After

```text
Host/Admin/HostOrderAdminEffectiveAccessReader.cs
  -> Tooba.BuildingBlocks.Security.IPlatformEffectiveAccessReader   (neutral seam)
  -> Tooba.BuildingBlocks.Security.PlatformAccessOwnerKind          (neutral enum)
  -> Tooba.Order.Contracts.Admin.Operations
```

`ICurrentTenant` is no longer required by this reader because the neutral seam derives
the platform scope internally; the constructor was narrowed accordingly.

## Ownership

```text
BuildingBlocks        owns the neutral contract (no Tooba.AccessControl / Tooba.Order types)
AccessControl         owns the adapter implementation (PlatformEffectiveAccessReader)
Host                  owns only the mapping Order adapter (consumer)
Order.Contracts       owns IOrderAdminEffectiveAccessReader / OrderAdminEffectiveAccess / OrderAdminPermissionGrant
```

## Mapping (behavior-preserving)

| Neutral seam | Order contract |
| --- | --- |
| `PlatformPermissionGrant.PermissionId` | `OrderAdminPermissionGrant.PermissionId` |
| `PlatformPermissionGrant.DeniedByCeiling` | `OrderAdminPermissionGrant.DeniedByCeiling` |
| `PlatformAccessOwnerKind.Platform`, `ownerScopeId: null` | platform-scoped effective access |

The previous Host reader pre-filtered ceiling-denied grants; the neutral seam does not.
The downstream `AdminOrderOperationsOrchestrator.Has/HasAny/Prefer` already filter
`!p.DeniedByCeiling` before any decision, so no authorization semantic changes.

No new error path, no new authorization policy, no new exception behavior.
