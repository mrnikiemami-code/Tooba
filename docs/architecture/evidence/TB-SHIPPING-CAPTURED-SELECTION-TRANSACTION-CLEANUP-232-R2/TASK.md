PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-CAPTURED-SELECTION-TRANSACTION-CLEANUP-232-R2
Parent-Task: TB-SHIPPING-CAPTURED-SELECTION-ATOMIC-WRITER-232-R1
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
Mode: ONE_TRANSACTION_LIFETIME_AND_PRIMARY_FAILURE_REPAIR
HARD20 / source10 / final10. One correction round. Stop/report incomplete if deadline reached.
Read232-R1/ARCHITECT-REVIEW.md. R1 group correlation, fresh preflight, coupon refusal and ordinary rollback repairs are ACCEPTED WITHIN SCOPE. Do NOT reopen them. Overall232 writer is NOT yet accepted: this task closes one transaction-lifetime/failure-cleanup blocker. No233.

ONE EXISTING ADAPTER REPAIR
StorefrontShippingDraftStore.ReplaceCapturedSelectionAsync and CreateCapturedSelectionAsync allocate IDbContextTransaction but never Dispose/DisposeAsync it. RestoreRejectedAttemptAsync and create catch also perform rollback before owned detachment with only InvalidOperationException caught; another rollback error aborts detachment, replaces the PRIMARY failure, and suppresses own cleanup. Recovery DB query can similarly mask the original failure. Fix this same adapter with no new framework or price/Domain/schema change.

- Dispose EVERY owned transaction exactly once on all paths: successful create/replace, conflict, begin failure, save failure, cancellation and rollback failure. Use awaited finally/owned scope and bounded helper(s) as needed. Do not dispose a caller transaction; reject unsupported pre-existing outer transaction before mutation instead of nesting/adopting it.
- Cleanup token None, caller token retained in ORIGINAL thrown cancellation. Never let rollback/disposal/recovery-query secondary exceptions replace the primary save/cancellation failure. Map only the existing genuine concurrency/exact uniqueness errors to conflict. Do not convert unexpected provider failures into success or conflict.
- Owned graph detachment MUST run even if rollback throws. Capture exact attempted/old root+groups+lines before detach fixups and detach ONLY those entries; unrelated graph/state unchanged. If DB recovery is available, dispose own completed transaction before re-reading the restored graph. If DB recovery is unavailable, leave own attempt detached (no fabricated restored graph, no pending write); preserve the primary error and record the recovery limitation in evidence. No global Clear/sweep, no retry, no silent caller flush.
- EF/Npgsql may clear Database.CurrentTransaction on successful Commit/Rollback: do not claim that absence proves Dispose, nor claim a completed transaction necessarily survives. Prove actual disposal using the available real-provider diagnostic/interceptor/wrapper seam, not source-text assertions or a production hook.
- No further changes to typed evidence, owner, group partition, planner/read, coupon policy, destination behavior, options or migrations.

FOCUSED FACTS ONLY
Existing isolated231 PostgreSQL target15433/order_tb231/tb231, identity beforeDDL, normal migrations once, one disposable existing image, removed and portclosed.
1 Successful create AND replacement release/dispose their transaction; same context can start/complete the next explicit operation without a retained own transaction/handle.
2 Actual cancellation AFTER first deletion/root save (cancel via test interceptor at stage boundary, not pre-cancelled token). DB old whole graph, no Added/Modified/Deleted attempt, original caller token identity, owned transaction disposed. Existing old-scalar/originals regression stays green.
3 Primary sentinel/save exception followed by injected rollback failure: same primary instance propagates, owned entries do not leak to later SaveChanges, unrelated graph untouched, transaction disposed. Distinguish a scripted rollback fault from a real database outage; do not overclaim availability. Also cover recovery query failure preserving primary if the chosen code still performs it.
Use test-only EF/ADO seams, no production hooks. New exact class plus existing232 storage and writer classes ONCE. Build affectedOrder projects only. No285/310/312/fullStorefront/Shipping/Host/solution/guard sweep. Sequential CAS facts stay described as such, not simultaneous race.

BOUNDS / FINISH
Backend adapter + narrow tests/docs only. No frontend/HMR, no Host/FE restart, no migration apply, no live API/business write, no browser. The user's VISIBLE Chrome review window is USER unsaved work: never enumerate/read/automate/reload/close. Host/FE/localpreviewONcount7/user cart/draft/rates/pointer/assignments unchanged.
Commit/ProjectPaymentDraft stay not_ready for captured evidence even flagsOFF; SelectionEnabled checked-in false. 231/232 migrations sharedNOTAPPLIED; staleEFsnapshot remains disclosed. No captured checkout/payment/history endpoint.
Task verbatim + RESULT under this task folder, append correction toR1RESULT (its cancellation fact is pre-cancelled, NOT proof at write boundary). Update all3TOPs. Exactpathscommit/push/fetch parity; no amend/force-with-lease/force-push/reset/stash/rebase. Leave unrelatedCRLFdrift alone.
Report exacttests/actualtime/secondaryfailurelimits. STOP_FOR_ARCHITECT; Automatic-Next-Task NONE. No usertrial/fullcheckout/VisualACCEPT claim. TB185paused/defaultOFF/PendingAllocation and other known recorded gaps retained.
