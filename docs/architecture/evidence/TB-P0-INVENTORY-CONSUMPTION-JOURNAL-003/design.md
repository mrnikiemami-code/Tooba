# TB-P0-INVENTORY-CONSUMPTION-JOURNAL-003 — Design (Inventory-owned atomic consumption)

Mode: `IMPLEMENT_BOUNDED_INVENTORY_ATOMIC_CONSUME_WITH_STOP_GATES`
Parent: `TB-P0-INVENTORY-FULFILLMENT-RETRY-SAFETY-002` (BLOCKED_ARCHITECT_DECISION → Design E approved in principle)
Starting HEAD: `fa2c9209ec2d03216e823cf4e2c069178df868c4`

## 1. Problem restated (from the parent evidence)

`InventoryDirectory.ConsumeAsync` did three things in **three separate physical statements with no
enclosing transaction**:

1. `UPDATE inventory.stock_positions SET on_hand = on_hand - q, reserved = reserved - q`
   (immediate, autocommitted),
2. `StockReservation.MoveTo(Consumed)` — only reachable *after* step 1 succeeded,
3. `SaveChangesAsync()` for the reservation row + outbox.

Consequences (parent `failure-matrix.md`, windows W6–W8):

* failing between 1 and 2/3 leaves stock deducted with the reservation still `Held` (debt with no
  durable record);
* re-invoking consume on a terminal reservation could deduct again when `Reserved >= q` happened to
  still hold (double deduction);
* there was no durable way to prove that a repeated consume belongs to the *same* logical
  consumption, because `inventory.reservations` carries no consumer identity (`ExternalReference`
  is an order-process level id shared by all lines) and `IFulfillmentInventoryLifecyclePort`
  accepted only `Guid reservationId`.

## 2. Implemented design (Design E)

### 2.1 Durable consumption journal (Inventory-owned, single PK = reservation)

```text
inventory.reservation_consumptions
  reservation_id  uuid        NOT NULL  PK (pk_reservation_consumptions)
  stock_item_id   uuid        NOT NULL
  quantity        numeric(18,6) NOT NULL
  consumer_key    varchar(128)  NULL      -- NULL only for callers without a consumer identity
  consumed_at     timestamptz   NOT NULL
```

* The PK makes "exactly one durable consumption per reservation" a **database-enforced** fact
  (`ReservationId` is PK, `ValueGeneratedNever`), not an application convention.
* `quantity`/`stock_item_id` are stored so a replayed claim can be checked against the recorded
  consumption (mismatch ⇒ typed error), which is what makes replay detection *sound* rather than
  "reservation is Consumed ⇒ success".
* **No backfill.** Historical `Consumed` rows keep no journal row; a missing row is never treated as
  proven replay. Callers cannot fake ownership of an old consumption.

### 2.2 One physical Inventory transaction

`ConsumeReservationAsync` (private core, shared by both public entry points) now runs:

```text
await _db.Database.BeginTransactionAsync(ct)          -- ONE local Inventory transaction
  read reservation (tracked)
  read journal row (replay decision)
  CAS: UPDATE inventory.reservations
       SET status='Consumed', updated_at=@now
       WHERE reservation_id=@id AND status='Held'     -- state-guarded claim, 0 rows ⇒ loser
  UPDATE inventory.stock_positions
       SET on_hand = on_hand - q, reserved = reserved - q, updated_at=@now
       WHERE stock_item_id=@s AND reserved >= q AND on_hand >= q
  INSERT inventory.reservation_consumptions (…)        -- PK = reservation_id
  reservation.MoveTo(Consumed, now)                    -- domain transition preserved
  position.RecordConsumed(...) / SyncQuantities(...)   -- domain events preserved
  SaveChangesAsync()                                   -- reservation + journal + Inventory outbox
CommitAsync()
```

* `ExecuteUpdateAsync` now executes inside the explicit transaction, so the position deduction, the
  reservation transition, the journal insert and the Inventory outbox rows are rolled back together
  when anything fails (proven by the injected-failure test).
* The reservation claim is a **compare-and-set** (`WHERE status='Held'`). PostgreSQL serialises
  racing consumers on that row: the loser observes 0 affected rows and cannot deduct twice.
* The domain transition is still performed (`StockReservation.MoveTo`) so the aggregate's own
  invariant, `UpdatedAt` and `IdempotencyKey` clearing semantics are preserved. The CAS is the
  durable guard; `MoveTo` remains the domain expression of the same transition.
* The property refresh order of the original code is preserved: the position entity is loaded
  *after* the atomic deduction, so the tracked snapshot equals the post-deduction row and no stale
  write-back can occur.

### 2.3 Replay and conflict semantics (the whole truth table)

`consumerKey == null` = legacy/unkeyed call (existing callers, unchanged strictness).
`consumerKey != null` = protected, replay-aware call.

| # | reservation | journal row | call | result |
|---|---|---|---|---|
| 1 | Held | none | keyed / legacy | consume (one deduction, one journal row) |
| 2 | Consumed | matches (consumerKey, stockItemId, quantity) | keyed same key | **proven replay** — success, no deduction, no new row, no outbox |
| 3 | Consumed | exists, key differs | keyed | `inventory.reservation.consume_mismatch` (409), no deduction |
| 4 | Consumed | exists, key equal, stockItemId/quantity differ | keyed | `inventory.reservation.consume_mismatch`, no deduction |
| 5 | Consumed | none (legacy/historical row) | keyed | `inventory.reservation.consume_mismatch` — **no synthetic success** |
| 6 | Released / other non-Held | any | keyed | `inventory.reservation.not_active` (409) |
| 7 | Released / Consumed / other non-Held | any | legacy | `inventory.reservation.not_active` (409) — unchanged strictness |
| 8 | Held but lost the CAS race | re-read: matches own key | keyed | proven replay (success) |
| 9 | Held but lost the CAS race | re-read: other key / absent | keyed | `inventory.reservation.consume_mismatch` |
| 10 | Held but position guard fails (`reserved < q` or `on_hand < q`) | — | any | `inventory.reservation.consume_mismatch` + full rollback |
| 11 | unknown reservation id | — | any | `inventory.reservation.not_found` (404) |
| 12 | blank/whitespace consumer key | — | keyed | `inventory.reservation.consume_mismatch`, no stock touched |

Case 7 is stricter than the old code in a *safe* direction: previously the deduction ran first and
the error was decided by stock arithmetic (a terminal reservation could be deducted and then throw).
Now a non-`Held` reservation is rejected **before** any write, with the same pre-existing typed code.

### 2.4 Consumer-key-aware port (compatibility story)

`IFulfillmentInventoryLifecyclePort` (Inventory Contracts) gains exactly one additive member:

```csharp
Task ConsumeAsync(Guid reservationId, CancellationToken cancellationToken);                     // unchanged, strict
Task ConsumeAsync(Guid reservationId, string consumerKey, CancellationToken cancellationToken);  // new, replay-aware
```

* No existing member changed; every existing caller keeps compiling and keeps the strict legacy
  behaviour. `FulfillmentInventoryGateway` (Fulfillment) is **not** touched, so no Fulfillment
  behaviour changed and no wire/flow semantics moved.
* The only implementor is `InventoryDirectory` (registered in DI for both seams), so the additive
  member required no unrelated refactor. Inventory's own Application port
  (`IInventoryDirectory.ConsumeAsync`) and the Host test stub are untouched.
* **Error codes**: no new code was declared. `InventoryModuleAmsc001W3CertGuardTests` pins the
  Inventory code catalog to exactly 29 declared codes and 29 descriptors, and
  `Assert.Equal(29, declared.Length)` is a **certification lock**. Adding a 30th code would mean
  editing that lock — forbidden here ("no weakening tests or guards"). The existing, catalogued and
  bilingual-mapped `inventory.reservation.consume_mismatch`
  (Conflict/409 — "Reservation consumption does not match the held quantity") already expresses
  "this consumption claim does not match the durable record" and is reused for cases 3–5, 9, 10, 12.
  A dedicated code would need an explicit Architect decision to reopen the W3 lock.

### 2.5 Explicit non-goals (deferred, unchanged by this task)

* Fulfillment dispatch orchestration, W6 skip logic and the canonical Fulfillment consumer key —
  a separate follow-up wires `packageId/memberId/lineId` into the new keyed member.
* No change to live packages, global tracking index, UI, auth, tenant isolation, order state.
* No migration was executed against a live server; the migration only exists in the repository.

## 3. Atomicity boundary (stated honestly)

Guaranteed: **within the Inventory database**, deduction + reservation transition + journal + outbox
are one transaction, and exactly-once is enforced by a durable unique key with a CAS guard.

NOT guaranteed (unchanged by this task): atomicity **across modules** — the Fulfillment/Order
transaction is still a different physical transaction, and cross-module compensation remains
outside Inventory's boundary. This task delivers the Inventory-side foundation only.
