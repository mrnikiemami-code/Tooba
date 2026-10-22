PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_TASK
Task-ID: TB-SHIPPING-ADMIN-ATOMIC-EDITOR-DOCUMENT-199-R1
Parent-Task: TB-SHIPPING-ADMIN-ATOMIC-EDITOR-DOCUMENT-199
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_EDITOR_POSTCOMMIT_AUTH_AND_COMPLETE_ATOMICITY_PROOF
Expected-Base-SHA: b0b3bb804228826a1144b9db86ee0c1362b372bf
Hard-Timebox-Minutes: 30
Stop-New-Implementation-And-Tests-At-Minute: 23
Automatic-Next-Task: NONE
END_TOOBA_WORKER_TASK

Goal / scope

Complete the specific TB199 proof omissions identified in its ARCHITECT-REVIEW.md. TESTS AND EVIDENCE ONLY. Keep the delivered atomic-editor implementation; no next feature, task200, UI or new production behavior. Read AGENTS, latest recovery, backend feature skill, TB199 review and current tests first. Do not replay accepted196/197/198 or broaden into audit/certification.

One focused completion, not another broad battery: add new-route auth and a catalog sentinel; strengthen the EXISTING editor storage facts using ONE structural snapshot helper; strengthen the existing static boundary comparator with precise negatives. Reuse current harness and fixture target, no new framework/package.

Fetch/verify HEAD==origin/main expected base. Safe fast-forward only; report unexplained source divergence. Preserve unrelated tracked/untracked/stashed work; no reset/rebase/stash/force-push, shared app/output/cache cleanup or Bridge dummy payload.

A. Actual NEW-route denials and post-commit sentinel

Extend ShippingAuthoringEditorDocumentEndpointTests with real shipped editor GET/PUT delegates, real ShippingAuthoringOwnerAuthorizer/body reader/MediatR handlers/directory. Only external platform/Contracts facts controlled, no mock own authorizer.

View-only admin can GET but malformed PUT is denied403 admin.authorization.denied BEFORE dispatch/body read. Measure stream ReadCalls==0, SentCount delta0, handler-arrival0, catalog/context reads0, unchanged draft.
SingleStore seller GET and malformed PUT return canonical404 owner_scope_unavailable BEFORE seller resolution, dispatch, body/catalog/context. Marketplace seller with category-only grant similarly403 owner_scope_denied before dispatch/body. Batch these closely related audience cases in a compact theory/fact.
Dedicated successful composite PUT with non-null selected type: scripted catalog supplies one valid active fact on FIRST lookup and throws a sentinel on ANY SECOND lookup. Make a real stored change and assert200, exactly1 catalog read, saved actual draft id/revision and active type in response, persisted revision exactly+1. This must run the NEW ReplaceShippingAuthoringEditorDocumentHandler, not197's handler or a fake directory.
A subsequent deliberate GET may hit the throwing dependency and fail; verify the already-committed tree/revision/time remain unchanged. Label this read failure separately from PUT success. Reset seed counters before PUT. Clear path zero lookup may reuse an existing executed fact if exact new-handler branch is covered; otherwise include one compact clear assertion.
Restore fixture permissions/cultures/state in finally. Keep exact root and child codes for shaped invalid requests; no weakening to status-only.
B. Strengthen EXISTING relational storage facts, no extra confidence runs

Use one immutable structural TreeSnapshot of EVERY authored root scalar and ALL own children:
root id/owner/binding/type/revision/created/updated; ordered group ids/order/currency; all area ids+kind/node+parent group; all rate ids/parent/three bounds/fees/duration; translation ids/language/name/description; group-title ids/language/text; schedule header/timezone/timestamps if any and seven days/remembered hours; settings values/timestamps if any. Read fresh AsNoTracking context; compare structural arrays/values, preserving null vs empty. Don't use record reference equality or counts as whole-tree equality.

Seed a COMPLETE nontrivial graph once per existing fact (distinct descriptions, rate amounts, type, schedule, both settings). Use canonical domain/directory setup, no copied rule engine. Exact existing method APIs remain untouched.

No-op / late invalid / retry:
Deliberately persist root updated_at well earlier than current time (test-owned DB only), re-read baseline and assert boundary is non-vacuous. Same nonempty document at current revision preserves EXACT root timestamp/revision and complete snapshot; stale identical409 before foreign lookup where tested. Include valid-first/bad-LAST child: same loaded aggregate stays value-identical, all tracker entries Unchanged and later SaveChanges0, fresh complete snapshot baseline, then a valid retry in SAME context succeeds once. Existing unknown group/row tests retained. Use the captured REAL other group's row id (currently foreignRowId is created but unused and the test sends Guid.NewGuid) in an appropriate rejection case; do not claim foreign identity proof from an unknown random id.

Existing race:
Preserve first-SavingChanges two-arrival bounded barrier, actual OriginalValues[Revision]==1 for both, exactly1 success and1 canonical409, root revision exactly2. Derive winner from typed outcomes, not stored text. Assert complete winning proposal against fresh snapshot including preserved schedule/type/unchanged languages, all new/retained rows and selections, no hybrid. Server-created ids come from actual winning result/proposal identity mapping, not guessed UUIDs. ACTUAL loser tracker must be clean BEFORE its later SaveChanges0; re-read loser and fresh contexts to equal the winning snapshot.

Existing failure/cancellation:
After FIRST save has completed, fail SECOND staged save with one named sentinel instance. Assert specific exception/identity (respect real EF wrapping if applicable), exactly2 attempted saves/first completed/second not completed; don't accept any Exception as the proof. Fresh full snapshot IMMEDIATELY AFTER this failure must equal baseline, before cancellation attempt.
Then repeat caller cancellation at second-save boundary with real caller CTS: IsCancellationRequested true and propagated OCE.CancellationToken equals caller token; fresh full snapshot baseline again, no staged residue; actual writing context clean and its later SaveChanges0. Retry at original revision in same writing context (remove/disable the one-shot interceptor) succeeds once with complete expected tree. Do not dispose that context before its retry.

No repeated races for confidence or per-capability multiplication. Existing metadata/registry tests are reusable only for their actual narrow scope; don't call them new-editor dynamic-registry execution.

C. Exact static transaction boundary proof (test code only)

Current ShippingTransactionBoundary.CheckAllowedBoundaryShape ONLY counts one begin; report/docs claim more. Strengthen it for THIS existing typed helper in THIS exact file, with narrowly extracted member bodies/call sites:

exactly one begin in SaveAuthoringReplacementAsync, staged save sites + commit + rollback path detected;
actual call-site enclosing-member universe EXACTLY {ReplaceRegionGroupsAsync, ReplaceEditorDocumentAsync}; no generic callback path;
executed synthetic negatives: unexpected third caller in SAME file; removed commit or rollback; removed required save; second begin; unexpected transaction in other file/member. Keep startup-migrate and operational money/capacity negatives; no filename skip/general allowlist or broad guard rewrite.
State the static limit: marker/member checks are not control-flow/data-flow enforcement; real relational tests supply runtime evidence. Don't claim the scanner proves runtime atomicity.
No production source modification or new permission/schema authority.
Allowed files and verification

Only Shipping.Tests/Authoring/ShippingAuthoringEditorDocumentEndpointTests.cs,
ShippingAuthoringEditorDocumentStorageTests.cs (one small local test-only snapshot helper allowed),
Shipping.Tests/Compatibility/ShippingTransactionBoundary.cs and narrowly affected OperationalQuoteCompatibilityTests.cs negative fixtures;
received task artifact, own evidence RESULT.md, APPEND-ONLY TB199 RESULT correction, one recovery worker pointer.

NO Domain/Application/Infrastructure/Contracts/Endpoints/DI/Host/frontend/schema/migration/designer/snapshot/manifest/skill/AGENTS production change. If a real defect surfaces, STOP and report exact failing behavior/regression; don't absorb a production repair into a test task or weaken assertion.

Compile Shipping.Tests+affected references only. Execute focused new editor endpoint/storage facts and exact compatibility negatives once green. No whole645 repeat, Host/Fulfillment/solution build/test, broad Architecture run or unrelated audit.
Commands <=120s, bounded setup <=8min. At most TWO bounded harness corrections; if exhausted report precise PARTIAL, don't confidence-loop.

Real PostgreSQL only existing parsed fail-closed task target127.0.0.1:55432/db shipping_tb190r1/role tb190r1; verify current_database/current_user before DDL, locally present postgres:17-alpine image, no download. Disposable task-owned container exact identity, cleanup in finally, removed and port55432 free before report. No shared5432/tooba_alpha/store-alpha/sharedapps3000/3419/5088/Redis/RabbitMQ or process restart. No credentials in output.

Evidence and timing

TB199 remains partial until review. Preserve its original645 report as history; append precise correction that only one omission was NOT accurate (new-route denials, complete snapshot/token/time and scanner scope were also missing). No history rewrite or certification promotion.

TB199's UTC22:5x and timebox overrun need honest bookkeeping: recover exact claim/finish/commands from available receipt/log evidence if present. Otherwise explicitly UNKNOWN_NOT_RECONSTRUCTABLE; never invent times, elapsed minutes or compliance. Disclose whether a whole-solution build was actually attempted rather than merely saying not used as proof. No unrelated investigation to reconstruct logs.

30min HARD from new claim; stop new test work at23; reserve7 for cleanup/diff/evidence/commit/push/fetch parity/result. Return precise executed vs reused counts and any failures/corrections; no unexecuted PASS.

Result BEGIN_TOOBA_WORKER_RESULT: task/parent/mode/base/code/evidenceSHA/parity; new-route permission/body zero-read facts; real new-handler single-catalog-lookup proof; complete snapshot/noop time/race/actual loser/failure/caller-token/samecontext retry results; static negatives/limits; production byte parity; actually run command filters; UTC/timebox/correction ledger; cleanup; Production-Functional-Acceptance:PENDING; Shared-Database-Data-Flag-Changes:NONE; Stop-State:STOP_FOR_ARCHITECT; automaticNextImplementationTask:NONE; Automatic-Next-Task:NONE.