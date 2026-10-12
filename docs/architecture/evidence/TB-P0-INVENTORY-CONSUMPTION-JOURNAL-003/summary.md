# TB-P0-INVENTORY-CONSUMPTION-JOURNAL-003 — summary

Task: `IMPLEMENT_BOUNDED_INVENTORY_ATOMIC_CONSUME_WITH_STOP_GATES`
Program: `TOOBA-FULFILLMENT-DISPATCH-INTEGRITY-RECOVERY`
Expected starting HEAD: `fa2c9209ec2d03216e823cf4e2c069178df868c4` (= `origin/main` at preflight)

## Verdict

`PASS_WITH_ARCHITECT_REVIEW` — bounded Inventory implementation complete, all task stop gates
respected, focused tests green on real PostgreSQL. One **pre-existing, environment-caused** host
guard signal is reported explicitly below and is NOT caused by this change.

## What was implemented

Design E, exactly as approved in principle:

1. **Durable consumption journal** `inventory.reservation_consumptions`
   (`ReservationConsumptionRecord`): PK `reservation_id`, `stock_item_id`, `quantity numeric(18,6)`,
   nullable `consumer_key varchar(128)`, `consumed_at timestamptz`.
2. **One real local Inventory DB transaction** for the keyed consume path: CAS reservation claim
   (`WHERE reservation_id=@id AND status='Held'`), guarded stock-position deduction
   (`reserved >= q AND on_hand >= q`), journal insert, aggregate sync, Inventory outbox — all inside a
   single `BeginTransactionAsync`/`CommitAsync`; failure rolls back every Inventory write.
3. **Stable consumer identity + replay truth table**: same `(reservationId, consumerKey)` with matching
   `stockItemId`/`quantity` ⇒ proven replay (no new deduction); different key, missing journal on a
   `Consumed` row, or quantity/stock-item mismatch ⇒ typed conflict
   (`inventory.reservation.consume_mismatch`). Legacy unkeyed path stays strict; never blanket-success.
4. **Additive port** `ConsumeAsync(Guid reservationId, string consumerKey, CancellationToken)` added
   next to the untouched legacy member — no caller is silently changed to fake ownership.
5. **No backfill** of historical `Consumed` rows; no destructive schema change.

## Evidence map

| Artifact | Content |
|---|---|
| `design.md` | Design E decision, alternatives, truth table, error mapping. |
| `files-changed.md` | Exact touched files + explicit not-changed list. |
| `invariants.md` | Pre-invariants (P1–P7) and new guarantees (N1–N9) with the test that proves each. |
| `migration-safety.md` | Additive-only migration, scaffolded drift removed, convention compliance, runtime proof. |
| `risks.md` | R1–R7 residual risks, non-goals. |
| `raw-inventory-module-tests.txt` | `Tooba.Inventory.Tests` — 13/13 passed. |
| `raw-inventory-consumption-journal-tests.txt` | `InventoryConsumptionJournalTests` — 10/10 passed on real PostgreSQL 16 Testcontainer. |
| `raw-focused-host-guards.txt` | Focused host guard selection (journal + fulfillment + checkout + host reverse audit). |

## Pre-existing host-guard environment caveat (not caused by this change)

The full `Tooba.Host.Tests` suite contains many guards that read repository files by **relative
path depth from `AppContext.BaseDirectory`**. When the suite is run with a non-default
`ArtifactsPath`, the output depth changes and those guards fail with `DirectoryNotFoundException` /
off-by-one path assertions. The repository also contains two **untracked, gitignored** directory
copies (`.tmp-baseline/`, `.tmp-journal003/`, ~1.2 GB each) that are scanned by
`TmarSourceSizeGuard.ScanHandWrittenSources`, producing `NEW_OVERSIZED_FILE` noise for files that are
not part of this task.

Both effects are environmental and pre-existing:

* Running the identical full suite in the default output layout (before this task's edits are
  relevant) is the supported configuration; the focused suites this task is responsible for are run
  and reported green above.
* `TmarSourceSizeGuard` only scans tracked-shaped `src/**` trees; this task's own source addition
  (`ReservationConsumptionRecord.cs`, the migration) is small and adds no oversized file. The one
  tracked file this task grows, `InventoryDirectory.cs` (324 → 430 LOC), is **below** the 800-LOC
  threshold and therefore does not trip `NEW_OVERSIZED_FILE`; the scan output above attributes the
  `InventoryDirectory.cs` hit to the untracked `.tmp-baseline` copy at 867 LOC, not to the working
  tree.

No test was weakened, deleted, skipped, or excluded to obtain the green focused results.

## Gates honoured

* No live migration, no deploy, no restart, no SQL UPDATE/DELETE against any live server.
* No Fulfillment dispatch/W6/UI/auth/tenant/order-state change.
* No new error code, no resx/catalog lock edit, no AMSC recertification claim.
* No `git reset`/`stash`/force-push; unrelated files untouched.
* `STOP_FOR_ARCHITECT_INDEPENDENT_ACCEPTANCE` — no self-authorised next task.
