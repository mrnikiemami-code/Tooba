# ownership-and-plan.md — TB-270-R2

## The invariant R2 enforces

Two explicit durable stages inside the ONE existing operator workflow:

1. **Fulfillment** creates (or reconciles) the physical parcel and its original source allocation through its existing
   occurrence mechanism, exactly as before. `Created` + `slug`/`origin receipt` facts stay Fulfillment's.
2. **Order** deliberately confirms an **own** dispatch plan for that exact existing `Created` parcel.

Between the stages the parcel is legitimately «نیازمند زمان‌بندی»: it exists, it is visible, it is recoverable, and it is
**not dispatch-authorized**. There is no Order-generated Fulfillment id, no cross-module partial-commit claim, no
distributed transaction, no scheduler job and no compensation daemon. The single enforceable rule is:

> a resend/replacement parcel may never be dispatched (single, bulk or consolidated package) without its own validly
> bound confirmed plan.

## Who owns what after R2

| Fact | Owner | Type |
| --- | --- | --- |
| Physical parcel, its items, its source allocation/occurrence, the retained hold | Fulfillment | `fulfillment.shipments` (unchanged) |
| Damaged decision / replacement reservation identity | Fulfillment | `fulfillment.damaged_resolutions` (unchanged) |
| Confirmed own dispatch plan (immutable) | **Order** | `order.shipment_dispatch_plans` (new, additive) |
| Captured scheduling inputs (weeks, minutes, transport days) | Shipping | read-only Contracts read from R1 |
| Calendar/deadline arithmetic | Shipping | `ShippingDispatchSchedulingAuthority` (reused, not copied) |
| Original accepted group plan | Order | `order.order_shipping_groups` (untouched, never lent) |

Contracts seam: `Tooba.Order.Contracts.Shipping.IOrderShipmentDispatchPlanReader` +
`OrderShipmentDispatchPlanFact` (+ `OrderShipmentDispatchPlanKinds`). It reads **only** Order rows; it performs no
join, no scheduling, no settings read. Consumers are Fulfillment's dispatch guard and Fulfillment's queue engine — both
one-way (`Fulfillment → Order.Contracts`), so there is no `Order → Fulfillment → Order` read chain and no DI cycle.

## Table shape (additive, one migration)

`order.shipment_dispatch_plans`, PK `plan_id`, **unique index on `shipment_id`** (the database half of "one immutable
plan per physical parcel"), plus non-unique indexes on `(seller_order_id, shipping_group_id)` and `checkout_id`.

Stored binding/identity: `checkout_id`, `seller_order_id`, `fulfillment_id`, `shipping_group_id`, `attempt_kind`
(`resend|replacement`), `source_shipment_id`, `damaged_resolution_id`, `replacement_reservation_id`,
`source_fingerprint` (exact `orderLineId=quantity` set of this parcel), owner scope (`owner_kind`,
`owner_tenant_key`, `owner_seller_party_id`), method/service/option, and the captured provenance
(`snapshot_id`, `draft_id`, `source_revision`, `format_version`, `selection_version`, `area_group_id`, `rate_row_id`).

Stored selection/derived: customer receiving date + local window ticks + zone, handoff date + local window ticks + zone,
the UTC triple `preparation_start_utc <= transfer_departure_utc <= handoff_deadline_utc`, `preparation_minutes`,
`handoff_minutes`, `transport_working_days`, `policy_version`, `readiness_source`, `readiness_at_utc`,
`occurrence_key`, `confirmed_by_user_id`, `confirmed_at_utc`.

Check constraints make the row honest even if application code regresses: attempt/source agreement, half-open windows,
`handoff_date <= customer_receiving_date`, non-negative allowances, deadline ordering. A partial or decreasing triple is
refused by the database, not only by the command.

### Immutable captured inputs vs present eligibility — the exact distinction

- **Immutable (stored, audited, reproducible):** every captured input and every derived deadline the authority produced
  for the *selected* slot. Nothing is re-derived at read time, and an elapsed promise is never recalculated.
- **Present (evaluated per request, never stored as authority):** actor permission, checkout/seller-order state,
  payment/`Paid` supply authorization, parcel `Created` + `DispatchedAt == null`, active common-package membership,
  source receipt/hold lawfulness, and the freshly computed feasible calendar used to validate the selection.

A captured week's mere presence is **not** activation, eligibility or coverage. Historical presence does not silently
rebind to a newer draft revision: the plan is bound to the accepted evidence's `snapshot_id`/`source_revision`, and a
newer seller draft is irrelevant to it.

## Readiness provenance (physical, never "the clock")

| Attempt | Proven by | Stored as |
| --- | --- | --- |
| `resend` | the SOURCE parcel is `Returned` with an origin receipt, and its **usable** received quantity per line covers this parcel's line quantity | `readiness_source = "return_receipt"`, `readiness_at_utc = ReturnedAt ?? receipt.ReceivedAt` |
| `replacement` | this parcel's own damaged decision is `Replacement` with status `Reserved`/`Fulfilled`, AND its `replacement_reservation_id` equals the parcel's, AND the decision's source/quantity cover this parcel | `readiness_source = "replacement_hold"`, `readiness_at_utc = decision.UpdatedAt` |

The effective scheduling anchor is `max(server now, readiness_at)`. `Shipment.CreatedAt` is **not** used as a
substitute for stock validation: creation proves allocation happened, not that the origin was lawfully ready, so the
owning receipt/hold facts above are read explicitly. Damaged units never fund a resend; a sibling returned parcel never
makes another parcel ready.

`PreparationStartUtc` keeps its meaning: **the last moment to START preparation**, not "packing finished". Departure and
handoff close keep their distinct meanings. Preparation and transfer allowances are applied **once** by the existing
authority — they are never added to readiness and then subtracted again.

## Known limitation (stated, not hidden)

There is no separate activation/coverage ledger for a captured shipping method: the check performed here is the same
authoritative binding the accepted evidence itself carries (accepted group evidence + captured revision present +
`Paid` seller order + lawful physical readiness). If a method were deactivated in a *newer* draft, this plan would still
honour the capture it was accepted under — by design, because rebinding to a newer revision is forbidden.
