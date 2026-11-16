PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273
Parent-Task: TB-RETURNS-CREATE-OWNERSHIP-EXACT-REPLAY-AND-QUANTITY-INTEGRITY-272-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: fcb2fe1cd2325b9378e85e55554bacadcef01b42
Mode: SAME_DURABLE_REFUND_ATTEMPT_RECOVERY_AND_SEPARATE_PHYSICAL_DISPOSITION_BACKEND
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

INTENT / ACCEPTED CHECKPOINT
272-R1 ACCEPT within A–C source/evidence repair scope, parent272 creation implementation closed through repair.
Read docs/architecture/evidence/TB-RETURNS-CREATE-OWNERSHIP-EXACT-REPLAY-AND-QUANTITY-INTEGRITY-272-R1/ARCHITECT-REVIEW.md and the Returns completion plan.
Original272 committed-fixture/delete deviation remains historical and must never be repeated.
ONLY273 IS ISSUED. 274–276 NOT ISSUED. One worker/task; no delegation, screenshots, polling/implementation loop.
This is a complete BACKEND recovery increment over existing approved attempts, with callable admin CQRS and truthful read facts. Full operator UI/mobile remains275; do not claim UI recovery is delivered by a backend-only result.

BASE / GOVERNANCE
Fetch main normally; expected base must exist and be ancestor of HEAD/origin/main; record actual base/incoming delta.
Expected base is Architect docs-only acceptance commit after b5d9b9aecdcc13909099c9117887861e2bf0ae46.
Read current AGENTS/START-HERE/RECOVERY, .cursor/skills/tooba-backend-feature/SKILL.md, Returns/Payment/Wallet/Inventory relevant Contracts, existing Returns admin authorizer/endpoints/handlers/DI/error resources and focused tests.
Preserve module ownership/CQRS/Result/typed errors/Contracts-only. No foreign Application/Infrastructure/Domain reference or foreign SQL. No changes to272 creation/precision/lock protocol unless a directly necessary compatibility fix is proved.
Unsafe overlapping dirty changes/divergence -> RECOVERY_CONFLICT; no force/amend/reset/stash/rebase.

SOURCE-CONFIRMED ROOT
ReturnDirectory.cs:

ExecuteRefundAsync retains a Pending attempt after a transport/unknown exception; status remains RefundProcessing.
Only courier-origin Approve/resume can resume the recorded attempt. RetryRefundAsync normally accepts only RefundFailed and mints another attempt.
Successful money is persisted as Completed before TrySettlePhysicalDispositionAsync; a stock failure is logged and leaves Disposition=None.
Disposition-only retry is special-cased only for completed courier-origin cases, and is not expressed as a distinct recovery intent/read fact.
AdminReturnQueueFilters/Order projection expose retry only for RefundFailed; no authoritative unknown/disposition recovery description exists.
Payment.Contracts/Returns/PaymentReturnContracts.cs: IPaymentRefundGateway.RefundAsync explicitly carries the idempotency key; GatewayRefundResult only definite success/failure. FakePaymentRefundGateway is sandbox, Production FailClosedPaymentRefundGateway throws payment.refund.gateway.unconfigured.
CustomerDelivered has no explicit receipt/inspection record yet. Do not treat approval/default Usable as physical receiving evidence.

A — ONE AUTHORITATIVE RECOVERY DESCRIPTION
Create a cohesive Returns-owned read policy over STORED case/decision/attempt/item/closure facts, reused by the recovery command and additive admin snapshot/read projection. Distinguish:
1 no approved financial attempt / no recovery;
2 unknown/pending money with exactly ONE bound Pending attempt;
3 definite financial failure (existing deliberate retry path, never mislabeled unknown);
4 money confirmed with unfinished physical disposition;
5 fully settled / recorded completion;
6 inconsistent/ambiguous binding or missing owning physical evidence -> typed blocker, no guessed action.
Expose explicit phase, exact attempt identity (where applicable), recovery kind/capability and stable blocker/reason, with financial completion separate from physical completion. Preserve existing numeric enum/wire values; additive fields/Contracts only, no duplicate lifecycle/table/state framework.
Completed money must not imply all-stock-settled. Reads never contact the provider, retry money, restock, or create a durable case/attempt.
Keep existing UI action codes working; do not publish a new executable menu verb to a frontend that cannot handle it. New additive recovery facts and a real backend endpoint are the deliverable; frontend consumption is275.
Do not rewrite queue counts/filter/mobile/product summary in this increment.

B — EXPLICIT ADMIN SAME-ATTEMPT RECOVERY COMMAND
Add a real Returns-owned MediatR command+validator+handler and admin endpoint using the existing admin authority and ApiResponseFactory. Reuse admin route family; add the smallest Contracts adapter method needed for module consumers. No parallel Order financial authority or duplicate endpoints with divergent policy.
Server resolves actor/permission/scope on EVERY call, including recorded completion. Client never supplies authoritative payment/amount/currency/customer/destination.
Request binds return id + explicit recovery kind + expected STORED attempt id when money-related. A stale/wrong/missing/ambiguous identity refuses before an effect; no latest/first arbitrary fallback.
Pending-money recovery:

requires an already recorded approval and exact bound pending attempt (case/payment/amount/currency coherent);
uses its EXISTING idempotency key/payment/amount/currency and case's fixed refund destination. No new attempt/key, no fresh approval, no destination override/Wallet substitution, no amount recomputation.
allowed recovery uses the existing idempotent owning gateway/Wallet Contracts. First inspect semantics; if a supported adapter cannot safely repeat the exact attempt, return explicit unavailable/unknown instead of sending a fresh refund.
a repeated same intent after outcome persisted returns that durable outcome; terminal failure is not automatically retried under a new key.
missing Payment adapter/explicit payment.refund.gateway.unconfigured is a truthful unavailable blocker with pending intent retained, not a successful refund or invented terminal provider rejection.
transport/timeout/5xx/cancellation after possible send stays unknown with same attempt. Do not promote any exception into definite financial failure or synthetic provider reference. No message-text classification.
result false is a definite refusal only according to the current typed Contract; result true is persisted before stock work, with real returned/owning reference semantics. Fake sandbox success never becomes real PSP readiness.
Preserve Wallet's wallet-refund-credit:{caseId} once-only key and zero PSP calls for Wallet. OriginalPayment never credits Wallet as fallback.

C — PHYSICAL RECOVERY IS A DIFFERENT INTENT
For confirmed-money + pending-disposition, the explicit physical recovery path must perform ZERO refund/Wallet calls and create ZERO attempts. Reuse the existing stable per-item restock keys and damaged-non-saleable rule.
Courier-origin: preserve physical receipt, exclusive claim, Order obligation closure, source/stock settlement sequencing and MarkRefundReleasedWhenFullyDisposed. Re-read owning evidence/closure needed for the remaining step; completed lineage is history, not new receipt or allocation.
CustomerDelivered: no trustworthy receiving/inspection fact exists today. Therefore recovery must expose an explicit receipt/inspection-required blocker and perform NO new saleable restock from default Usable or approval alone. Do not implement the new receiving stage or fabricate its evidence; that is274.
Ensure changed/shared recovery helpers cannot accidentally invoke the old unconditional ordinary restock through a new route. For legacy ordinary financial completion, keep the confirmed money and pending physical blocker; never refund again to repair that gap.
Already-settled case returns its recorded state with no effect. A failed stock call retains pending disposition and a truthful failure; next recovery reuses the same per-item identity, never marks SaleableRestock before Inventory confirms.
No automatic background worker/scheduler/daemon, no timed auto-retry, no cross-module compensation loop.

D — DURABILITY / CONCURRENT AND LOST ANSWERS
Do not hold a database transaction or row lock across an external gateway/Wallet/Inventory call.
Reuse short Returns-owned coordination to read/revalidate the exact durable attempt and persist a monotonic outcome. Re-read durable terminal outcome before applying a late answer; stale success/failure cannot downgrade or produce another attempt/event. Preserve existing approve-vs-reject authority; a rejected/cancelled/unapproved case cannot gain recovery.
Concurrent pending recoveries may rely on the owning Contract's SAME-key idempotency, but cannot mint new keys or double-credit. Prove returned durable state and state-change/outbox behavior with controlled tests; do not claim "one HTTP provider call" if same-key concurrent calls can occur.
Do not reintroduce broad DbUpdateException catches or ChangeTracker.Clear. Classify only the exact expected constraint if arbitration is necessary; preserve caller transaction/tracked work. Domain/DB persistence errors must not be swallowed into a false success or "unknown provider" answer.
An external outcome whose local save fails is still uncertain to the caller. A subsequent call must reconcile the same attempt, not trust unsaved tracked success. Use durable re-read/new scope as needed within existing infrastructure, no generic new transaction framework.
No migration authorized. Existing facts suffice for recovery/read projection; if an actual schema gap blocks a requirement, deliver independent safe portions and identify it precisely as PARTIAL rather than inventing durable proof.

FOCUSED PROOF ONCE
Add bounded tests through the real recovery handler/directory, including real authorization/route contract validation where feasible:

unauthorized/cross-scope/missing approval/rejected/cancelled/wrong attempt/ambiguous pending attempts -> no effect;
external success with lost/throwing answer then same stored key recovery -> one durable financial outcome, no new attempt/key;
repeated transport failure retains pending; definite failed outcome stays failed and cannot use pending recovery to mint retry;
explicit unconfigured adapter produces truthful unavailable, pending unchanged;
Wallet exact case key credits once, no PSP; OriginalPayment never credits Wallet;
confirmed money + stock failure -> physical-only recovery; Inventory may be retried with same item key, PSP/Wallet remain zero, damaged never saleable;
ordinary missing receiving evidence -> explicit blocker, no restock/new receipt, money remains confirmed;
concurrent/late same-attempt completions cannot regress durable outcome or duplicate outcome events; label controlled evidence accurately;
local persistence failure after external result never answers undurable success or new-key retry;
additive read description is pure and separates pending money, definite failure, pending disposition and fully settled.
Use controlled idempotent provider/Wallet/Inventory ports; no real money/stock operation. Build focused fixtures from stored approved facts instead of trying a live refund.
Relational tests ONLY if needed: canonical tooba_alpha loopback5432 target validation, caller-owned rollback transactions, explicit FAIL/framework-SKIP if unavailable, NO CommitAsync/cleanup/delete/seed/backfill. Do not rerun historical committed-race tests or fabricate a live case. If durable-commit/external-call interaction cannot be demonstrated in rollback-only relational tests, use controlled fault injection and disclose proof limit.
Run only named new/affected focused suites once after final changes; backend build once. No whole Returns/Host/architecture/Order/Fulfillment/Shipping/parity suite or baseline-worktree comparison. Known pre-existing failures already recorded.
FE unchanged: no FE test/build/restart or screenshots.
Deploy Host-only if safely publishable: inspect startup effects, graceful stop before publish to avoid locked DLLs, one publish/start with same existing config; GET health/readiness/provenance. No apply migration/seed/backfill. Report actual artifact fingerprint and whether changed backend served.
Current live Returns table was reported empty: no live actionable scenario is promised. Read-only existing routes/GET only; controlled proof is not a live operator walkthrough.

BOUNDARIES
ONE canonical tooba_alpha127.0.0.1:5432 SingleStore/store-alpha, Host5088, FE3000. No second database/Docker/15432/15433, edition/connection/port/appsettings/keys/CapturedPreview change.
No migration/DDL, committed business fixture, live case approval/reject/refund/recovery mutation, provider/PSP/waybill/notification/money call, user order/draft/history/stock/hold change, reset/reseed/backfill/cleanup/delete/restore.
No new customer receipt/inspection workflow, rejection-reason persistence, full queue/detail/mobile redesign, money formula/provider integration or financial-completion claim.
Preserve272-R1 accepted creation scope, shipping271-R1/270-R4 and all previous audit/history. Do not "repair" old unauthorized test activity by cleanup or restore.
Existing terminal-failure new-refund retry is a DIFFERENT intent: do not call it for unknown recovery or claim this task fully audits that whole legacy operation. Record any directly connected unsafe seam precisely; no speculative extra task.

HANDOFF
Persist THIS received task verbatim:
docs/ai/tasks/TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273/TASK.md
Evidence:
docs/architecture/evidence/TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273/{TASK.md,RESULT.md,RECEIPTS.md,result-contract.txt,recovery-state-and-contract-proof.md,same-attempt-and-disposition-proof.md}
Include source/action/state table, exact request/gateway/Wallet/Inventory identities, concurrency/lost-response/local-save failure proof and real limits. State explicitly backend-only; frontend275 and physical receiving274 NOT ISSUED.
Update TOP AGENTS/START-HERE/RECOVERY/Returns plan coherently: worker outcome awaiting Architect, never overwrite272/R1 historical results/reviews or self-declare Architect/user acceptance.
Commit+push normally; expected/actual base, code/evidence/final HEAD and fresh origin parity, exact files/checks/runtime/limits. Preserve unrelated scratch. No force/amend/reset/stash/rebase or credentials. No self-hash pin loop.
Post ONE complete canonical Bridge result after finalization, no placeholder/probe:
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273
Parent-Task: TB-RETURNS-CREATE-OWNERSHIP-EXACT-REPLAY-AND-QUANTITY-INTEGRITY-272-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA
Status: PASS | PARTIAL | BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA / Base-SHA / Code-SHA / Evidence-SHA / Final-HEAD / origin parity
Decision; implemented A–D; exact checks; served artifact; data/financial boundaries; backend-only and proof limits.
Worker PASS != Architect ACCEPT; no user/provider/financial/full-Returns acceptance.
automaticNextImplementationTask: NONE
END_TOOBA_WORKER_RESULT

STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK