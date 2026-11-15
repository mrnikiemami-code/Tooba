# TB-270 — replay and write boundaries (C/D) — DESIGN, not implemented

Status: **DESIGN ONLY.** No table, command, reader or UI was added. This file records the exact durability contract the
implementation must satisfy, and the recovery route at every interruption boundary, so the next run cannot improvise it.

## 1. Identity and uniqueness

| Guard | Purpose |
| --- | --- |
| PK `shipment_id` on `"order".shipment_dispatch_plans` | one immutable plan per physical attempt; a second parcel always needs its own row |
| unique `occurrence_key` | the reviewed selection is replayable exactly; a changed intent under the same key is a typed conflict with zero write |
| FK-free identity columns (`checkout_id`, `seller_order_id`, `shipping_group_id`, `source_shipment_id`, `damaged_resolution_id`) | the plan is bound to the exact source/attempt, never resolved by "latest" or "earliest" |
| `status` in {Pending, Committed} + `committed_at_utc` | distinguishes "plan written, parcel not yet returned" from "parcel exists" |

## 2. Submit order and crash windows

1. **Authorize first** (`order.handle` / `fulfillment.manage`), before any evidence read — a retained browser envelope is
   never authorization (same rule as the existing post-dispatch gate).
2. Resolve the authoritative scheduling inputs and validate the reviewed selection (feasible date + window contained in
   the server's own calendar).
3. Insert the plan row `Pending` (Order transaction) with the parcel id Order generates.
4. Call `CreateResendShipmentAsync(..., shipmentId)` (Fulfillment, its own serializable transaction + occurrence replay).
5. Update the row to `Committed` with the recorded parcel id.

| Interruption | Durable state | Recovery route | Forbidden |
| --- | --- | --- | --- |
| after 2 | nothing | re-submit; identical result | writing a plan with an unvalidated selection |
| after 3 | `Pending`, no parcel | the same occurrence repeats 4 with the **same** parcel id | deleting the row and minting a new id |
| after 4 | parcel exists, plan `Pending` | the plan is already authoritative; the occurrence reconciles to `Committed` and answers the recorded facts | computing a second plan from "now" |
| lost response to the operator | `Committed` | exact replay answers the recorded plan + parcel id | a second parcel or a re-derived deadline |
| changed date/window/source/quantity under the same key | `Committed` with a different intent | typed conflict (`order.dispatch_plan.intent_conflict`), zero write | silent clamp/replace |
| replacement with no healthy stock | no plan row | the durable damaged decision stays pending, with **no** actionable dispatch promise | a plan based on a reservation that does not exist |
| parcel cancelled before dispatch | plan stays (immutable history) | a **new** attempt gets a **new** plan; the retained hold is never released to refresh a plan | reusing the cancelled attempt's plan for the new parcel |
| parcel already dispatched | `Committed` | history only; never becomes actionable | re-planning a dispatched attempt |

## 3. Read boundaries

* Queue (`AdminFulfillmentWorkUnitProjection.BuildParcelRow`): `ResendOfShipmentId is null` → group plan (unchanged);
  otherwise `AttemptPlan(shipment.ShipmentId)` only. No sibling, no seller-order fallback, no count inflation.
* Detail (`AdminShipmentView`): the attempt plan is rendered beside — never instead of — the original customer promise,
  which stays readable on the group.
* Completed/cancelled history: `NotQueued`/`InTransit` classification is untouched, so a future plan can never make a
  finished parcel actionable.
* No per-read recomputation from current settings: the stored row is the only source, exactly like the accepted group
  plans today (`OrderDispatchPlanFactsBridge` projects stored columns only).

## 4. What would have been proven (and was not)

Focused cases the implementation owes, listed here so the next run can execute them instead of re-deriving them:
usable-receipt readiness vs healthy-stock readiness (no early promise, no second preparation allowance, no plan for a
stock-less replacement); carrier cutoff / closed days / elapsed original date → a newly feasible deliberate selection
(no borrowing); stored plan survives reload and first dispatch is unchanged; review→submit expiry and settings-revision
change refuse truthfully with the envelope retained and zero unintended write; exact replay before/after each durable
boundary returns the same plan+parcel while a changed intent conflicts; two concurrent submissions cannot
double-allocate; replacement pending-stock → later review → one retained hold/one linked parcel with no hold release on
parcel cancellation; an existing unplanned parcel gets an explicit confirmation instead of a backfill; legacy uncaptured
rows keep an honest blocker.

None of the above is claimed as verified in this result.
