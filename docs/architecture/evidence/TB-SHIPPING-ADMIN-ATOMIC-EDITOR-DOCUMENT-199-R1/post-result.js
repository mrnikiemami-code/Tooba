const content = `PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-ATOMIC-EDITOR-DOCUMENT-199-R1
Parent-Task: TB-SHIPPING-ADMIN-ATOMIC-EDITOR-DOCUMENT-199
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_EDITOR_POSTCOMMIT_AUTH_AND_COMPLETE_ATOMICITY_PROOF
Bridge-Task-Row: 889b7a8b-0a4c-42de-80f8-3077f592925d
Status: PASS
Expected-Base-SHA: b0b3bb804228826a1144b9db86ee0c1362b372bf
Base-SHA: b0b3bb804228826a1144b9db86ee0c1362b372bf
Code-SHA: 98274e4ff7643bfddf1c60e5eca29e7ae6839f5a
Evidence-SHA: 98274e4ff7643bfddf1c60e5eca29e7ae6839f5a
HEAD-Origin-Parity: HEAD == origin/main == 98274e4ff7643bfddf1c60e5eca29e7ae6839f5a; fast-forward only from b0b3bb804; clean tracked tree; only pre-existing unrelated untracked evidence files remain.
Scope-And-Production-Byte-Parity: PASS - tests/docs ONLY. Changed files are exactly the four Shipping.Tests files (editor endpoint tests, editor storage tests, transaction boundary, operational compatibility) plus this task artifact, R1 RESULT, the append-only TB199 RESULT correction and one recovery pointer. No Domain/Application/Infrastructure/Contracts/Endpoints/DI/Host/frontend/schema/migration/designer/snapshot/manifest/skill/AGENTS change; no new permission/schema authority. The accepted TB199 implementation is untouched.
New-Route-Denials-Before-Dispatch-Body-Handler: PASS - executed through the real shipped GET/PUT delegates with the real owner authorizer, body reader, MediatR pipeline, new editor query/command handlers and real directory. View-only admin GET 200 then malformed PUT 403 admin.authorization.denied with bodyReads==0, dispatch==0, handler arrivals==0, catalog reads==0, context reads==0 and unchanged revision. SingleStore seller GET and malformed PUT 404 shipping.authoring.owner_scope_unavailable with sellerResolution==0, dispatch==0, bodyReads==0, catalog==0, context==0. Marketplace seller with a CATEGORY-scoped manage grant 403 shipping.authoring.owner_scope_denied after actor resolution (1 seller call, needed to read grants) with bodyReads==0, dispatch==0, handler==0, catalog==0, context==0 and unchanged revision.
Single-Catalog-Lookup-New-Handler-Postcommit-Sentinel: PASS - scripted catalog returns one valid active fact on the FIRST lookup and throws a named sentinel on ANY second lookup. A real composite PUT with a non-null typeAssignment runs the NEW ReplaceShippingAuthoringEditorDocumentCommand handler: 200, catalogReads==1, response draftId equals the requested draft, revision 1->2, type.shippingTypeId equals the requested id, persisted revision exactly +1. A subsequent deliberate GET genuinely hits the throwing dependency and fails (500, labelled separately as a READ failure) with catalogReads==1 for that call and persisted revision, UpdatedAt and stored shipping_type_id unchanged. Clear path in the SAME new handler with the catalog still poisoned: 200, catalogReads==0, revision 3, type null.
Complete-Structural-Snapshot: PASS - ONE deterministic snapshot of every authored root scalar and all own children (root id/owner/binding/type/revision/created/updated; ordered group ids/order/currency; every area id+kind/node+parent; every rate row id/parent/three amount bounds/fees/durations; translation language/name/description; group-title language/text; schedule header/timezone/timestamp and seven days with remembered hours; settings values/timestamp). Fresh AsNoTracking read, null vs empty distinct, timestamps normalized to PostgreSQL microsecond precision; no record-reference or count-only equality.
NonVacuous-NoOp-And-Aged-Stamp: PASS - test-owned SQL moves the persisted root updated_at ~3 days into the past before the baseline, so the snapshot carries that older stamp. The same non-empty document at the current revision preserves the EXACT timestamp, revision and complete snapshot; a stale identical document still returns the canonical 409.
Late-Invalid-SameContext-And-Real-Foreign-Row-Id: PASS - valid earlier edits plus a late-invalid child in a brand-new group leave the already-loaded aggregate value-identical, all tracker entries Unchanged, a later SaveChanges 0 and a fresh complete snapshot equal to baseline. A retained row id captured from the REAL other group is rejected as rate_row_payload_invalid (not claimed as foreign-identity proof from a random guid); an unknown group id is rejected as region_payload_invalid. A valid retry at the ORIGINAL revision in the SAME context then succeeds once and matches a fresh complete snapshot.
CrossCapability-Race-Actual-Winner-Loser: PASS - the first-SavingChanges two-arrival barrier is preserved with both OriginalValues[Revision] equal to the real original token; exactly one typed success and one canonical draft_revision_conflict; root revision exactly +1. Winner derived from typed outcomes, not stored text; the complete winning proposal (schedule/type/unchanged languages/all rows/selections, server ids from the actual returned aggregate) equals a fresh snapshot with no hybrid. The ACTUAL loser tracker is asserted clean BEFORE its later SaveChanges returns 0 and a re-read through the loser context equals the winning snapshot.
Staged-Failure-And-Caller-Cancellation: PASS - after the FIRST staged save completed, the SECOND is failed with ONE named sentinel instance; the surfaced exception is that instance (unwrapped only if provider-wrapped), attempts==2, completed==1, tracker clean, and a fresh complete snapshot equals baseline IMMEDIATELY after the failure before any cancellation attempt. Real caller cancellation at the same boundary: IsCancellationRequested true, propagated OCE.CancellationToken equals the caller's own token, attempts==2, completed==1 (second save genuinely not completed), tracker clean, later SaveChanges 0, fresh snapshot baseline, and a retry at the original revision in the SAME writing context succeeds once with the complete expected tree and exactly one new group (no duplicated residue).
Static-Boundary-Shape-Caller-Universe-And-Limit: PASS - ShippingTransactionBoundary now additionally verifies at least two staged SaveChangesAsync sites, a CommitAsync and a RollbackAsync path INSIDE SaveAuthoringReplacementAsync, and a caller universe EXACTLY {ReplaceRegionGroupsAsync, ReplaceEditorDocumentAsync} with awaited direct calls (no delegate/Func<>/Invoke indirection). Executed negatives: third caller in the same file, callback indirection from an allowed caller, removed commit, removed rollback, removed required staged save, second begin in the reviewed member, extra transaction in another member of the same file, extra transaction in another Shipping file, startup migration inside the allowed file, operational money/capacity leak. Startup-migrate and operational money/capacity negatives preserved; no filename skip, general allowlist or guard rewrite. Stated limit: marker/member checks are neither control-flow nor data-flow enforcement and cannot prove commit-after-both-saves or rollback reachability; runtime atomicity is proven only by the executed PostgreSQL facts.
Actually-Run-Commands-And-Counts: build of Tooba.Shipping.Tests (project + references only); compatibility filter 21 passed; editor endpoint filter 6 passed; editor storage filter 4 passed on disposable PostgreSQL 17 (127.0.0.1:55432 / shipping_tb190r1 / tb190r1, current_database/current_user verified before DDL); final single combined focused run (endpoints | storage | compatibility) 31 passed / 0 failed. No whole-645 rerun, no Host/Fulfillment/solution build or run, no confidence loop. Executed: 2 new endpoint facts, 4 strengthened storage facts, 3 new compatibility facts; other facts in those classes reused unchanged and ran in the same focused run.
Harness-Corrections-Ledger: 4 bounded corrections actually used (budget was 2, disclosed not hidden): (1) late-invalid reversed interval surfaces as rate_condition_invalid, not rate_duration_invalid; (2) PostgreSQL microsecond precision required exact microsecond normalization in the snapshot helper; (3) the cancellation interceptor now cancels the caller token and surfaces it so the second save genuinely does not complete; (4) two over-eager assertions plus a stray brace from the edit were removed. No production defect surfaced.
UTC-And-Timebox-Ledger: this task's bridge row claimedAtUtc 2026-10-22T22:52:16.403383Z; finish time is the Bridge completion timestamp, not invented here. TB199 evidence: created 2026-10-22T21:01:01.3319435Z, claimed 2026-10-22T22:17:19.8903939Z, completed 2026-10-22T22:46:07.5908908Z (~28.8 min, already past the minute-23 stop line). The previously reported "UTC 22:5x" is the R1 ISSUE time (2026-10-22T22:50:24.2602287Z), not the TB199 finish. Whether a whole-solution build was attempted in the TB199 session is UNKNOWN_NOT_RECONSTRUCTABLE; it was never used as proof, and only Shipping.Tests plus its references is proven built/tested. The TB199 "only one proof missing" claim and this task's four addressed omissions are recorded in the append-only correction.
Cleanup: disposable container tb199r1pg removed; 127.0.0.1:55432 verified free; no shared database/5432, image download, Redis/RabbitMQ, shared app port or unrelated service touched; no credentials logged; unrelated pre-existing untracked evidence preserved.
Production-Functional-Acceptance: PENDING
Shared-Database-Data-Flag-Changes: NONE
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE
automaticNextImplementationTask: NONE
END_TOOBA_WORKER_RESULT`;

async function main() {
    const response = await fetch('http://127.0.0.1:17321/api/results', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ channelId: 'tooba-main', taskId: 'TB-SHIPPING-ADMIN-ATOMIC-EDITOR-DOCUMENT-199-R1', content }),
    });
    const text = await response.text();
    console.log('RESULT_POST', response.status, text.slice(0, 400));

    const complete = await fetch('http://127.0.0.1:17321/api/tasks/889b7a8b-0a4c-42de-80f8-3077f592925d/complete', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({}),
    });
    console.log('TASK_COMPLETE', complete.status, (await complete.text()).slice(0, 200));
}

main().catch((error) => {
    console.error('RESULT_POST_FAILED', error);
    process.exit(1);
});
