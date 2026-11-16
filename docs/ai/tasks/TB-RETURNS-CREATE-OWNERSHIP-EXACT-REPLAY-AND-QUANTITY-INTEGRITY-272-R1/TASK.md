PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-RETURNS-CREATE-OWNERSHIP-EXACT-REPLAY-AND-QUANTITY-INTEGRITY-272-R1
Parent-Task: TB-RETURNS-CREATE-OWNERSHIP-EXACT-REPLAY-AND-QUANTITY-INTEGRITY-272
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 71f5170f7a534b8d54facbcac4475efaa11672b3
Mode: REPAIR_CREATION_PRECISION_BINDING_TRANSACTION_SCOPE_AND_ROLLBACK_ONLY_PROOF
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

ARCHITECT DECISION
272 = PARTIAL, NOT ACCEPTED. Read its ARCHITECT-REVIEW.md; preserve original RESULT/RECEIPTS/proof unchanged.
The ownership-first, duplicate refusal, RefundFailed capacity and ordinary per-order lock improvements remain.
ONLY THIS REPAIR IS ISSUED. 273–276 NOT ISSUED. One worker, no delegation, no screenshots or automatic next task.

BASE / REQUIRED READING
Fetch normally, verify expected base exists and is ancestor of HEAD/origin/main; record actual work base and incoming delta.
Expected base is the Architect docs-only review commit following b9df36a1dcdc63c8d5d5ec61761d0deb3da4bc3c.
Read TOP AGENTS/START-HERE/RECOVERY, .cursor/skills/tooba-backend-feature/SKILL.md, Returns Contracts/errors/persistence/creation and relevant callers/tests.
No unrelated dirty edits discarded, no stash/reset/rebase/amend/force. Unsafe overlap/divergence -> explicit RECOVERY_CONFLICT.
Finish this coherent repair; do not substitute an analysis-only task or broaden into refund/receipt/UI work.

A — EXACTLY REPRESENTABLE QUANTITY AND FULL IMMUTABLE ORDER BINDING
Root: Creation.cs Capture/Matches rounds quantity to6 decimals only for replay comparison, but quantity eligibility, lineSnapshots and aggregate RefundAmount use raw input; DB persists quantity numeric(18,6), money/price numeric(18,4).
Thus different raw quantities can compare identical, tiny positive quantity can persist zero, and initial tracked response can differ from a fresh read.
Decision: ordinary creation MUST REFUSE any quantity not exactly representable at six fractional decimals, at reachable validation AND the owning creation/domain boundary, BEFORE new write or successful replay. Do not silently round requested goods. Keep legitimate fractional quantities and trailing-zero numeric equality. Reuse an existing canonical precision rule if one exists.
No money formula/currency precision redesign, no migration; do not modify historical rows. Stored facts at six digits remain valid. New error, if needed, must be typed/catalogued/localized using current registry/resources; do not hardcode language count.
Once unrepresentable inputs are refused, exact comparison must not conceal changed accepted quantities. Test 1.0000001 vs1.0000002, 0.0000004, 0.0000006, valid0.000001, boundary remaining, identical1 vs1.000000 and reordered lines. Invalid precision creates no case/item/outbox and cannot receive an existing snapshot as success.
Extend OrdinaryCreationIntent's stored binding check with CheckoutId and SellerPartyId from OrderReturnContextSnapshot, besides existing sellerOrder/customer/origin. A row carrying wrong checkout/seller binding must refuse with no payload/write. Preserve actor authorization before reply.
Do not make the mutable operational refund destination into a new historical authority or add creation metadata/schema in this repair; note existing destination-override compatibility if relevant, without silently changing approval.

B — PRESERVE CALLER TRANSACTION/TRACKING; KEEP ORDINARY FACTORY INVARIANT UNCONDITIONAL
Root: collision catch calls _db.ChangeTracker.Clear(), including when joining a caller-owned transaction. It detaches unrelated caller changes. Creation also publicly exposes enforceOneLinePerOrderLine:false.
Replace global tracker clearing with cleanup limited to entries introduced by THIS failed creation, including any task-owned outbox additions if applicable. Preserve caller-preexisting tracked entities, original states/values, and transaction ownership. Do not clear pending caller work or re-save a failed duplicate.
Inspect real EF/Npgsql savepoint behavior: owning transaction collision recovery may roll back/dispose its own failed transaction before winner read; a caller transaction must remain caller-owned and usable (or propagate an honest failure if savepoint recovery is impossible). Do not commit/rollback the caller's transaction. Do not claim caller support based only on ownsTransaction:false.
No unnecessary transaction abstraction or cross-module transaction. Keep exact unique-constraint filtering; unrelated database faults/cancellation propagate canonically.
Ordinary public ReturnRequest.Create must always enforce one line per order line and new precision rule. Remove its public bypass flag. Courier-origin may use a private shared constructor/factory path retaining its legitimate (line,condition) identity; no courier workflow/claim/stock changes.
Regression tests: caller-preexisting tracked entry survives a failed create/collision path with same values/state; no leaked added duplicate request/items; outer transaction remains caller-controlled, rollback removes test facts; owned-transaction replay still works; wrong constraint is not transformed. Use actual owning boundary, not only helper assertions.

C — REPLACE THE UNSAFE RELATIONAL HARNESS, NO MORE COMMITTED TEST RECORDS
Root: ReturnsCreateIntegrity272RelationalTests commits fake cases on canonical DB then ExecuteDeleteAsync cleanup, contrary to the explicit rollback-only task. All5 facts silently return on missing TB272_CONNECTION; accepted target is unvalidated.
Do NOT rerun that class before repairing it. No committing fixture/seed/business records, no cleanup/deletes/restore, no "commit then remove" exception.
Refactor its runnable proof to ONLY task-owned writes inside transactions always rolled back in finally/async disposal. A protected rollback-only fixture must not allow the method under test to commit its transaction. Eliminate the committed race/cleanup path from executable tests; its old observation remains historical evidence only.
Validate configured target before opening/writing: canonical PostgreSQL tooba_alpha at loopback5432 only; verify actual database identity read-only. Reject arbitrary target, do not print secrets. Missing setup must explicitly FAIL or framework-SKIP as visible test outcome, never pass via return.
Keep useful rollback-safe real relational proof: same lock contention across independent connections with contender cancelled/rolled back before any commit; own transaction/savepoint behavior; exact index classification in rollback/savepoint; within-transaction persisted precision vs tracked answer and full binding checks.
Actual committed winner-vs-loser two-writer proof is NOT REQUIRED and NOT AUTHORIZED. If a case cannot be demonstrated under rollback-only boundaries, use focused controlled failure injection and declare the relational limit. Do not manufacture a new database/harness/daemon to chase full concurrency evidence.
No live existing case needed; current canonical Returns table was reported empty. Never create a demo order/customer/parcel to show a screen.

PROOF ONCE
Run focused ReturnsCreateIntegrity272Tests (extend it for A/B) and the REPAIRED selected relational class once after final changes. Before running, inspect its diff for commits/deletes/unchecked targets/silent passes. No whole Returns project, no unfiltered Host/architecture/Shipping/Order/Fulfillment suite, no detached baseline worktree re-run; the four pre-existing failures are already documented.
Small scoped canonical error-set guards may run only if changed descriptors require those exact guards; do not repair historical migration-list pins.
Backend build once. FE untouched: no FE tests/build/restart.
If deploying changed backend, inspect startup effects, gracefully stop the known DLL-locking Host BEFORE one publish, restart only Host5088 with unchanged config, read-only health/readiness + provenance. No migration/seed/backfill. If no safe refresh, report actual state without faking served code.
Tests never execute real refund/Wallet/Inventory/provider; controlled creation ports must record/assert zero external effects.
Report test counts honestly, rollback-only proof separately from prior unauthorized committed observation. No "zero writes" claim: rolled-back task writes and startup transport effects are distinct from no committed business writes.
No stress harness, screenshot, repeated proof loop or proof-driven live mutation.

ENVIRONMENT / NON-GOALS
ONE tooba_alpha127.0.0.1:5432 SingleStore/store-alpha; Host5088, FE3000. No second database/Docker/15432/15433, no connection/edition/keys/appsettings/CapturedPreview change.
No migration/DDL, committed live business records, cleanup/delete/reseed/reset/backfill/restore, payment/provider/waybill/notification/money operation or user order/draft/history mutation.
Do not reopen shipping acceptance or missing pre-apply-backup history. No new receiving stage, refund retry/reconciliation command, reason persistence, queue/detail/mobile redesign.
The prior deviation cannot be undone by another cleanup/restore or retroactive "approved" wording.

HANDOFF
TASK verbatim:
docs/ai/tasks/TB-RETURNS-CREATE-OWNERSHIP-EXACT-REPLAY-AND-QUANTITY-INTEGRITY-272-R1/TASK.md
Evidence:
docs/architecture/evidence/TB-RETURNS-CREATE-OWNERSHIP-EXACT-REPLAY-AND-QUANTITY-INTEGRITY-272-R1/{TASK.md,RESULT.md,RECEIPTS.md,result-contract.txt,precision-and-transaction-proof.md,rollback-only-proof.md}
Reference original272/ARCHITECT-REVIEW.md without rewriting worker history. Correct TOP AGENTS/START-HERE/RECOVERY and Returns plan pointer consistently; original272 still PARTIAL until Architect reviews repair, 273–276 NOT ISSUED.
Commit/push normally, final HEAD/origin parity after fresh fetch. Exact code/evidence SHAs and source paths, expected/actual base, actual tests, runtime and all residual limits. Preserve unrelated scratch files. No self-hash pin loop: final HEAD in canonical Bridge result.
Complete the real Bridge task lifecycle with ONE full result (never placeholder/probe POST):
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-RETURNS-CREATE-OWNERSHIP-EXACT-REPLAY-AND-QUANTITY-INTEGRITY-272-R1
Parent-Task: TB-RETURNS-CREATE-OWNERSHIP-EXACT-REPLAY-AND-QUANTITY-INTEGRITY-272
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA
Status: PASS | PARTIAL | BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA / Base-SHA / Code-SHA / Evidence-SHA / Final-HEAD / origin parity
Decision; A/B/C evidence; actual checks; rollback/transaction and runtime limits; prior deviation retained.
Worker PASS != Architect ACCEPT; no user/customer/provider/financial/full-Returns acceptance.
automaticNextImplementationTask: NONE
END_TOOBA_WORKER_RESULT

STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK