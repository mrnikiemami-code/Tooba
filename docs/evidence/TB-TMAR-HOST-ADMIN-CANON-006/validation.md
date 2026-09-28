# TB-TMAR-HOST-ADMIN-CANON-006 — Validation

## Commands run

| # | Command | Result |
| --- | --- | --- |
| 1 | `dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj` | PASS (0 errors) |
| 2 | `dotnet test Tooba.Host.Tests --filter Canon006\|Canon005\|Canon004 guards` | PASS — 30/30 |
| 3 | `dotnet build Tooba.AccessControl.Infrastructure` (owning AccessControl project) | PASS (0 errors) |

Validation-command-runs = 3. Repair-iterations = 0. No command failed, so no repair retry
was required.

## Focused guard result

```text
Passed!  - Failed: 0, Passed: 30, Skipped: 0, Total: 30
```

`HostAdminCanon006GuardTests` (8) + `HostAdminCanon005GuardTests` (8) + `HostAdminCanon004GuardTests` (14).

## Guard assertions proven

- `HostOrderAdminEffectiveAccessReader.cs`: zero `Tooba.AccessControl`, zero
  `IAccessControlDirectory`, zero `AccessOwnerScope`, zero service-locator usage.
- Reader uses `IPlatformEffectiveAccessReader` +
  `PlatformAccessOwnerKind.Platform` + `GetEffectivePermissionsAsync`.
- Reader keeps `Tooba.Order.Contracts.Admin.Operations` and zero `Tooba.Order.Application`.
- `new OrderAdminPermissionGrant(p.PermissionId, p.DeniedByCeiling)` mapping pinned.
- Neutral seam declares no `Tooba.AccessControl` / `Tooba.Order` types and owns both
  `bool DeniedByCeiling` and `PlatformAccessOwnerKind`.
- AccessControl module owns the adapter and the DI registration targets it.
- `Host/Admin` `.cs` count == 15.
- CANON-005 (Order.Contracts authority) and CANON-004 (authorizer surface) preserved.

## Baseline debt (unchanged, out of scope)

- `Tooba.Order.Tests` pre-existing missing AccessControl reference — not run, not repaired.
- `reservation.policy.*` duplicate composed-catalog descriptors — untouched.
- Local `bin/`/`obj/` artifacts untouched.
