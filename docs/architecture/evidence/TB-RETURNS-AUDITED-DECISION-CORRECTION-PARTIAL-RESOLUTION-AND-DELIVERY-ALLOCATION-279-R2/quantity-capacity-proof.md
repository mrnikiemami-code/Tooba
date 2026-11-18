# TB-279-R2 — QUANTITY, CAPACITY AND UNIT PROOF (C, D, E)

## 1. One occupancy authority

`ReturnEligibilityEvaluator.ReadOccupancyAsync` (lines 157–227) now:

1. resolves the ACTIVE-status case ids (Requested/Approved/RefundProcessing/RefundFailed/Completed);
2. resolves **every** case id of the seller order and reads live (`ReleasedAt IS NULL`) allocations for all of them, so a
   rejected-only seller cannot present an empty active set and gain free capacity;
3. reads item quantities only for the active set, and treats cases WITHOUT allocation rows as conservative legacy
   unallocated quantity (never as zero).

The allocation rows themselves, not the case status, decide what is held.

`TrimActiveAllocationsToHeldAsync` (`ReturnDirectory.Allocations.cs:136-190`) releases newest-delivery-first and splits a
straddling row, so an older window keeps its claim and only the returned portion is released. Custody calls it with
`accepted + OwedCustody(condition)` (`ReturnDirectory.Resolutions.cs:375-390`), NOT with the owed remainder alone.

## 2. Required counterexample — stored fact, not arithmetic

`R2_a_handed_back_remainder_keeps_the_accepted_units_occupied` seeds a real claim (delivered/requested/received **3**,
3-decimal-capable line, one active allocation of 3), publishes `acceptedUsable=2 / rejectedReceivedDamaged=1`,
approves, then hands back **0.5** of the rejected damaged unit.

| Step | Live allocation | Eligibility remaining |
| --- | --- | --- |
| after approval | 3 | 0 |
| after 0.5 handback | **2.5** (one row 2.5 + one released 0.5) | **0.5** |

A "trim to owed custody" implementation would have produced 0.5 occupied / 2.5 free; the fact fails in that case.

## 3. Reviewed plan + version at the decision boundary

`EnsureReviewedPlanVersion` (`ReturnDirectory.Corrections.cs:237`) is applied at case load (`ReturnDirectory.cs:225`), on
the locked re-read (`:243`) and on the recovery path (`:339`). The fact
`B_a_decision_over_a_reviewed_plan_must_carry_that_plan_version` proves:

- missing plan revision ⇒ `return.resolution.review_version_required`, status still `Requested`, `ApprovedAmount` null;
- stale plan revision ⇒ `return.correction.version_stale`, zero money calls;
- the reviewed revision ⇒ approval moves status, effective decision, plan `FromReviewedPlan` and the frozen amount
  together.

The approved amount remains `accepted × STORED unit price` and is written once; `RefundAmount` keeps the requested amount.

## 4. Owning unit quantum at the write boundary

`PublishResolutionAsync` reads the owning line precision for every item (`Resolutions.cs:104-108`) and refuses a portion
that is not aligned (`:145-149`). `RecordCustodyAsync` reads the same precision for the handback item (`:334-338`) and
refuses a mis-aligned quantity (`:339-344`). Missing metadata follows the existing conservative refusal, never six
decimals.

Facts:

- `R2_a_plan_portion_finer_than_the_owning_unit_is_refused_before_any_row` — `0.0001` on a 3-decimal line ⇒
  `return.resolution.portion_invalid`, zero resolution rows.
- `R2_a_handback_finer_than_the_owning_unit_and_an_unproven_carrier_are_refused` — `0.0001` ⇒
  `return.custody.quantity_invalid`; zero custody rows.

## 5. Evidenced custody only

- `ResolveCustodyProofKind` (`Resolutions.cs:428-445`) accepts ONLY `operator_attestation`; `carrier_delivered_back` is
  recognized and refused with `return.custody.proof_kind_unavailable` because no owning carrier reader exists.
- The ceiling is `OwedCustodyOfCondition` for the named condition (`:312-327`), so a usable handback can never discharge a
  damaged obligation and never-received quantity is not handback-able.
- The recipient is server-derived from the case (`request.RequestedByUserId`), never from the request body.

## 6. Legacy slice attribution

`ReturnAllocationPolicy.HasAmbiguousLegacy` (`:108`, used at `:133`) now considers every plausible historical origin,
including expired slices, so `An_old_legacy_case_is_never_guessed_onto_one_of_two_delivery_slices` refuses instead of
picking oldest-first. `A_legacy_case_with_one_delivery_is_discounted_from_that_only_possible_origin` keeps the one lawful
case working, and `Two_deliveries_at_the_same_instant_merge_into_one_honest_slice` keeps the same-instant merge.

## 7. Limits

- All of the above run on in-memory EF over the real directory/evaluator; the relational rollback-only facts cover the
  lock/budget/atomic-approval path only.
- No live case, payment, provider, stock or notification effect was produced.
