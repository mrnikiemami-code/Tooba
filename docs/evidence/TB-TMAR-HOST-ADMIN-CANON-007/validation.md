# TB-TMAR-HOST-ADMIN-CANON-007 — Validation

## Commands run

| # | Command | Result |
| --- | --- | --- |
| 1 | `dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj` | PASS (0 errors) |
| 2 | `dotnet test Tooba.Host.Tests --filter Canon007\|Canon006 guards` | FAIL (guard literal self-check, repaired) |
| 3 | rerun #2 (repair attempt 1) | FAIL (guard literal self-check, repaired) |
| 4 | rerun #2 (repair attempt 2, successful) | PASS — 17/17 |

Validation-command-runs = 4. Repair-iterations = 1 (single concern: two guard string literals did
not match the source text — `public const string AdminEmail` vs `const string AdminEmail`, and the
multiline `new AdminDevActorSnapshot(...)` argument split).

## Focused guard result

```text
Passed!  - Failed: 0, Passed: 17, Skipped: 0, Total: 17
```

`HostAdminCanon007GuardTests` (9 tests) + `HostAdminCanon006GuardTests` (8 tests).

## Guard assertions proven

- `AdminDevActorBootstrap.cs`: zero `Tooba.Identity.Infrastructure`, zero `RequestServices`.
- Identity dependency is Contracts-only (`using Tooba.Identity.Contracts;` +
  `IIdentityAuthenticationService`).
- Zero `catch (InvalidOperationException)` / `catch (System.InvalidOperationException)`.
- Typed duplicate registration handling retained (`catch (IdentityDuplicateIdentifierFault)`).
- Membership written through `IAuthorizationTupleWriter` with
  `AuthorizationObjectTypes.Tenant` + `AuthorizationRelations.Member`.
- Behavior surface preserved: `AdminEmail`, Development password, snapshot label/tenant,
  static `Gate` + `lock (Gate)`, `ICurrentTenant`.
- Tuple-writer idempotency pinned: SpiceDB `Touch` default, InMemory `_tuples[key] = 1`.
- `Host/Admin` `.cs` count == 15.
- CANON-006 reader boundary preserved (neutral seam, zero `Tooba.AccessControl`).

## Baseline debt (unchanged, out of scope)

- `Tooba.Order.Tests` pre-existing missing AccessControl reference — not run, not repaired.
- `reservation.policy.*` duplicate composed-catalog descriptors — untouched.
- Local `bin/`/`obj/` artifacts untouched.
