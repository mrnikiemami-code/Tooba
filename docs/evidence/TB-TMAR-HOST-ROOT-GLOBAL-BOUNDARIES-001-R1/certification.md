# TB-TMAR-HOST-ROOT-GLOBAL-BOUNDARIES-001-R1 — Certification

## Scope

This certifies the **bounded Host-root global-boundaries migration surface**, not
whole-module `ARCH-COMPLETE-002` for Order/Payment/Catalog.

## Required zero states

### Host root

| Requirement | State |
|-------------|-------|
| All six requested root files | **ZERO** (`Test-Path` false for each) |
| Settlement Application/Infrastructure/Domain global usings | **ZERO** |
| Host root business registration for these responsibilities | **ZERO** (no matches in `Program.cs`) |

### New / touched boundaries

| Requirement | State |
|-------------|-------|
| Payment hold implementation → Host | **ZERO** |
| Payment hold implementation → `Catalog.Infrastructure` | **ZERO** |
| Payment hold implementation → `Catalog.Domain` | **ZERO** |
| Payment → Catalog boundary | **Contracts-only** (`IStoreHoldPolicyHoursReader`) |
| Order checkout hold → Payment boundary | **Contracts-only** (`Payment.Contracts.Hold`) |
| moved Order worker → Host | **ZERO** |
| new Order registration file → Host | **ZERO** |

### Certification quality (this recovery's touched surface)

| Requirement | State |
|-------------|-------|
| foreign Application dependency introduced/touched | **ZERO** |
| foreign Infrastructure dependency introduced/touched | **ZERO** |
| foreign Domain dependency introduced/touched | **ZERO** |
| cross-module DbContext access | **ZERO** |
| cross-module EF join | **ZERO** |
| service locator in migrated business code | **ZERO** (only `AddHostedService`/`Configure` composition) |
| message parsing | **ZERO** |
| new hardcoded user-facing error contract | **ZERO** |
| parallel logging/telemetry/correlation pipeline | **ZERO** |
| path/namespace exact | **PASS** |
| stale copies / compatibility aliases | **ZERO** |

## Touched-surface re-read

Every production file changed by this recovery was re-read:

- `Host/Program.cs` — one composition line added (`AddOrderReservationCycleBoundaries`); no business logic.
- `Host/Composition/ToobaModuleComposition.cs` — namespace-explicit import only; still composition-only.
- `Order/ReservationCycle/ReservationCycleRegistration.cs` (new) — DI composition only.
- `Order/ReservationCycle/UnpaidOrderExpiryWorker.cs`, `...WorkerOptions.cs` (new) — Order-owned, Host-independent.
- `Order/Integrations/Payment/CheckoutReservationHoldPolicyAdapter.cs` (new) — Contracts-only.
- `Payment/Adapters/CommerceHoldPolicySource.cs` (moved) — Catalog Contracts-only.
- `Payment/Tests/Behavior/CommerceHoldPolicySourceTests.cs` (new) — test-only.
- `Payment/Tests/Behavior/PaymentPrecertHygieneTests.cs` — test-only stub additions.

`OrderModule.cs` and `Tooba.Order.Infrastructure.csproj` are **not** in the touched
surface: `git diff` against `origin/main` is empty for both.

## Certification blocker resolution

`Order-Certification-Blocker-State`:

```text
ISOLATED_FROM_TOUCHED_SURFACE_AND_CERTIFIABLE
```

The pre-existing foreign `Payment.Application` edge on Order.Infrastructure was not
introduced or touched by this recovery; the migration's registration no longer
touches the dirty registration/project surface at all.

## Verdict

```text
BLOCKER RESOLVED
FOCUSED_VALIDATION PASS (58/58 focused Host guards, 0 new module regressions)
BOUNDED HOST-ROOT MIGRATION SURFACE: CERTIFIED
```

No whole-module `ARCH-COMPLETE-002` is claimed for Order, Payment, or Catalog.
