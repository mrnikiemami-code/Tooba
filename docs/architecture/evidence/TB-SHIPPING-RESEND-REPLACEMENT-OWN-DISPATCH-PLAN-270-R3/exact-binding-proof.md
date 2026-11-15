# exact-binding-proof.md — repairs A and B

## A. The captured read addresses the CAPTURED area group, never the Order shipping group

### The defect R2 carried

The plan preview/confirm resolved its scheduling inputs from the accepted authoring capture. The capture revision holds
`Groups[].RateRows[]`, and the rate row that was quoted belongs to the **area group of the captured payload**. The
Order side keeps its own `ShippingGroupId`. R2 let the reader address the capture with the Order-side id, so:

- a lookup could succeed while pointing at a group the rate row was never captured for, and
- nothing in the code proved which of the two identities had been used.

### The repair

`AdminOrderOperationsOrchestrator.ShipmentPlan.cs`

- `binding.AreaGroupId` is the only identity handed to `IShippingAttemptSchedulingLookup`
  (captured group + `binding.RateRowId` + snapshot/draft/revision/format/selection).
- The response's own group must equal `binding.AreaGroupId` (`:388`), otherwise the typed
  `binding_mismatch` blocker is returned — never a guessed calendar.
- `ShippingGroupId` stays where it belongs: the Order-side identity written into the plan row and compared by the
  binding rules.

### Proofs

| Case | Test | What it asserts |
| --- | --- | --- |
| A1 | `Preview_addresses_the_CAPTURED_area_group_and_rate_row_not_the_order_shipping_group` | with `AreaGroupId != ShippingGroupId`, the seam receives the **area** group and the exact rate row; the returned preview binds to that capture |
| A2 | `A_capture_that_answers_for_the_order_shipping_group_is_refused_as_a_binding_mismatch` | a capture answering for the Order group is refused with a typed blocker and **no** calendar |
| A3 | `The_order_side_group_identity_cannot_address_the_captured_read` | using the Order group as the lookup identity cannot resolve the capture |
| A4 | `The_real_captured_seam_resolves_the_area_group_and_the_created_parcel_gets_its_own_plan` (+ `ShippingAttemptSchedulingLookupTests` 8/8) | the **real** `IShippingAttemptSchedulingLookup` adapter (not a fake) resolves the captured group/rate row for a `Created` linked parcel, and that parcel receives its own plan |

`AdminShipmentPlan270R2Tests` is 25/25 with these four cases included; the retained R2 cases (preview write-freedom,
immutable confirm, exact replay, changed selection conflict, different key, expired selection, missing selection,
stock-pending blocker, reserved-hold scheduling, usable-receipt funding, non-plannable first dispatch, dispatch history,
permission on replay, unpaid order) still pass unchanged.

## B. ONE shared binding, applied by every reader

### The authority

`src/backend/Modules/Order/Tooba.Order.Contracts/Shipping/OrderShipmentDispatchPlanBindingRules.cs`

`Validate(plan, candidate)` returns `null` only when **all** of the following are true, and otherwise a typed code:

| Facet | Compared |
| --- | --- |
| parcel | `plan.ShipmentId == candidate.ShipmentId`, non-empty |
| ownership | checkout, seller order, fulfillment, shipping group |
| attempt kind | ordinal equality (`Resend` vs `Replacement`) |
| source | source shipment / damaged resolution / replacement reservation |
| quantity | canonical line fingerprint (`lineId=quantity` sorted by line id) |
| method | the parcel's own recorded method must exist and equal the plan's |
| coherence | snapshot/draft/revision, `AreaGroupId`/`RateRowId`, service, customer date, open<close, zone, and `preparation <= transfer <= handoff` |

Reason codes: `binding_missing_plan`, `binding_parcel_mismatch`, `binding_source_mismatch`,
`binding_quantity_mismatch`, `binding_capture_unverified`, `binding_method_unverified`, `binding_corrupt`.
Failures are **closed**: an absent plan, an unreadable row, or a duplicated/conflicting row is never "planned".

### Consumers

| Consumer | Uses |
| --- | --- |
| Fulfillment dispatch guard (single path) | the shared rules against the owning unit/parcel facts |
| Consolidated package dispatch | the same rules for **every** member before any effect; one unplanned/wrongly-bound member refuses the whole package |
| Queue projection (`AdminFulfillmentWorkUnitProjection`) | the same rules to decide `Unspecified` vs the stored promise; a sibling's or the original group's plan is never borrowed |
| Bulk dispatch preflight (`ExecuteAdminFulfillmentBulkCommand` + `IFulfillmentDirectory.VerifyLinkedDispatchPlansAsync`) | one all-member preflight **before the first effect**, so a bulk request cannot half-send |

### Proofs

| Case | Test | What it asserts |
| --- | --- | --- |
| B1 | `Unplanned_resend_parcel_is_refused_with_zero_consume_and_no_dispatch` | refusal happens before consume/state/event |
| B2 | `Planned_resend_parcel_dispatches_and_consumes_exactly_once` | the owned plan authorizes exactly one dispatch/consume |
| B3 | `A_plan_of_the_wrong_attempt_kind_does_not_authorize_dispatch` | a resend plan cannot authorize a replacement |
| B4 | `A_plan_bound_to_another_identity_never_authorizes_dispatch` | wrong parcel/order/source/hold is refused |
| B5 | `A_missing_plan_source_refuses_instead_of_defaulting_to_authorized` | an absent reader refuses; there is no client-side bypass |
| B6 | `First_dispatch_without_any_plan_keeps_its_existing_behavior` | the original parcel path is unchanged |
| B7 | `Consolidated_package_with_one_unplanned_linked_member_is_refused_as_a_whole` | whole package refused, no partial send |
| B8 | `Bulk_dispatch_preflights_every_member_before_any_effect` | no member is dispatched/consumed when one refuses |
| B9 | `Bulk_dispatch_with_a_valid_set_still_executes_each_member` | the preflight does not block a valid set |
| B10 | `Non_dispatch_bulk_does_not_consult_the_plan_preflight` | non-dispatch actions keep their behavior |
| B11 | `A_replacement_readiness_fact_for_another_parcel_never_authorizes_this_one` | a readiness fact is parcel-bound |
| B12 | `OrderShipmentDispatchPlanStoreFilter270R3Tests` 3/3 + relational case | only `ux_shipment_dispatch_plans_shipment` + `23505` is arbitrated as a concurrent replay; every other uniqueness failure is surfaced |

Counts: `DispatchPlanGuard270R2Tests` **7/7**, `AdminFulfillmentBulkTests` **12/12**,
`AdminFulfillmentOwnPlanProjection270R2Tests` **3/3**, `OrderShipmentDispatchPlanStoreFilter270R3Tests` **3/3**.
