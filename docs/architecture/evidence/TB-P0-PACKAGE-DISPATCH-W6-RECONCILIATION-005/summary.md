# TB-P0-PACKAGE-DISPATCH-W6-RECONCILIATION-005 — Evidence Summary

Mode: `IMPLEMENT_SINGLE_BOUNDED_W6_SAFETY_GATE_STOP_FOR_ACCEPTANCE`
Parent: `TB-P0-FULFILLMENT-KEYED-CONSUME-WIRING-004`
Program: `TOOBA-FULFILLMENT-DISPATCH-INTEGRITY-RECOVERY`

## Preflight

| Item | Value |
| --- | --- |
| Expected-Starting-HEAD | `eca66731f96ebb181ef3bfa9db37eacb07971b26` |
| Starting HEAD | `eca66731f96ebb181ef3bfa9db37eacb07971b26` |
| Starting `origin/main` | `eca66731f96ebb181ef3bfa9db37eacb07971b26` |
| Tracked worktree | clean (`git status --porcelain --untracked-files=no` empty) |
| DB / migration / deploy / live Host mutation / restart / live dispatch | **none** |
| Deferred findings register | **not** read |

Read: `AGENTS.md`, `.cursor/skills/tooba-backend-feature/SKILL.md`, journal-003, wiring-004,
fix-004R and the W6 evidence (`TB-P0-INVENTORY-FULFILLMENT-RETRY-SAFETY-002/failure-matrix.md`).

## Authoritative state traced (before any edit)

| Fact | Source |
| --- | --- |
| `FulfillmentItem.ReservationId` — stable per-line reservation identity, not an Inventory FK | `FulfillmentItem.cs` |
| `FulfillmentItem.ReservationConsumed` — the Fulfillment-side proof that this line's reservation was consumed through the dispatch seam | `FulfillmentItem.cs:42,150` |
| `FulfillmentItem.QuantityShipped` / `QuantityOrdered` — gate for "fully shipped line" | `FulfillmentItem.cs:29,36` |
| `Shipment.Status` / `DispatchedAt` — `Created` / `Dispatched` / `InTransit` / `Delivered` | `Shipment.cs` |
| Package dispatch skip: `BuildDispatchStepsAsync` returns `NeedsDispatch = false` for an already dispatched member | `FulfillmentDirectory.Packages.cs:319-323` |
| Consume seam: `ConsumeInventoryForShipmentAsync` skips when `ReservationId is null || ReservationConsumed` | `FulfillmentDirectory.cs:515-528` |
| Keyed Inventory port + durable journal + proven replay / typed conflict | `IFulfillmentInventoryLifecyclePort.ConsumeAsync(Guid, string, ct)`, `InventoryDirectory.ConsumeReservationAsync` |
| Deterministic fail-closed consumer key | `FulfillmentInventoryConsumptionKey.For(packageId, shipmentId, orderLineId)` |

The only dispatch early returns/skip paths are: package already `Dispatched`/`Delivered` (whole
command is idempotent no-op), member shipment already dispatched (W6 branch), and the per-line
`ReservationId is null || ReservationConsumed` skip. All were checked.

## W6 classification: **FIXED** (not BLOCKED)

The task's STOP condition ("proving Held-vs-Consumed requires a NEW cross-module read contract,
journal semantics, historical backfill, or an ambiguous replay identity") was **not** triggered:
existing verified state is sufficient and the replay identity is unambiguous.

* `FulfillmentItem.ReservationConsumed == false` on a fully-shipped, already-dispatched member line
  is the durable Fulfillment-side marker that the reservation was **not** consumed through the
  dispatch seam — exactly the W6 state.
* The keyed port already distinguishes all three outcomes without any new contract:
  `Held` → consumes exactly once; proven replay (journal row + same key/stock item/quantity) →
  idempotent success with no second deduction; `Consumed` without a matching journal row or a
  mismatched consumer → **typed conflict** (`inventory.reservation.consume_mismatch`), never silent
  success.
* No Inventory read contract was needed, so the guard **fails closed with an existing typed error**
  rather than assuming a flag proves Inventory state: Fulfillment only asserts *its own* durable
  marker and delegates the Inventory truth decision to Inventory.

## Bounded implementation (one production file)

`src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Directories/FulfillmentDirectory.Packages.cs`
— `DispatchConsolidatedPackageAsync`, phase 2.

Before: `foreach (var step in steps.Where(x => x.NeedsDispatch)) { ApplyShipmentDispatched(...);
ConsumeInventoryForShipmentAsync(...); }` — a pre-dispatched member never entered the loop, so its
`Held` reservation was silently skipped (W6 leak) and never released (paid-order holds have
`ExpiresAt = null`).

After: the loop iterates **every** step; `ApplyShipmentDispatched` is still guarded by
`step.NeedsDispatch` (no re-dispatch, no status/tracking change, no new domain event for
pre-dispatched members), while `ConsumeInventoryForShipmentAsync` now runs for every step and keeps
its own unchanged per-line gate (`ReservationId is null || ReservationConsumed` → skip;
`QuantityShipped >= QuantityOrdered` → consume + `MarkReservationConsumed`).

Behaviour delta, precisely:

| Case | Before | After |
| --- | --- | --- |
| Pre-dispatched member, `ReservationConsumed = true` | skipped | **skipped (idempotent, unchanged)** |
| Pre-dispatched member, `ReservationId = null` | skipped | skipped (unchanged) |
| Pre-dispatched member, `Held` reservation, line fully shipped | **silently skipped → stock leak** | **consumed once via the keyed seam, flag persisted** |
| Pre-dispatched member, `Held` reservation, line not fully shipped | skipped | skipped (unchanged; same rule as the normal path) |
| Newly dispatched member | dispatched + consumed (keyed) | unchanged |
| Member with a legacy reservation and no journal row | not reached | typed conflict, no silent success |
| Retry after a Fulfillment-commit failure | re-consume risk | proven replay via the same stable key (idempotent) |

Preserved invariants (unchanged by this task):

* parent two-phase validation (`BuildDispatchStepsAsync` writes nothing) and **exactly one**
  `Fulfillment` `SaveChangesAsync` per package — the consume step still runs before that single
  commit, so a keyed failure still persists **no** partial dispatch;
* existing already-dispatched member tracking/status and generated tracking codes;
* retry-safe keyed journal semantics, the key format and the fail-closed default interface member;
* the unkeyed seam is still never used from the consolidated-package path;
* no non-consolidated-package path was touched (single-shipment dispatch, returns, cancel, release
  stay unkeyed);
* no Inventory schema, migration, journal, DTO, UI, auth, index, config, guard baseline or
  AMSC/manifest record was modified.

## Focused validation (single run each, no loops)

```text
$ dotnet build src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Tooba.Fulfillment.Infrastructure.csproj
Build succeeded.  0 Warning(s)  0 Error(s)

$ dotnet test src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Tooba.Fulfillment.Tests.csproj \
    --filter "FullyQualifiedName~FulfillmentCharacterizationTests"
Passed! - Failed: 0, Passed: 8, Skipped: 0, Total: 8, Duration: 1 s - Tooba.Fulfillment.Tests.dll (net8.0)
```

Two new narrowly targeted tests in
`src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Behavior/FulfillmentCharacterizationTests.cs`
(plus a small test-only state builder `SimulatePreDispatchedMemberAsync` that reproduces the
historical partial state by dispatching the member and deliberately leaving the flag):

1. `Consolidated_package_dispatch_consumes_pre_dispatched_member_reservation_via_keyed_seam` —
   **W6 Held/unconsumed member is detected and prevented from silent success.** Asserts the
   pre-dispatched member's `Held` reservation reaches the keyed seam with the exact deterministic
   key, `ReservationConsumed` is persisted, the member is **not** re-dispatched and its pre-existing
   tracking code is **not** rewritten, while the second member dispatches normally.
2. `Consolidated_package_dispatch_leaves_already_consumed_pre_dispatched_member_unchanged` —
   **already consumed pre-dispatched member is not changed.** Asserts zero consume calls (keyed or
   unkeyed) for the consumed member, `ReservationConsumed` stays true, status/tracking unchanged, and
   only the other member is consumed.

**Environmental block (recorded, not chased).** `Tooba.Host.Tests` (which owns
`ConsolidatedPackageTests`, the only place the consolidated-package dispatch is exercised against
real PostgreSQL) could **not** be built in this session: the running live Host process (`Tooba.Host`,
PID 15068) holds its own output directory, so `MSB3027/MSB3021` copy failures block the test build.
No live Host was stopped, restarted or otherwise mutated (task forbids it). The 6 pre-existing
`ConsolidatedPackageTests` assertion mismatches recorded in the previous task therefore remain
un-re-verified in this session; they are unrelated to this change (they are the baseline tracking/
`NoopInventoryGateway` assertions) and the pre-existing baseline is unchanged.

Not run (by instruction): full suite, AMSC, broad architecture guards, retry cascades, browser tests,
migrations, deployment, any live Host write/dispatch/retry/cancel, any SQL `UPDATE`/`DELETE`, and any
manipulation of the identified real order/package.

## Residual risks

* **R1 — Cross-module atomicity is still not claimed.** The Fulfillment commit and the Inventory
  consume remain two transactions; this task only closes the *silent skip* and relies on the already
  proven Inventory-side replay safety.
* **R2 — W6 is only reconciled on the next package-dispatch attempt.** A package already in
  `Dispatched` state returns idempotently before phase 2, so a stuck package that was completed
  before this change still needs one explicit re-dispatch attempt (or an authorized repair) to close
  its leak. No live repair was performed here.
* **R3 — Journal-003's migration is still undeployed in the live Host**, so the keyed path stays
  dormant until the Architect authorizes that deployment. Until then, in the live environment, a W6
  member would surface as the fail-closed typed failure rather than a silent success.
* **R4 — Historical `Consumed` rows without a journal row** remain a typed conflict on the keyed
  path (intentional, inherited from journal-003; no blanket `Consumed ⇒ success` shortcut was added).
* **R5 — Non-fully-shipped pre-dispatched lines** keep the existing skip (unchanged rule); they were
  not widened by this task.

## Real-order safety

The identified real order/package (`01a12e97-…`, package `01a12f79-6ef1-…`) was **not** touched: no
live Host call, no dispatch, no retry, no DB access, no migration, no restart, no SQL write. The
change is source + tests + receipt + evidence only.

## Rollback

Revert this single commit. No schema, migration, journal, DTO, UI, config or baseline change is
involved, so rollback is a pure source revert with no data migration.
