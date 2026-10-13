# TB-P0-CHECKOUT-PAYABILITY-ATOMIC-014 — bounded analysis (design A)

Parent: TB-P0-CHECKOUT-PAYABILITY-GATE-013. Base SHA `d6ce094c0cfe344cf0e26ac3138b564058163938`
(= HEAD = origin/main at claim time).

Verdict: **BLOCKED_ARCHITECT_DECISION**. No production code changed. Two independent gaps make the
atomic payability boundary un-enforceable inside the allowed file set — and a cycle-only CAS is provably
*insufficient* (live evidence in §3), so shipping it would be the "dead/optimistic guard" the task forbids.

## 1. Money-entry map (unchanged from 013, re-verified)

```
StorefrontPaymentOrchestrator.InitiateAsync           Payment.Application/.../StorefrontPaymentOrchestrator.cs:110-241
  :120 actor gate · :121 ownership+payable amount · :125-158 method selection (wallet|manual|gateway)
  :159 _payments.InitiateAsync  ->  PaymentDirectory.InitiateAsync   Payment.Infrastructure/Directories/PaymentDirectory.cs:60-210
      :61-135 idempotency replay (returns persisted payment) · :139 succeeded guard · :143-149 GetPayableAsync
      :167 gateway resolve · :192 gateway.InitiateAsync (provider money movement) · :193-201 Record+Assign+SaveChanges
  :168-198 wallet immediate verify -> WalletPaymentGateway.VerifyAsync -> IWalletOrderPaymentPort.SpendForOrderPaymentAsync
```
`PaymentDirectory.InitiateAsync` is the only money-entry choke point for all three methods.

## 2. Design A — what "single atomic linearization point" would have to be

The decision "is this checkout payable *now*" and the write that records the initiation must be one
atomic database step, and **the stock authority must be part of it**, not only the Order cycle row.

Candidate primitive (Order-owned, single statement, no long transaction, no provider call inside it):

```sql
UPDATE order.reservation_cycles
   SET payment_attempt_id = @paymentAttemptId
 WHERE checkout_id = @checkoutId AND status = 'Active' AND expires_at > @serverNow
-- affected rows = 0  =>  fail closed, no Payment row, no provider call
```
Combined with an authoritative Inventory check of the cycle's bound reservations (`Held`), this is the
smallest honest linearization point. It cannot be built with today's seams — see §3 and §4.

## 3. Why a cycle-only gate is NOT sufficient — live PostgreSQL evidence (SELECT only)

`order.reservation_cycles` and `inventory.reservations` expire **independently**, in different modules,
closed/released by **different workers** (Order `UnpaidOrderExpiryReconciler` → `CloseExpiredDueAsync`,
15 s poll; Cart `CartExpiryScanner` → `InventoryDirectory.ReleaseExpiredHoldsAsync`, 5 s poll).

Measured on `tooba_alpha` (read-only, via the running `postgres-db` container):

| checkout | cycle `expires_at` | cycle `ended_at` (row closed) | close lag | reservation released at | release lag |
| --- | --- | --- | --- | --- | --- |
| 01a13309-f587… | 02:15:13.127436 | 02:15:24.021494 | **+10.89 s** | 02:15:17.431506 | +4.30 s |
| 01a09b8b-3e28… (c#2) | 17:53:32.259181 | 17:53:46.797934 | **+14.54 s** | 17:53:33.025213 | +0.77 s |
| 01a09b8b-3e28… (c#1) | 16:14:09.032498 | 16:14:21.671370 | **+12.64 s** | 16:14:13.945904 | +0.91 s |
| 01a09b33-751e… | 14:38:15.934186 | 14:38:29.609072 | **+13.67 s** | 14:38:17.745835 | +1.81 s |
| 01a09b2e-8848… | 14:32:53.160405 | 14:32:59.395605 | **+6.24 s** | 14:32:56.987029 | +3.83 s |
| 01a09b2b-4e35… | 14:29:21.685807 | 14:29:29.215954 | **+7.53 s** | 14:29:26.410354 | +2.72 s |

Facts derived:

- **F1 (status-only guard is unsafe).** For every observed expired cycle the row stayed
  `status = 'Active'` for **6–15 s after `expires_at`**, while the Inventory hold had already been
  `Released` (`inventory.reservations` today: 25 `Released`, 6 `Consumed`, 0 `Held`). A gate that trusts
  the cycle row's `status` alone authorizes money on released stock inside that window. The canonical
  `ReservationCyclePayabilityPolicy.IsActive` (`ReservationCyclePayabilityPolicy.cs:10-14`, `Active && SecondsRemaining > 0`)
  does check `expires_at`, so it rejects inside this window — a naive `status == 'Active'` gate does not.
- **F2 (the two deadlines coincide only by construction coincidence, not by invariant).**
  `reservation.expires_at == cycle.expires_at` to the microsecond in all observed rows because both are
  computed from the *same* `now` and the same store policy (`policy_source='store'`,
  `effective_hold_minutes=1` in all 11 cycles on this deployment). The code path that computes them is
  however *not* the same call:
  - reservation: `CheckoutDirectory.Reservations.cs:42` → `:114-131` (`_cyclePolicy` → `now.AddMinutes(policy.InitialHoldMinutes)`,
    **else** `_holdPolicy.ResolveInitialExpiresAt(now) ?? now.AddHours(2)`)
  - cycle: `CheckoutDirectory.Reservations.cs:83-111` (`now.AddMinutes(policy.InitialHoldMinutes)`) with
    policy from `:133-146`, whose **fallback is `new ReservationCyclePolicySnapshot(120, 120, 3, "platform")`**.
  If `_cyclePolicy` is null while a store/category hold setting clamps the reservation TTL to minutes, the
  cycle stays `Active` for up to 120 minutes after the stock hold is gone. Nothing in code or schema
  enforces `reservation.expires_at == cycle.expires_at`.
- **F3 (release is not subordinate to the cycle).** `InventoryDirectory.ReleaseExpiredHoldsAsync`
  (`Directories/InventoryDirectory.Reclaimer.cs:14-63`) releases purely on `status='Held' AND expires_at <= now`
  with `FOR UPDATE SKIP LOCKED`; `CloseExpiredDueAsync` (`ReservationCycleDirectory.cs:165-186`) only closes
  cycle rows and never touches Inventory. Neither waits for the other.

Consequence: the atomic boundary must include a reservation-authoritative, **locking** read of the cycle's
bound Inventory reservations — `ReservationCycle.ParseReservationIds()` (the row still carries them,
verified in DB: `reservation_ids` is a comma-separated uuid list).

## 4. Gap 1 — no locking seam exists in Inventory (new Inventory surface required)

`IOrderInventoryLifecyclePort` (Inventory.Contracts/Orders/OrderInventoryLifecycleContracts.cs:91-163)
exposes only **non-locking** reads for this concern:
`FindReservationAsync(Guid)` → `OrderInventoryReservationView` (`:135-138`) and
`GetSupplyStatusDetailAsync` (`:155-158`). A read of `Held` without a row lock is TOCTOU against the
reclaimer's `FOR UPDATE SKIP LOCKED` release.

Closing it needs an **Inventory-owned** primitive, e.g.
`TryHoldForPaymentAsync(IReadOnlyList<Guid> reservationIds)` implemented as
`SELECT … FOR UPDATE` (or a conditional `UPDATE … WHERE status='Held' AND expires_at > now` returning
affected rows) inside one short transaction. That is a new Inventory Contracts file + Inventory
implementation file, i.e. **outside the allowed file set** (Payment/Order and their Contracts), and it
implicitly decides whether an initiation may *consume*/freeze a hold — an Inventory state-machine
decision (today consumption happens only on verified payment success:
`CommitReservationForPaidOrderAsync`, and reacquisition on paid supply: `EnsurePaidDurableSupplyAsync`).

## 5. Gap 2 — typed error surface is pinned by Host.Tests (unchanged from 013)

`PaymentOperation.ExecuteAsync` maps a fault to `Result` only when `PaymentErrorCodes.IsKnown(code)`
(`Payment.Application/Composition/PaymentOperation.cs:19-51`); a foreign code propagates — proven by
`PaymentR1CqrsContractTests.cs:109-110` ("order.some_foreign_code"). The certified guards that pin the
Payment code surface are Host.Tests and are forbidden to edit here:
`PaymentModuleAmsc001W1MigrateGuardTests.cs:238` (`Assert.Equal(28, declared.Length)`) and `:89-127`
(bilingual key list for `PaymentErrors.resx` / `PaymentErrors.fa.resx`). Same analysis as 013 §3 — reuse of
an existing code is possible only by repurposing its meaning (`payment.unpaid.supply_unavailable` is
registered as "unpaid retry cannot be supplied"; `inventory.reservation.retry_limit_reached` covers only
the retry-limit sub-case), which `PaymentErrorCodes.cs:5-8` forbids.

An Order-owned code (e.g. `checkout.reservation.not_payable` + `OrderErrorCatalogContributor` descriptor +
`OrderErrors.resx`/`.fa.resx`) would propagate typed through the global boundary without touching any
Payment code constraint — **but** it needs the four Order error-surface files, and any new Payment-side
call-site also needs a dependency edge that the existing construction sites must tolerate (see §6).

## 6. Additional constraint found (not in 013): PaymentDirectory construction sites

`PaymentDirectory` is constructed **positionally in 7 Host.Tests files** (forbidden to edit):
`PaymentFoundationTests.cs:190,265,276`, `SettlementFoundationTests.cs:158`, `ReturnFoundationTests.cs:185`,
`WalletCheckoutRefundTests.cs:199`, `MarketplaceSettlementEventPathTests.cs:170`, `R2LiveMarketplaceSmokeTests.cs:101`.
Adding a required ctor parameter breaks Host.Tests compilation; an **optional** parameter would compile but
introduces a fail-open default (guard skipped) — the "optimistic guard" the task forbids. The clean
placement is therefore the DI-only `StorefrontPaymentOrchestrator` ctor (no positional construction exists:
`new StorefrontPaymentOrchestrator(` → 0 hits; Host registers it via DI at `Host/Program.cs:221`).

## 7. Probe feasibility (why the atomic invariant cannot be behaviourally proven here)

The only PostgreSQL-backed test harness in the repository is under `Host/Tooba.Host.Tests` (e.g.
`CheckoutOrderFoundationTests`), which this task forbids touching and whose DLLs the running Development
Host locks. Every module test project uses EF **InMemory** (`Tooba.Payment.Tests.csproj` →
`Microsoft.EntityFrameworkCore.InMemory`), which has no row locks, no `FOR UPDATE`, no cross-context
`TransactionScope` enlistment — so "simultaneous worker expiry" cannot be behaviourally proven there.
Implementing the CAS now would therefore ship an invariant that is provably unverifiable in the allowed
environment, and the task explicitly forbids claiming atomicity without that proof.

## 8. Minimal split proposal (for Architect authorization)

- **Step 1 (Inventory, new, ~2 files + tests):** `Inventory.Contracts` — add
  `Task<bool> TryLockHeldForPaymentAsync(IReadOnlyList<Guid> reservationIds, DateTimeOffset now, CancellationToken)`
  (name/semantics = Architect's choice; returns false, never throws for "not held"), implemented in
  `InventoryDirectory` with a single conditional `UPDATE`/`SELECT … FOR UPDATE` inside one short
  transaction. Decide explicitly whether initiation *freezes* the hold (recommended: read-lock verify only;
  consumption stays on verified success).
- **Step 2 (Order, ~3 files):** `Order.Contracts` port `IOrderCheckoutInitiationPayabilityPort`
  `EnsureInitiationPayableAsync(checkoutId, paymentId, ct)`; `Order.Infrastructure` adapter doing the
  conditional cycle `UPDATE` (§2) **and** the Step-1 Inventory verify, throwing the Order-owned typed code;
  Order module DI registration. Requires the error-surface decision of §5 (Order-owned code + descriptor +
  both resx) or an explicit named reuse.
- **Step 3 (Payment, ~2 files):** `StorefrontPaymentOrchestrator` ctor gains the Order.Contracts port and
  calls it after `GetForMutationAsync` and before `_payments.InitiateAsync` (covers wallet/manual/online in
  one place; no `PaymentDirectory` ctor change, no Host.Tests impact). Provider call stays outside any DB
  transaction — the Order CAS commits in its own short transaction before the provider request.
- **Step 4 (verification):** one Host.Tests PostgreSQL test (requires lifting the Host.Tests restriction
  for that task) proving (a) expired cycle → 0 payment rows, 0 provider calls; (b) released reservation
  while cycle row still `Active` → rejected; (c) valid active cycle → accepted; (d) replay unchanged.
  Without it the invariant stays **UNVERIFIED** and the final-click endpoint must still not be shipped.

## 9. Residual races that remain even after the above (must not be described as race-free)

Between the CAS commit and the provider request the hold can elapse by wall-clock (inherent; handled today
by the paid-supply reacquisition path `EnsurePaidDurableSupplyAsync`), and the cycle close worker can mark
the cycle `Expired` after our CAS (harmless ordering, same reason). The CAS closes the **competing
transaction** race only; it is not a global "no money after expiry" proof.

## 10. Verification performed

- Source inspection with file:line evidence (§1-§7). Production code changed: **0**. Schema/migrations: **0**.
  DB writes: **0** (`SELECT` only, `tooba_alpha`, via the running `postgres-db` container; the Development
  Host was not stopped or restarted). Host/Program/config/frontend: untouched. Hold durations/quotas: untouched.
- Tests executed: **none** — no code change exists to test, and the atomic invariant cannot be behaviourally
  proven with the available harnesses (§7). No PASS is claimed. The locked Host.Tests guards were read, not run.
