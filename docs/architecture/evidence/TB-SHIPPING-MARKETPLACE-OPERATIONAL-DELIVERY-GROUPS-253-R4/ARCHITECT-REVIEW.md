# ARCHITECT-REVIEW (append) — TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R4

Concise correction recorded while issuing `TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R5`
(2026-11-08). R4's worker PASS is **not** Architect ACCEPT.

## Retained from R4 (accepted as implemented bounded improvements)

- Durable, additive `order.order_operational_group_occurrences` + `_members` envelope claimed before the
  first allocation, and typed `workflow_intent_conflict` on a changed envelope under the same occurrence.
- Explicit `Create` / `ReuseOnly` / `Replay` plan kinds; reuse-only never calls the create binder and
  reuse ownership is resolved by `shipment.ShippingGroupId`.
- Completed occurrences answered from their own durable links with no remaining-quantity gate.
- Canonical migration `20261108153000_AddOperationalGroupOccurrences`, Host publish, FE rebuild and the
  recovery/TOP updates.

## Still PARTIAL — two reproducible source paths

1. **Unresolved attempt can silently mint a second operation.**
   `resolveOperationalGroupAttempt` mints a NEW occurrence whenever the current request fingerprint
   differs from the stored unresolved attempt, and the NORMAL Create button stays enabled — it
   recomputes the request from the current review and calls that resolver. An extra reconcile button does
   not close that path. A failure can leave real parcels; an edited quantity or a refreshed projection
   followed by the main button can therefore abandon the recorded envelope and start a second operation.

2. **Reconciliation after Create persists but BEFORE the member link persists is unreachable.**
   `MarketplaceOperationalGroupWorkflow.BuildPlan` tests every member against the current remaining packed
   quantity before the exact-key recovery block. When member A's parcel was durably created under the
   occurrence DispatchKey but `RecordMemberAllocationAsync` failed before linking it, the retry sees no
   member link and a consumed quantity, so it throws `workflow_selection_invalid` and the R4 recovery code
   below that gate is never reached.

Correction issued as 253-R5: block the unresolved remint at both the resolver and the JSX boundary, add an
explicit safe transition only for a typed refusal that proves no claim, and reconcile the exact recorded
occurrence parcel during complete-plan preparation (before the remaining-quantity gate) with typed
conflict on mismatched evidence.
