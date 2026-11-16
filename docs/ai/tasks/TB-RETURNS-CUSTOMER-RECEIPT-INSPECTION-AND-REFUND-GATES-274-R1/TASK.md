PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-RETURNS-CUSTOMER-RECEIPT-INSPECTION-AND-REFUND-GATES-274-R1
Parent-Task: TB-RETURNS-CUSTOMER-RECEIPT-INSPECTION-AND-REFUND-GATES-274
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 145bb1a0747e6a97c607c06d22c374e34a001bc8
Mode: REPAIR_EXPLICIT_INSPECTION_RECEIPT_INTEGRITY_TRUTHFUL_STOCK_AND_SCOPED_DECISION_RECOVERY
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

DECISION / SCOPE
274 is PARTIAL, NOT ACCEPTED. Read274/ARCHITECT-REVIEW.md. Preserve its partial/mixed receipt ledger, coverage gate and persisted reason; repair only the concrete omissions below.
Only this task is issued.275/276 NOT ISSUED. Backend only; one worker/task, no delegation/screenshots/automatic next task or proof loop.
272-R1 and273-R2 acceptance remain bounded and preserved. Keep all previous result/audit history, including274 proof/code discrepancy and overbroad validation/pin maintenance.

RECOVERY
Fetch normally. Expected base is Architect docs-only review following7e7c2ee3d97506408112432775abeeb56715c3fd; verify it exists and is ancestor of HEAD/origin/main, record actual base and incoming delta.
Read TOP AGENTS/START-HERE/RECOVERY, .cursor/skills/tooba-backend-feature/SKILL.md and actual274 implementation/proofs/Contracts/callers.
Preserve existing module/CQRS/Result/typed-error/localization/DI rules. No unrelated work loss, force/amend/reset/stash/rebase.
Unsafe overlap/divergence -> RECOVERY_CONFLICT. Do not convert a precise source repair into another design-only result.

A — INSPECTION MUST BE EXPLICIT AT EVERY REACHABLE ENTRY
Source:
AdminReturnReceiptLine.Condition defaults "usable"; ReturnItemConditionParser.Parse(null/blank) succeeds Usable; CustomerReceiptLine has optional Usable default.
These contradict274's explicit receiving/inspection requirement.

Fix:

missing/null/blank/unknown inspection token is typed receipt.line_invalid (or existing shape code), BEFORE case lookup/write; no omission/default/fallback into usable.
remove optional Usable defaults from the new receipt wire/typed command/Contracts construction. A caller must deliberately supply the defined condition. Keep required numeric enum values and explicitly provided usable/damaged tokens stable.
update controlled fixtures to state their intended inspection, not weaken the rule.
no new operator receipt record may be inferred from default ReturnItem.Condition.

Prove exact missing/null/blank/unknown wire/parser cases and explicit two valid tokens, plus direct owning construction. Include an endpoint-contract assertion if the repository harness permits: authorized receipt body omitting condition refuses before dispatch/directory read; do not POST a live valid case to test this.

B — STORED RECEIVING EVIDENCE AND OWNERSHIP MUST FAIL CLOSED
Source:
ReturnReceivingSummary treats every non-Damaged enum as Usable and does not reject negative/zero/undefined stored quantities/conditions. It can sum malformed rows into complete usable coverage. RecordReceiptAsync does not refuse an already-incoherent cumulative summary before adding lines.
Applied schema has no positive/condition CHECK; independent single-id FKs do not bind receipt-line ReturnRequestId to BOTH header and requested item's owning case.

Fix:

the ONE shared summary rejects nonpositive/unrepresentable/undefined stored receipt facts, unknown/duplicate requested identity or invalid requested quantity, foreign item and over-coverage. Never subtract invalid quantities or classify undefined inspection as usable.
join/validate the receipt header/item owning binding before a line can count toward the case; no borrowed receipt. Every read/action uses the same coherent summary.
before a NEW receipt insert, incoherent existing evidence blocks with typed evidence conflict and0new rows.
owning seller scope is validated BEFORE decision/receipt writes or replay: Approve/Reject must honor their supplied SellerPartyId inside the Directory, not only a handler that internal adapters may bypass. For receipt command/adapter use existing authenticated authority and server-derived expected owner/case scope; do not trust browser owner, or build a parallel role/permission system.
authorized admin behavior remains valid; foreign seller/internal expected-scope calls cannot record/approve/reject another case.

NEW ADDITIVE MIGRATION AUTHORIZED:
Add quantity>0 and stored condition IN ('Usable','Damaged') CHECKs on customer_return_receipt_lines, plus local composite ownership links tying (receiptId,caseId) to receipt header and (returnItemId,caseId) to requested item (appropriate local alternate keys/indexes as needed).
No cross-module FK, trigger, new lifecycle/table redesign, data repair/backfill or status rewrite. Keep cumulative upper bound in the serialized owning code (not a false cross-row CHECK claim).
Update EF model/snapshot/designer consistently. DO NOT edit/reapply274's already-applied20261116122318 migration.
If actual existing data violates new constraints, report exact non-sensitive count/binding and stop before apply; no delete/rewrite to make it pass.
Proof: shared-summary malformed facts block; real rollback-only PostgreSQL rejects nonpositive/undefined rows and crossed header/case/item links, while normal partial/mixed receipts/replay remain valid.

C — NEVER MARK STOCK COMPLETE WITHOUT OWNING INVENTORY CONFIRMATION
Source:
SettlePhysicalDispositionAsync wraps Inventory call in if ReservationId exists, then marks SaleableRestock unconditionally. A fully inspected usable line with no reservation can be reported settled without any stock confirmation.

Fix:

inspected Usable>0 + missing/empty owning stock binding -> explicit typed pending blocker, Disposition stays None,0Inventory calls, money unchanged, no new attempt.
same blocker is visible in recovery/read facts; don't advertise executable physical recovery that just invents completion. Do not guess/reserve/create a binding.
after a valid Inventory confirmation only, persist settlement. Retain stable existing per-item key and reload-on-save-failure semantics.
mixed goods: use the immutable complete receiving summary's Usable amount, damaged portion never sent to saleable Inventory. Keep separate usable/damaged read quantities; old single-condition/disposition fields must not announce that the WHOLE mixed requested amount is saleable.
Existing per-item restock identity may stay if the complete inspected usable total is immutable before ANY restock and proof shows no later receipt/change can alter it; no new key/stock rewrite just for granularity.
rejected/cancelled case with recorded received goods has an honest remaining physical-resolution blocker (no recovery kind, no fake completion), while its receipt history remains visible. No automatic restock/refund/ship-back/new shipping workflow.

Prove through actual Directory: missing/null/empty reservation + received usable quantity -> no false SaleableRestock/Settled; money remains confirmed, no new attempt/provider call. Valid usable/mixed/damaged, lost restock answer/local save and same-key retry remain correct. Rejected receipt case keeps quantities and unresolved physical blocker.

D — DECISION COLLISION RECOVERY MUST MATCH ITS CLAIMED PROOF
Source:
ReturnDirectory.Decisions.cs STILL calls ChangeTracker.Clear in collision catch, despite worker report/proof claiming removal. It only checks constraint name and returns the winner without normal reason-intent comparison; caller savepoint recovery is absent.

Fix:

classify ONLY23505 on the ACTUAL decision PK + owning table/schema; unrelated faults propagate.
one winner comparison is reused on initial stored read AND collision winner read (decision kind/reason semantics). Same normalized reason replays; changed reason conflicts without mutation; don't overwrite legacy null.
use scoped tracking/savepoint-safe recovery preserving caller entities/state/values and caller transaction ownership; never global Clear or rollback/commit caller work. Reuse272's bounded pattern if lawful, no generic transaction framework.
detach only failed decision additions/interceptor-owned artifacts, not unrelated caller entries. No raw catch-all "another decision won".
scope refusal runs before recording the durable decision.
Do NOT reopen the separate legacy concurrent approval attempt algorithm or full financial retry redesign in this task.

Prove real owning collision branch with controlled exact error injection (label honestly), unrelated dirty entity preserved, same-vs-changed winner reason, wrong table/schema/constraint propagate; small rollback-only real savepoint/constraint fact if needed, never committed fixtures/races.
New evidence must explicitly correct the original proof discrepancy without overwriting original274 reports.

E — BACKUP / SAFE CONSTRAINT APPLY / NARROW PROOF
274 migration remains applied. Original reported pre-backup path:
D:\temp\tb274-backup\tooba_alpha-pre-tb274.dump,2966890B,SHA256 ADCE728BD0D5732C09F5EECDC219287859CB7FE56173F9F5BD07A10BDC64CD16.
Receipts show hash/size/apply summary but not readable-archive check/complete command chronology. Verify that archive NOW without restoring and label current verification time honestly; do not manufacture retroactive pre-apply evidence or upload the dump.

Before NEW migration:
1 verify canonical SingleStore/store-alpha/tooba_alpha5432 and all-module pending plan; only this task's new Returns constraints pending;
2 read-only data compatibility/preflight;
3 take a NEW actual private pg_dump; verify successful exit, nonzero size, readable pg_restore listing, full checksum and timestamp BEFORE apply. Evidence must show ordered backup/verification/runner apply receipts.
4 use existing Tooba.MigrationRunner once for store-alpha only after backup/plan/data match; no manual DDL, other migration, restore, Down or reapply;
5 verify actual constraint names/definitions and pending0; no business data changes.
Backup/target/pending/data mismatch -> stop BEFORE apply, preserve reviewable code/migration and report BLOCKED/PARTIAL without requesting repeated authorization.
No second DB/Docker/15432/15433, committed test row, seed/backfill/cleanup/delete/reset/restore. Existing running canonical container may be used by repository tools; do not create another environment.

VALIDATION:
Only new/affected274 named unit suite and directly affected recovery cases ONCE after final code, plus named NEW/affected rollback-only274 relational facts after safe schema apply; backend build once.
Do NOT run whole Returns/Host/AMSC/273 relational/project/parity suites or detached baseline comparisons. Do not repeat old unchanged tests to seek a green total.
Exact descriptor/validator pins only IF changed by this repair, with fully-qualified exact selection. No stale baseline/migration-pin cleanup or AMSC recertification.
Do not undo old test pins/harness merely to recreate known failures, and do not treat their274 maintenance as financial/module certification.
Use controlled idempotent ports for effects. All relational writes caller-owned and rolled back, canonical target validated, missing setup visible FAIL/framework-SKIP,0committed rows. No live case needed.
If a proof fails due to an actual code/test defect, repair it and disclose the necessary rerun; don't run unrelated validation.
FE untouched: no build/test/restart/screenshots.
Host-only safe refresh after applied compatible schema: startup-effects inspection, graceful stop before one publish, same config, GET health/readiness/provenance + dll hash. No automatic schema/seed/backfill on start.
Do not create demo case/order/hold to obtain UI/HTTP proof; missing-id/validation responses are not real receiving/refund proof.

HANDOFF / STRICT BOUNDARIES
ONE canonical tooba_alpha127.0.0.1:5432 SingleStore/store-alpha, Host5088/FE3000; no keys/appsettings/edition/connection/ports/CapturedPreview change.
Aside from authorized additive constraint schema/history: no committed live business write, provider/PSP/Wallet/Inventory/waybill/notification/money operation or user order/draft/history/stock/hold mutation.
No full queue/detail/mobile/UI, new physical-resolution lifecycle, provider integration or financial formula change.275/276 NOT ISSUED.
Task VERBATIM:
docs/ai/tasks/TB-RETURNS-CUSTOMER-RECEIPT-INSPECTION-AND-REFUND-GATES-274-R1/TASK.md
Evidence:
docs/architecture/evidence/TB-RETURNS-CUSTOMER-RECEIPT-INSPECTION-AND-REFUND-GATES-274-R1/{TASK.md,RESULT.md,RECEIPTS.md,result-contract.txt,receipt-and-stock-integrity-proof.md,decision-collision-proof.md,migration-and-backup-proof.md}
Map A–E to actual code/gates/tests, correct original proof discrepancy and explicitly separate controlled/relational/served/untested facts.
Update TOP AGENTS/START-HERE/RECOVERY/Returns plan consistently (all4, not only some):274 awaiting Architect repair review,275/276 NOT ISSUED. Preserve273-R2/272-R1/shipping acceptance and original audit/results/reviews.
Commit+push normally; exact expected/actual base/code/evidence/final HEAD/fresh origin parity, files/checks/schema/backup/runtime/remaining limits. No force/amend/reset/stash/rebase, scratch/credentials/private backup artifacts or self-hash loop.
ONE complete canonical Bridge result, no placeholder/probe POST:
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-RETURNS-CUSTOMER-RECEIPT-INSPECTION-AND-REFUND-GATES-274-R1
Parent-Task: TB-RETURNS-CUSTOMER-RECEIPT-INSPECTION-AND-REFUND-GATES-274
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA
Status: PASS | PARTIAL | BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA / Base-SHA / Code-SHA / Evidence-SHA / Final-HEAD / origin parity
Decision; A–E changes/proof; migration+ordered pre-backup; served state; backend-only and proof limits.
Worker PASS != Architect ACCEPT; no user/provider/financial/full-Returns acceptance.
automaticNextImplementationTask: NONE
END_TOOBA_WORKER_RESULT

STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK