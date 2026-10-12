# TB-P0-INVENTORY-FULFILLMENT-RETRY-SAFETY-002 — Gate B/D: contract decision

Verdict: **BLOCKED_ARCHITECT_DECISION** — no production code was changed, nothing speculative was pushed.

## Gate B — can replay-safety be *proven* with the current data/contract?

| Required proof (task text) | Available evidence today | Provable? |
|---|---|---|
| The retry is for the **same logical consumption** | Reservation is the consumption unit and the caller supplies only `ReservationId`; `Quantity` is immutable on the reservation, so a “same reservation” call cannot request a different amount. | **Partially** — “same reservation” yes, “same logical consumption event” not attested (no consumption journal). |
| The retry is for the **same fulfillment/shipment** | Nothing binds a consumption to a consumer: no consumer/owner column on `reservations`, `ConsumeAsync` has no consumer parameter, `ExternalReference` is an *order-process* key shared by every line of the order, the Fulfillment flag lives in another database. Live proof of the sharing: all 5 member-1 reservations carry the identical `external_reference = order-commit:…:process:…`. | **NO** |
| The retry is for the **same owner/tenant** | Tenant isolation is physical (one database per tenant); the reservation is read in the caller's own tenant database. | Yes (by construction) |
| **Quantity cannot be deducted twice** | Refuted by the code: the deduction is an immediate autocommitted `ExecuteUpdateAsync` that is guarded only by stock arithmetic (`Reserved >= q && OnHand >= q`), never by the reservation state; `MoveTo` throws *after* that statement ran (W7/W8), and there is no unique consumption row. | **NO** |
| **Conflicting consumers / reused ids are rejected** | Nothing to reject with: no consumer identity exists to compare, and a lookup of a `Consumed` reservation cannot be distinguished from a replay. | **NO** |

Because three of the five required proofs fail on **current data and contracts**, the task's own Gate B rule applies verbatim: *“If this cannot be proven with current data/contract, STOP this scope rather than add a broad `Consumed`→success shortcut.”* A blanket “`Status == Consumed` ⇒ return success” inside `ConsumeAsync` is exactly that forbidden shortcut: it would grant silent success to *any* caller presenting a consumed reservation id, would mask W6/W4 anomalies, and would contradict the deliberately locked state-machine invariant (below).

## Locked invariants that a shortcut would violate

* `StockReservation.MoveTo` throws for any non-`Held` state, and the repo pins this on purpose:
  * `Tooba.Inventory.Tests/Behavior/InventoryReleaseIdempotencyTests.cs:65-68` — `Domain_move_to_released_rejects_already_terminal_status` asserts `MoveTo(Released)` then `MoveTo(Consumed)` both throw.
  * `Tooba.Host.Tests/PaidOrderReservationLifecycleTests.cs:146` — `CommitForPaidOrder_rejects_consumed_without_resurrect`.
  * `Tooba.Host.Tests/PaidOrderReservationLifecycleTests.cs:313` — `Commit_on_released_reservation_throws_not_active`.
* Two **source-text architecture guards** pin the *release* idempotency and slice `InventoryDirectory.cs` between `"public async Task ReleaseAsync(Guid reservationId"` and `"public async Task ConsumeAsync"` (`InventoryReleaseIdempotencyTests.cs:35-61`, `InventoryArchitectureGuardTests.cs:147-157`). Consume has no equivalent pinned behaviour — i.e. the “replay no-op” pattern exists only where it is *provably* safe (release must not credit stock twice).

## Gate D — candidate approaches

| Option | Shape | Gate-D status |
|---|---|---|
| **A. Blanket `Consumed` ⇒ success in `ConsumeAsync`** | pure code change, no schema, no contract change | **REJECTED.** Fails Gate B (no consumer identity, no double-deduction proof); masks conflicting consumers; contradicts the locked terminal-state invariant; makes W7/W8 worse by hiding them. |
| **B. Fulfillment-side “skip when already consumed” using a status read** | needs a new read on `IFulfillmentInventoryLifecyclePort` | **REJECTED for this task.** Cross-module public contract change *and* still TOCTOU-unsafe (read `Held` → someone else consumes → we consume again). |
| **C. Use `inventory.outbox_messages` as the idempotency ledger** | query the existing outbox row for the reservation | **REJECTED.** The outbox is a publish-only integration bus: opaque JSON payload, no indexed `reservationId` path, no uniqueness invariant, retention is an ops concern, and it still carries **no consumer identity**. Making a domain command depend on bus bookkeeping is an anti-pattern. |
| **D. Consumer-side intent journal in Fulfillment (commit “attempted” before the inventory call, skip on retry)** | new column/table in `fulfillment` | **REJECTED.** Requires a migration *and* is unsafe in the intent-committed-but-consume-never-happened window (would skip a **needed** consumption ⇒ lost reservation). Cannot be made safe without (E). |
| **E. Inventory-owned consumption journal + atomic consume (recommended)** | `reservations`-owned idempotency row, single transaction, optional consumer key in the port | **REQUIRES EXPLICIT ARCHITECT DECISION** — new table ⇒ **migration**, `IFulfillmentInventoryLifecyclePort` change ⇒ **cross-module public contract change**, and `ConsumeAsync` replay semantics ⇒ **new business semantics**. All three are Gate-D decision items. |
| **F. Move consumption to an outbox *command* consumed by Inventory (saga)** | Fulfillment publishes a command; Inventory consumes with its own inbox | Requires schema (inbox), a dispatcher change and contract change; larger surface than (E) with the same gate items. |

## Recommended bounded design (E) — for Architect approval, not implemented

Mirror the module's own `return_restock_inbox` precedent, but close its two-commit window:

1. **Schema (Inventory, one migration)** — `inventory.reservation_consumptions`:
   `ReservationId uuid PK`, `StockItemId uuid`, `Quantity numeric(18,6)`, `ConsumerKey varchar(128) null`, `ConsumedAt timestamptz`.
   The PK on `ReservationId` is the serialization primitive: two concurrent consumes can never both insert.
2. **Single atomic unit inside `ConsumeAsync`** — open one explicit transaction; perform the deduction and the `reservations.Status` compare-and-set (`WHERE reservation_id = … AND status = 'Held'` → `'Consumed'`) as *state-guarded* statements, insert the journal row, add the domain event, then **one** `SaveChangesAsync` + commit. This removes W7/W8 (the deduction can no longer outlive a failed status change) and W4 (the CAS + PK serialize concurrent callers).
3. **Replay semantics with proof** — if `Status == Consumed`: read the journal row by PK.
   * row present **and** `StockItemId`/`Quantity` match the reservation ⇒ proven replay of the same reservation ⇒ return success (no deduction).
   * row absent, or mismatch ⇒ throw a new typed error (e.g. `inventory.reservation.consume_conflict`) — *never* silent success. This is the W6/anomaly detector.
4. **Consumer identity (the only way to satisfy Gate B literally)** — extend the port compatibly:
   `ConsumeAsync(Guid reservationId, string consumerKey, CancellationToken ct)` (keep an overload for existing callers), where Fulfillment passes a stable per-dispatch key, e.g. `fulfillment:package:{packageId}:member:{shipmentId}:line:{orderLineId}`; the journal stores it, so a replay is verified against the *same* consumer, and a different consumer presenting the same reservation is rejected instead of granted.
   Note: both source-text guards slice between `ReleaseAsync(Guid reservationId` and the literal `public async Task ConsumeAsync`, so they keep matching if the parameter list grows.
5. **Fulfillment side (bounded, after the Inventory contract exists)** — keep the parent's single `SaveChangesAsync`; the retry path becomes: consume (now replay-safe) → mark flag → commit. W6 (“`Dispatched` member with a not-consumed reservation”) must be resolved by reconciling via the new journal/read rather than the unconditional `Consume=false` skip.

### Tests that must accompany (E)

* Failure injection **at each commit boundary**: fail before Inventory commit, after Inventory commit / before Fulfillment commit, after Fulfillment commit.
* Replay of the same `(reservationId, consumerKey)` ⇒ exactly one deduction, success on retry.
* Same `reservationId`, **different** `consumerKey` ⇒ typed conflict, zero deductions.
* Journal row missing while `Status == Consumed` ⇒ typed conflict (no silent success).
* Two concurrent consumes ⇒ exactly one deduction, one success + one proven replay or typed conflict.
* W6 reconciliation: `Dispatched` member + `Held` reservation ⇒ reported/surfaced, not silently skipped.
* Guard update: extend `InventoryReleaseIdempotencyTests`/`InventoryArchitectureGuardTests` so the new consume invariant is pinned the same way release is.

## Gate E — deployment guidance for the parent repair (HOLD/GO)

**GO (constrained).** The parent change (`04e3aa79…`) only converts per-member persistence into a two-phase, single-commit dispatch and validates generated tracking codes before any write; it strictly **narrows** the exposure because collision/validation failures now happen *before* any inventory call, and it removes the per-member partial commit that produced `MP-01A12F796EF1`. It does not create a new replay path and it does not change `ConsumeAsync`. Therefore the parent repair may be deployed, with these constraints:

* The Gate-C runbook may be used for the live package **only** as a single execution against a currently-`Held` reservation (member 2 is `Held`, `expires_at = null`) — verified read-only above.
* **HOLD** on any operator action that retries a dispatch whose reservation is already `Consumed` (W1/W7/W8). Until (E) lands, such a package must be recovered by Architect-approved manual reconciliation, not by re-running dispatch.
* No live mutation, no migration and no service restart was performed by this task.
