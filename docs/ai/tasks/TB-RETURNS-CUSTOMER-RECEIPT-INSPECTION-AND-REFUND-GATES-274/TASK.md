PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-RETURNS-CUSTOMER-RECEIPT-INSPECTION-AND-REFUND-GATES-274
Parent-Task: TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273-R2
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: ca2dbed34ba67251dea5b60fd048d1040eef02dd
Mode: CUSTOMER_DELIVERED_PHYSICAL_RECEIPT_INSPECTION_AND_LAWFUL_REFUND_GATES_BACKEND
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

INTENT / ACCEPTED CHECKPOINT
273-R2 ACCEPT within its three-gate source/evidence scope;273 backend recovery foundation closed through R1+R2.
Read273-R2/ARCHITECT-REVIEW.md and docs/planning/returns/RETURNS-AND-REFUNDS-COMPLETION-PLAN-272-276.md.
Prior PARTIAL reviews, broader-test deviations and original272 fixture/delete audit remain history.
ONLY274 is issued.275 (operator UI/mobile) and276 (connected acceptance) NOT ISSUED.
One worker/task; no delegation, screenshots, idle polling or automatic implementation loop.

BASE / ARCHITECTURE
Fetch main normally; expected base exists and is ancestor of HEAD/origin/main; record actual base/incoming delta.
Expected base is the Architect docs-only acceptance/issue checkpoint after ce471c71b1bf5f64f7e2d55ff482eba96fe34ca5.
Read current AGENTS/START-HERE/RECOVERY, .cursor/skills/tooba-backend-feature/SKILL.md and the relevant Returns/Order/Fulfillment/Inventory/Payment/Wallet Contracts/callers/DI/errors/tests.
This task EXPLICITLY authorizes the bounded additive Returns migration below; don't interpret an otherwise generic skill migration stop as requiring the user to approve it again. No other schema/cross-module redesign is authorized.
Preserve CQRS/Result/Contracts-only ownership, no foreign DbContext/SQL or module cycle. Unsafe overlap/divergence -> RECOVERY_CONFLICT; no reset/stash/rebase/amend/force.

SOURCE-CONFIRMED GAP
CustomerDelivered ReturnItem defaults Condition=Usable but carries no receiving/inspection evidence. ApproveAsync can start money and shared SettlePhysicalDispositionAsync can restock those default units.
273 recovery deliberately blocks ordinary physical recovery with receiving_required. Actual received quantities/conditions, receipt actor/time and decision reason must now become durable facts.
RejectAsync currently discards command.Reason; ReturnDecisionRecord stores kind/actor/time only.
Courier-origin ALREADY has Fulfillment-owned receipt+claim+conditions/closure: NEVER create another receipt for it.

A — RETURNS-OWNED CUSTOMER RECEIVE + INSPECT FACTS
Implement one coherent backend operation to explicitly record ACTUAL received quantities AND their inspected usable/damaged condition, on an EXISTING CustomerDelivered return case.
The operator deliberately submits inspection results; no default Usable, no inferred physical receipt from Requested/Approved/Completed/payment/tracking/clock.
Returns owns these customer RMA facts; Fulfillment keeps courier receipt ownership. Don't reuse foreign schema or build a second Fulfillment receipt.
Use additive Returns-owned receipt header + received/inspected lines (or equivalently cohesive owned entities) with:

server IIdGenerator ids; exact case/item/order-line/owner binding from owning stored case;
caller's reviewed occurrence/idempotency key, actor from authenticated route and server-recorded timestamp; distinguish recorded time from a claimed historical physical-arrival instant (no browser clock as authority);
immutable received quantity + EXPLICIT defined Usable or Damaged condition, no Unknown=>Usable fallback;
per-line pending/settled physical disposition facts, with owning Inventory result/identity where applicable.
Support partial physical receiving across multiple explicit occurrences without over-receiving: cumulative inspected quantity per ReturnItemId <= requested Quantity; receiving complete only when EVERY requested item is exactly covered.
A single requested item may legitimately contain BOTH usable and damaged units. Represent separate inspected portions; don't split/duplicate the requested return item to bypass272's one-line rule, label a mixed item wholly usable, or refund twice.
Receipt/inspection RECORDING performs0refund/Wallet/Inventory/stock/hold effects. Merely receiving goods does not approve/refund them or make them saleable.
Reject zero/negative/over-precision quantity with the existing exactly-representable six-digit rule; refuse missing/foreign item, undefined condition, duplicate (item,condition) row in one submitted occurrence and empty input. Different occurrences may contain further legitimate partial portions within the cumulative cap.
Authorization before history/replay: server authority + case/seller scope, including internal Contracts callers, not only browser disables.
Same case+key+same normalized item/quantity/condition set (order-independent) -> exact stored receipt, no new line/event. Changed key intent -> typed conflict; no overwrite or silent merge.
Serialize cumulative check+insert under a short Returns case-row lock/own transaction. No process-only lock; quantity re-read AFTER lock. Preserve safe caller-owned rollback staging for this purely local write operation; no external effect belongs inside it.
Rejected/cancelled and courier cases cannot receive a NEW ordinary receipt. An exact authorized recorded replay is history, no new effect. Do not mutate an already settled historical case or fabricate old receipt facts.
For preexisting financially progressed ordinary cases with no physical disposition, real newly recorded receiving may unblock remaining physical work; never re-run their money. Cases already carrying legacy restock/disposition facts must not gain new receipt-line restock identities that would double-restock: explicit legacy/inconsistent blocker, no backfill/guess.

B — ONE SHARED RECEIVING AUTHORITY GATES EVERY ORDINARY MONEY/STOCK PATH
Compute a pure stored receiving summary: requested/received/usable/damaged/remaining per requested item, receipt list, complete/incomplete and physical settlement; no page-count or first-item shortcuts.
Reuse it in read snapshots/Contracts, common approve/financial-execution gates and ReturnRecoveryPolicy. Read capabilities and action refusal must agree.
For ordinary cases:

no receipt or incomplete receiving => typed receiving/inspection-required or incomplete blocker; cannot start approval/refund, create approval decision/attempt or restock. Do not reuse delivery as return arrival.
full coherent receipt coverage is required BEFORE a NEW approval decision/attempt/money start. Admin, seller, Order operations adapter and direct Directory callers share this gate.
unknown/pending ordinary money recovery and definite-failure retry MUST NOT bypass the same physical-evidence rule. Preserve existing attempt key and fixed destination; do not mint a new key to bypass a blocker.
money already succeeded remains confirmed and is NEVER resent; once real receiving is complete, expose physical-only recovery using the ordinary receipt/inspection facts.
saleable restock is ONLY the actually inspected USABLE quantity and ONLY after confirmed money under existing policy. DAMAGED never invokes saleable Inventory. Use current Inventory.Contracts gateway, no no-op/silent success substitute.
each inspected portion has a stable durable restock identity reused after timeout/lost local save; save settlement only after owning Inventory confirms. Missing reservation/owning stock proof => explicit pending blocker, no mark-as-restocked invention.
partial receiving never triggers partial/guessed money. Full existing RefundAmount/Currency snapshot remains unchanged; no new fee/tax/discount/damage deduction or partial-refund algorithm.
No transaction/enlisted/ambient scope spans gateway/Wallet/Inventory calls; preserve273 guard and short durable outcome coordination.
Remove the default-true ordinary disposition bypass from the modified paths; every ordinary effect derives from receipt facts, not a caller boolean or ReturnItem's legacy default.
Courier-origin lifecycle, source claim, existing receipt/inspection, original per-item stock identities and closure stay unchanged. No second receive, no claim release by ordinary path.
Preserve old numeric enum/wire values; add receipt/summary/decision facts. Don't mislabel mixed inspected quantities using old single-condition fields. Treat historical settled rows as history, not permission to generate missing evidence.
A rejected case with recorded received goods must retain its receipt/quantities visibly; do not erase or auto-restock/return those goods or announce physical completion. If no lawful existing remaining disposition exists, expose an honest physical-resolution blocker; do not invent a return-to-customer shipping workflow in274.

C — PERSIST THE REJECTION REASON ON THE EXISTING WINNING DECISION
Add a nullable bounded reason to the existing Returns decision fact (old null stays unknown, no backfill).
New rejection requires a meaningful trimmed reason at reachable validation AND owning path; actor/time/reason persist atomically with the WINNING rejection decision, BEFORE source claim release or later effects.
Same rejected decision + same normalized reason replays the stored outcome; changed reason is typed conflict, no silent overwrite. Approval cannot be turned into rejection or vice versa. Legacy null reason must not be invented on replay.
Expose the stored decision reason distinctly from customer's request reason in owning snapshot/Contracts.
Use existing RecordReturnDecisionAsync first-writer authority/case lock; no parallel audit/decision table/Host authority.
When touching that helper, narrow expected unique-constraint arbitration to its real key identity and remove its broad catch/global tracker Clear in favor of scoped failure handling; don't swallow provider/cancellation faults or discard caller work.
Retain seller-scope checks for handler AND internal owning calls before recording decision/receipt. Do not require new customer consent or send notification.

D — BOUNDED ADDITIVE SCHEMA + SAFE APPLY (EXPLICIT AUTHORIZATION)
Authorized change: ONLY Returns customer receipt headers/inspection portions with local owning FKs/indexes/checks + nullable decision reason, and the necessary EF model/migration/snapshot metadata.
Use existing precision numeric(18,6), unique receipt occurrence identity, positive quantity/defined condition checks and suitable owning-case/item links; no foreign FK/table or status-value rewrite.
One cohesive additive migration, no destructive Down execution, no status/data backfill, seed or business row changes. No other module schema. Model constraints cannot alone enforce a cross-row sum; keep the serialized owner check.
BEFORE APPLY:
1 Inspect migration Up/Down/diff and all-module runner plan for verified canonical SingleStore store-alpha/tooba_alpha5432. Record exact migration ids and expected pending set. Unexpected other pending migration -> do NOT apply it.
2 Create an ACTUAL private PRE-APPLY pg_dump of canonical DB, verify exit success/nonzero size/readable archive listing, record path/size/full checksum/time BEFORE the apply. A plan JSON or old/post-apply dump is NOT a backup.
3 Keep private backup outside git/evidence uploads; no credentials/data dump in repository or final artifact.
4 ONLY if backup verified AND pending set matches this task's additive Returns migration, apply ONCE via existing Tooba.MigrationRunner for store-alpha.
5 Record applied migration/history and runner plan pending0; read-only verify tables/constraints/new facts empty. Do not restore/reapply/drop to prove it.
If backup or exact target/pending verification fails: stop BEFORE apply, retain reviewable code/migration and report concrete BLOCKED/PARTIAL deployment state. Do not ask the user to repeat authorization or skip pre-backup.
No manual DDL, parallel/disposable database/Docker/15432/15433. Existing tooba_messaging transport ensure is distinct and must be disclosed accurately.

E — FOCUSED PROOF ONCE, BACKEND ONLY
New named receipt/inspection/decision suite through owning handler/directory:

scope refusal, wrong origin/case/item, empty/undefined/duplicate condition inputs, positive-six-digit boundaries;
multiple partial receipts accumulate correctly, mixed usable+damaged covers one requested item, cumulative over-receive refused without write;
exact replay/reordered payload returns same receipt, changed quantity/condition/key intent conflict;
incomplete/no evidence blocks EVERY common ordinary approval/retry/unknown-recovery/disposition entry before money/new decision/attempt/stock;
complete inspected coverage allows ordinary approval with unchanged amount/currency/destination; Wallet/PSP routing and once-only keys preserved;
receiving alone has0external effects; usable restocks exact inspected amount; damaged0saleable calls; missing stock binding remains pending;
lost restock answer or settlement-save => same portion key/reload durable state; money already succeeded is never repeated by physical recovery;
rejection reason required/persisted/winning decision replay/conflict; receipt/history retained after rejection, no invented physical completion;
courier retained behavior unchanged (reuse targeted existing courier tests only as actually affected, not entire module);
-273 recovery invariants retained with updated ordinary positive fixtures containing REAL receipt/inspection evidence, not a bypass flag or fabricated default.
Small named relational facts on canonical DB ONLY after authorized schema is applied: each task-owned write is inside a caller-owned rollback transaction, target validated, visible FAIL/framework-SKIP on missing setup; assert0committed rows. Prove occurrence uniqueness, FKs/checks, serialized cumulative read/insert and decision reason persistence with real schema. Independent lock contention is allowed; committed two-writer race is NOT required or authorized.
No committed test fixtures/cleanup/delete, provider call or live refund. Controlled ports only for effects; state exactly which behavior is unit vs relational vs served proof.
After final code: named new/affected suites ONCE, backend build once. No whole Returns/Host/AMSC/Order/Fulfillment/Shipping/parity suites or detached baseline worktrees. Exact descriptor/route/validator pin checks only if legitimately changed; no broad filters or old migration-pin repair/baseline weakening.
FE unchanged: no FE test/build/restart/screenshot. Full operator UI is275, so backend receipt endpoint availability is NOT user click-through acceptance.
Publish/restart ONLY Host5088 once, gracefully stop locking process first, unchanged config, after safe schema apply and startup-effects inspection. GET health/readiness/provenance + real served artifact hash. No surprise migrations/seed/backfill at startup.
Current live Returns data reported empty; GET/validation/missing-case probes only. Do NOT create a demo case/order/hold to make a walkthrough.

HANDOFF / ENVIRONMENT
ONE canonical tooba_alpha127.0.0.1:5432 SingleStore/store-alpha; Host5088/FE3000. No edition/connection/ports/appsettings/keys/CapturedPreview change.
Aside from the explicitly authorized additive schema/history apply: no committed live business write, user order/draft/history/stock/hold change, approval/reject/refund/receive mutation, provider/PSP/Wallet/Inventory/waybill/notification/money operation, reset/reseed/backfill/cleanup/delete/restore.
Task verbatim:
docs/ai/tasks/TB-RETURNS-CUSTOMER-RECEIPT-INSPECTION-AND-REFUND-GATES-274/TASK.md
Evidence:
docs/architecture/evidence/TB-RETURNS-CUSTOMER-RECEIPT-INSPECTION-AND-REFUND-GATES-274/{TASK.md,RESULT.md,RECEIPTS.md,result-contract.txt,receiving-and-approval-proof.md,disposition-and-decision-proof.md,migration-and-preapply-backup-proof.md}
Document exact ownership/state/write ordering, normalized occurrence comparison, partial/mixed quantity authority, all effect entry gates, source/schema paths, precise tests, backup and migration receipts, served artifact and residual limits.
Update TOP AGENTS/START-HERE/RECOVERY/Returns plan consistently:273 closed throughR2 scope;274 worker result awaiting Architect;275/276 NOT ISSUED. Preserve all original results/reviews/deviations.
Commit+push normally with expected/actual base, code/evidence/final HEAD and fresh origin parity. Exact changed paths/checks and actual data/schema boundaries. No force/amend/reset/stash/rebase, scratch/credentials/backup artifacts, or recursive self-hash pin loop.
Post ONE real complete Bridge result after finalization, never placeholder/probe:
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-RETURNS-CUSTOMER-RECEIPT-INSPECTION-AND-REFUND-GATES-274
Parent-Task: TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273-R2
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA
Status: PASS | PARTIAL | BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA / Base-SHA / Code-SHA / Evidence-SHA / Final-HEAD / origin parity
Decision; A–E implementation/proof; exact migration/backup/served artifact; backend-only and proof limits.
Worker PASS != Architect ACCEPT; no user/provider/financial/full-Returns acceptance.
automaticNextImplementationTask: NONE
END_TOOBA_WORKER_RESULT

STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK