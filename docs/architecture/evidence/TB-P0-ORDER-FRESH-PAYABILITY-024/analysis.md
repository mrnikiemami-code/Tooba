# TB-P0-ORDER-FRESH-PAYABILITY-024 — Order-side read-only fresh-payment payability projection

Scope: Order module only. Read-only, additive, no money route, no payment provider call, no Host change.

## 1. Delivered shape

| Path | Kind | Purpose |
| --- | --- | --- |
| `src/backend/Modules/Order/Tooba.Order.Contracts/Payments/OrderPaymentFreshPayabilityContracts.cs` | new (Contracts) | `IOrderPaymentFreshPayabilityPort`, typed `OrderFreshPaymentPayability` outcomes, `IOrderCheckoutReservationSetReader` narrow read port |
| `src/backend/Modules/Order/Tooba.Order.Application/Checkout/Process/OrderPaymentFreshPayabilityService.cs` | new (Application) | the read-only evaluator |
| `src/backend/Modules/Order/Tooba.Order.Infrastructure/Integrations/Payment/OrderCheckoutReservationSetReader.cs` | new (Infrastructure) | reads persisted line reservations from `OrderDbContext` (no ownership check, no write) |
| `src/backend/Modules/Order/Tooba.Order.Infrastructure/OrderModule.cs` | +2 DI lines | registers the reader and the evaluator |
| `src/backend/Modules/Order/Tooba.Order.Tests/Application/OrderPaymentFreshPayabilityTests.cs` | new (tests) | 16 focused tests, fakes only |

Why a new port instead of extending `IPayableCheckoutReader.GetPayableAsync`: the architect fixed "preserve readable/verifyable
historical payments after reservation expiry". `GetPayableAsync` is the read used by payment result/verification/callback
paths, so adding a payability verdict there would have made expired cycles throw on historical reads. The new port is
consumed by nothing yet, which is exactly the desired blast radius for this slice.

Avoided the authorized-but-risky test wiring on purpose: because the evaluator depends on narrow Order-owned ports
(`IOrderCheckoutReservationSetReader`, `IReservationCycleDirectory`, `IOrderInventoryLifecyclePort`), the 16 tests run in
`Tooba.Order.Tests` with **zero** csproj changes — no `ProjectReference` to `Tooba.Order.Infrastructure`, no
`Microsoft.EntityFrameworkCore.InMemory`.

## 2. Decisions encoded

1. **Set completeness from persisted state**: every line must carry a non-empty reservation id, and the id set must be
   duplicate-free, else `ReservationSetIncomplete` (a reservation id bound to two lines is ambiguity, not a second hold).
2. **Cycle authority**: `ReservationCyclePayabilityPolicy.IsActive` on the canonical `ReservationCycleProjection`.
   Absent authority (`_cycles is null`) → `AuthorityUnavailable`, **never** `Payable` and never a permissive `NotEvaluated`.
3. **Real hold, not cycle status**: each reservation id is read through the Inventory-owned
   `IOrderInventoryLifecyclePort.FindReservationAsync` and must be `Held` with `ExpiresAt` absent or `> now`.
4. **Retry distinction**: a closed cycle that also satisfies `ReservationCyclePayabilityPolicy.HasReachedRetryLimit`
   reports `RetryExhausted`, keeping "payment retry" separate from "reservation renewal". No numeric ceiling is invented;
   `RetryCountRemaining`/`EffectiveMaxCycles` are echoed from the effective policy projection.
5. **No TTL arithmetic**: expiry is compared against `IClock.UtcNow` only. Nothing extends, renews, releases or writes.
6. **No throw on expiry**: absence/expiry/closed cycle are typed outcomes, so the projection never breaks a historical read.

## 3. Verification

`dotnet test src/backend/Modules/Order/Tooba.Order.Tests/Tooba.Order.Tests.csproj --filter "FullyQualifiedName~OrderPaymentFreshPayability|FullyQualifiedName~Architecture"`
→ 128 passed, 0 failed (16 new + all existing Order architecture guards). No Host.Tests, no solution build, no PostgreSQL.

Covered: active cycle + all Held → Payable; wall-clock expired cycle; closed cycle; retry quota exhausted; Released hold;
hold expiring exactly at `now`; missing reservation row; absent authority; zero lines; line without reservation; duplicate
reservation id across lines; unknown checkout; `Guid.Empty`; cancellation propagation; source guards (no TTL arithmetic,
no write, no payment call, port registered exactly once, `OrderPaymentBridge` unchanged).

**Limits (explicitly UNVERIFIED)**: fakes prove the decision logic, not EF mapping/row locking; the reader↔`OrderDbContext`
mapping and cross-module race behaviour are unproven here. Between this read and a later initiation, a cycle or hold can
expire or be released; closing that residual window belongs to the canonical reacquire/refund path, not to this projection.

## 4. Remaining work (next slice, not authorized here)

Payment-side consumption: the storefront money-entry route
(`POST /v1/storefront/checkout/{checkoutId}/payments` → `StorefrontPaymentOrchestrator` → `PaymentDirectory.InitiateAsync`)
still has **no** payability gate — the old public route remains unguarded, as expected. The next step is to inject
`IOrderPaymentFreshPayabilityPort` into that orchestration (not into result/verification/callback reads), map every
non-`Payable` outcome to a typed, catalogued Payment refusal, and keep ownership enforcement where it is today.
