# TB-P0-INVENTORY-FULFILLMENT-RETRY-SAFETY-002 — Gate C: failure-window matrix

Notation: **F** = `FulfillmentDbContext` commit (package dispatch, item flags), **I** = Inventory consume commit
(`inventory.stock_positions` deduction + `inventory.reservations` status + outbox). Base SHA `04e3aa79…`.

| # | Window | Resulting durable state | Retry behaviour today (code-level) | Verdict |
|---|--------|-------------------------|------------------------------------|---------|
| W1 | **I commits, F fails** (the P0 window) | reservations `Consumed`, positions deducted, `reservation_consumed = false`, shipment still `Created`, package `Created` | Retry re-enters `ConsumeInventoryForShipmentAsync` (flag false) → `InventoryDirectory.ConsumeAsync` does **not** short-circuit on `Consumed`; it runs the guarded `ExecuteUpdate` first and only then `MoveTo(Consumed)`, which throws `inventory.reservation.not_active`. | **BROKEN.** Package is permanently undispatchable, *or* worse (W7). |
| W2 | **F commits, response fails** | members `Dispatched`, flags true, tracking persisted | Retry: `BuildDispatchStepsAsync` sees `Dispatched` member → step with `Consume=false`; item flag true → no consumption. Idempotent. | OK (parent repair holds). |
| W3 | **I fails before F commit** | nothing persisted (reservation still `Held`, position unchanged) | Retry consumes normally. | OK. |
| W4 | **Two concurrent dispatches** | — | Both callers load `Held`, both may pass the guarded `ExecuteUpdate` (the guard is `Reserved >= q && OnHand >= q`, not a reservation-state CAS), so a second deduction can commit; the loser then throws from `MoveTo`. No row version, no unique journal row, no `Status = 'Held'` predicate in the UPDATE. | **UNSAFE (double deduction possible), unproven.** |
| W5 | **Preexisting `Dispatched` member with consumed reservation** | member dispatched, flag true | Skipped by both the `Dispatched → Consume=false` branch and the flag check. | OK. |
| W6 | **Preexisting `Dispatched` member whose reservation is still `Held`/not consumed** (flag false, e.g. historical partial state) | member `Dispatched`, reservation `Held` forever | The `Dispatched` branch unconditionally sets `Consume=false`, so the needed consumption is **silently skipped**; the reservation is never consumed and never released (no expiry either when committed for a paid order, `ExpiresAt = null`). | **Silent skip / stock leak.** Fulfillment cannot detect this from its own data. |
| W7 | **W1 retry where the position still satisfies the guard** | e.g. other holds keep `Reserved >= q` and `OnHand >= q` on the same `StockItemId` | `ExecuteUpdateAsync` is executed **immediately and autocommitted** (outside `SaveChanges`) → the stock is deducted a **second time**; `MoveTo` then throws, so the reservation row, the outbox and the Fulfillment flag are unchanged → **untraceable extra deduction** (no `inventory.reservation_consumed.v1` row is emitted for it). | **SILENT STOCK CORRUPTION.** |
| W8 | **Crash between the `ExecuteUpdate` and `SaveChangesAsync` inside `ConsumeAsync`** | deduction committed; reservation still `Held`; no outbox row | Retry deducts again. | **UNSAFE even without any cross-module failure** — the deduction is not atomic with the reservation status. |

## Live-data corroboration (READ-ONLY, no mutation performed)

Order `01a12e97-ee9e-7000-826c-a9eca536b223`, package `01a12f79-6ef1-7000-8578-dd7cc841195d`, database `tooba_alpha`:

| Artifact | Observation |
|---|---|
| `fulfillment.items` (member 1 = `01a12e98-77aa-7000-9147-6b2d69ca0ee7`) | 5 lines, `quantity_shipped = quantity_ordered`, `reservation_consumed = true` |
| `inventory.reservations` for those 5 lines | all `Consumed`, `reserved = 0` on their positions (e.g. `on_hand 199/198/194/196`, `reserved 0`) |
| `fulfillment.items` (member 2 = `01a12e98-77d1-7000-aa6c-78f53c983947`) | 1 line, `quantity_ordered = 5`, `quantity_shipped = 0`, `reservation_consumed = false`, `reservation_id = 01a12e97-ef78-7000-9854-7b1a4affaad3` |
| that reservation | **`Held`**, `quantity 5`, `expires_at = null`, `idempotency_key = checkout-process:0b6e4c54-…:line:01a12e474c7a7000a49c38c3a936ffe6`, `external_reference = order-commit:…:process:…`; position `on_hand 200`, `reserved 5` |
| `inventory.outbox_messages` | 116 rows, 5 × `inventory.reservation_consumed.v1` — matches exactly the 5 consumed member-1 reservations |

Consequences: (i) the live stuck package sits in **W1 with a still-`Held` member-2 reservation**, so a one-shot recovery consumes it exactly once — the parent Gate-C runbook is *not* an input to W7/W8 as long as it is executed once; (ii) `external_reference` is **shared by all lines of the same order process**, so it cannot identify the consuming shipment; (iii) the hold `idempotency_key` is still present on member 2's reservation and will be **destroyed by the consume itself**, confirming that the only surviving key cannot be used as a replay key.

## Residual P0 risks (unchanged by the parent repair)

1. W1/W7: Fulfillment-commit failure after a successful consumption leaves the package undispatchable, with a possible silent double deduction.
2. W8: the deduction is not atomic with the reservation row even inside Inventory.
3. W6: `Dispatched` members skip consumption unconditionally, so a pre-existing partial state can leak a `Held` reservation silently.
4. W4: concurrent dispatches are not serialized on the reservation state.
