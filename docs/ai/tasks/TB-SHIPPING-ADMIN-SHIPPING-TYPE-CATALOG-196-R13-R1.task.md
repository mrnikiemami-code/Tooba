PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R13-R1
Parent-Task: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R13
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 7c19e63403d8ab66405b9762ba620bd76ede04e0
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_THREE_WINNER_AND_FULL_TREE_ASSERTION_REPAIR
Hard-Timebox: 20 minutes HARD incl setup/cleanup/commit/push/result. Stop new coding/tests at minute15; reserve5. Setup<=8min, commands<=120s. One harness correction/rerun max; second need=>STOP/PARTIAL.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Correct ONLY three R13 relational test facts and their narrow helpers so source proves the exact claims in R13 ARCHITECT-REVIEW. No production defect was found, no production change authorized. No fresh analysis wave or full35-test rerun.

RECOVERY
Fetch verify HEAD==origin/main==Expected-Base-SHA and safe tree before edits. Mismatch=>STOP exact state. No reset/stash/rebase/force-push; preserve unrelated dirty/untracked/stashes.
Read AGENTS/current recovery, R13 ARCHITECT-REVIEW and the three facts/helpers in ShippingTypePostgresStorageTests.cs plus relevant existing fixture. Reuse accepted phased fixture as dependency; don't redesign it or replay historical tasks.
Allowed edits: ShippingTypePostgresStorageTests.cs and narrowly necessary test-only helper additions (prefer same file; existing fixture helper only if essential), task/evidence docs and worker recovery pointer. ALL production/migration/Designer/snapshot/schema/config/Contracts/DI/frontend/Host/manifest/certification files byte-identical.

UNIQUE CREATE RACE — ACTUAL RESULT / COMPLETE WINNER
Current RunAsync(Func<Task>) discards CreateAsync's Guid. Replace this race's result collection with typed outcomes carrying participant identity, actual returned Guid on success and canonical error on failure.
Preserve the two-arrival pre-write barrier (both prechecks complete), two distinct generated root ids and same code.
Assert exactly one typed success/nonempty returned id and one exact canonical duplicate; NEW-context persisted root id == SUCCESS RESULT'S Guid, not simply nonempty id read from DB.
Compare full persisted root (code/sort/inactive/revision1/timestamps consistent with actual create) and exact ALL translations (LanguageId/name/description, exact count, no mixed/loser/orphan rows) against THAT successful participant's proposal. No conditional based on whatever text happens to be stored. Actual loser clean; later SaveChanges=0 and winner tree unchanged.
Keep all failures observable: raw provider exceptions must not masquerade as a business code.

REVISION RACE — COMPLETE PROPOSALS / COMPLETE WINNER
Use two DISTINCT multi-language proposals (at least2LanguageIds), different sort/isActive/text so hybrid root or translations are detectable.
At each actual participant's FIRST SavingChanges: capture actual tracked root OriginalValues[Revision]; BOTH1, exactly2arrivals, bounded20s, before either write. Preserve source of actual token evidence and one-winner/one-exact409 assertions.
Map participant to winner from actual result. NEW-context revision EXACTLY2; full root id/code/sort/isActive/CreatedAt/UpdatedAt and ordered complete translations equal winning proposal/unchanged identity facts. Exact translation count and identities; no extra loser-language row. Advance controlled clock if necessary to make timestamp assertions non-vacuous.
ACTUAL loser has no pending entries; later SaveChanges0; re-read via loser AND fresh context sees complete winner tree, not just one name/revision. No literal "winner" text as authority.

FAILURE + CALLER CANCELLATION — FULL PERSISTED SNAPSHOT
RawRow currently omits code/sort/isActive and every nonselected translation. Add a focused complete snapshot helper read through NEW context (AsNoTracking + Include) or exact own-schema SQL:
all persisted root columns (id/code/sort/isActive/revision/created/updated) and ALL translation rows ordered by stable identity/LanguageId (row id,parent id,LanguageId,name,description).
Compare structure/value equality, not reference equality of List members. Preserve NULL vs empty.
Seed >=2existing translations with distinct descriptions so same-count different-content cannot pass.
Before failure capture that COMPLETE tree. After injected SECOND-WRITE failure capture and assert complete equality; after REAL caller cancellation capture and assert complete equality BEFORE retry. No row/translation can change unnoticed.
For BOTH injected failure and cancellation explicitly assert first real write completed, second attempted and never completed (completed count1), record actual write operations, caller CTS actually cancelled and same token propagated. Preserve injected exception identity proof where applicable.
Clean retry advances revision once and persists EXACT intended final root+all text; explicit patch semantics preserve untouched languages. Exact final tree/count accordingly (do not assume one translation if initial has2).

FOCUSED EXECUTION
Fresh task-owned local-image postgres:17-alpine only;127.0.0.1:55436,role tb196,DBs fulfillment_tb196 + fulfillment_tb196_model; port free before setup. Same parsed fail-closed ValidateTarget and per-DB live current_database/user/container binding before DDL. No shared5432/tooba_alpha/store-alpha, no image/package download, no Host/MigrationRunner/shared service.
Build Fulfillment.Tests only +required references (isolated output if locks; never stop Host).
Run ONLY the three repaired facts with the existing phased collection fixture/real teardown as dependency. Explicit filter for their exact method names; no required one excluded, no skipped facts counted. Existing fixture necessarily reexecutes phases; that's not a fresh broad schema audit.
Reuse other R13 raw SQL/storage/default/constraint evidence by source identity, do not rerun the35-test battery/Host/solution/module/Architecture suites or repeated confidence races.
If real production behavior fails, STOP/BLOCKED exact failing assertion+reproducer; no runtime repair. After one harness correction allowance exhausted STOP/PARTIAL, no loop.

EVIDENCE / DELIVERY
Persist received task docs/ai/tasks/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R13-R1.task.md.
docs/architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R13-R1/RESULT.md: actual typed winner id vs persisted id; original tokens and participant winner; complete root+translation snapshots/case traces (synthetic only), both rollback full-tree assertions, second-write/cancellation proof, retry; exact3facts executed counts/failures/timebox; production byte-parity hashes and cleanup.
Append correction to R13 RESULT.md (do not rewrite history): prior create-result and full-tree claims exceeded source; these exact facts supersede them. Other accepted evidence remains unchanged.
Update recovery worker pointer only. No new business/decision/manifest promotion or full196 verdict by Worker.
Cleanup exact task-owned container/output only;55436free. Commit scoped files,push fast-forward,fetchverifyHEAD==origin/main,postcompleteResult,STOP_FOR_ARCHITECT.
No dummy/probeBridgepayload, no next task generation/execution, no Shipping assignment/UI/customer quote/activation/deployment. TB185paused/defaultOFF/PendingAllocation unchanged.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R13-R1
Parent-Task: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R13
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_THREE_WINNER_AND_FULL_TREE_ASSERTION_REPAIR
Bridge-Task-Row: ...
Status: PASS|PARTIAL|BLOCKED
Expected-Base-SHA: 7c19e63403d8ab66405b9762ba620bd76ede04e0
Base-SHA: ...
Code-SHA: ...
Evidence-SHA: ...
HEAD-Origin-Parity: ...
UniqueRace-ActualReturnedWinnerId-And-ExactTree: ...
RevisionRace-TwoOriginalTokens-And-CompleteWinnerLoserState: ...
FailureCancellation-CompleteSnapshots-SecondWrite-And-Retry: ...
FocusedThreeFacts-ExecutedCounts-And-CorrectionBudget: ...
ProductionMigrationMetadata-ByteParity: ...
Isolation-And-Cleanup: ...
Changed-Files: ...
Claim-UTC: ...
Result-UTC: ...
Elapsed-Minutes: ...
Hard-Deadline-Compliance: ...
Out-Of-Scope-Commands: NONE
TB196-Acceptance: PENDING_ARCHITECT_REVIEW
Production-Functional-Acceptance: PENDING
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
Automatic-Next-Task: NONE
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK