# TB-P0-PAYMENT-FRESH-ENTRY-GUARD-023 — analysis

Base: `8187ddde120918f22e31bac6bc80ea89dfe0cbc5` (== origin/main == Expected-Base-SHA, verified before any write).
Mode: BOUNDED_IMPLEMENTATION_OR_BLOCKED. Outcome: **BLOCKED_ARCHITECT_DECISION**.
Production/Test files changed: **0**. Endpoint behaviour **unchanged** (no partial gate published).

This file does **not** re-derive the 022 inventory (route, call order, 4d authority, 021 gate). It records only
what is new: two decisive constraints discovered while trying to implement the authorized slice, plus the
ready-to-apply design.

---

## 1. Constraint that decides the shape of the gate — Host.Tests compose the real bridge WITHOUT the cycle authority

The architect directed me to extend the existing `IPayableCheckoutReader`/`PayableCheckoutSnapshot` projection
(avoiding a required `PaymentDirectory` ctor parameter). That is the right boundary, but it puts the verdict into
`OrderPaymentBridge.GetPayableAsync`, which **six forbidden-to-edit Host.Tests files instantiate with only two
arguments**, i.e. `cycles: null, cyclePolicy: null`, and then perform real fresh initiations that they assert
succeed:

| file:line of `new OrderPaymentBridge(...)` | second arg | what it does next |
|---|---|---|
| `Host.Tests/PaymentFoundationTests.cs:188` | `new OrderInventoryLifecycleAdapter(new UnusedInventoryDirectory())` | `PaymentDirectory.InitiateAsync(...)` and asserts `Pending`, amount `207100m`, redirect `/payment/sandbox`, then a same-key replay (`:193-208`) |
| `Host.Tests/MarketplaceSettlementEventPathTests.cs:169` | same | real fresh initiation |
| `Host.Tests/R2LiveMarketplaceSmokeTests.cs:100` | same | real fresh initiation |
| `Host.Tests/ReturnFoundationTests.cs:183` | same | real fresh initiation |
| `Host.Tests/SettlementFoundationTests.cs:157` | same | real fresh initiation |
| `Host.Tests/WalletCheckoutRefundTests.cs:196` | same | real wallet/manual fresh initiation |

Those checkouts are seeded directly (`PaymentFoundationTests.SeedCheckout:304+`) with **no reservation cycle** and
**no Inventory reservations**, and the injected `UnusedInventoryDirectory` throws on any Inventory access.
Consequences:

* a fail-closed verdict when `_cycles is null` breaks **all six** files;
* a fail-closed verdict when a line has no `ReservationId` breaks them too (their lines are seeded by hand);
* any Inventory read on that path throws in their composition.

Editing or running those files is forbidden by this task, and I cannot verify them.

**Only shape that keeps the composed app safe and those tests green:** the projection reports a distinct
`NotEvaluated` state when the Order cycle authority is absent, and `PaymentDirectory` blocks every state except
`Payable` and `NotEvaluated`. Production is unaffected by this concession because the composed Host always injects
the authority through DI — `OrderModule.cs:90` registers `IReservationCycleDirectory`,
`OrderModule.cs:99` registers `IPayableCheckoutReader, OrderPaymentBridge` — so in the deployed application
`_cycles` is never null and the gate is live. The concession is real and must be named: **in any composition that
omits `IReservationCycleDirectory`, the guard is inert by design, not by accident.** The architect must approve
this explicitly (D1 below), because the alternative is a storefront regression in six certified Host test files.

A "no cycle for a checkout that *has* the authority" case is different and stays fail-closed:
`ReservationCyclePayabilityPolicy.IsActive` is false ⇒ `CycleNotPayable` ⇒ 409.

## 2. Constraint that decides the delivery — the Order-side truth cannot be proved in this slice

The architect's rule: *"If tests cannot prove these claims in time/scope, implement no publicly reachable change
and report BLOCKED"*, and *"NEVER publish an optimistic gate."* The gate's authority is the Order-side
computation, so it is the part that must be proven. It cannot be, in this project, without new test scaffolding:

* `src/backend/Modules/Order/Tooba.Order.Tests/Tooba.Order.Tests.csproj` has **no** `Microsoft.EntityFrameworkCore.InMemory`
  package and **no** `ProjectReference` to `Tooba.Order.Infrastructure`, and the project contains **no**
  `UseInMemoryDatabase` harness anywhere (verified by scan). `OrderPaymentBridge` lives in
  `Tooba.Order.Infrastructure` and needs an `OrderDbContext` with `CheckoutGroup → SellerOrders → Lines`.
* Proving the bridge therefore needs, at minimum: two csproj additions (a package + a project reference), a
  description-matching in-memory Order harness, and two fakes (`IReservationCycleDirectory`,
  `IOrderInventoryLifecyclePort`) — plus a second suite in `Tooba.Payment.Tests` for the enforcement side.
* `Tooba.Payment.Tests` can only prove the **Payment-side** enforcement (block/permit, no row creation, check
  ordering). It cannot prove that the Order-side verdict is truthful.

Publishing the enforcement without the Order-side proof would ship untested logic on the money path — exactly the
optimistic gate the task forbids. Timebox: ~16 of the 20 HARD minutes were consumed by the above investigation; the
remaining budget cannot produce csproj additions + an Order harness + two fakes + two suites + two focused runs.

## 3. Ready-to-apply slice (exact files, decisions D1/D2 pending)

**S1 — `src/backend/Modules/Payment/Tooba.Payment.Contracts/Checkout/OrderPaymentProjectionContracts.cs`** (additive)
```csharp
/// <summary>حکم «پول تازه» از دید مالک سفارش/موجودی. فقط Payable اجازهٔ شروع پرداخت تازه می‌دهد.</summary>
public enum FreshPaymentPayability
{
    NotEvaluated = 0,          // ترکیب بدون اقتدار چرخه (فقط Composition قدیمی/تست) — به‌صراحت و نه پنهانی
    Payable = 1,
    ReservationSetIncomplete = 2,
    CycleNotPayable = 3,
    RetryLimitReached = 4,
    HoldUnavailable = 5,
}
// PayableCheckoutSnapshot gains: FreshPaymentPayability FreshPaymentPayability = FreshPaymentPayability.NotEvaluated
```
The optional trailing parameter keeps every existing construction site (Host.Tests `StubPayableReader`
`PaymentFoundationTests.cs:270`, Payment tests) compiling and unchanged.

**S2 — `src/backend/Modules/Order/Tooba.Order.Infrastructure/Integrations/Payment/OrderPaymentBridge.cs`**
`GetPayableAsync`: add `.ThenInclude(x => x.Lines)` to the existing read and compute the verdict read-only:
`_cycles is null ⇒ NotEvaluated`; else lines/reservation ids from persisted lines only
(missing/empty/one-id-on-two-lines ⇒ `ReservationSetIncomplete`); cycle via
`IReservationCycleDirectory.GetProjectionAsync(checkoutId, DateTimeOffset.UtcNow, supplyStatus, ct)` +
`ReservationCyclePayabilityPolicy.IsActive` (false ⇒ `CycleNotPayable`; `HasReachedRetryLimit` ⇒
`RetryLimitReached`); then per distinct reservation `IOrderInventoryLifecyclePort.FindReservationAsync` requiring
`Status == "Held"` (the Inventory-owned projection of `StockReservationStatus.Held`) and
`ExpiresAt is null || ExpiresAt > now` (else `HoldUnavailable`). No write, no extension, no new TTL, and
**never a throw merely because the cycle expired** (verification/refund reads keep working).

**S3 — `src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Directories/PaymentDirectory.cs`**
Fresh path (after the 021 conflict gate and mode check, before amount/allocation/`gateway.InitiateAsync`):
`if (payable.FreshPaymentPayability is not (Payable or NotEvaluated)) throw new ContractOperationException(PaymentErrorCodes.ReservationNotPayable);`
Failed-prior branch (4c): read `_orders.GetPayableAsync(existing.CheckoutId, …)` before the retry
`gateway.InitiateAsync` and apply the same rule (null ⇒ not payable) so a failed prior cannot bypass fresh-money
rules; same-key succeeded/pending replay and the 021 conflict semantics stay untouched.

**S4 — error surface** (authorized additive): `PaymentErrorCodes.ReservationNotPayable =
"payment.reservation.not_payable"` added to `KnownCodes`; catalog descriptor
`D(..., ErrorClassification.Conflict, StatusCodes.Status409Conflict, "Conflict")`; one key in
`PaymentErrors.resx` and `PaymentErrors.fa.resx`. Count-only alignment in the two authorized Host guard files:
`PaymentModuleAmsc001W1MigrateGuardTests.cs:237` 29→30 and `PaymentModuleAmsc001W3CertGuardTests.cs:253` 29→30,
`:288` 25→26, `:320` 25→26. No assertion deleted or weakened; no other Host file touched. Historical
`tmar-current-state.json` R1/R2/R3 remain byte-identical (documented for a separate additive R4 follow-up, as
already reported in 021 §4).

**S5 — tests (the reason this slice is BLOCKED, see §2)**
* Order.Tests: new harness + `IReservationCycleDirectory` / `IOrderInventoryLifecyclePort` fakes proving
  valid Held + active cycle ⇒ `Payable`; expired cycle ⇒ `CycleNotPayable`; cycle Active but hold `Released` ⇒
  `HoldUnavailable`; retry-exhausted ⇒ `RetryLimitReached`; no cycle for an authority-bearing composition ⇒
  `CycleNotPayable`; `_cycles is null` ⇒ `NotEvaluated`.
* Payment.Tests: `NotEvaluated`/`Payable` ⇒ initiation proceeds; every blocked state ⇒
  `payment.reservation.not_payable`, **no gateway invocation and no payment/attempt row**; failed-prior retry
  cannot bypass; 021 cases and identical-method replay stay green.
* No PostgreSQL locking or cross-module atomicity claim is made or faked.

## 4. Truthfulness / TOCTOU (declared, not hidden)

The verdict is a **point-in-time read**, not a reservation of capacity: the Inventory row can be released or
expire between `GetPayableAsync` and the gateway call, and the cycle can close in the same window. The read is
truthful about the instant it was taken (real Inventory row state through the owner-provided port, real cycle
projection through the canonical Order policy) and it is *not* presented as atomic. Closing the residue needs the
Order-owned authoritative reacquisition path (already reachable via `/payments/{paymentId}/unpaid-retry` →
`RetryUnpaidCoreAsync` → `IOrderUnpaidRetrySupplyPort.EnsureRetrySupplyAsync`, `Orchestrator.cs:446`) or a future
correlated attempt+hold contract — not a bigger optimistic check here.

## 5. Verification performed

Static reads only (S1–S5 are not written to disk): `OrderPaymentProjectionContracts.cs`,
`OrderPaymentBridge.cs:50-79`, `PaymentDirectory.cs:61-222`, `PaymentStorefrontEndpoints.cs:22-55`,
`StorefrontPaymentOrchestrator.cs:57-198`, `OrderPaymentHoldPreparationService.cs:80-205`,
`ReservationCycleContracts.cs`, `ReservationCyclePayabilityPolicy.cs`, `ReservationCycle.cs` (status values),
`OrderInventoryLifecycleContracts.cs:136`, `OrderInventoryLifecycleAdapter.cs:174-242`, `InventoryDirectory.cs:216-238`,
`StockReservationStatus.cs`, `OrderModule.cs:90-101`, `PaymentModule.cs:40-105`, the six Host.Tests construction
sites, `PaymentFoundationTests.cs:180-300`, and the Order/Payment test csproj contents.
No build, no test run: zero production and zero test files changed, and Host/Host.Tests must not be built or run.
Honest counts: 0 files changed, 0 tests added, 0 tests run.

## 6. Decisions requested

* **D1** Approve the `NotEvaluated` concession (§1) — the only shape that keeps the six certified Host.Tests files
  green without editing them — or authorize the alternative (a new required port registration plus authorized
  edits to those six Host.Tests files, which this task forbids).
* **D2** Authorize the two `Tooba.Order.Tests.csproj` additions (`Microsoft.EntityFrameworkCore.InMemory`,
  `ProjectReference → Tooba.Order.Infrastructure`) needed to prove the Order-side verdict, or designate another
  seam for that proof.
* **D3** Confirm the error surface in S4 (code, 409, the two guard files, counts 30/30/26) and that the additive
  R4 SoT follow-up stays out of this wave.
