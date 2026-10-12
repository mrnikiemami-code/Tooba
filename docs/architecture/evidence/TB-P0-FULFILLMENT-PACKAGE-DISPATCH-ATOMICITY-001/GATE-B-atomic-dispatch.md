# GATE B — Atomic consolidated-package dispatch (F2)

Verdict: **PARTIAL_PASS_WITH_ARCHITECT_STOP**

* Fulfillment-owned atomicity (all member tracking + status updates + the package transition in
  **one** commit, with fail-fast validation before any write): **IMPLEMENTED and proven**.
* End-to-end atomicity **including the cross-module inventory reservation consumption**:
  **BLOCKED — requires an Architect decision.** It cannot be achieved with the current repository
  transaction/UoW semantics, and every naive way of forcing it produces a strictly worse,
  permanently-stuck state.

## 1. Traced write path before the repair (why a partial dispatch could be persisted)

Call chain of `FulfillmentDirectory.DispatchConsolidatedPackageAsync` (pre-repair):

```
foreach (active member)                       // no transaction around the loop
    AssignTrackingCoreAsync(...)              // -> _db.SaveChangesAsync()   [COMMIT #1 per member]
    DispatchShipmentCoreAsync(...)            // -> ConsumeInventoryForShipmentAsync(...)
                                              // -> _inventory.ConsumeReservationAsync(...)  (immediate, other module)
                                              // -> _db.SaveChangesAsync()   [COMMIT #2 per member]
statuses = LoadMemberShipmentStatusesAsync(...)   // AsNoTracking read
package = LoadMutablePackageAsync(...)
package.MarkDispatched(...)
await _db.SaveChangesAsync();                 // [COMMIT #3] package transition
```

* All member/package persistence goes through **one** `FulfillmentDbContext` (`_db`).
* EF Core already wraps a single `SaveChangesAsync` in an implicit transaction, so *one*
  `SaveChangesAsync` **is** an atomic DB commit for Fulfillment-owned state.
* The defect was therefore not "no transaction primitive": it was **N commits inside the loop**.
  Member 1 was committed and stayed `Dispatched`; member 2 threw; the package stayed `Created`
  and (because the package lock blocks direct member dispatch and the loop is not retry-safe for
  a partially-written batch) the package could never be completed.
* **External side effect:** `ConsumeInventoryForShipmentAsync` → `IFulfillmentInventoryGateway`
  → `InventoryDirectory.ConsumeAsync` runs on the **Inventory module's own DbContext** and uses
  `ExecuteUpdateAsync` + its own `SaveChangesAsync`. It is a *separate connection/transaction*
  and is physically outside any transaction opened on `FulfillmentDbContext`.
* `StockReservation.MoveTo` throws `inventory.reservation.not_active` unless the reservation is
  still `Held` — i.e. **`ConsumeAsync` is NOT idempotent** and cannot be safely replayed.

## 2. Implemented repair (bounded, safe, no unsafe transaction hack)

`DispatchConsolidatedPackageAsync` is now two phases:

* **Phase 1 — validate everything, write nothing** (`BuildDispatchStepsAsync`):
  * loads every active member and validates its shipment status
    (`Dispatched/InTransit/Delivered` → *skip*, anything other than `Created` → throw
    `fulfillment.package.member_state_changed`);
  * generates the collision-free member references (Gate A) and rejects duplicates **inside the
    batch** (`Dictionary<string, Guid>` keyed case-insensitively) before touching the DB;
  * runs **one** query (`EnsureGeneratedTrackingReferencesAreFreeAsync`) that checks every newly
    generated code against the global unique guard, excluding the members themselves;
  * a failure here throws **before any mutation**, so nothing can be persisted.
* **Phase 2 — apply and commit once**:
  * applies all generated tracking references, all `ApplyShipmentDispatched` transitions and the
    inventory consumption, then
  * builds the member status dictionary from the tracked (in-memory) shipments,
  * calls `package.MarkDispatched(...)` and exactly **one** `SaveChangesAsync` → one atomic commit
    covering every member and the package transition.

Deliberately **not** done:
* no `BeginTransactionAsync` wrapping the loop (it would not make the Inventory consumption
  atomic, and it would convert a *retryable* failure into a *stuck* one — see §3);
* no silent skipping of not-yet-dispatched members;
* no invented state transitions, no schema/migration change.

`LoadMemberShipmentStatusesAsync` is intentionally no longer used by this path; the statuses are
read from the tracked entities loaded by `LoadAsync`, which are the same rows the commit writes.
(It is still used by `DeliverConsolidatedPackageAsync` and is untouched.)

## 3. Why Gate B's cross-module half is BLOCKED (Architect decision required)

The package loop must consume reservation(s). Every achievable ordering has a hole:

| Ordering | Failure mode | Result |
| --- | --- | --- |
| consume inventory *inside* the Fulfillment transaction (current relative order, one commit) | Inventory consumption already committed on a foreign connection, then the Fulfillment commit fails | Reservation is `Consumed`, `ReservationConsumed` flag rolled back to `false`, dispatches not persisted. Retry re-consumes → `inventory.reservation.not_active` → **permanently stuck** |
| consume inventory *after* the Fulfillment commit | commit succeeds, consumption fails | Package is `Dispatched`, next call returns early (idempotent) → reservations stay `Held/Reserved` → **silent stock leak, no error surfaced** |
| put a real DB transaction over both modules | Requires single-connection / shared-transaction / outbox-based coupling across module boundaries | Violates Contracts-only module isolation; out of the bounded scope and an architecture change |

Consequence: the repaired path is strictly better than the pre-repair path for every failure that
occurs **at or before validation** (the collision class, which is the reported defect — it can no
longer persist anything), but the residual cross-module window documented above remains and
**must not be claimed as atomic**.

Recommended follow-up (Architect-owned, not implemented here):
1. make reservation consumption idempotent (accept `Consumed` as a no-op success) so a retry after
   a failed commit is safe; then
2. either consume inventory for a whole package as one cross-module unit, or record the
   consumption intent through the existing outbox and let a consumer replay it.

## 4. Focused failure-injection test (passing)

`Package_dispatch_is_single_commit_and_leaves_no_partial_state_on_late_failure`
(`focused-package-dispatch-tests.txt`) injects `ThrowingInventoryGateway(failOnConsumeCall: 2)`,
so validation succeeds and the failure happens on the **second** member's consumption — after the
first member was already mutated in memory.

Assertions (read back with `AsNoTracking`, i.e. committed DB truth):
* both member shipments remain `Created`;
* neither member has a persisted tracking reference;
* the package is still `Created`;
* then a healthy directory completes the same package: both members `Dispatched`, package
  `Dispatched`, codes equal `<central>-<shipmentId:N>`, and the previously-consumed member 1 is
  not rewritten.

Second focused test `Package_dispatch_rejects_pre_taken_generated_code_without_partial_dispatch`
pre-registers the code that member 2 will generate on a decoy shipment in another checkout; the
dispatch fails with `fulfillment.tracking.duplicate` and **nothing** is persisted (both members
`Created`, no tracking, package `Created`, decoy code untouched). Under the pre-repair code the
first member would already have been dispatched and committed.

## 5. Outstanding risk / rollback

* Residual risk: the cross-module window in §3 (unchanged in kind from before the repair, now
  strictly narrower because validation failures can no longer persist anything).
* Rollback: revert the single commit; behaviour returns to per-member commits plus the
  eight-character suffix collision.

## 6. Honest limitation

The existing live stuck package has **not** been repaired by this code change. Deploying this fix
does not mutate any row; it only makes the retry path (Gate C) able to complete the package.
