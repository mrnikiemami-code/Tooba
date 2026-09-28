# TB-TMAR-HOST-ADMIN-CANON-006 — Closure

## Outcome

PASS. The Host Order admin effective-access reader is now Contracts/seam-only toward
AccessControl; direct AccessControl Application/Domain coupling is ZERO; the AccessControl
adapter implementation remains module-owned.

## Changes

| File | Change |
| --- | --- |
| `src/backend/Host/Tooba.Host/Admin/HostOrderAdminEffectiveAccessReader.cs` | depends on `IPlatformEffectiveAccessReader` (neutral) + Order.Contracts; AccessControl Application/Domain removed |
| `Host/Tooba.Host.Tests/Architecture/HostAdminCanon006GuardTests.cs` | NEW durable guard (8 tests) |
| `Host/Tooba.Host.Tests/Architecture/HostAdminCanon005GuardTests.cs` | reader-coupling guard updated to the neutral seam |
| `Host/Tooba.Host.Tests/Architecture/HostAdminCanon004GuardTests.cs` | out-of-scope reader guard updated |
| `docs/architecture/tmar-current-state.json` | `hostAdminCanon006` SoT entry |

No new project was created: the canonical neutral seam
(`Tooba.BuildingBlocks.Security.IPlatformEffectiveAccessReader`) already existed and was reused.

## Compliance

- Host reader AccessControl.Application: ZERO
- Host reader AccessControl.Domain: ZERO
- Host reader `IAccessControlDirectory`: ZERO
- Boundary: neutral BuildingBlocks seam; implementation still AccessControl-owned
- Order.Contracts authority: PRESERVED (CANON-005)
- `PermissionId` / `DeniedByCeiling` mapping: PRESERVED
- `Host/Admin` count: 15
- CANON-004 preserved; routes/frontend/schema untouched
- Known baseline debt untouched

## Final state

- Commit-SHA: `928e83358bd3618a74dd2c6d4796b7e740e63abb`
- HEAD == origin/main: YES
- Tracked working tree: clean
- User work preserved: yes
