# TB-P0-INVENTORY-CONSUMPTION-JOURNAL-003 — pre/post invariants

## Pre-invariants (must stay true — verified after the change)

| # | Invariant | How it is preserved / verified |
|---|---|---|
| P1 | `StockReservation` state machine: `Held → Consumed` only, terminal states are not resurrectable | `reservation.MoveTo(Consumed, now)` is still called; the durable CAS (`WHERE status='Held'`) enforces the same rule in SQL. `InventoryReleaseIdempotencyTests.Domain_move_to_released_rejects_already_terminal_status` still green. |
| P2 | `ReleaseAsync` treats `Released`/`Consumed` as an idempotent no-op | Untouched code; source-slice guards `InventoryArchitectureGuardTests.Checkout_release_adapter_no_silent_catch_and_directory_release_is_idempotent` and `InventoryReleaseIdempotencyTests.Directory_release_source_...` still green. |
| P3 | One consume call performs exactly one stock deduction for a live reservation | `InventoryFoundationTests.Return_restock_increases_on_hand_idempotently_after_consumed_reservation` (OnHand 5→3 for qty 2) still green; new tests assert the same. |
| P4 | Domain errors for `NotHeld`/`Released`/unknown/`insufficient` remain typed and unchanged (`inventory.reservation.not_active`, `…not_found`, `…consume_mismatch`) | New focused tests assert the exact codes; no new code was added, so the W3 catalog lock (29 codes / 29 descriptors, both resx) is untouched. |
| P5 | Inventory stays `INTERNAL_ONLY`, Contracts-only across modules, no foreign DbContext/join, no new project reference | No csproj/Host/module-boundary file changed; `Tooba.Inventory.Tests` architecture guards green. |
| P6 | Terminal reservation keeps `IdempotencyKey` cleared on consume | `MoveTo` still runs and still clears the key; EF persists it inside the same transaction. |
| P7 | Inventory outbox rows are written in the same `SaveChanges` as the aggregate mutation | Outbox interceptor path unchanged; rollback test proves the outbox row is part of the same transaction. |

## Post-invariants (new guarantees)

| # | Invariant | Enforced by | Test evidence |
|---|---|---|---|
| N1 | At most one durable consumption per reservation | `pk_reservation_consumptions` (PK on `reservation_id`) | `Journal_primary_key_is_a_durable_unique_constraint` (pg_constraint + real `23505`), `Same_consumer_replay…`, `Concurrent_*` |
| N2 | Stock deduction, reservation transition, journal row and Inventory outbox are one physical transaction | explicit `BeginTransactionAsync` + `ExecuteUpdateAsync` on the same context + single `SaveChangesAsync`/`CommitAsync` | `Failure_after_deduction_rolls_back_stock_reservation_journal_and_outbox` |
| N3 | Unkeyed (legacy) consume never double-deducts a terminal reservation | pre-write status check with the pre-existing typed error | `Legacy_unkeyed_consume_stays_strict_and_never_double_deducts` |
| N4 | A keyed replay succeeds only for the *same* `(reservationId, consumerKey)` with matching `stockItemId`/`quantity` | journal lookup + `IsProvenConsumption` | `Same_consumer_replay…` |
| N5 | A different consumer never gets a second deduction | journal mismatch ⇒ typed conflict | `Conflicting_consumer_is_rejected…`, `Concurrent_different_consumers…` |
| N6 | A historical `Consumed` row without journal yields a typed conflict, never synthetic success | explicit `Consumed && journal == null` branch | `Legacy_consumed_reservation_without_journal_is_a_typed_conflict` |
| N7 | Racing consumers are serialised by a durable row-lock/CAS; the loser re-checks the journal and can only replay or conflict | `UPDATE … WHERE status='Held'` + journal re-read | `Concurrent_same_consumer…`, `Concurrent_different_consumers…` |
| N8 | A failed guard (`reserved < q` / `on_hand < q`) rolls back the claim and all writes | same transaction; nothing committed on throw | `Insufficient_reserved_stock_keeps_the_typed_error_and_rolls_back` |
| N9 | A blank consumer key cannot silently degrade to the unprotected path | whitespace check before any write | `Blank_consumer_key_is_rejected_without_touching_stock` |

## Explicitly NOT guaranteed

* Cross-module atomicity. Fulfillment/Order still commit in their own transaction; the Inventory-side
  effect is now atomic and replay-safe, but the cross-module compensation seam is unchanged and is
  addressed by the follow-up that wires a canonical Fulfillment consumer key.
* W6 skip behaviour in Fulfillment dispatch: unchanged by design (out of scope for this task).
