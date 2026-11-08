# ARCHITECT REVIEW — TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R5

Scope of review: R5 corrected the two R4 recovery defects. This review accepts what it actually fixed and
rejects one newly introduced mechanism.

## Accepted within the R5 code/test scope

1. **Changed-review remint is blocked.** `resolveOperationalGroupAttempt` no longer mints a new occurrence
   when the reviewed envelope differs from a retained attempt. It returns `blockedByRetainedAttempt: true`
   with the recorded occurrence, members, selections and fingerprint unchanged, and the section disables the
   normal Create path from the current review fingerprint.
2. **Recorded occurrence parcels reconcile before the remaining-quantity gate.** `BuildPlan` inspects the
   exact occurrence `DispatchKey` for a member without a durable link, accepts only evidence matching the
   immutable envelope (`RecordedParcelEvidenceMatches`), plans `MemberPlanKind.ReconcileRecorded` with those
   exact ids, links them without a replacement parcel, and fails closed as `workflow_intent_conflict` on a
   mismatch. Case G (fault injected on the member-link insert *after* the Fulfillment parcel committed) and
   case H (wrong-quantity seeded parcel) are the right seams for these claims.

## Not accepted — error-code-based abandonment of an unresolved operation

`OPERATIONAL_GROUP_NO_CLAIM_REFUSAL_CODES` +
`releaseOperationalGroupAttemptAfterNoClaimRefusal` treat a bare server code as proof that the recorded
occurrence was never durably claimed, and `OperationalCard` invokes that release automatically on both the
normal action path and the reconcile path.

That inference is unsound:

- `workflow_group_foreign`, `consolidate_requires_multi_seller`, `consolidate_incompatible`,
  `workflow_occurrence_required` and `order.operation.denied` are evaluated **before**
  `_occurrences.GetAsync`/`ClaimAsync` on the current request. They prove something about *this request's
  path*, not about an earlier request that already claimed the same occurrence and persisted a constituent
  parcel before failing.
- A retry of that same occurrence can therefore meet `order.operation.denied` or changed lifecycle
  compatibility while real durable progress exists. Deleting the only unresolved hint there loses the exact
  envelope and can mint a second operation later, which is the abandonment R3/R4/R5 were built to prevent.

A code list is not a durable-state proof. Only an authoritative completed/resolved result — or a structured
server result stating that **this** occurrence has no claim/progress under the authorized scope — may clear
a retained envelope. No such server proof exists, so the release mechanism must be removed rather than kept.

Decision: **R5 PARTIAL**. Both requested fixes stand; the no-claim release exception is rejected and is the
only remaining correction for the recovery path. Correction issued as
`TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R6`.
