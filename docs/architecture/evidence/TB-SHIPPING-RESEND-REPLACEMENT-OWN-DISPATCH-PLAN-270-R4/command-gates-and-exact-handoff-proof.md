# New-confirmation gates vs stored history, and exact parcel handoff — proof (Repairs B and C)

## B — REPLAY FIRST, NEW-WRITE GATES SECOND

### Gate composition

`AdminOrderOperationsOrchestrator.RequireNewPlanGatesAsync` is now the ONE composition used by the confirmation insert and
by the new-calendar preview:

```
RequireNewPlanGatesAsync(located) =
    IsCheckoutCancelled(located.Checkout)  → order.cancelled.blocks_action
    THEN RequirePlannableAsync(located)    → active package membership (Created|Dispatched) → order.shipment_plan.package_locked
                                           → RequirePlannable(...)  (linked attempt, live Created, Paid, group present)
```

`RequirePlannableAsync` is the ASYNC gate that reads `_fulfillment.GetActiveMembershipByShipmentIdsAsync`. The
confirmation previously called the synchronous `RequirePlannable` only, so the membership read never ran. The stored-plan
replay sits **before** this method, so an exact authorized replay is still answered after the parcel/package/checkout
later changed, and no live eligibility work is done for it.

### Command-level cases (Order `AdminShipmentPlan270R2Tests`, **28/28**)

| Case | Assertion |
| --- | --- |
| `A_direct_plan_confirmation_inside_an_active_package_is_refused_with_zero_insert` | preview and a direct `plan_shipment_dispatch` each answer `order.shipment_plan.package_locked`; `Store.Rows` stays empty — the hidden-UI scenario cannot insert |
| `A_direct_plan_confirmation_on_a_cancelled_order_is_refused_with_zero_insert` | preview and confirm each answer `order.cancelled.blocks_action`; zero inserts, no calendar |
| `A_recorded_plan_answers_its_own_history_after_cancellation_and_never_offers_a_new_calendar` | a genuine authorized confirmation writes one row; the SAME parcel read after cancellation returns `HistoryOnly = true`, no options, the stored promise; an exact replay answers `replayed: true` with no extra row; a different occurrence answers `order.shipment_plan.conflict` and still no extra row |
| retained | denied actor (`order.operation.denied`, even on replay), unpaid seller order (`payment_required`), cross-checkout / unknown parcel (`order.operation.invalid`) unchanged |

Fixture provenance: the cancelled checkout is the owning fact `SellerOrderStatus.Cancelled` (what
`IsCheckoutCancelled` reads); the package lock is the owning `ActivePackageMembershipSnapshot(…, Created)` returned by the
Fulfillment port. No production rule was relaxed to make a test pass.

## C — EXACT PARCEL HANDOFF, NO SINGLE-CANDIDATE SHORTCUT

### Resolver (`features/order-operational-groups/created-parcel.ts`)

- **Resend**: requires a non-empty `sourceShipmentId` **and** a non-empty reviewed key; the child must be `Created`, must
  record `resendOfShipmentId === source`, must have no `damagedResolutionId`, and must record exactly the reviewed
  `shippingDispatchKey`. Exactly one such child → its id; zero or more than one → `null`. The former "only remaining
  `Created` candidate" branch is gone.
- **Replacement**: the reviewed decision must live on the **requested source parcel**, have `outcome = replacement`,
  `status ∈ {Reserved, Fulfilled}`, `occurrenceKey === key`, and a non-empty `replacementShipmentId`. All matches must
  resolve to ONE replacement id (two ids = ambiguous → `null`). That parcel must exist, be `Created`, carry a
  `damagedResolutionId` that is one of the matched decisions, and — when it records a source — that source must be the
  requested one. Cross-source decision scanning is removed.

### Helper evidence — `created-parcel.test.ts` **13/13** (run explicitly)

- one `Created` child with a DIFFERENT key → `null`;
- a missing/blank key with exactly one `Created` child → `null`;
- the same key on a child of ANOTHER source → `null`;
- an exact child that already dispatched → `null` (history, not a planning target);
- two still-created candidates without a key → `null`;
- replacement of the requested source, its key and its own `Created` child → exactly that child;
- the reviewed key recorded on ANOTHER source's decision → `null` (this source's child never opens);
- two decisions answering one key with different parcels → `null`;
- a decision that never linked a parcel → `null`;
- a linked id whose parcel is not the decision's own `Created` child → `null`.

### Connected panel evidence — `admin-order-operational-delivery.mount.test.ts` **75/75**

New: `TB-270-R4 mounted: a child WITHOUT the reviewed key, a decision of ANOTHER source and an ambiguous pair open NO
calendar` (three sub-cases through the real `AdminOrderItemsShippingPanel` + real POST/review flow):

```
(1) resend POST returns the ONLY Created child of the source with shippingDispatchKey = "key-of-another-occurrence"
    → 0 plan modals, 0 shipment-plan requests
(2) replacement POST returns a snapshot whose reviewed decision lives on another source parcel
    → 0 plan modals, 0 plan requests
(3) one reviewed key answers for TWO different Created replacement parcels on the requested source
    → 0 plan modals, 0 plan requests
```

Retained positive connected cases still open exactly the created child and request its own preview:
`TB-270-R3 mounted: a returned-source resend create opens the plan for the EXACT created child`
(`/shipment-plan/<RESEND_CHILD>`, never the source) and `… a replacement with real stock opens the plan for the EXACT
replacement child`. No duplicate create, no second wizard, no changed creation identity policy, no owning-result
enrichment was needed — the existing command/reconcile payload already carries the exact identity.
