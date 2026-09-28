# TB-TMAR-HOST-ROOT-GLOBAL-BOUNDARIES-001-R1 — Validation

## Builds (each run once)

| Project | Result |
|---------|--------|
| `Tooba.Catalog.Contracts` | succeeded, 0 errors |
| `Tooba.Catalog.Infrastructure` | succeeded, 0 errors |
| `Tooba.Payment.Infrastructure` | succeeded, 0 errors |
| `Tooba.Order.Infrastructure` | succeeded, 0 errors |
| `Tooba.Order.Application` (unchanged; built for graph proof) | succeeded, 0 errors |
| `Tooba.Host` | succeeded, 0 errors |
| `Tooba.Host.Tests` | succeeded, 0 errors |

## Focused Host guard run

```text
HostRootGlobalBoundariesGuardTests
UnpaidOrderExpiryTests
ReservationCycleFoundationTests
CartLifetimeSeparationTests
HostFolderStructureTests
HostCartResidualGuardTests
HostOrderReverseAuditGuardTests
```

Result: **Passed! Failed: 0, Passed: 58, Skipped: 0, Total: 58.**

## Payment module suite

`Tooba.Payment.Tests` — **Passed: 94, Failed: 1, Total: 95** including the 8 new
`CommerceHoldPolicySourceTests` facts.

The single failure is `Payment_endpoints_cqrs_and_host_ownership_are_enforced`,
which **also fails on clean `main` `a2e01cf5`** (baseline: Passed 86, Failed 1). It
is pre-existing and untouched by this recovery.

## Baseline comparisons (anti-regression proof)

| Suite | Clean `main` `a2e01cf5` | This branch | Regression |
|-------|--------------------------|-------------|------------|
| `HostOrderReverseAuditGuardTests` | Failed 2 / 10 | Failed 0 / 10 | none (improved) |
| `Host` focused guard set (7 classes) | n/a (new 001 guard absent) | Failed 0 / 58 | none |
| `Payment.Tests` | Passed 86, Failed 1 | Passed 94, Failed 1 | none |
| `Order.Tests` | **pre-existing compile failure** (`Tooba.AccessControl` namespace missing in `OrderSellerPanelArchitectureGuardTests.cs`) | identical pre-existing compile failure | none |

`Tooba.Order.Tests` does not compile on clean `main`; this is pre-existing and
outside the bounded scope (no Order test project change was required by this Task).

## No guard weakening

- No assertion was deleted to reach PASS.
- No baseline was widened.
- `HostOrderReverseAuditGuardTests` retained every historical historical-record
  assertion and re-expressed the two stale `files`-list lookups against the correct
  historical update records.

## Validation commands

```text
dotnet build  <each project above> -v q --nologo
dotnet test   src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --filter "<7 guard classes>"
dotnet test   src/backend/Modules/Payment/Tooba.Payment.Tests
```

No solution-wide test run. No unrelated repair loop. One repair iteration per
deterministic local failure.
