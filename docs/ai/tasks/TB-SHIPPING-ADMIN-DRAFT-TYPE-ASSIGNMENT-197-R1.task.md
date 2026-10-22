PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-DRAFT-TYPE-ASSIGNMENT-197-R1
Parent-Task: TB-SHIPPING-ADMIN-DRAFT-TYPE-ASSIGNMENT-197
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: cc24c9c1b84e6283c0eed15f895e6b5143621395
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_POSTCOMMIT_LOOKUP_REPAIR_AND_ASSIGNMENT_FAILURE_DI_TRACE_PROOF
Hard-Timebox: 30 minutes HARD incl cleanup/commit/push/result. STOP NEW coding/tests at minute23; reserve7. Setup<=8min; commands<=120s. ONE harness-only correction/rerun maximum; another need=>PARTIAL/STOP. No confidence suite.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Fix ONE concrete response-composition defect in197 and finish narrowly identified proof gaps. No next product feature.
PUT currently validates a selected type, commits the root, then ComposeAsync performs a SECOND foreign lookup. An outage on the second call gives failure AFTER persisted success. Remove that ambiguity without cross-module transactions/cache.
R197 schema/owner/Contracts/race evidence is retained, but final acceptance awaits this repair;626passing is not proof of the missing cases.

RECOVERY / AUTHORITY
Fetch verify HEAD==origin/main==Expected-Base-SHA; inspect dirty/untracked work; mismatch=>STOP without edits. No reset/stash/rebase/force-push.
Read AGENTS/current recovery,197 ARCHITECT-REVIEW, rules27 and existing handlers/query composition/body reader/directory/adapter/test fixtures, applicable backend-feature/changed-surface architecture instructions.
Production edits ONLY:
Application/Authoring/Commands/ReplaceShippingAuthoringDraftTypeAssignmentCommand.cs
Application/Authoring/Queries/GetShippingAuthoringTypeQueries.cs
Minimal own Application helper in existing capability only if required, no new framework.
Tests/docs may change narrowly for the proof below. NO Domain/Contracts DTO wire/Endpoints/auth/Infrastructure adapter+DI/directory/schema/migration/Designer/snapshot/Host/frontend/project/resource/flag changes. If another production defect needs a wider file, STOP and report exact evidence.

A. RESPONSE REPAIR / DETERMINISTIC REGRESSION
For every non-null PUT, keep ONE successful exact-active catalog resolution BEFORE persistence (including active identical no-op). Retain that immutable request-local fact; after directory success construct response synchronously using ACTUAL saved root id/revision and that fact. No foreign call after SaveChanges/no-op and no artificial cancellation check/fallible dependency after commit.
Clear remains zero foreign calls and Unconfigured. Do not pre-read definitions before owner/revision checks; no-op/stale/active-only/presence/owner/permissions contracts unchanged. No cache across requests or type-state lock.
GET still resolves fresh facts and may truthfully show later Inactive/Missing: response on PUT is based on the validation-time fact, not an atomic guarantee against subsequent catalog edits. Update misleading comments accordingly.
Regression FIRST: scripted catalog returns valid active fact on first lookup, throws/cancels if a SECOND lookup occurs. Old handler should exhibit the defect; fixed handler must succeed with exactlyONE read, exactlyONE persisted revision advance, correct root/type/revision and no post-commit dependency. A subsequent fresh GET can fail with that dependency error without changing the persisted success. Record actual before/after behavior; no repeated confidence run.

B. GET BAD FACTS ARE NOT MISSING
Current GET ComposeAsync treats wrong-id/duplicate/nonempty inconsistent response as Missing. True empty returned list=>Missing preserving stored id. Exactlyone matching fact=>Active/Inactive.
Any NONEMPTY inconsistent response=>fail closed via EXISTING canonical shipping.authoring.configuration_unavailable semantics (503), no new error code/resource or DTO shape. Prefer Result failure composition rather than introducing ad-hoc exception mapping.
Test empty Missing vs wrong-id/duplicates503 separately; saved id/revision/text unchanged. Exceptions/caller cancellation still propagate to existing canonical boundary, not converted into missing/unconfigured/listempty.
No change to Fulfillment.Contracts or owner catalog definitions.

C. SAME-CONTEXT REJECTION AND NON-VACUOUS NO-OP
Use real handler/directory/context, controlled labelled catalog facts, known existing full authored graph:

scalar Guid.Empty (direct directory/domain defense), well-shaped unknown/inactive command selection: all reject BEFORE first tracked mutation. Capture complete baseline (root id/type/revision/UpdatedAt and existing groups/areas/rates/currency/text/titles/week/settings).
SAME DbContext after failure holds no non-Unchanged entries; later SaveChanges publishes0; fresh context graph equals baseline. Then valid assignment works in that same context. No invented "last batch entry": payload is scalar.
seed a meaningfully earlier persisted UpdatedAt or use existing lawful clock boundary (test-only), then same active selection/current revision and repeated clear/current revision preserve EXACT old time/revision. A mistaken restamp to current time must fail; no sleeps/no new production clock redesign.
Existing stale checks remain before catalog I/O even for identical/clear.

D. GENUINE DB WRITE FAILURE / CLEAN RETRY
Disposable PostgreSQL only, real assignment handler/directory and ShippingDbContext.
Inject a real provider-side failed assignment UPDATE using a TASK-OWNED named BEFORE UPDATE OF shipping_type_id trigger/function, LIMITED to ONE synthetic draft id. SQLSTATEP0001 or another explicit task error, no message parsing. This tiny test-only trigger DDL is authorized solely in the disposable DB and MUST NOT enter production migrations.
Real catalog validation succeeds; assignment reaches provider then fails. Assert no successful Result, canonical provider exception propagation (no new mapper), no root/type/revision/time/other-graph change on NEW-context readback. ACTUAL writing context cleared; later SaveChanges0. Drop exact owned trigger/function (finally cleanup), retry SAME intended assignment with original revision in SAME context succeeds once and preserves all other capabilities.
No explicit transaction/new production trigger or global failure policy. Trigger cleanup verified even if assertion fails. Never run this on shared/live DB.
Alternative test-only fault mechanism only if it proves a real server-aborted write, not merely a synthetic exception before any SQL; report exact boundary.

E. REAL MODULE DI + REAL PARENT/CORRELATION TRACE
Existing endpoint harness directly substitutes the APPLICATION catalog port; standalone adapter listener tests have NO parent. They do not prove module DI/parent continuity.
Invoke actual ShippingModule.AddServices plus canonical foundation registration. Controlled boundaries: module DbContext options may be replaced by InMemory for DI proof only, external IShippingTypeReader is a labelled Contracts fixture, platform context fixtures where needed. Do NOT hand-copy/register the adapter as the DI proof.
Assert exactlyone scoped IShippingTypeCatalogReader descriptor, resolves FulfillmentShippingTypeCatalogReader; same instance in one scope/different in another; mandatory canonical IModuleCallTracer resolves from foundation, no duplicated registration. Real command/query handler activation or a read through ISender if existing harness can support it without wider refactor.
Resolve adapter through REAL DI, then under actual parent Activity + actual CorrelationIdContext scope call ListActive/GetByIds:
one span per ACTUAL read; constant sourceShipping/targetFulfillment/operation/request_kind/correlation tags; ParentSpanId and TraceId match parent; single stop/disposal and Activity.Current restored to parent after each await.
Success, SAME sentinel dependency exception instance, and SAME caller token/OCE identity where fixture throws known instance; SetError TYPE only, no message/ids/codes/names/translations/tenant in exported tags/events. No-parent root test may remain as its own proof; it is not the parent test.
Empty ids/stale/foreign-owner/clear early branches cause zero foreign reads/spans as appropriate. Error branch and cancellation cannot become empty-success. No production tracer/adapter reimplementation.

EXECUTION BOUNDARIES
Fresh task-owned local postgres:17-alpine only127.0.0.1:55432,dbshipping_tb190r1,roletb190r1. Existing ShippingRelationalTestTarget must parse exact host/port/db/role BEFORE connection; verify current_database/current_user +container binding before DDL. Port free first; no credentials logged. Setup8minfail=>BLOCKED+cleanup.
No shared5432/tooba_alpha/store-alpha/Host/MigrationRunner/Redis/RabbitMQ/image/package download. Apply EXISTING Shipping migrations only to disposable target; no real Fulfillment DB, labelled Contracts facts only.
Build affected Shipping.Tests only. Run new/affected197 handler/endpoint/storage/DI-trace facts with explicit bounded filter; retained whole626 evidence reused, do NOT rerun full626/Fulfillment/Host/solution/Architecture suites.
Intentional pre-fix regression failure is proof, not harness correction; save it. ONE actual harness correction only. Another needed=>STOP/PARTIAL with exact issue; don't exceed to finish reporting green.
Any production fix beyond authorized Application composition=>STOP/BLOCKED, no absorbed refactor.
No cutoff/UI/quote/activation/deployment/item/offer compatibility/taxonomy admin/currency/language/owner rules/flags. TB185paused/defaultOFF/PendingAllocation unchanged. Preserve unrelated dirt/stashes/bin/obj/.next/cache/apps.

DELIVERY
Persist task verbatim docs/ai/tasks/TB-SHIPPING-ADMIN-DRAFT-TYPE-ASSIGNMENT-197-R1.task.md.
docs/architecture/evidence/TB-SHIPPING-ADMIN-DRAFT-TYPE-ASSIGNMENT-197-R1/RESULT.md + scripted lookup before/after, exact counts and persisted state, bad-fact503vsMissing, same-context snapshots/0save, real DB failed-write/trigger cleanup/retry, real DI/parent-trace facts, no-op timing boundary, exact test commands/counts/failures/UTC/timebox and unchanged schema/other production hashes.
Append correction to197 RESULT.md (post-commit lookup and which earlier proofs were limited); no historical rewrite or false preexisting attribution. Worker recovery pointer only.
Remove exact task-owned DB container/output/trigger helpers;55432free. Commit scoped changes,pushfastforward,fetchverifyHEAD==origin/main,postcompleteResult,STOP_FOR_ARCHITECT. No Bridgeprobe/dummyresult/automaticnexttask or certification promotion.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-DRAFT-TYPE-ASSIGNMENT-197-R1
Parent-Task: TB-SHIPPING-ADMIN-DRAFT-TYPE-ASSIGNMENT-197
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_POSTCOMMIT_LOOKUP_REPAIR_AND_ASSIGNMENT_FAILURE_DI_TRACE_PROOF
Bridge-Task-Row: ...
Status: PASS|PARTIAL|BLOCKED
Expected-Base-SHA: cc24c9c1b84e6283c0eed15f895e6b5143621395
Base-SHA: ...
Code-SHA: ...
Evidence-SHA: ...
HEAD-Origin-Parity: ...
PreFixRegression-And-OneLookupBeforeSave-NoPostCommitRead: ...
TrueMissing-Versus-InconsistentFacts503: ...
SameContextRejection-NoOpTime-And-ZeroLaterSave: ...
RealDbFailure-FullGraphUnchanged-TriggerCleanup-SameContextRetry: ...
RealModuleDI-ParentCorrelationTrace-ExceptionCancellationIdentity: ...
FocusedTests-ActualCounts-CorrectionBudget: ...
MigrationSchema-OtherProduction-ByteParity: ...
Changed-Files: ...
Isolation-And-Cleanup: ...
Claim-UTC: ...
Result-UTC: ...
Elapsed-Minutes: ...
Production-Functional-Acceptance: PENDING
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
Automatic-Next-Task: NONE
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK