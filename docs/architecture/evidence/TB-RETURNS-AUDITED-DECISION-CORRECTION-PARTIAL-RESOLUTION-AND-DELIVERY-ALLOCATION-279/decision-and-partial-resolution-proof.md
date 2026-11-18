# TB-279 — Decision authority, audited correction and partial resolution proof

Scope proven here: **A** (append-only effective decision authority with version-fenced reopen) and **B** (reviewed
per-item resolution that freezes only the accepted money). Money/stock ports are controlled; no real PSP/Wallet/Inventory
call exists anywhere in this evidence.

## 1. One effective-decision authority

| Fact | Where it lives | What it guarantees |
| --- | --- | --- |
| version-0 baseline | existing `returns.return_request_decisions` row | never updated, never deleted — the original review is immutable history |
| every later act | new `returns.return_request_decision_revisions` | append-only: version, decision, reviewed reason, actor, timestamp, occurrence key |
| single read | `ReturnDirectory.ReadDecisionStateAsync` | one read used by `MapSnapshotAsync`, the work/capability projection, `ApproveAsync`, `RejectAsync`, `StartFirstRefundAsync`, `StartFirstRefundAsync` retry and the correction commands, so no surface can disagree about the effective decision or its version |
| stale review | `ExpectedDecisionVersion` on decide/reopen | a reviewer who started before a correction gets `409 return.correction.version_stale` with zero writes instead of overwriting a newer review |
| replay | occurrence key per correction | the same key replays the recorded revision with **zero effect**; the same key with a changed reason is `return.correction.occurrence_conflict` |

Proof: `A_reopen_appends_one_reviewed_revision_and_keeps_the_corrected_decision_visible` (baseline row intact, one
revision appended, effective decision flips, snapshot reports the corrected decision),
`A_post_correction_decision_binds_the_current_revision_and_a_stale_one_cannot` (a decision at the current version
succeeds and lands as the next version; a decision carrying the pre-correction version is refused),
`A_same_occurrence_replays_and_a_changed_reason_conflicts`,
`A_stale_reviewed_version_is_refused_before_any_row`.

## 2. Reopen is a real gate, not a UI flag

`POST /v1/admin/returns/{id}/reopen` evaluates, in order, before any write:

| Refusal | Code | Why it must refuse |
| --- | --- | --- |
| courier-origin case | `return.correction.source_unsupported` | courier claims are not the customer's reviewed decision |
| no effective decision | `return.correction.not_applicable` | there is nothing to correct |
| status not decided | `return.correction.state_invalid` | only a decided case can be reopened |
| any refund attempt (Pending / Failed / unknown / Succeeded) | `return.correction.financial_effect_recorded` | money may already have moved; history is not erasable |
| any custody fact | `return.correction.custody_recorded` | goods were handed back |
| any item disposition | `return.correction.physical_effect_recorded` | stock was already settled |
| any settlement/closure fact | `return.correction.source_effect_recorded` | the case is closed on real facts |

A case whose decision had **no** money or physical effect is therefore correctable (that is the user's "reconsider a
no-money decision" requirement), and a case with effects is not. Proof:
`A_recorded_money_attempt_blocks_the_correction` (typed refusal, 0 revisions, 0 attempts),
`A_no_money_decision_may_be_corrected_and_a_started_money_fact_may_not` (policy table),
`An_undecided_case_has_nothing_to_correct`, `A_handed_back_item_or_a_courier_case_is_never_correctable`.

## 3. Capacity is re-acquired, not double-counted

A rejection released the claim's delivery-slice allocation. On reopen the case must take its own released capacity back,
and it must compete with *other* cases only — never with its own historical quantity.

- `IReturnEligibilityEvaluator.EvaluateAsync(..., Guid? excludeReturnRequestId = null)` and
  `ReturnEligibilityEvaluator` subtract every **other** case's occupying quantity only.
- `ReturnDirectory.Allocations.cs` passes the case's own id when re-acquiring and when re-checking a rejection.
- A sibling case that already took the released capacity is `return.correction.capacity_conflict` with zero rows.

Proof: `A_rejected_case_re_acquires_its_own_released_capacity` (1 allocation row, 0 provider calls) and
`A_rejected_capacity_that_a_sibling_case_already_took_is_a_typed_conflict` (typed conflict, zero rows written).

> This was an in-task defect: before the fix every legacy correction refused with `capacity_conflict`. The fact that
> caught it is retained in the suite.

## 4. Partial resolution — accepted quantities only

`POST /v1/admin/returns/{id}/resolution` publishes one reviewed plan version; `ReturnResolutionPolicy` is the single
authority for what it means.

| Rule | Behaviour |
| --- | --- |
| coverage | one row per requested item; `accepted + rejected == requested`, otherwise `return.resolution.incomplete` |
| ceiling | `accepted <= received` (cumulative inspected quantity), otherwise `return.resolution.received_exceeded` — refused, **never clamped** |
| money | the approved amount is derived from `accepted × the line's STORED unit price`; the requested `refund_amount` stays visible and immutable |
| freezing | approval writes `return_requests.approved_amount` once, before the first attempt; later plans are `return.resolution.version_stale`/`state_invalid` |
| all-rejected | zero owed — not "refunded" |
| unaccounted | a plan that leaves part of the claim unaccounted is `return.resolution.incomplete`, not a silent partial |

Proof: `B_a_partial_plan_freezes_only_the_accepted_money_before_the_first_attempt`,
`B_a_plan_that_accepts_more_than_was_received_is_refused_with_no_row`,
`B_a_plan_that_leaves_part_of_the_claim_unaccounted_is_refused`,
`A_partial_resolution_refunds_only_the_accepted_quantities`,
`A_plan_that_accepts_more_than_was_received_is_refused_not_clamped`,
`A_plan_that_leaves_part_of_the_claim_unaccounted_is_incomplete`,
`No_plan_and_no_receipt_keeps_the_legacy_amount_out_of_the_physical_math`.

## 5. A reopened case stays decidable

`ReturnWorkPolicy` used to gate `CanDecide` on `RecordedDecision is null`; because the baseline decision row must survive
a correction, a reopened case published `category=decision` with every action false. The gate now reads the
**effective** decision, so a reopened case publishes `reopen_for_review`/`publish_resolution`/`decide` truthfully and a
settled case publishes none of them.

Proof: the directory fact above plus the live read — the settled user case returns
`correction.canReopenForReview=false` with `return.correction.state_invalid` and no action set.

## 6. Refusal-first ordering (no half acts)

Every correction command resolves the case under the per-case row lock, re-reads the decision state, re-checks the
effect gates and only then writes. A refusal is typed and writes nothing; an occurrence replay is answered from storage
before any live gate. The live probes in `RECEIPTS.md` §4 show each refusal against the real settled case with the case
row unchanged.
