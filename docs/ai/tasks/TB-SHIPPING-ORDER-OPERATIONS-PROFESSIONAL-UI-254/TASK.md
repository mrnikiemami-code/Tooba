# TB-SHIPPING-ORDER-OPERATIONS-PROFESSIONAL-UI-254

Parent: `TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R6` (worker PASS, Architect accepted scope)
Base SHA: `8469a82f472f1902ae215c640761a98dc0aab4cc`
Channel: `tooba-main` · Protocol `BRIDGE-WAKE-V1` · Bridge task row `28d324fe-83eb-4a07-92de-6755f4c0db19`
State: **ISSUED / ACTIVE — user visual rejection of the order shipping-operations page**

## Why this task exists

The user rejected the current Marketplace order "shipping operations" presentation. On the normal order page
the operational delivery section mixes three different things in one dense block: inline per-line checkbox and
quantity inputs, the parcel/package presentation, and — next to it — a second seller-aside that repeats the same
parcels with their own action buttons. The result is ambiguous: it is unclear which control belongs to which
group, which surface owns a parcel, and whether the inline numbers are a review step or an edit of business data.

Decision word: `REMOVE_AMBIGUOUS_INLINE_SELECTION_AND_DUPLICATE_SHIPMENT_PRESENTATION`.

## Scope

### A. The normal operational delivery section is display-only

- The «ارسال سفارش» operational-group card shows facts only: delivery method, Jalali date, LTR time window,
  the group's lines with quantity/status, fee breakdown, and — for a Marketplace cross-seller group — ONE
  explicit entry point to the workflow.
- It must contain **no** `input[type="checkbox"]`, no quantity field, no reuse-parcel selection and no inline
  item selection of any kind. Selection is never a default-visible, always-on set of inputs.

### B. Multi-seller parcel preparation moves into a dedicated dialog

- A new, clearly titled dialog (`AdminOperationalGroupWorkflowDialog`) owns: line/quantity review for members
  with remaining packed quantity, explicit parcel-reuse selection, the single submit, the retained-attempt
  review, the reconcile entry and the inline error.
- The dialog is opened deliberately from the group card; it is not rendered inline and it does not duplicate
  the card's facts.

### C. Exactly ONE owner for parcel/package display and operations

- A parcel whose shipping group is covered by an operational group is displayed and operated ONLY in that
  group's card (track / create / dispatch / deliver / cancel as the server permits).
- The seller aside no longer repeats those parcels. It keeps only legacy/orphan shipments that no operational
  group covers, with its own empty state.
- No seller line-level create/edit control is added back under the seller sections; create/editable quantities
  remain inside the existing purposeful create dialog when server/actor eligibility allows.

## Preserved invariants (must not be reopened)

- 253-R4 durable occurrence envelope (actor scope, checkout, member set, exact quantities, reuse ids, accepted
  method) claimed before the first allocation; identical envelope replays, changed envelope is a typed
  `workflow_intent_conflict` with zero writes.
- 253-R5/R6 rules: never mint while a retained attempt is unresolved; no bare server code releases an occurrence;
  only an authoritative completion clears it; reconcile sends the RECORDED envelope verbatim.
- Synchronous single-flight on submit (one mutation per tick), viewer/scope switch clears in-memory refs.
- No backend, contracts, DI, owner, quote, calendar, price, schema or migration change; no business-data write
  from this UI rework.

## Verification required

1. Mount tests prove the section renders no selection control (checkbox / quantity / reuse) and that a covered
   parcel has exactly one owner; the dialog carries the review and its retained/reconcile affordances.
2. A store-owned group (no consolidation) also exposes no selection control while keeping its parcel actions.
3. Typecheck and production build green; FE3000 restarted on the new build; served chunk contains the new dialog
   surface and the canonical order page responds 200.

## Not authorized

Backend/schema/migration, business SQL, reseed/reset, alternate database, Tipax/provider work, Store promotion,
cart/checkout/payment/order mutation, screenshots as delivery proof, any change under the read-only Shopeiva
reference tree.

## Exit

`STOP_FOR_ARCHITECT` after execution; `automaticNextImplementationTask: NONE`.
