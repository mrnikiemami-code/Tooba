# TB-279-R1 — Architect review: PARTIAL / NOT ACCEPTED
Date: 2026-11-18 (Asia/Tehran)
Reviewed HEAD: 3f8b670cd4a16db9ef8e1c4766704b53a075d364
Actual parent: fd0b24cc16e7f6dd55f3fa9770c3efc5b002dadc (the exact docs-only issuance commit).
Method: independent source and Git diff review at this SHA. Worker test, deployment, backup and database receipts are reported evidence; no Architect localhost execution, live POST, SQL or screenshot.
Do not replace the original worker RESULT, task envelope, receipt or earlier acceptance history.

## Retain the real improvements
The first and NEW failed-child retry now call the same reservation method, owning payment reads have moved outside its lock, and missing/mismatched payment facts refuse. Approval/rejection now persist their transition inside the locked decision scope. Actual ordinary Inventory settlement uses AcceptedUsable and separate usable/damaged completion fields. The customer modal uses the shared Dialog and v2 scoped envelopes with additional storage checks. These are useful changes; they do not close the findings below.

## R1-A — operator commands do not satisfy the new owning wire contract (blocking)
Source: admin-return-workspace.tsx:450–539,555–676,1808–1847; return-api.ts:1376–1384,1492–1535; ReturnAdminEndpoints.cs:143–192; ReturnCorrectionCommands.cs and ReturnDirectory.Resolutions.cs.
1. Start-refund freezes only {kind:"start_refund"}. The endpoint binds expectedDecisionVersion from QUERY, not this body; RequireReviewedDecisionVersion consequently refuses every NEW start from this surface. Retry sends only expectedFailedAttemptId and also omits the required decision version.
2. The resolution modal still sends acceptedQuantity/rejectedQuantity. The new command requires acceptedUsable/acceptedDamaged/rejectedReceivedUsable/rejectedReceivedDamaged/rejectedUnreceived, and the owning writer refuses their missing values. The operator cannot publish the reviewed plan from this UI.
3. Custody modal omits proofKind, while the endpoint parser refuses an omitted proof kind BEFORE the command. Its ceiling is still total rejected minus all handbacks, including never-received rejected quantities; it does not use the condition-specific owed quantity.
4. Frontend ReturnResolutionLine mapping still does not carry the new portion fields. The forms/display cannot accurately review them.
Required closure within the existing scope: align the REAL modal -> exact retained intent -> API transport -> owning route -> command -> Directory contracts, including the correct frozen versions. Exercise actual adapters and route parameter binding with controlled effects; a mock that accepts every POST cannot prove this.
Positive cases: first start after approval, reviewed failed retry, explicit mixed-condition plan, whole-rejection actual handback. Missing/stale versions and omitted portions/proof must still refuse.

## R1-B — held/accepted allocation capacity is still wrong (blocking)
Source: ReturnEligibilityEvaluator.cs:173–185,197–220; ReturnDirectory.Resolutions.cs:343–356; ReturnDirectory.Allocations.cs:86–143.
1. ReadOccupancyAsync RETURNS EMPTY when there is no Requested/Approved/RefundProcessing/RefundFailed/Completed case, BEFORE it reads allocation rows from rejected cases. A seller order with only a rejected case that still holds received goods therefore reports no occupancy, even when its HELD allocation rows remain.
2. RecordCustodyAsync trims active allocations to ONLY OwedCustody. For an approved mixed outcome this drops ACCEPTED quantities too. Example: received 3, accepted 2, rejected received 1; hand back 0.5 of the rejected portion => allocations shrink from 3 to 0.5, making 2.5 available although 2 accepted units must stay consumed. Remaining capacity is derived from allocations, so the coarse Returned display counter does not repair this.
3. A rejected legacy case with no rows can fail allocation in TrimActiveAllocationsToHeldAsync and silently return. Because rejected cases are absent from the legacy item set, its held goods can then disappear from occupancy.
Required: one quantity authority distinguishing accepted/consumed, unresolved held, and actually released portions. Read allocation/held facts even when there are zero active-status cases. Never drop accepted consumption on handback; unknown legacy held occupancy fails closed. Cover rejection-only seller, mixed accepted+partial handback and legacy no-row held goods through the actual evaluator/Directory.

## R1-C — reviewed resolution/version and unit boundaries remain incomplete (blocking)
Source: ApproveReturnCommand, StartReturnRefundCommand, RetryReturnRefundCommand; ReturnDirectory.cs:220–251; ReturnDirectory.Corrections.cs:184–220; ReturnDirectory.Resolutions.cs:111–165,239–247.
Approval still chooses reviewedResolution.Version from a fresh server read rather than a frozen client expected-resolution version. The commands do not contain ExpectedResolutionVersion. Two operators can therefore review different plans at the same decision version; an old approve command can approve the latest plan/amount it never reviewed. ResolveExistingDecision allows a NEW decision with expectedVersion absent; missing versions do not universally fail closed as claimed.
Resolution and custody writers check six-decimal representability but never check the OWN accepted line quantum. For a whole-piece item, accepted 0.5 + rejected-received 0.5 can pass the writer's total/condition checks and reach fractional money/stock. UI restrictions are not the owning boundary.
Required: carry the reviewed resolution version and require it at the new-decision seam; compare after lock. Require a reviewed decision version for a NEW decision, preserving exact replay. Apply the owning unit rule to all new resolution and custody portions; no guessed precision, no rounding. Facts must include plan changed between modal review and approve, missing version after reopen, and half a whole-piece item refused before any row/effect.

## R1-D — custody proof and legacy slice ambiguity still fail closed incompletely (blocking)
Source: ReturnCustodyProofKindParser; ReturnDirectory.Resolutions.cs:395–407; ReturnAllocationPolicy.Describe.
The parser and Directory ACCEPT carrier_delivered_back from client text with only a proof reference. No owning carrier delivery read verifies it. The closure matrix says this path is unavailable, but code accepts it. Until a real existing owner can verify delivered-back, refuse that kind; implement the deliberate actual in-person operator attestation already authorized.
Legacy ambiguity is flagged only when MORE THAN ONE slice is CURRENTLY in-window. An expired slice plus one live slice can still have ambiguous historical origin, yet the code guesses oldest-first. Current eligibility of a slice does not prove where an old return originated. Likewise reacquisition still blindly reactivates recorded rows after aggregate checks rather than proving those exact slice capacities.
Required: positive origin evidence across all plausible deliveries, including expired ones, and exact-slice reacquisition; typed ambiguity otherwise. No backfill, invented dates or new carrier subsystem.

## R1-E — customer intent fencing/compatibility is incomplete (blocking)
Source: return-request-modal.tsx:184–208,359–394; return-submission.ts:31–32, readReturnSubmission, recordChildOutcome.
1. The effect increments epoch on open/scope change but has NO cleanup. Unmounting the modal does not invalidate the old epoch. A delayed first answer can continue sending subsequent children and run stale callback/toast after close/unmount. The loop checks epoch only AFTER each answer, not before each send.
2. Prefix/version changed from tb278/v1 to tb279/v2. The reader checks ONLY the new key. A possibly-sent v1 act is left in storage but treated as absent, permitting a new key/body and duplicate creation. Preserving bytes without noticing them does not preserve their unresolved meaning. Legacy evidence must be detected and explicitly blocked/reviewed without adopting another viewer's act.
3. recordChildOutcome compares only submissionKey/childKey, not the full expected originating envelope/body. A replaced/tampered same-key envelope can receive an old callback's outcome. Apply a scoped exact expected-envelope compare before the update.
Required mounted negative controls: unmount/close during first of two children => no second send/no stale UI; existing v1 uncertainty => no new act; replace a body under the same act/child key before response => original answer cannot resolve/clear it. Keep controlled successful children and exact-body retries.

## R1-F — claimed proof does not support the closure matrix
172/172 is the retained whole Returns suite count. The changed 279 directory suite still has the original 11 facts; pure policy suite still has 13. These do not contain the matrix's claimed failed-A/B-reserved/A-retry budget refusal, missing/foreign authority, actual partial Inventory argument or newly required end-to-end wire regressions.
The three new relational facts prove a budget sum query, advisory-lock contention and atomic approval. They do not execute the NEW first/retry reservation path; health/migration cannot substitute. Preserve their valid limited proof, but correct matrix/test names and add the missing focused behavioral facts. Re-run only changed focused suites and relevant build/contracts, not another whole-project loop.
The recorded 44/44 customer/operator mounted total passed mocks which missed R1-A and unmount R1-E. Wire-shape assertions must reject a missing required version/portion/proof, not return success regardless of body.

## Repository incident — protected material was committed (blocking containment)
The actual diff adds:
- src/backend/Host/Tooba.Host/App_Data/dataprotection-keys/key-6db9ecd9-d991-4408-8189-8a8d839da3d5.xml
- 20 historical tb266r1/tb270r2/tb276/tb277r1/tb278/tb279 scratch scripts, including a seed script
- an unrelated historical 277-R1 RESULT.bridge.txt.
The XML was inspected WITHOUT printing/copying the value: it contains a plain masterKey/value, no encryptedSecret, and an unencrypted-storage warning. No key bytes appear in this review. This violates the explicit protected-key/untracked-work boundary and is absent from the worker deviation list.
Containment must be controlled: preserve required local files privately OUTSIDE tracked paths before untracking; remove the accidentally tracked key/scratch material from future source through a NORMAL commit and narrowly scoped ignore rules. Do not delete user scratch work or blindly delete an active runtime key. Git removal does not undo historical disclosure. Assess and retire/replace the exposed environment key through the owning ASP.NET Data Protection mechanism with a reviewed session/decryption impact; no arbitrary file deletion, secret output, source-history rewrite or unapproved destructive purge.
No claim is made here that a credential was used or an account compromised. Exposure itself is source-confirmed.

## Protocol/evidence discrepancies
Worker explicitly reports rebase, amend and force-with-lease despite their prohibition. Preserve this deviation; do not repeat it.
The resulting code commit's parent IS the issued fd0b24cc1 and main is at 3f8b670, so current ancestry is independently confirmed. Actual claim SHA remains the worker's unverified assertion, not the parent.
The diff from fd0b24cc1 does NOT change either TASK.md or issuance-receipt.json; the receipt still contains the Architect's 24,446-byte / 8913a285... SHA. The worker's statement that the alternative Bridge bytes replaced both persisted tasks/receipt is contradicted by final tracked state. Preserve the original issuance and explain the transport variant separately; never overwrite it to make a report match.
Only a Host.Tests build command is recorded in RECEIPTS where the result claims a solution build; record the exact command actually executed. Do not invent an additional run retroactively.

## Decision / current recovery
**PARTIAL / NOT ACCEPTED**. Retain code improvements and the applied additive migration; no Down/reapply, no new business fixture or reset of completed user cases.
279 and 278 remain PARTIAL. F1 shared-budget source seam improved; F2 accepted-usable physical seam improved; atomic decision transition improved. R1-A–E and containment are still blocking. Correct the overstated closure/evidence before declaring PASS.
No new task/envelope issued by this review; activeTask NONE, automaticNextImplementationTask NONE, STOP_FOR_ARCHITECT. Complete the existing consolidated goals with exact counterexample proofs when a repair envelope is issued; do not split every finding into a separate task.
