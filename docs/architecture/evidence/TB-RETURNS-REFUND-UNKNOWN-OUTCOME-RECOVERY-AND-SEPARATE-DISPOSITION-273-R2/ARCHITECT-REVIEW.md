# Architect review — TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273-R2
Date: 2026-11-16
Decision: ACCEPT WITHIN THE ISSUED THREE-GATE REPAIR SCOPE.
Reviewed source/evidence: ce471c71b1bf5f64f7e2d55ff482eba96fe34ca5
Code: 2481d40b98182bd17cf72614ff5c45173ef310ad
Parent273 backend recovery implementation scope is closed through R1+R2 repairs. Original273 and273-R1 PARTIAL reviews/results remain historical; this is not retroactive acceptance of their execution.

## Independent review
Compared issued02af22610 to final main (2 commits ahead, no divergence); read complete policy, enlisted guard and new relational fact, three-gate proof and relevant receipt/decision/inventory seams for next-task planning.
No Architect local tests, database/browser/runtime execution. Worker counts/health/provenance/empty-case receipts are attributed to worker evidence.

## Three issued gates accepted
A: PendingMoney now checks the SAME HasCoherentCourierSource predicate before offering a courier recovery; succeeded money remains preserved while Physical blocks incoherent source.
B: RefundReleased missing either prior closure timestamp is evidence_conflict, not Settled. Existing coherent marker-only state remains PendingDisposition.
C: the same guard adds owning relational connection EnlistedTransaction state, beyond CurrentTransaction and ambient Transaction.Current. The new fact asserts both latter properties null and explicit enlistment present before refusal,0external calls, ownership preserved and0committed task rows.

## Evidence / limits retained
Worker29/29 controlled recovery suite; one new exact-filter rollback relational fact1/1; build/publish0errors and Host5088 refreshed; FE untouched. Unit suite first failed because a new helper used Single on a two-item fixture, then corrected and rerun. That targeted correction is disclosed; it is not evidence of repeated broad validation.
Explicit-enlistment relational test intentionally has no persisted case at action time; it proves the guard runs before lookup/effects, not successful recovery.
Outcome FOR UPDATE/reload/application remains relationally unexercised. Raw DbConnection enlistment outside the owning context is not separately observable through this supported guard; no claim extends to that bypass.
No live return case/operator walkthrough, real PSP/Wallet/Inventory effect, committed concurrency or independent Architect runtime proof.
Earlier broad-suite deviations and original272 committed-fixture/delete audit remain recorded. No cleanup/restore follows from acceptance.

## Next
Issue only TB-RETURNS-CUSTOMER-RECEIPT-INSPECTION-AND-REFUND-GATES-274: backend customer-delivered receiving/inspection evidence, refund/stock gates and persisted rejection reason. UI/mobile275 and connected acceptance276 remain NOT ISSUED.
A valid ordinary receipt must replace inferred default Usable for new physical actions; courier receipt/claim authority remains unchanged. Additive schema is separately scoped by274 with actual pre-apply backup before repository MigrationRunner apply.
No user/provider/financial/full-Returns acceptance or module recertification.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
