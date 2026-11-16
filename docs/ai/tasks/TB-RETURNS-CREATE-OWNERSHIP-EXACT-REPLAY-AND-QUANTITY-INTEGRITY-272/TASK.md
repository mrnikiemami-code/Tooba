PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-RETURNS-CREATE-OWNERSHIP-EXACT-REPLAY-AND-QUANTITY-INTEGRITY-272
Parent-Task: TB-SHIPPING-EMPLOYEE-WORK-QUEUE-JOB-STAGES-AND-TIME-SCOPES-271-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 9d1a64e49102dd4e4429caad987beb7219f070ae
Mode: RETURNS_AUTHORIZED_EXACT_CREATE_REPLAY_AND_ATOMIC_QUANTITY_INTEGRITY
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

USER / ARCHITECT INTENT
Complete Returns and refunds sequentially, starting with a safe creation foundation.
Read docs/planning/returns/RETURNS-AND-REFUNDS-COMPLETION-PLAN-272-276.md.
ONLY THIS TASK IS ISSUED. 273–276 are roadmap candidates, no execution authority.
One worker, no delegation. No screenshots, screenshot requests, polling loop or automatic next implementation.
Do the coherent repair, not another analysis-only replacement result.

RECOVERY / BASE
Fetch main normally; verify Expected-Base-SHA exists and is ancestor of HEAD and origin/main. Record actual base and any incoming delta before coding. The expected base is the Architect's documentation-only plan/recovery commit, after source audit c492443179b86f3efc065121b3ae2a140105937d.
Inspect existing dirty/untracked changes; preserve unrelated work. Unsafe divergence or overlapping unexplained edits -> RECOVERY_CONFLICT with exact evidence, no reset/stash/rebase/amend/force.
Read current AGENTS/START-HERE/RECOVERY top pointers, .cursor/skills/tooba-backend-feature/SKILL.md, Returns architecture/contracts, relevant handlers/endpoints/validators/error resources and focused tests. Follow owning Returns CQRS, Result and Contracts-only rules.

SOURCE-CONFIRMED ROOT
src/backend/Modules/Returns/Tooba.Returns.Infrastructure/Directories/ReturnDirectory.cs:
CreateInternalAsync returns a global IdempotencyKey match before Order ownership validation and without comparing intent.
Each item checks its own quantity against the same remainingByLine value, so repeated OrderLineId can bypass a total.
src/backend/Modules/Returns/Tooba.Returns.Infrastructure/Evaluators/ReturnEligibilityEvaluator.cs:
GetAlreadyReturnedQuantitiesAsync includes Requested/Approved/RefundProcessing/Completed but omits RefundFailed.
No relational critical section is visible around ordinary eligibility read + create; a per-request quantity check alone cannot protect two different keys.

IMPLEMENT A — AUTHORIZED, EXACT REPLAY
Use current authenticated customer/admin gates on EVERY invocation, including replay. Customer must own the submitted order AND the stored request returned; cross-order/customer/seller/checkout/origin matches must never return another case. Admin follows existing permission/scope authority; do not replace this with an actor-id equality rule or add a dev bypass.
Load and validate immutable binding before mapping any replay snapshot. Existing customer/admin authorization must not be weakened by an internal requireOwner:false path.
Same key + same ordinary creation intent -> return the same stored case/items, no new row and no effects, even if the return window has since elapsed or remaining quantity is now occupied by that very case.
Same key + changed seller order, item ids/quantities, refund destination or persisted reason -> typed stable idempotency conflict, no payload leak and no mutation. Different item ordering is the same intent; use one deterministic comparison with explicit normalization matching persisted semantics. Origin mismatch (e.g. courier case key) refuses; never borrow courier source evidence.
Inspect unique-index arbitration: handle ONLY the actual idempotency constraint, re-read winner with authorization + exact comparison after a genuine concurrent same-key collision. Unexpected database errors must propagate through canonical handling, not be reclassified by a broad DbUpdateException catch.
No silent key replacement, no random key on retry, no "already exists" success for a changed request.

IMPLEMENT B — ONE ORDINARY REQUEST LINE PER ORDER LINE
Refuse duplicate OrderLineId (same or differing quantities) via endpoint-reachable validation and the owning/domain creation invariant; no silent merge or double price/refund.
Retain nonempty items, positive quantities, line membership and decimal/unit semantics.
Run the SAME rule for customer and admin ordinary creation; do not add a parallel validator/controller rule.

IMPLEMENT C — REFUND FAILURE DOES NOT RELEASE RETURN CAPACITY
RefundFailed still occupies its requested line quantities: money failure is not rejected/cancelled physical intent.
Requested/Approved/RefundProcessing/RefundFailed/Completed consume capacity; Rejected/Cancelled release it according to existing lifecycle.
Use the existing canonical quantity evaluator, not a second UI/helper counter. Preserve paid/delivered per-line slices and captured return-window behavior.

IMPLEMENT D — ATOMIC ORDINARY CREATION CHECK + INSERT
Serialize customer/admin ordinary create decisions for the same canonical seller order INSIDE Returns' relational transaction, covering fresh eligibility quantity read through persisted request/items. Both entry paths must use the same boundary; a process-local lock is insufficient.
Reuse the repository's owning PostgreSQL transaction/locking pattern if available. A transaction-scoped advisory lock with a deterministic namespaced key over the full order identity is permissible; no foreign Order/Fulfillment table lock or direct foreign-schema SQL, no schema migration.
Acquire/release correctly under failure/cancellation; re-read quantity after acquiring, not a stale pre-lock eligibility. No external money/stock/provider operation belongs inside this transaction.
Inspect existing callers' transaction ownership and nested transactions: participate safely or use one coherent owner, do not invent a distributed transaction.
The courier-origin path retains its Fulfillment receipt/claim ownership and existing ordering; do not force its undelivered physical source into ordinary delivered-line eligibility. Audit key collisions with it and refuse wrong-origin replay. Cross-origin shared physical-capacity redesign is outside this slice and must be reported if found.
No migration authorized. If existing relational architecture truly cannot support this bounded invariant without one, report the precise blocked seam before starting schema work; finish independent A–C safely and label remaining scope PARTIAL.

PROOF ONCE — TARGETED ONLY
Add focused cases at the real owning command/directory boundary, not tests of a new helper alone:

foreign actor/key and same key submitted for a different order cannot return snapshot; no unauthorized admin bypass;
exact replay after elapsed eligibility returns original id/items once; reordered items replay;
changed destination/reason/quantity/line/order/origin conflict with zero new case/items;
duplicate lines refused through reachable validator AND direct owning creation, quantity zero/overremaining still refused;
RefundFailed blocks a new over-capacity request, Rejected/Cancelled do not; partial returns preserve remaining quantities;
customer/admin use one atomic creation boundary; fresh check sees occupied capacity;
exact concurrent-key winner mapping compares full intent, changed intent conflicts; unrelated DB exceptions are not swallowed.
Relational evidence must use real PostgreSQL and Returns context on canonical tooba_alpha only, task-owned records in rolled-back transactions, no committed live fixtures or business SQL. Prove actual transaction/lock contention with independent contexts/connections and cancellation/release if feasible within that rollback boundary; explicitly state whether a real committed two-writer result was or was NOT proven. Sequential uniqueness tests are not a race proof. Do not create another DB, Docker, ports15432/15433, or silently skip configured relational tests.
Use existing fake/stub module ports in controlled tests so gateway/Wallet/Inventory calls remain zero for creation. Do not approve/reject/refund a live case to obtain proof.
Run affected focused Returns tests once after final changes and backend compile once. No whole Shipping/Order/Fulfillment/Composition/parity suites or frontend tests/build (FE unchanged).
If backend behavior is deployed: publish current Host once after graceful stop of the DLL-locking process, restart ONLY Host5088 with existing configuration, verify health/readiness and served provenance read-only. No FE rebuild/restart. Inspect startup effects before launch; refuse any unexpected migration/seed/backfill. Report whether runtime refreshed.
Read-only existing returns list/detail and /admin/returns may be inspected; empty live data is a declared limit, not permission to invent fixtures. No screenshot artifacts.

STRICT BOUNDARIES
No refund execution/retry/reconciliation redesign, customer receipt/inspection stage, rejection reason persistence, finance formulas or queue/detail/mobile rewrite in 272.
No migration/schema/DDL, live order/parcel/case/item/plan/stock/hold/decision/settings write, reset/reseed/backfill/cleanup/restore, provider/PSP/waybill/notification/money operation.
ONE tooba_alpha127.0.0.1:5432 SingleStore/store-alpha, Host5088, FE3000; preserve keys/CapturedPreview/edition/ports/connections and user shipping draft/history.
Preserve shipping271-R1/270-R4 acceptance, R2/R3 PARTIAL history and missing R2 pre-apply-backup disclosure. No reopen/retelling all-shipping claim.

HANDOFF / CANONICAL RESULT
Persist THIS envelope verbatim at:
docs/ai/tasks/TB-RETURNS-CREATE-OWNERSHIP-EXACT-REPLAY-AND-QUANTITY-INTEGRITY-272/TASK.md
Evidence:
docs/architecture/evidence/TB-RETURNS-CREATE-OWNERSHIP-EXACT-REPLAY-AND-QUANTITY-INTEGRITY-272/{TASK.md,RESULT.md,RECEIPTS.md,result-contract.txt,creation-integrity-proof.md}
Proof must map each A–D to exact source paths, actual test names/results, authorization ordering, compared intent fields/normalization, occupied statuses, transaction and constraint identity, rollback/concurrency limits.
Update TOP AGENTS/START-HERE/RECOVERY and the Returns plan pointer accurately: 272 worker outcome awaiting Architect, later tasks NOT ISSUED. Do not overwrite historical results or declare Architect/user acceptance.
Commit+push normally; record expected/actual base, code/evidence commits, final HEAD, fresh origin parity, changed paths, test/build/runtime evidence and explicit limits. No force/amend/reset/stash/rebase or credential artifacts.
Do not commit unrelated scratch files. A contract cannot truthfully embed its own hash; report final HEAD in the canonical Bridge result, referencing evidence commit without a recursive pin-commit loop.
Post ONE complete canonical result through the real Bridge task lifecycle:
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-RETURNS-CREATE-OWNERSHIP-EXACT-REPLAY-AND-QUANTITY-INTEGRITY-272
Parent-Task: TB-SHIPPING-EMPLOYEE-WORK-QUEUE-JOB-STAGES-AND-TIME-SCOPES-271-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA
Status: PASS | PARTIAL | BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA / Base-SHA / Code-SHA / Evidence-SHA / Final-HEAD / origin parity
Decision, source changes A–D, actual checks, runtime/data boundaries, unresolved limits, evidence paths.
No customer/provider/financial/full-Returns acceptance claim.
automaticNextImplementationTask: NONE
END_TOOBA_WORKER_RESULT
No placeholder result POSTs, no repeated submission or automatic next task.

STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK
