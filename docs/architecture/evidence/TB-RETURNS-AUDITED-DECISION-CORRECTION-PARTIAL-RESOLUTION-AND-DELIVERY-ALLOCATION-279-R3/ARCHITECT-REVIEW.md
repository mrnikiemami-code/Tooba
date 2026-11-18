# TB-279-R3 — ARCHITECT ACCEPT within the fixed closure scope
Date: 2026-11-18 (Asia/Tehran)
Reviewed implementation: 32b48c5b53ea263cd0aae00a5da59f34b4749671
Reviewed final source/evidence: 44e2cc6b3ea168b0e93de9103b861149d0f21fe7
Actual claim base: e7db94398e663b604a536b82bd86670bb50f72db, the permitted docs-only issuance above expected dd5c1d31fcd65bce7164c115599aacb5a4e33083.
Decision: **ACCEPT for the issued B1/B2 closure, C1/C2 owning effect proofs and D cleanup. This implementation increment is CLOSED.**
No next implementation task is required for these reviewed findings. Active task NONE; automaticNextImplementationTask NONE.

## Review method and boundary
Independent GitHub source/diff review at the final SHA, including the actual owner methods, the shipped adapter, the new held-goods policy, evaluator reads, new test bodies and evidence receipts. The Architect did NOT run the Windows environment, execute dotnet tests, call localhost, send live POST/SQL, or take screenshots. Test/deployment receipts below are worker-executed and were assessed against their actual test/source contents.
Acceptance is bounded to the issued remaining gates and direct affected-path regressions. It is not a new whole-module audit, user visual/functional acceptance, live PSP/Wallet/Inventory readiness, universal refund-balance certification or full Marketplace launch approval.

## Accepted gates

| Issued gate | Source verified | Meaningful proof reviewed | Verdict |
| --- | --- | --- | --- |
| B1 / A1–A3: a NEW approve/reject requires its reviewed decision version after the recorded replay branch | ResolveExistingDecision now refuses null and unequal expected versions with return.correction.version_stale inside the existing decision scope; an actual same recorded decision still answers first | R3_B1_a_reopened_case_refuses_an_unversioned_new_decision_then_accepts_the_reviewed_one and R3_B1_a_reopened_case_refuses_an_unversioned_new_rejection_before_any_row assert counts/status/frozen amount/allocation unchanged on refusal, current revision succeeds and replay adds no effect | CLOSED |
| B1 / A4: reachable adapter never substitutes a fresh revision for a decision | IReturnAdminOperations approve/reject forward the supplied reviewed decision/resolution values; the prior fresh-read fallback was removed from these decision methods | R3_B1_A4_the_reachable_admin_contract_never_invents_the_reviewed_revision resolves the shipped ReturnsModule registration, refuses the absent version, then accepts the actual reviewed version | CLOSED |
| B2: unallocated legacy rejected/cancelled received goods remain occupied | ReadOccupancyAsync reads released-state cases and their own receipt/resolution/custody evidence; ReturnHeldGoodsPolicy describes accepted consumption plus per-condition owed handback; unallocated quantities join the legacy occupancy input instead of disappearing | R3_B2_a_legacy_rejected_case_with_received_goods_keeps_its_occupancy_without_rows asserts zero remaining/no invented allocations; R3_B2_an_ambiguous_expired_and_live_origin_is_typed_blocked_not_guessed_free flags both slice origins and refuses Allocate | CLOSED |
| B2: incoherent physical evidence cannot release as zero held | DescribeRejectedHeldAsync rejects incoherent receiving before the decision/status write; ReleaseRejectedDeliveriesAsync consumes the same described held map. Conservative unprovable reads do not infer that no goods arrived | R3_B2_an_incoherent_receipt_blocks_the_rejection_without_releasing_its_live_row asserts typed evidence conflict, zero decision/revision changes, unchanged Requested status and live allocation sum | CLOSED |
| B2 retained mixed handback | Accepted consumption remains included with TOTAL outstanding custody, across conditions | Retained R2_a_handed_back_remainder_keeps_the_accepted_units_occupied asserts occupied2.5 / remaining0.5 after accepted2 + rejected-received1 - handback0.5 | RETAINED |
| C1: actual money/Inventory argument through the owning Directory | Test instantiates real ReturnDirectory/ReturnEligibilityEvaluator and invokes publish -> decision-only approve -> separate first start. Scripted recording ports are outside the live services | C1_the_real_first_refund_restocks_exactly_the_accepted_portion_and_replays_with_zero_effect asserts one durable case-derived attempt with stored amount/key/reference; one gateway call; actual Inventory argument EXACTLY2 for the owning reservation, persisted UsableRestockedQuantity2, rejected1 still owed/PendingDisposition, duplicate zero new effects | CLOSED |
| C2: new failed-child retry respects cross-case payment budget | Real RetryRefundAsync uses the existing shared reservation path; controlled sibling obligations are stored in the same task-owned in-memory database | C2_the_real_failed_child_retry_refuses_a_cross_case_budget_overrun_and_mints_nothing asserts 60+60>100 refusal/no child/no port calls; C2_an_independent_lawful_control_mints_exactly_one_deterministic_child_and_replays_it proves 40+60=100 succeeds once and repeats without a new child/effect; C2_a_missing_payment_authority_refuses_the_retry_with_zero_attempt_and_effect verifies missing authority refusal | CLOSED |
| D: historical posting source-membership cleanup | Current diff removes the named 277-R1 RESULT.bridge.txt and adds its architecture-evidence ignore. Original RESULT.md/other acceptance evidence unchanged | Worker receipt records index-only untracking, retained local working copy and check-ignore. Source removal/ignore independently confirmed from diff | CLOSED |

The new 4-fact effect suite uses recording SUCCESS ports rather than throw-if-called negative ports. This closes the specific earlier proof gap about the actual Inventory quantity and executed failed-child budget branch. Its EF in-memory behavior is NOT promoted to relational race/provider evidence.

## Validation and protected-state assessment
Worker receipts report:
- ReturnsDecisionCorrection279DirectoryTests: 21/21.
- ReturnsFinalClosure279R3Tests: 4/4.
- Host ReturnsModuleAmsc001: 34/34.
- backend Release solution build: zero errors; Host publish zero errors.
- disclosed extra whole Returns run187/187, outside the requested focused selection. This deviation is retained; the acceptance basis is the exact named focused assertions, not that broad total.
Reported Host5088 PID39776 DLL SHA25604CD98A718E4BCB9702AF12F54BA4872E6B11FEB6956604AF9A65E4055ED34C5, health/ready200 and store-alpha; frontend3000 PID9904 unchanged because no frontend production change.
Reviewed diff adds no migration/config/keys/provider integration and does not alter frontend production source. Normal ancestry has three commits ahead of the issued base and main parity was independently observed at44e2cc6. No rebase/amend/force is reported in R3.

No live business POST, demo creation, provider/stock/money effect, reset/backfill/restore or schema apply was authorized or reported. The GET receipts demonstrate the two named historical cases still Completed with one attempt and their old updatedAt. Counts or absence of other cases are NOT independently established by those two GETs; do not infer a full live dataset inventory from them.

## Retained limits — not additional implementation blockers for this closure
- Financial/Inventory tests run through the real Directory with controlled in-process ports, not real PSP/Wallet/Inventory services.
- No committed two-writer race was created. Existing PostgreSQL rollback-only lock/budget-query/atomic-approval facts retain their earlier limited scope.
- No active served customer/operator walkthrough occurred. Existing case links are historical read-only review; no fixture was manufactured to create an active flow.
- Security-Exposure-State stays CONTAINED_PENDING_REVIEWED_RETIREMENT. The key is out of current Git, but its historical exposure is not erased. No key was revoked/rotated/deleted by this task; a retirement decision still requires actual protected-payload impact review. This separate security decision does not reopen the accepted Returns increment.
- Prior PARTIAL reviews/results remain immutable historical records. Their SPECIFIC B1/B2 and effect-proof findings are resolved by this accepted R3; do not rewrite the old verdicts or infer blanket certification of every prior scope.

## Recovery / next work
Last Architect-accepted Returns closure: TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R3.
Current implementation reference: 44e2cc6b3ea168b0e93de9103b861149d0f21fe7 (code32b48c5b).
279 remaining audited closure items B1/B2/C1/C2/D: CLOSED within this envelope.
activeTask: NONE
automaticNextImplementationTask: NONE
Recovery-Next-Task: NONE
Next-Recommended-Implementation-Task: NONE
Await normal user review when an authorized active case is available; no automatic case creation, retest loop, follow-on implementation or key operation.
