# RESULT — TB-SHIPPING-ORDER-OPERATIONS-PROFESSIONAL-UI-254

Status: **PASS within the issued UI/correction + FE deploy scope**
Decision: **REMOVE_AMBIGUOUS_INLINE_SELECTION_AND_DUPLICATE_SHIPMENT_PRESENTATION**

NOT Architect ACCEPT, NOT user Visual ACCEPT, NOT PSP/provider work.

- Parent: `TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R6`
- Base: `8469a82f472f1902ae215c640761a98dc0aab4cc`
- Channel: `tooba-main` · Bridge task row `28d324fe-83eb-4a07-92de-6755f4c0db19`
- Canonical FE3000 PID 30816, BUILD_ID `o7FdiG_1QEjT376HubPBj` → Host5088 (unchanged) SingleStore / store-alpha / `tooba_alpha`

## What the user rejected

The «ارسال سفارش» operational delivery block on the normal order page showed, simultaneously, inline per-line
checkbox/quantity selection, the parcel cards, and a second seller aside repeating the same parcels with their own
action buttons. Ambiguous in three ways: which control belongs to which group, which surface is allowed to operate
a parcel, and whether the inline numbers are a review step or a live edit of business data.

## What landed

### A — the operational delivery section is display-only

`admin-order-operational-delivery-section.tsx` renders facts only: group responsibility/method, Jalali date, LTR
time window, the member list with quantity + status badges, the fee breakdown, and the group's constituent parcel
cards with the server-permitted next action. It contains no `input[type="checkbox"]`, no quantity field, no
reuse-parcel selection and no inline member selection; `Select-String` over the file finds no `input`, `checkbox`,
`onChange` or `setSelections` at all. A Marketplace cross-seller group exposes exactly ONE deliberate entry point
(`admin-order-operational-prepare-<groupKey>`), plus the retained-attempt review entry
(`admin-order-operational-reconcile-<groupKey>`) when the recorded operation is unresolved.

### B — one dedicated dialog owns the group workflow

New `admin-operational-group-workflow-dialog.tsx` (`admin-order-operational-dialog-<groupKey>`): line/quantity
review for members with remaining packed quantity (`admin-order-operational-qty-<lineId>`), explicit reuse-parcel
selection limited to that member's own existing parcels (`admin-order-operational-reuse-parcel-<shipmentId>`), the
single submit, the retained-envelope read-only review, the reconcile the RECORDED envelope path, and one inline
error (`admin-order-operational-workflow-error`). It is titled, dismissible, and never rendered inline on the page.

### C — exactly one owner for parcels and actions

`admin-order-items-shipping-panel.tsx` builds `operationalGroupShippingGroupIds` from the operational groups and
filters seller shipments to `orphanShipments` only:

```startLine:424:429:src/frontend/app/admin/admin-order-items-shipping-panel.tsx
          const shipments = seller.shipments ?? [];
          // TB-254: one parcel/action owner. Parcels already displayed under their operational group
          // are NOT repeated here; only legacy shipments without a covered group stay visible.
          const orphanShipments = shipments.filter(
            (shipment) => !shipment.shippingGroupId || !operationalGroupShippingGroupIds.has(shipment.shippingGroupId),
          );
```

The aside is titled «مرسوله‌های مستقل این فروشنده», renders only when orphans exist, and keeps its own empty state.
A group-covered parcel therefore appears once, in its operational card, and its actions (`assign_tracking`,
`dispatch_shipment`, `deliver_shipment`, `cancel_shipment`) are wired through `parcelCapabilities` /
`onParcelTrack` / `onParcelOperation` from the same panel. A Store-owned (non-consolidated) group exposes no
selection control either — only its own parcel card and the permitted action, with create still gated by the
server/actor eligibility that already existed.

## Invariants preserved (253-R4/R5/R6 — no reopening)

- The full prebound envelope (actor scope, checkout, member set, exact quantities, explicit reuse ids, accepted
  method) is still persisted before the first POST; the dialog replays the RECORDED selections and reuse ids
  verbatim instead of re-deriving them from rows after partial server progress.
- An unresolved retained attempt still blocks a changed review and never re-mints; no bare server code releases an
  occurrence; only an authoritative completion clears it; a viewer/scope switch clears in-memory refs.
- Submit remains synchronous single-flight (`useRef`), with `try/finally` and inline error presentation.

## Executed proof

- Mount/state tests: **22 / 22** (see RECEIPTS for the exact case list), including the four new TB-254 contracts:
  display-only static render, no selection control anywhere outside the dialog, exactly one owner per covered
  parcel + orphan stays in the seller list, and a Store-owned group with existing parcels having no selection
  control but one tracking action.
- `tsc --noEmit`: 0 diagnostics. `next build`: success, `o7FdiG_1QEjT376HubPBj`.
- FE3000 restarted on that build and serving it; `/fa` 200; canonical order page
  `http://127.0.0.1:3000/admin/orders/01a1b92d-e97e-7000-999a-143350d0edaf` 200; the served chunk carries the
  dialog/quantity/reuse testids and exactly one prepare entry.
- No backend change, no migration, no business SQL, no cart/checkout/payment/order mutation. Host5088 untouched.

## Known / disclosed

- `frontend-source-size.guard.test.ts` and `frontend-flat-folder.guard.test.ts` are **pre-existing RED** in this
  area; this slice adds one file to that unbaselined family and grows the already-oversized mount test. Neither
  baseline JSON was edited (no rebaselining, no guard sweep).
- The canonical order page was verified at the HTTP/build-artifact level; the authenticated admin DOM was not
  re-walked in a browser in this slice (mount tests are the DOM authority for the change).
- Tipax / external provider remains deferred. Store was never promoted.

## Not delivered

No Architect/Visual acceptance, no PSP/payment/order flow work, no full Shipping or Order suite, no screenshot
matrix, no backend hardening. `automaticNextImplementationTask: NONE`. `STOP_FOR_ARCHITECT`.
