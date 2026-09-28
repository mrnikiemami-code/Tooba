# TB-TMAR-HOST-ADMIN-CANON-001 — Validation

## Focused builds

| Project | Result |
|---|---|
| `Tooba.Catalog.Contracts` (via `Tooba.Host` build) | PASS |
| `Tooba.Catalog.Infrastructure` | PASS |
| `Tooba.Party.Contracts` / `Tooba.Party.Infrastructure` | PASS |
| `Tooba.Order.Contracts` / `Tooba.Order.Infrastructure` | PASS |
| `Tooba.Host` | PASS |
| `Tooba.Host.Tests` | PASS |

Commands:

```text
dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj -v q
dotnet build src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj -v q
```

## Focused tests

| Filter | Result |
|---|---|
| `HostAdminCanon001` (new boundary guards) | PASS |
| `AdminPanelComposition` | PASS |
| `AdminDbNativeGrid` | PASS |
| `AdminOrderCancelPrecedence` | PASS |
| `AdminReservationCycleAudit` | PASS |
| `HostAdminAmcW19`, `HostAdminAmcW33`, `HostAdminAmcW36` (KEEP + count guards) | PASS |

```text
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj \
  --filter "FullyQualifiedName~HostAdminCanon001|FullyQualifiedName~AdminPanelComposition|... "
Passed! - Failed: 0, Passed: 48, Skipped: 0, Total: 48
```

## Guard updates (justified, behavior-preserving)

Guards that previously *required* the foreign persistence/Application shape on this path were
updated to require the new Contracts-only shape:

| File | Change |
|---|---|
| `Host/Tooba.Host.Tests/AdminPanelCompositionTests.cs` | composer now asserted against Contracts ports; old ORM field assertions removed |
| `Host/Tooba.Host.Tests/AdminDbNativeGridQueryTests.cs` | sellers engine asserted free of any `DbContext` and free of `IQueryable`; requires count/`Skip`/`Take` paging markers |
| `Modules/Order/Tooba.Order.Tests/Architecture/OrderAdminPanelResidualArchitectureGuardTests.cs` | composer/grid asserted to use Order Contracts ports and to contain no `Tooba.Order.Application` |

## Pre-existing, out-of-scope failures (not caused by this task)

These failures exist on the parent commit and are unrelated to the Admin panel read path:

| Failure | Cause |
|---|---|
| `TmarSourceSizeAndInfraAppTests.*` (3 tests) | repository-wide size baseline and infra→foreign-Application edge baseline are stale from prior accepted waves (relocated `Directories/*` files, `Content` certifications, etc.) |
| `HostOrderReverseAuditGuardTests.Host_OrderDbContext_consumers_are_enumerated_in_inventory` | R7 inventory lists the stale path `Admin/ProductWorkspaceDevelopmentBootstrap.cs` while the file lives at `Development/ProductWorkspaceDevelopmentBootstrap.cs` |
| `TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable` | SoT `structureLock.certifiedModules` now contains `Content` |

Each requires its own baseline/SoT edit wave and is excluded by the task's "do not expand scope"
rule.

## Direct verification

Scripted source assertions (composer + sellers grid) confirm: Contracts ports present, foreign
`Infrastructure`/`Application`/`Domain` imports absent, no `DbContext`, no `IQueryable`, no
`Microsoft.EntityFrameworkCore`, Offer boundary preserved.
