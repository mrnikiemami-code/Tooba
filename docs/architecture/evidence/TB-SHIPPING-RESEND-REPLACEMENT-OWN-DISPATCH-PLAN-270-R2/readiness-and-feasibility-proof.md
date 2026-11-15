# readiness-and-feasibility-proof.md — TB-270-R2

## Preview is a pure read

`GET /v1/admin/orders/{checkoutId}/operations/shipment-plan/{shipmentId}`
→ `PreviewShipmentDispatchPlanQuery` → `AdminOrderOperationsOrchestrator.PreviewShipmentDispatchPlanAsync`.

It requires the same permission as the confirmation (`order.handle` or `fulfillment.manage`), refuses a cancelled
checkout, locates **exactly** the requested parcel inside this checkout's own fulfillments, and then returns either
feasible options or a typed blocker. It never writes, never reserves stock, never creates a parcel and never commits a
plan (asserted by the focused unit case "Preview of a live created resend offers feasible customer dates without
writing").

## Which parcels can be planned at all

| Guard | Refusal |
| --- | --- |
| not a linked attempt (first dispatch of the order) | `order.shipment_plan.first_dispatch_not_plannable` |
| not `Created`, or already `DispatchedAt` | `order.shipment_plan.not_created` |
| locked in an active `Created`/`Dispatched` common package | `order.shipment_plan.package_locked` |
| seller order not `Paid` | `order.shipment_plan.payment_required` |
| parcel without a committed shipping group | `order.shipment_plan.group_missing` |

A dispatch-`/`delivered`/`cancelled` history parcel can therefore never gain a planning action again.

## Feasible calendar — one authority, no local arithmetic

Inputs come from the R1 Contracts read:

```
IShippingAttemptSchedulingLookup.LookupAsync(
    ownerKind, ownerTenantKey, ownerSellerPartyId, draftId, sourceRevision,
    serviceCode, optionCode, shippingGroupId, rateRowId, snapshotId)
```

bound from the **accepted evidence** (`AcceptedShipmentGroupBinding`) — never from browser-supplied owner/revision/row/
method. Then:

- `!lookup.HasInputs` → the lookup's own stable `ReasonCode` is the blocker (e.g.
  `shipping.attempt_scheduling.captured_revision_unavailable`); it is never flattened into "no dates".
- missing week/minutes → `order.shipment_plan.captured_inputs_incomplete`. No weekday, slot or minute is invented.
- otherwise `ShippingDispatchSchedulingAuthority.BuildReceivingCalendar(anchor, carrierWeek, customerWeek,
  maxDeliveryDays, 10, slotDuration, preparationMinutes, handoffMinutes, processingWeek, continuous)`:
  the pre-264 `PreparationDays`/`LeadDays` path is untouched; the explicit pair is used once; `Continuous` means a
  deliberately 24/7 preparation mode, `WorkingWeek` counts inside the stored warehouse week; the half-open
  `[open, close)` carrier rule and MAX transport working days are preserved.
- every offered slot is re-derived backwards through
  `ResolvePreparationDeadlines(commitment, carrierWeek, maxDeliveryDays, prep, handoff, processingWeek, continuous)`;
  a slot whose deadline is unprovable (same-day boundary / DST) is **skipped**, and a `PreparationStartUtc` earlier than
  the anchor is skipped. Customer receiving date/timezone and carrier handoff date/timezone stay independent.
- a failed calendar surfaces the authority's own reason (`shipping.scheduling.timing_policy_unconfigured`,
  `no_carrier_working_day`, `no_processing_working_day`, `deadline_unprovable`, …), never a fabricated date.

The preview returns: exact parcel/source/source-fingerprint, attempt kind, binding (method, snapshot, draft, revision,
format version, area group, rate row), readiness source + instant, server `now`, the option list with each option's
stored-meaning deadlines, the already-confirmed plan (if any) and a blocker code + Persian blocker text when nothing is
feasible.

## Proof cases (worker-run, once)

`AdminShipmentPlan270R2Tests` (17 cases, `Tooba.Order.Tests`), controlled clock/ports:

| Case | Fact asserted |
| --- | --- |
| Preview of a live created resend offers feasible customer dates without writing | options > 0, zero store writes |
| A reserved replacement hold schedules its own parcel | own hold funds it |
| A stock-pending replacement hold is an honest blocker and offers no date | `replacement_hold_missing`, no calendar |
| Only the source's usable received quantity funds the resend | damaged-only receipt cannot schedule |
| A parcel without a usable origin receipt is not plannable | `source_not_ready` |
| An incomplete historical capture is a typed blocker and never a guessed week | `captured_inputs_incomplete`, `options == []` |
| A refused captured read keeps its own stable reason | reason passthrough |
| The first dispatch parcel of the order is not plannable here | `first_dispatch_not_plannable` |
| A dispatch history parcel never gains a planning action again | `not_created` |
| An unpaid seller order cannot be scheduled | `payment_required` |
| An actor without the physical operation permission is denied even on replay | `order.operation.denied` |
| A selection outside the fresh calendar is refused as expired | `selection_expired` |
| A request without a reviewed selection is refused | `selection_required` |

## Limits

- The focused cases use controlled `IClock`/lookup/fulfillment ports; they prove behaviour, not a live carrier calendar.
- No live eligible unplanned parcel exists in the canonical data today (see `user-review.md`), so no live positive plan
  row was produced by the worker.
