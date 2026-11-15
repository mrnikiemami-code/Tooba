# replay-and-dispatch-guard-proof.md — TB-270-R2

## Confirm: one immutable row, exact replay, typed conflict

`POST /v1/admin/orders/{checkoutId}/operations` with `type = plan_shipment_dispatch` →
`PlanShipmentDispatchCommand`/validator → `AdminOrderOperationsOrchestrator.PlanShipmentDispatchAsync`.

Ordering of the request (each step before any write):

1. checkout exists, action not blocked by cancellation, actor gate `order.handle`/`fulfillment.manage` **re-checked on
   every request including a replay**;
2. `shipmentId` + a non-empty reviewed `idempotencyKey` are required;
3. the exact live parcel is located and re-validated (Created, linked attempt, not package-locked, Paid, group present);
4. **stored plan first**: if a plan exists for the parcel, its `occurrence_key` and its selected customer date/window are
   compared. Identical → `replayed: true` with the stored row (no new feasibility work, no recalculation from *now*).
   Different key or different selection → `order.shipment_plan.conflict` with **zero extra write**;
5. otherwise readiness/captured inputs are re-resolved and the calendar recomputed, the reviewed selection must match
   exactly one fresh option (`selection_required` / `selection_expired` otherwise — never clamped to a neighbour);
6. one immutable `OrderShipmentDispatchPlan` is created and inserted; the database unique index on `shipment_id`
   arbitrates two independent confirmations:
   - `Inserted` → new plan;
   - `DuplicateExact` → the winner row is read back and answered as `replayed: true`;
   - `DuplicateConflict` (`23505` on a non-identical row) → `order.shipment_plan.conflict`.

Nothing in this command creates or modifies a parcel, a reservation, a damaged decision or stock.

### Relational proof on the canonical database

`AdminShipmentDispatchPlan270R2RelationalTests` — 2 cases, real PostgreSQL `tooba_alpha`, task-owned rows inside a
transaction that is **rolled back** (no demo dataset left behind):

- **A** — a stored plan is immutable: a second insert for the same parcel is answered as exact-duplicate by the store
  when the intent matches, and as a conflict when the selection/binding differs. Secondary identities are derived
  deterministically from `shipment_id`, so "same intent" is a real comparison rather than accidental new GUIDs.
- **B** — the database itself refuses a second plan row for the same parcel (PostgreSQL `23505` on
  `ux_shipment_dispatch_plans_shipment`), so the guarantee survives an application regression.

### Unit proof (controlled ports/clock, `AdminShipmentPlan270R2Tests`)

| Case | Fact |
| --- | --- |
| Confirm writes one immutable plan and an exact replay answers the same row | `planId` stable, `replayed` flips |
| The confirmed plan is read from storage after time elapsed and is never recalculated | identical values after the clock moves |
| Same key with a changed selection is a conflict with zero extra write | store count unchanged |
| A different key never overwrites the one confirmed plan | first row untouched |
| An actor without the physical operation permission is denied even on replay | `order.operation.denied` |

## Dispatch guard: every existing path, before any consume

`FulfillmentDirectory.EnsureLinkedAttemptsPlannedAsync` reads
`IOrderShipmentDispatchPlanReader.ListForShipmentsAsync(...)` and requires, for **every** linked member in the batch, a
plan whose `AttemptKind` matches the parcel's nature (`resend` vs `replacement`). Otherwise it throws
`fulfillment.dispatch.plan_required`.

Call sites (checked before any consume/state/event):

- `FulfillmentDirectory.DispatchShipmentAsync` (the single dispatch command path, line ~690) — **before**
  `EnsureSupplyPaymentAuthorizedAsync`, the stock consume and `ApplyShipmentDispatched`; a bulk selection dispatch runs
  the same per-parcel command, so it inherits the guard without a second gate;
- consolidated common-package dispatch in `FulfillmentDirectory.Packages.cs` — **before phase 2**, so one unplanned
  member refuses the WHOLE package (no partial package dispatch).

A missing reader (composition without the seam) refuses rather than defaulting to authorized. First dispatch of an
order keeps exactly its pre-270 behavior.

### Behaviour proof (guarded command, not helper text)

`DispatchPlanGuard270R2Tests` — **6/6**, `Tooba.Fulfillment.Tests`:

| Case | Fact |
| --- | --- |
| Unplanned resend parcel is refused with zero consume and no dispatch | `plan_required`, `Consumed == 0`, `DispatchedAt == null`, status unchanged |
| Planned resend parcel dispatches and consumes exactly once | one consume, one `DispatchedAt` |
| A plan of the wrong attempt kind does not authorize dispatch | `plan_required` |
| Consolidated package with one unplanned linked member is refused as a whole | zero members dispatched |
| A missing plan source refuses instead of defaulting to authorized | `plan_required` |
| First dispatch without any plan keeps its existing behavior | unchanged |

## Queue and detail projections agree with the guard

- `AdminFulfillmentWorkUnitProjection.BuildParcelRow`: a linked attempt's plans are **only** its own confirmed plan;
  without one the plan list is empty → `Unspecified` bucket, null handoff/receiving/deadline, and
  `ProjectParcelActionCodes(..., linkedAttemptUnplanned: true)` yields `plan_shipment_dispatch` + tracking actions and
  **no** `dispatch_shipment`. The original accepted group plan is never lent.
- `PlansForPackageMember`: a linked member contributes only its own plan; unplanned linked members contribute nothing,
  so a package row can never look scheduled by a sibling's promise.
- `AdminFulfillmentWorkQueueQueryEngine` reads all linked plans in ONE batch (no per-row query, no scheduling call, no
  settings read) and passes them through the existing context.
- `AdminOrderOperationsOrchestrator` projection adds the `plan_shipment_dispatch` action only for
  `supplyAuthorized && linkedAttempt && !linkedPlanned && Created && !packageLocked`, and gates `dispatch_shipment` on
  `!linkedAttempt || linkedPlanned`. It also emits `shipmentDispatchPlans` (the stored rows) so the detail surface shows
  the stored promise rather than deriving one.

`AdminFulfillmentOwnPlanProjection270R2Tests` — **3/3** (`Tooba.Order.Tests`): own plan vs original group plan;
no sibling lending inside a package; a completed linked parcel gains no planning action.

## Limits

- Cross-module atomicity is **not** claimed: a plan committed for a parcel that is cancelled concurrently is inert
  history; dispatch re-reads live Fulfillment state and refuses a non-`Created` parcel.
- No stress/concurrency harness: the race is proven by the database unique index plus two independent relational
  contexts, not by load testing.
- Bulk dispatch is covered through the shared `MarkShipmentDispatched` guard and the package batch guard; no separate
  bulk-specific test class beyond those six cases.
