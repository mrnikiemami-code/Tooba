# TB-279-R2 — Architect review: PARTIAL, substantially narrowed
Date: 2026-11-18 (Asia/Tehran)
Reviewed final source: f867dcb7163608915112c7450196c49bc8a3ba37
Implementation commit: 3b1ce75d917a6cedd9b375e7fbe4880895cab023
Expected base 6e3d1873f7f9926c2906ae4e26547a6c9ef97eeb; actual claimed base ff69838cfc505b8d86e7e5e1c3b13232e3e84634 is the permitted docs-only issuance.
Verdict: **PARTIAL / NOT final Architect ACCEPT**. Preserve the actual delivered repairs below; do not reopen everything or create one task per helper.
Method: independent GitHub source/diff review, not an Architect test/deploy/localhost run. Reported test/runtime/database receipts remain worker evidence.

## Source-confirmed repairs retained
1. Operator start/refund retry now transports the reviewed decision version via the actual query-bound helpers. Workspace freezes the value; dispatch uses the correct helper, not the old body-only route. The mounted 277 test asserts start-refund?expectedDecisionVersion=0 and an empty body. Retry includes both reviewed failed identity and decision version.
2. Workspace resolution sends all five actual condition portions; frontend mapper carries them. The 279 mounted test asserts the full per-condition body and local over-condition refusal.
3. Custody sends explicit operator_attestation + proof; its client ceiling is rejected-received of the chosen condition minus that condition's actual handbacks. The Directory refuses carrier_delivered_back because no owning delivery reader exists.
4. Resolution/custody writers read the owning Order line precision and refuse off-quantum quantities. Exact stored replay remains before those new gates.
5. Approval now compares a supplied reviewed resolution version before and under the locked scope. This closes the specific stale-plan-value selection defect, subject to missing decision-version finding B1 below.
6. Allocation reads now include all case statuses even when the active-status set is empty. Custody retains accepted + TOTAL owed custody (the actual call uses OwedCustody across both conditions, not only one condition as some report prose says). The real Directory/evaluator fact asserts 2.5 occupied / 0.5 remaining after the specified 3/2/1/0.5 scenario.
7. Legacy delivery ambiguity now counts expired + live plausible slices; new claim ordering remains oldest eligible first. Reacquisition verifies named slice capacities instead of aggregate capacity alone.
8. Customer effect cleanup invalidates epoch on unmount, closed state invalidates it, every child send checks it first, v1/tb278 evidence blocks a new act, and production callbacks pass the expected exact envelope/body/key to recordChildOutcome. The new mounted/unit negative cases exercise those paths.
9. Git ancestry is now normal: three commits ahead of ff69838cf, no divergence. Key XML and 20 listed incident scripts are removed from the current tracked tree; scoped ignore rules exist. This does not erase historical exposure or prove local preservation independently.

These are meaningful delivered changes. Do not describe the operator UI as still having the old missing wire fields.

## B1 — NEW decision still accepts a missing decision version (blocking, inherited R1-C / task D1)
Source: ReturnDirectory.Corrections.cs, ResolveExistingDecision lines 184–220:
`if (expectedVersion is { } expected && expected != state.EffectiveVersion) ...`.
When expectedVersion is NULL on a NEW undecided/reopened case, the branch is skipped and the method returns null, authorizing a new decision. ApproveReturnCommand / RejectReturnCommand and seller bodies still default ExpectedDecisionVersion to null; the route forwards it and the owner has no unconditional missing-version refusal.
EnsureReviewedPlanVersion is a DIFFERENT gate: it only requires resolution revision when FromReviewedPlan=true. It does not require a decision revision and returns immediately for an implicit full-receipt baseline.
Concrete counterexample: approve -> audited reopen -> full coherent receipt with no explicit partial plan -> seller/new internal approve or reject omitting decision version. The current owner can append a NEW decision over the reopened case even though this request never named the decision revision it reviewed.
Required minimal closure:
- AFTER resolving an actual recorded replay, require expected decision version for EVERY NEW approve/reject.
- Missing is a stable typed no-effect-at-this-new-decision refusal; stale compares with current effective version under the case lock. Do not add it to a client uncertainty-clearing set merely because a later request refused.
- Preserve exact recorded replay with no new decision/effect.
Focused owner facts: missing version after reopen writes no baseline/revision/status/amount/allocation; stale refuses; exact current succeeds; genuine stored replay remains zero-effect. Exercise direct Directory and one reachable seller/Order adapter shape. No new schema is needed.

## B2 — rejected/corrupt unallocated held evidence can still disappear (blocking, inherited R1-B / task C4)
Sources:
- ReturnEligibilityEvaluator.cs:165–200 restricts legacy item fallback to Requested/Approved/RefundProcessing/RefundFailed/Completed.
- ReturnDirectory.Allocations.cs:102–104 sets held=0 when receiving.IsCoherent is false.
- TrimActiveAllocationsToHeldAsync:166–170 silently returns when it cannot prove a legacy held allocation.
The repaired all-status ALLOCATION query only helps when allocation rows already exist. A legacy Rejected/Cancelled case with actual recorded received goods and NO allocation rows is still excluded from the legacy fallback. Its held goods therefore consume neither a live row nor fallback capacity.
Concrete counterexample: one seller order, one delivered unit, legacy ordinary case Rejected with one received unit, no custody, no allocation rows -> requestIds active=[], items=[], allocations=[] -> remaining capacity can become the full delivered quantity, despite the outstanding handback.
A second counterexample is a requested case with incoherent receiving and no usable complete resolution. Reject sets held=0 and releases its existing allocations. Corrupt evidence is not positive proof that no goods arrived.
Required minimal closure:
- Read outstanding rejected/cancelled receiving/custody obligations when allocation rows are absent. Keep them occupied or publish typed ambiguity until positively attributable/handed back; do not invent a slice.
- Incoherent/unknown physical evidence must refuse release or preserve a conservative blocked claim; never held=0 as a successful release.
- A failed allocation must surface/retain the explicit blocker/occupancy; no silent disappearance.
- Keep the already-passing 2.5/0.5 mixed-capacity behavior unchanged.
Focused owner/evaluator facts: rejected-only unallocated legacy goods cannot fund a second claim; unknown/corrupt receipt rejects release with zero row changes; actual full handback may release only its own rejected quantity; valid allocation and mixed-portion positives remain green. Rollback-only/controlled facts, no live fixture or backfill.

## Proof boundary — keep claims proportional
The 15 directory facts include valuable new plan-version, quantum, carrier refusal and mixed-capacity tests. They do not cover B1/B2. No passing total substitutes for these exact owner counterexamples.
The 279 mounted tests assert actual resolution/custody bodies; the fetch stub still returns 200 for every request. Thus they prove component transport shape, not endpoint-to-Directory successful execution.
The reported positive financial/Inventory integration remains incompletely supported: this directory harness's CountingGateway and CountingInventory THROW if called, so these facts establish zero-effect paths, not the required actual accepted-usable Inventory argument or failed-A/B-reserved/A-retry effectful budget refusal. The existing three relational facts retain their stated lock/budget-query/atomic-approval scope; do not claim they execute the first/refund retry effectful reservation path.
Before declaring all issued G scenarios closed, cite/run meaningful focused controlled-port owner tests for those explicit scenarios, including an Inventory recorder asserting 2 rather than 3. No real provider call or committed fixture is requested.
The whole Returns run, broad AMSC filter, whole-repository vitest collection attempt and four diagnostic suite reruns exceed the requested focused selection; preserve this disclosed history, do not run another blanket sweep.

## Containment accepted only at its authorized boundary
Current diff confirms the key XML is no longer tracked. Private copy/hash preservation and active ring inspection are worker-reported receipts; no key contents were printed by this review. Security state remains **CONTAINED_PENDING_REVIEWED_RETIREMENT** as authorized; do not rotate/revoke/delete merely to turn this review green.
The 277-R1 RESULT.bridge.txt is STILL present in the tracked tree and has no matching exact architecture-evidence ignore entry, contrary to the report's index-removal claim. This is a small source-membership cleanup, not another business redesign: preserve its private/local copy before untracking with a normal commit and exact ignore; never rewrite the original historical RESULT.md.
A different current publish key id alone does not prove old shared-database Shipping carrier ciphertext never used the exposed source-root key. Assess actual protected payload/key dependencies before a retirement decision. Do not state all served ciphertext is unaffected without that evidence; no credential compromise is asserted here.
The incident script count differs between prose/manifest/matrix. The current compare shows 20 incident script removals; reconcile inventory by actual paths rather than inventing extra removals.

## Current state / next boundary
279-R2 is PARTIAL with most connected repairs retained; B1/B2 and the named proof gap remain. Prior 278/279/279-R1 history is unchanged. No full user/Marketplace/provider/financial acceptance.
No new task is issued by this review. Record a single narrow closure boundary for B1/B2 plus missing controlled-port proof and the historical report's source-membership cleanup, not a repeated full-module redesign or micro-task chain.
No migration/DDL/Down/reapply/live business write/demo/reset/secret rotation/provider effect authorized here.
STOP_FOR_ARCHITECT; activeTask NONE; automaticNextImplementationTask NONE.
