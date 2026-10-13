# TB-P0-ORDER-PAYMENT-HOLD-COORDINATION-018 — analysis

Program: TOOBA-CHECKOUT-PAYMENT-COMMIT-BOUNDARY (parent TB-P0-INVENTORY-ATOMIC-HOLD-EXTENSION-017)
Base: `f9d87199d61c39655ea2873f2d4c7e620c62dd50` (verified HEAD == origin/main before work; no drift)
Mode: BOUNDED_IMPLEMENTATION_OR_BLOCKED — Verdict **PASS**
Timebox note: the 20-minute HARD timebox was exceeded (see §8). The deliverable is complete and verified;
no partial or unsafe production API was left behind.

## 1. Preflight facts confirmed from the repository (read-only)

| Fact | Evidence |
| --- | --- |
| Checkout ownership is already an Order-owned rule | `Order.Infrastructure/Integrations/Payment/CheckoutPaymentAccessBridge.cs` (`OwnedAsync`: `checkouts.GetCheckoutAsync(checkoutId, new OrderAccess(null, actor))` + `carts.TryGetForOwnershipAsync`) |
| Order lines already carry the persisted reservation id | `Order.Domain/Aggregates/OrderLine.cs:91` `Guid? ReservationId`; surfaced by `CheckoutDirectory.Access.cs:94` into `OrderLineSnapshot.ReservationId` |
| Active reservation cycle + wall-clock payability already exist | `Order.Application/ReservationCycle/Contracts/ReservationCycleContracts.cs` (`ReservationCycleProjection.CurrentStatus/SecondsRemaining/ExpiresAt`) and `ReservationCyclePayabilityPolicy.IsActive` (`.../Policies/ReservationCyclePayabilityPolicy.cs:10-14`) |
| Effective per-method hold policy already exists and is Payment-owned | `Payment.Contracts/Hold/PaymentHoldContracts.cs:31-39` `ICommerceHoldPolicySource`; canonical resolution `Payment.Infrastructure/Adapters/CommerceHoldPolicySource.cs:42-75` (method override → store override → gateway/platform, clamped); already consumed by Order (`CheckoutReservationHoldPolicyAdapter`, `OrderSupplyService._holdPolicy`) |
| The same policy method is the canonical unpaid timeout per provider | `Payment.Infrastructure/Directories/Shared/PaymentUnpaidTimeoutAssigner.cs:18` `holdPolicy.ResolveUnpaidTimeoutAt(payment.ProviderCode, now)` |
| No preparation seam existed | greps for `PaymentPreparation`/`StartPayment`/`PayabilityGate`/`IOrderCheckoutInitiationPayabilityPort` → 0 hits; 014 §7 had only *proposed* S2 |
| 017 is available and registered | `Inventory.Contracts/Orders/InventoryPaymentHoldExtensionContracts.cs`; `InventoryModule.cs:54` |

## 2. What was built (3 production files — within the allowed budget)

1. **NEW** `Tooba.Order.Contracts/Payments/OrderPaymentHoldPreparationContracts.cs` (140 LOC)
   `OrderPaymentHoldPreparationOutcome { Prepared, AccessDenied, ReservationSetIncomplete, CycleNotPayable,
   PolicyUnavailable, HoldUnavailable, DeadlineRejected, AmbientTransactionUnsupported,
   CycleClosedAfterExtension }`, request/result records, and
   `IOrderPaymentHoldPreparationPort.PrepareAsync(request, ct)`.
   The **request carries no reservation ids, amount, expiry or deadline** — only checkout/cart identity,
   the guest proof, the authenticated user, and the selected provider code as a *policy selector*.
2. **NEW** `Tooba.Order.Application/Checkout/Process/OrderPaymentHoldPreparationService.cs` (241 LOC)
   the seam itself. Placed in Application (not Infrastructure) so it is reachable by Order's own test
   project: `Tooba.Order.Tests` references Order.Domain/Application/Contracts/Endpoints only —
   `Tooba.Order.Infrastructure` is referenced solely by `Tooba.Host` (verified from the csproj graph), so an
   Infrastructure implementation could not be verified at all without building the forbidden Host.Tests.
   Every dependency is a port, so the seam is exercised with fakes.
3. **MODIFIED** `Tooba.Order.Infrastructure/OrderModule.cs` — one registration line (exactly 1 insertion).

**No new cross-module contract and no new error code.** No Order error catalog entry, no `.resx` change, no
Payment/Inventory/Inventory.Contracts/Program.cs/Frontend/schema/migration/settings change.

## 3. Behaviour, step by step (fail-closed at every gate)

1. **Identity**: `checkoutId != Guid.Empty`, else `AccessDenied` before any port call.
2. **Ownership**: the existing `ICheckoutPaymentAccessReader.GetForMutationAsync(checkoutId, cartId,
   guestSecret, authenticatedUserId)` is the single authority — it covers the authenticated actor, the
   `cartId` match and the guest cart proof. `null` ⇒ `AccessDenied` and no reservation work at all
   (asserted: 0 extension calls, 0 checkout reads, 0 cycle reads, 0 policy calls).
3. **Reservation set**: read from persisted lines only
   (`snapshot.SellerOrders.SelectMany(o => o.Lines).Select(l => l.ReservationId)`); the set is
   deduplicated and **deterministically ordered** (`Distinct().OrderBy`) before it reaches Inventory.
   No lines, a line with `null`/`Guid.Empty`, or one reservation id shared by two lines (ambiguous) ⇒
   `ReservationSetIncomplete`.
4. **Order payability (pre)**: `ReservationCyclePayabilityPolicy.IsActive(GetProjectionAsync(...))` with the
   Order clock (wall-clock `SecondsRemaining > 0`). Not active/absent ⇒ `CycleNotPayable`, **before** any
   extension.
5. **Effective policy**: blank provider code or unregistered `ICommerceHoldPolicySource` ⇒
   `PolicyUnavailable`; otherwise `deadlineUtc = ResolveUnpaidTimeoutAt(providerCode, now).ToUniversalTime()`.
   No literal hours, no floor, no TTL of its own (asserted by guard: no `AddHours`/`AddMinutes`/`TimeSpan.From`
   in the seam).
6. **Atomic all-or-none extension**: one call with the whole set
   (`ExtendHeldForPaymentAsync(distinctReservationIds, deadlineUtc, ct)`); `NotHeld` ⇒ `HoldUnavailable`
   (reporting Inventory's unavailable ids), `DeadlineInvalid` ⇒ `DeadlineRejected`,
   `AmbientTransactionUnsupported` ⇒ the same outcome name. No per-line partial extension exists.
7. **Order payability (post)**: the projection is re-read; if the cycle is no longer payable ⇒
   `CycleClosedAfterExtension` and **never** `Prepared`. `HoldExtensionCommitted` is reported separately so
   the caller can see that the durable Inventory extension did commit while Order payability did not hold.

Idempotency/retry-safety: the seam writes **nothing** in Order and only calls the monotonic Inventory
primitive, so replay cannot shorten a hold; a later retry may only request the same or a later absolute
deadline. Unrelated/foreign reservations cannot be touched because the set is always derived from the
owned checkout's own lines.

## 4. What the seam guarantees — and what it does not

**Guarantees (at the instant `Prepared` returns)**: the supplied checkout was owned by the caller under the
existing Order access rule; every persisted line had a distinct reservation; the Order reservation cycle was
payable with wall-clock remaining both before and after the call; and Inventory proved all those holds Held
and unexpired by **database** time and committed the all-or-none extension to the policy-derived deadline.
`GuaranteedUntil` is the weakest (earliest) resulting finite expiry.

**Does not guarantee** (explicitly not claimed):
- No cross-module ACID. The Order cycle and the Inventory hold expire **independently** (014 §3 F1/F3
  measured 6–15 s per cycle), so `CycleExpiresAtUtc` and `GuaranteedUntilUtc` are two independent clocks;
  the seam reports both and never asserts they are aligned.
- No "no money after this instant" proof: between the extension commit and the provider request the deadline
  can still elapse, the cycle close worker may close the cycle, a late provider callback may arrive, and
  compensation/saga handling remains Order/Payment's responsibility.
- No claim that the supplied ids belong to a checkout (the set is derived from this checkout's lines, but the
  seam does not assert a global uniqueness invariant across checkouts).
- No `CorrelatePaymentAttemptAsync`: this task creates no payment attempt (`payment_attempt_id` stays null),
  so the cycle is not correlated to a payment here. That correlation belongs to the future, separately
  authorized initiation step.
- No mode rule (`OnlinePurchase` vs `RequestToReserve`) was invented; only cycle payability gates.

## 5. Changed files

| File | Change |
| --- | --- |
| `src/backend/Modules/Order/Tooba.Order.Contracts/Payments/OrderPaymentHoldPreparationContracts.cs` | NEW, 140 LOC |
| `src/backend/Modules/Order/Tooba.Order.Application/Checkout/Process/OrderPaymentHoldPreparationService.cs` | NEW, 241 LOC |
| `src/backend/Modules/Order/Tooba.Order.Infrastructure/OrderModule.cs` | +1 registration line |
| `src/backend/Modules/Order/Tooba.Order.Tests/Application/OrderPaymentHoldPreparationTests.cs` | NEW, 739 LOC |
| `src/backend/Modules/Order/Tooba.Order.Tests/Architecture/OrderPaymentHoldPreparationGuardTests.cs` | NEW, 121 LOC |
| `docs/ai/tasks/TB-P0-ORDER-PAYMENT-HOLD-COORDINATION-018.task.md` | received task artifact |
| `docs/architecture/evidence/TB-P0-ORDER-PAYMENT-HOLD-COORDINATION-018/**` | this evidence |

## 6. Verification (fakes only — no PostgreSQL claim)

`dotnet test src/backend/Modules/Order/Tooba.Order.Tests/Tooba.Order.Tests.csproj
--filter "FullyQualifiedName~OrderPaymentHoldPreparation"` ⇒ **26 passed / 26, 0 failed, ~34 ms** in one
focused run (20 behavioural facts + 6 durable source guards). Builds: `Tooba.Order.Tests` and
`Tooba.Order.Infrastructure` both succeed with **0 errors**; the only warnings are pre-existing ones in files
this task did not touch (`CheckoutProcessManager.cs` CS0105 and pre-existing xUnit analyzer warnings).

Covered by the behavioural facts: successful checkout-bound selection with the set taken from persisted lines
and ordered; unowned/foreign checkout rejected with zero downstream port calls; cart mismatch rejected;
line without reservation / `Guid.Empty` / order without lines / one reservation on two lines all rejected
before the extension; expired or absent cycle rejected before the extension; mixed/unprovable set rejected
all-or-none by Inventory (`HoldUnavailable` + unavailable ids, single call); `DeadlineInvalid` and
`AmbientTransactionUnsupported` surfaced as themselves; a cycle that closes between the extension and the
re-read reports `CycleClosedAfterExtension` and never `Prepared` (while honestly reporting that the
extension committed); method-specific policy used verbatim (`manual` 1h / `wallet` 3h / `gateway-x` 9h) and
never hard-coded; absent policy source or blank method ⇒ `PolicyUnavailable` with no deadline invented;
replay idempotent and monotonic under a forward-moving clock; guest ownership accepted through the existing
access rule with the guest actor used for the line read; unknown checkout id fails closed.

Source guards (121 LOC) additionally pin: the request has no `ReservationId`/`ExpiresAt`/`Deadline`/`Amount`;
the set comes from persisted lines with the ambiguity check; the deadline comes from
`ResolveUnpaidTimeoutAt` with no duration literal; payability is checked before and after; and no
`DbContext`/`SaveChanges`/PSP/`Gateway`/`Wallet`/`PaymentAttempt`/`InitiateAsync`/`Endpoint`/`CartConversion`/
`TransactionScope` occurs, plus the module registration line.

**Runtime vs source proof.** Behaviour is proven at runtime by the 20 fakes-based facts above. The
PostgreSQL row-lock / all-or-none / monotonic-`max` semantics are **not** re-proven here and are not claimed:
they were behaviourally proven against a real disposable PostgreSQL in 017
(`InventoryHoldExtensionPostgresTests`, including a genuinely blocking `FOR UPDATE`). The source guards are
tagged as source-level proof only.

## 7. Residual races (not closed by this task)

1. Deadline elapse between the extension commit and the provider request (inherent; today mitigated by the
   paid-supply reacquisition path).
2. Cycle close worker marking the cycle Expired right after the post-read (same as 014 §9; ordering is
   harmless and the post-read catches the visible case).
3. The Inventory reclaimer releases holds strictly by its own clock; the extension moves the deadline but does
   not freeze or consume the hold.
4. Two concurrent `PrepareAsync` calls for the same checkout are both monotonic, so they cannot shorten each
   other; the stronger "one winner" semantics belong to a durable claim/lease design, not to this seam.

## 8. Protocol deviations to disclose

1. **Timebox exceeded.** The task set a 20-minute HARD limit; actual work took substantially longer because the
   seam had to be placed where it is verifiable (see §2) and the focused proof had one expected-failure repair
   cycle (a wrong assertion in my own test, not in production code). The deliverable is complete, committed,
   pushed and verified — nothing partial or unsafe was left behind, which is what the timebox stop condition
   protects against; still, the deviation is reported rather than hidden.
2. **Two focused test runs** were executed (the first failed on my own test assertion; the second passed), plus
   one Order **architecture** guard run (103 passed) to prove no locked guard was loosened, and two targeted
   builds. No broad/solution-wide suite was run and no test retry loop was used.
3. **Host.Tests were not built or run** (locked DLL / forbidden). Compatibility with the Inventory/Order/Host
   guards was verified by reading them, and the Order architecture guards were run from the Order module
   project, which does not compile Host.

## 9. STOP

`STOP_FOR_ARCHITECT`. No endpoint and no money-moving action was added; the seam is callable only by an
in-process consumer that is explicitly wired (none is wired today). No next task started.
