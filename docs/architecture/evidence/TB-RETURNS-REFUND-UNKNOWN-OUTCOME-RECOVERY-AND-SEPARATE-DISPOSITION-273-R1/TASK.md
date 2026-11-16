PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273-R1
Parent-Task: TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 1e4e7cb677c25f2746a0a5b4c870808015aa95fc
Mode: REPAIR_RECOVERY_EVIDENCE_CLOSURE_TRANSACTION_BOUNDARY_AND_TRUTHFUL_REFERENCE
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

ARCHITECT DECISION
273 = PARTIAL, NOT ACCEPTED. Read docs/architecture/evidence/TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273/ARCHITECT-REVIEW.md.
Retain its explicit same-attempt money intent, separate zero-money physical intent, typed unavailable result, backend CQRS and additive recovery facts.
Only THIS REPAIR is issued.274–276 NOT ISSUED. One worker/task, no delegation, screenshots, loops or self-issued next implementation.
272-R1 acceptance and original272 committed-fixture/delete deviation remain unchanged.

BASE / REQUIRED READING
Fetch normally; expected base exists and is ancestor of HEAD/origin/main; record actual base and incoming delta.
Expected base is Architect docs-only review commit following bf2dc514fdd68ab83b512aa78d5388c55975fa6f.
Read current AGENTS/START-HERE/RECOVERY, .cursor/skills/tooba-backend-feature/SKILL.md, Returns/Payment/Wallet relevant Contracts, source/tests/receipts and273 review.
Preserve CQRS/Result/Contracts-only/current errors/localization/DI. No unrelated cleanup/refactor or architecture recertification.
Unsafe overlapping edits/divergence -> RECOVERY_CONFLICT, no reset/stash/rebase/amend/force.

A — ONE FAIL-CLOSED STORED EVIDENCE RULE, INCLUDING THE LAST CLOSURE STEP
Root:
ReturnRecoveryPolicy labels a single Pending row bound without checking payment/amount/currency/key, whereas the command checks those later. Completed accepts any Succeeded row and physical recovery trusts FinancialComplete.
RecoverAsync only rejects an opposite decision WHEN a decision row exists; absent approval row currently passes and positive tests seed no ReturnDecisionRecord.
Settled is based on item dispositions alone; courier closure markers/source/receipt binding are ignored. A lost final RefundReleased save leaves all dispositions durable, after which Settled early-return prevents finalization forever.

Repair:

Extend/reuse one pure owning recovery policy with the positive durable decision/attempt/item/closure facts it needs. Read projection AND command must use the same complete facts, not separate permissive vs strict rules.
Effectful recovery requires a recorded APPROVE decision, not an invented decision from a caller/default. Absent/opposite/inconsistent decision -> typed blocker, no effect. Do not create a decision/approve/repair historical rows in recovery.
An eligible pending/succeeded attempt must bind to this case and its stored PaymentId, exact amount/currency and nonempty id/key; no ambiguity, no first/latest fallback. Exactly one coherent current financial outcome is needed to authorize continuation; preserve valid older FAILED history without confusing it with Pending.
Requested/Rejected/Cancelled with contradictory attempts are inconsistent facts, not executable recovery. Wrong/missing expected attempt cannot bypass validation simply because the case looks settled; recorded success/replay is authorized and bound before returning.
A completed status with a foreign/mismatched/ambiguous succeeded attempt must NEVER authorize Inventory work. Do not report unproven FinancialComplete. Conversely preserve a positively bound confirmed money fact while other work is blocked; do not resend money to repair inconsistency.
Courier recovery must validate nonempty coherent case source/claim/receipt links and line receipt/source links (including usable/damaged condition), plus durable obligation/stock-closure markers. No new foreign SQL or source owner. Existing owning Contracts remain authority.
Distinguish all item dispositions durable BUT final closure marker pending from FULLY SETTLED. That former state MUST remain recoverable with zero new refund/Wallet/restock; its explicit physical intent resumes only the remaining owning closure/final marker.
Report Settled only once required durable courier closure and item facts are complete. Do not make ordinary legacy cases invent receiving evidence; retain explicit receiving-required blocker where evidence is missing.
No new table/state engine/schema; additive internal snapshot evidence if required. Do not expose sensitive decision metadata needlessly or break existing numeric enums/wire members.

B — NO CALLER/AMBIENT TRANSACTION MAY SPAN AN EXTERNAL EFFECT
Root:
Recover/ExecuteRefund/physical path accept an already-open caller transaction; RecordRefundOutcomeAsync joins it without commit, then Inventory may run and a success snapshot may return before the caller commits. The no-transaction/durable-before-stock claim is false for that branch.

Decision:

For effectful recovery, refuse a caller-owned, enlisted or ambient transaction BEFORE gateway/Wallet/Inventory/closure effects or new recovery writes, with a typed stable reason. A validated effect-free stored completion may still be read.
Never commit/rollback/dispose the caller's transaction to escape it; preserve pending entities and values. Do not open a second foreign-module connection/transaction as a workaround.
Enforce the external-call precondition at the appropriate shared boundary so future direct callers of the modified execution helper cannot silently bypass it. Keep scope tied to recovery and changed shared execution, do not redesign all legacy decision paths.
Short owning outcome coordination must finish successfully before any subsequent physical call and before durable-success response. Local save/commit failure propagates as uncertain; the next request re-reads the same attempt.
No database transaction/row lock can span gateway, Wallet or Inventory. Prove this also for supplied caller transaction, not only a null CurrentTransaction probe in InMemory.
No distributed transaction, lease table, daemon, lock timeout policy or generic UoW framework.

C — NO FABRICATED PSP REFERENCE; DURABLE PHYSICAL FLAGS AFTER FAILURE
Root: result.ProviderReference ?? $"refund-{attempt.RefundAttemptId:N}" still invents a PSP reference forbidden by273.
GatewayRefundResult.ProviderReference is already nullable, as is the column.

Preserve the authoritative success boolean honestly even if the adapter gives null/blank reference. Persist a truthful absent reference (or an explicitly typed incomplete result if required by an existing owning Contract), not an invented PSP id and not a fabricated provider rejection. Do not repeat real money solely to invent a reference.
Align RefundAttempt/owning mapper semantics with nullable reference without migration or broad provider rewrite. Never treat absence of reference alone as proof money failed. Preserve actual nonempty references.
Wallet's own ledger/case identity is distinct and may retain its documented wallet-refund:{caseId} label; never present that as a PSP reference or substitute destinations.
Check interrupted physical disposition/final-marker persistence. A save failure must not leave in-memory SaleableRestock/RefundReleased flags that a same-scope retry can promote to false completion while DB facts remain pending. Restore/reload only affected owning state, no global ChangeTracker.Clear.
Use durable re-read of necessary item/closure facts before marking final completion. Stable per-item Inventory identity remains unchanged; damaged never saleable; final-marker-only recovery calls no external owner again unnecessarily.
No price/quantity/finance formula change, no ordinary receiving stage, no new PSP integration.

D — FOCUSED REGRESSION PROOF, ACTUAL COVERAGE LABELS
Extend the current ReturnsRecovery273Tests through the real handler/directory with:
1 missing/opposite approval and contradictory terminal states blocked; positive fixtures carry actual stored approval decisions;
2 pending and succeeded payment/amount/currency/key mismatches/ambiguity -> same read blocker + command zero effect; no financial authorization from an unrelated succeeded attempt;
3 all courier items disposed + StockSettled but missing final RefundReleased -> pending finalization; recovery finishes marker, ZERO gateway/Wallet/Inventory/new attempts; fully settled replay remains zero-effect;
4 failure at last item-save and final marker-save -> database stays honest; same-scope/fresh-scope retry does not trust unsaved flags or double-restock;
5 caller-owned/enlisted/ambient transaction -> effectful recovery refuses, no external call/write and caller transaction/tracker untouched;
6 null/blank PSP reference -> truthful result with no synthetic id; repeat returns stored outcome without fresh money;
7 retained same-key unknown/timeout/unconfigured/Wallet/physical-only tests still pass; no regression to new attempt/key or changed destination.
Do not claim failure/cancellation/authority cases are covered only because they share a switch row; add the named direct negative when it protects these repaired seams.

Use a small actual rollback-only PostgreSQL fact for the caller-transaction guard and real outcome lock/reload/application if needed to substantiate the SQL branch. Test the actual owning coordinator, not copied SQL/helper text. Controlled internal testing may exercise staged outcome inside a rollback transaction WITHOUT any external call; label that as staged rollback proof, not a committed command result.
All canonical relational test writes MUST remain inside rollback-only caller transactions; validate loopback5432/tooba_alpha and server identity; visible FAIL/framework-SKIP on unavailable setup. NO CommitAsync test fixtures, cleanup/delete/seed/restore, second DB/Docker/15432/15433 or two-writer/stress demand. Missing viable rollback proof is a disclosed limit, not permission to commit fixtures.
Do not claim SQL outcome branch exercised merely because Host is relational: the prior empty/missing-case probes never reached it.
Test controlled ports only, no real PSP/Wallet/Inventory/money/stock operation. Preserve local-save failure semantics and exact keys.

VALIDATION ONCE / SERVING
Run only the new/affected named273 recovery suite and a narrowly selected rollback-only fact if added, once after final change. Backend build once.
NO whole Returns/Host/AMSC/Order/Fulfillment/Shipping/parity suites, baseline worktree reruns or broad filters. Prior broad runs were outside273 scope; known failures need no reconfirmation.
If new descriptor requires catalog pins, select only those exact pin test names/classes and describe selection, no broad AMSC scan/repair.
FE untouched: no FE tests/build/restart or screenshots.
If backend safely refreshable: inspect startup effects, graceful stop before one publish, restart Host5088 with existing config, GET health/readiness/provenance, record new artifact fingerprint. No migrate/seed/backfill. No live actionable case exists; do not manufacture one.
Read-only existing endpoint probes may validate errors without business mutation; failed validation/missing-id POST is not a successful recovery or SQL coordination proof.

BOUNDARIES
ONE tooba_alpha127.0.0.1:5432 SingleStore/store-alpha, Host5088, FE3000. No connection/edition/port/appsettings/keys/CapturedPreview change.
No migration/DDL, committed business test row, live approval/reject/refund/recovery mutation, provider/PSP/Wallet/Inventory/waybill/notification/money call, user order/draft/history/stock/hold change, reset/reseed/backfill/cleanup/delete/restore.
No customer receipt/inspection/rejection-reason workflow, full queue/detail/mobile/UI work.274–276 NOT ISSUED.
Preserve272-R1 acceptance and all shipping/prior partial/audit history; never overwrite original273 worker evidence or retroactively authorize its broader runs.

HANDOFF
Persist this task VERBATIM:
docs/ai/tasks/TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273-R1/TASK.md
Evidence:
docs/architecture/evidence/TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273-R1/{TASK.md,RESULT.md,RECEIPTS.md,result-contract.txt,binding-and-finalization-proof.md,transaction-and-reference-proof.md}
Map A–D to exact source/tests and durable boundaries; distinguish controlled tests, actual relational lock/guard coverage, served errors and no valid live-case execution.
Update TOP AGENTS/START-HERE/RECOVERY/Returns plan coherently:273 PARTIAL until Architect reviews repair, worker result awaiting review,274–276 NOT ISSUED.
Commit+push normally, exact expected/actual base/code/evidence/final HEAD and fresh origin parity; exact paths/checks/runtime/remaining limits. Preserve unrelated scratch; no force/amend/reset/stash/rebase or credentials. Final HEAD in canonical result, no recursive self-hash pin loop.
One complete canonical Bridge result only, no placeholder/probe POST:
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273-R1
Parent-Task: TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA
Status: PASS | PARTIAL | BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA / Base-SHA / Code-SHA / Evidence-SHA / Final-HEAD / origin parity
Decision; A–D changes/proofs; validation/runtime/data boundaries and actual proof limits.
Worker PASS != Architect ACCEPT; backend-only, no user/provider/financial/full-Returns acceptance.
automaticNextImplementationTask: NONE
END_TOOBA_WORKER_RESULT

STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK