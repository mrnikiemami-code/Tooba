PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273-R2
Parent-Task: TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 02af2261032adf3aeb299fa9c369ee3b5154718d
Mode: CLOSE_THREE_REMAINING_RECOVERY_GATES_ONLY
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

DECISION / FIXED SCOPE
273-R1 is PARTIAL, NOT ACCEPTED. Read its ARCHITECT-REVIEW.md.
Retain the implemented recorded-approval/attempt binding, truthful absent PSP reference, marker-only recovery and scoped restoration after save failure. No redesign or replay of completed work.
ONLY this task is issued: THREE concrete gate omissions, their focused negatives and accurate recovery pointers.274–276 NOT ISSUED.
One worker/task, no delegation, screenshots, repeated proof loop or automatic next implementation.

RECOVERY
Fetch normally; expected base exists and is ancestor of HEAD/origin/main; record actual base/incoming delta.
Expected base is Architect docs-only review following a2ffbf60de3151037c6512461289d0c99bc7ccca.
Read TOP AGENTS/START-HERE/RECOVERY, .cursor/skills/tooba-backend-feature/SKILL.md, active review and touched Returns policy/directory/Contracts/tests.
Preserve CQRS/Result/Contracts-only/error/localization rules. No reset/stash/rebase/amend/force or unrelated dirty work loss.
Unsafe overlapping edits/divergence -> RECOVERY_CONFLICT with exact paths; no silent substitutions.

A — COURIER SOURCE COHERENCE BEFORE PENDING MONEY
SOURCE:
ReturnRecoveryDescription.cs calls ReadMoney at113 and returns PendingMoney at123. HasCoherentCourierSource is called only in Physical at294 AFTER money succeeded.
RecoverPendingRefundAsync assumes the description already checked source, then calls EnsureCourierOriginClosureAsync. Existing StockSettled markers can make that closure helper a no-op without validating every case/item receipt/source binding.

REPAIR:
Use the SAME existing courier-source/condition coherence rule in the common policy BEFORE offering ANY effectful courier recovery, including PendingMoney. Do not duplicate the predicate in command vs read.
Missing/empty case source, receipt, claim or occurrence key; item source/receipt mismatch; illegal recorded condition/disposition -> typed return.recovery.evidence_conflict, no RecoveryKind, no external call/write.
Do not just patch the physical branch. Valid unknown courier money still resumes the SAME bound attempt/key after lawful existing closure.
Preserve a positively bound succeeded money fact when blocking physical/source inconsistency; never resend it.
No new receipt, claim, allocation, approval, amount, destination or source authority.

FOCUSED NEGATIVES:
Stored approved courier case + bound Pending attempt with missing source/claim; wrong item receipt/parcel; illegal condition/disposition. For each: the read is Blocked with the SAME typed reason as RecoverAsync; gateway/Wallet/Inventory and owning closure ports record0calls, no attempt/key/state write. Include one valid pending courier positive through the real directory, not helper-only.

B — SETTLED REQUIRES THE PRIOR DURABLE CLOSURE EVIDENCE TOO
SOURCE:
Physical currently reports Settled from allItemsDisposed + ClosurePhase==RefundReleased. HasCoherentCourierSource never checks ObligationClosedAt/StockSettledAt. The strict marker writer requires those timestamps, but the early Settled return bypasses it.

REPAIR:
Align the read decision with the prior durable closure facts already required by the finalization writer. A courier RefundReleased marker with either ObligationClosedAt or StockSettledAt missing is INCONSISTENT -> typed evidence conflict, PhysicalComplete=false, no recovery effect/automatic backfill.
Keep coherent StockSettled + both timestamps + all items disposed as PendingDisposition: marker-only recovery succeeds with0gateway/Wallet/Inventory calls.
Keep coherent RefundReleased + required prior facts + all items disposed as Settled and zero-effect replay.
No new chronological SLA/window rule or lifecycle stage. Do not repair old data or infer missing timestamps.
FOCUSED NEGATIVES: missing each timestamp independently and both missing under RefundReleased; policy + physical command refuse, money stays positively confirmed,0effects. Retain marker-only and genuinely settled positives.

C — EXPLICIT PROVIDER ENLISTMENT WITHOUT AMBIENT SCOPE
SOURCE:
RefuseConstrainedTransaction checks only Database.CurrentTransaction and Transaction.Current. An explicitly provider-enlisted System.Transactions transaction can exist with both null.

REPAIR:
Include actual supported EF/provider enlisted-transaction state in the SAME guard. Inspect the installed EF/Npgsql APIs and use the owning context/transaction manager, not string/reflection guessing or a new transaction framework.
All existing call sites continue using that single guard. Never commit/rollback/dispose a caller transaction in production; don't detach/alter tracked caller state.
Keep InMemory controlled tests working without pretending it proves provider enlistment.

ONE REAL ROLLBACK-ONLY NEGATIVE:
Extend ReturnsRecovery273RelationalTests with an explicit enlistment case (e.g. caller-owned CommittableTransaction explicitly enlisted on the existing context, outside an ambient TransactionScope).
Assert BEFORE the action that EF CurrentTransaction==null and Transaction.Current==null while provider enlistment is actually present; then RecoverAsync must throw return.recovery.transaction_owned with0gateway/Wallet/Inventory calls and no recovery write, caller enlistment and tracked entities preserved. Caller rolls back; separate read verifies0committed task rows.
If the installed provider cannot demonstrate this exact shape, show API/provider evidence and report the proof limit; do not substitute the already-covered ambient case.
Validate canonical target before use, no secrets. No Commit/Complete of task facts, cleanup/delete/seed/restore or second database. No new connection for external work.

VALIDATE ONCE — EXACT SELECTION, NO BROAD RUNS
After final code change:
1 ReturnsRecovery273Tests (new A/B cases + retained suite) ONCE.
2 ONLY the NEW explicit-enlistment relational fact, with an exact method-name filter (existing caller/ambient facts unchanged; don't rerun them for routine reassurance).
3 Backend build ONCE.
No whole Returns project, Host project, broad ReturnsModuleAmsc filter, Order/Fulfillment/Shipping/parity suite, detached base comparison or stress run.
No new error codes expected; reuse evidence_conflict/transaction_owned, so no catalog/route/validator count churn or architecture-guard run is necessary.
The earlier broad-suite deviation remains disclosed; do not repeat to re-prove known failures.
Do NOT chase the declared untested FOR UPDATE outcome branch: this task adds no proof requirement there. No committed race/fixtures allowed.

If changed backend is safely deployable: inspect startup effects, gracefully stop locked Host before one publish, restart ONLY Host5088 with current config, GET health/readiness/provenance, record dll hash. No migration/seed/backfill. FE untouched: no build/test/restart/screenshots.
Live Returns table was empty. No eligible case/served recovery walkthrough is promised; do not create a demo case. Read-only error/validation probes are not real recovery proof.

BOUNDARIES
ONE canonical tooba_alpha127.0.0.1:5432 SingleStore/store-alpha; Host5088/FE3000.
No migration/DDL, connection/edition/ports/appsettings/keys/CapturedPreview change; no committed business test row, live approval/reject/refund/recovery mutation, provider/PSP/Wallet/Inventory/waybill/notification/money operation.
No reset/reseed/backfill/cleanup/delete/restore or user order/draft/history/hold/stock mutation.
No receipt/inspection stage, rejection reason, full queue/detail/mobile/UI work or provider integration.274–276 NOT ISSUED.
Preserve272-R1 acceptance, previous273/R1 reviews/results, original272 fixture/delete audit and shipping acceptance. No new architecture findings register or AMSC recertification.

HANDOFF
Task verbatim:
docs/ai/tasks/TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273-R2/TASK.md
Evidence:
docs/architecture/evidence/TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273-R2/{TASK.md,RESULT.md,RECEIPTS.md,result-contract.txt,three-gate-proof.md}
Three-gate proof maps exact policy/guard paths to named cases and actual observed outcomes. Distinguish controlled unit proof, new actual enlisted relational refusal, served build/errors and no live case.
Update TOP AGENTS/START-HERE/RECOVERY and Returns plan consistently:273 awaiting Architect repair review,274–276 NOT ISSUED; don't leave a contradictory current pointer. Never overwrite original worker reports/reviews.
Commit+push normally; expected/actual base, exact code/evidence/final HEAD and fresh origin parity, touched paths/actual checks/runtime/remaining limits. Preserve scratch; no force/amend/reset/stash/rebase/credentials or self-hash pin loop.
Complete the actual Bridge lifecycle with ONE canonical result, no placeholder/probe POST:
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273-R2
Parent-Task: TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA
Status: PASS | PARTIAL | BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA / Base-SHA / Code-SHA / Evidence-SHA / Final-HEAD / origin parity
Decision; THREE gates and exact proofs; validation/runtime/data boundaries and proof limits.
Worker PASS != Architect ACCEPT; backend-only, no user/provider/financial/full-Returns acceptance.
automaticNextImplementationTask: NONE
END_TOOBA_WORKER_RESULT

STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK
