PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279
Parent-Task: TB-RETURNS-CUSTOMER-MULTI-SELLER-PARTIAL-SELECTION-AND-TEMPLATE-FEEDBACK-278
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 57cccbbe7cffea23c30ee0f474291cb3dda7bc58
Mode: FINAL_CONSOLIDATED_RETURNS_DECISION_PHYSICAL_FINANCIAL_AND_CUSTOMER_COMPLETION
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

ONE FINAL CONSOLIDATED DELIVERY
User has substantial other development and explicitly rejected10 long micro-tasks. This is the planned second/final main delivery, carrying the real278 durability blockers; do NOT spawn278-R1 or separate helper/Toast/test tasks.
One worker, no delegation, screenshots, automatic next task or background retry loop. Retain277/R1/R2 and useful278 work. Finish the connected paths, not just design/docs.

AUTHORITATIVE ENVELOPE / BASE RECOVERY
This FULL envelope is committed BEFORE execution at:
docs/ai/tasks/TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279/TASK.md
identical evidence TASK.md + issuance-receipt.json (SHA256).
If Bridge content is empty, read these exact repository bytes. DO NOT reconstruct scope from banners/reviews. Verify copies/hash, preserve original.
Expected-Base-SHA above is the VERIFIED CODE BASE. Architect's following issuance commit contains ONLY review/plan/TASK/receipt/recovery docs. It is explicitly authorized to fast-forward over that docs-only issuance commit; record actual starting HEAD and prove src/config/schema diff from expected code base is empty. No self-referential expected hash needed. If incoming SOURCE differs, inspect/declare before integrating. No reset/stash/rebase/amend/force or loss of unrelated work.

READ FIRST
AGENTS standing user rules;
docs/planning/returns/RETURNS-MARKETPLACE-COMPLETION-PLAN-278-279.md;
docs/planning/returns/RETURNS-279-EXECUTION-DECISIONS.md (decided minimal domain shape);
278/ARCHITECT-REVIEW.md, SOURCE-AUDIT and existing owning code/architecture rules.
278 is PARTIAL: selection/eligibility/template Toast are useful, but submit can overwrite retained unknown act; no mutation epoch; sessionStorage getter uncaught/unreadable treated as none; storage lacks viewer/body binding; same-line reason portions not in UI; custom role=dialog overlay. Close those here, no separate repair cycle.
Unknown-precision read blocked vs write integer-only is a disclosed278 limitation; do not claim identical capability. No guessed unit definition or new UOM framework.

A — FINISH THE CUSTOMER PATH, KEEP ACTUAL TEMPLATE TOAST
Repair existing return-request-modal.tsx / return-submission.ts / checkout integration:
- A found unresolved OR unreadable act blocks new submission at BOTH UI and send/storage boundary. Never unconditional store(newAct) over it.
- Guard sessionStorage getter/setter/read-back; silent discard/corrupt/mismatched body is visible pending/unavailable, zero blind send. Validate parsed exact body against frozen checkout/viewer/seller/line/reason/key/quantity/destination and version. No fabricated default destination or send of arbitrary stale bytes.
- Scope storage to authoritative authenticated viewer + checkout. No pending intent inherited on logout/login/actor switch; no browser identity as server permission.
- Capture epoch on every read/mutation/loop/refetch/finally/toast/callback. Close/unmount/retarget/identity change cannot release a newer lock or paint newer UI. Stop unsent child POSTs on superseded surface, retaining exact pending children; don't assume cancellation of the in-flight server request.
- Same-tick serialization survives close/reopen. Write/read/compare exact immutable envelope and child body before each possible send; mark outcomes/clear only if exact current act/bodies still match. Successful child is terminal; unknown gets same bytes/key. A later context/state/authorization refusal is not proof an earlier send did not write (including quantity_unit_invalid when metadata availability caused it).
- Positively bind successful child response to reviewed target/lines/quantities before calling it recorded. No current-status-only substitute.
- Add actual same-line MULTI-REASON portion editing. Shared exact ceiling across portions, canonical grouping per seller+line+reason, duplicate grain merged or refused before send (no colliding child keys). Match server's reason equality; lowercasing arbitrary reason text must not alias different bodies.
- Replace custom overlay with existing shared DS Dialog; preserve template look/RTL/focus/Escape/44px/internal scroll. Existing react-toastify via AppProviders, ONE command/summary notification; never a passive ToastRegion substitute/second container/GET-derived toast.
- Correct items stay unselected/untouched; user socks2of3 and other-seller damaged-only scenario remains. Unknown child blocks replacement; partial successes remain visible and resumable.

B — AUDITED DECISION CORRECTION, NOT FINANCIAL RESET
Implement Returns-owned append-only effective decision/resolution revision authority with actor, meaningful reason, timestamp, monotonic reviewed version and exact occurrence identity. Keep legacy original decision row/history as baseline; no delete/overwrite/backfill.
One authority serves reads, capability/action projection and every decision/start/recovery caller.
Add explicit reviewed ReopenForReview action in existing workspace/modal:
- Allowed before ANY refund attempt (Pending/Failed/unknown/Succeeded all block), irreversible disposition/restock/custody handback or courier closure/release effect. An ordinary receipt alone is evidence to retain, not a reason to erase it.
- Reopen appends correction and returns case to review; previous decision/reason stays visible. No money/stock/claim effect.
- Reject->reopen rechecks/re-acquires original claim capacity under owning seller-order serialization; another case may have occupied released quantity. Refuse exact conflict rather than steal/overrun capacity. Preserve positively proven original timely claim/allocation, no new units/window extension.
- New approve/reject after correction binds CURRENT reviewed revision. Stale pre-correction review cannot approve/reject/start money. Exact old operation replay returns its own recorded outcome or explicit stale/history result, never changes a newer revision.
- No blind undo for paid/physically completed history. UI explains when correction is available and why a real effect blocks it; remove arbitrary forever-not-reversible copy.
Keep approval decision-only. First money, Pending resume, reviewed Failed retry remain distinct.

CRITICAL SERIALIZATION
Correction, final decision/plan publication and first-attempt persistence must serialize against the SAME owning case version. StartFirstRefund currently reads Approved outside a case lock; do not allow stale approval to send money after a concurrent reopen.
Use short Returns-owned transaction/lock/CAS for effective version + case transition + durable attempt/budget reservation, committed BEFORE external calls. Re-read/reload current facts after lock, not stale tracked state.
Consistent lock order when quantities/payment touched: seller-order serialization -> case -> Returns-managed payment budget; no external Order/Payment/Wallet/Inventory/Fulfillment call under a transaction/lock. Refuse caller/ambient/enlisted effectful transaction as existing273 protocol requires.
All alternate adapters/Order operations/event consumers honor the same authority; no legacy combined approve+refund or correction bypass.

C — LEGACY MULTI-ITEM / PARTIAL RESOLUTION AND REJECTED GOODS
Do not clone historical receipts or split already-paid history. Implement a reviewed per-item/condition QUANTITY resolution plan on ordinary legacy multi-item cases:
- Original requested lines/quantities/amount stay visible and immutable audit facts.
- Accepted/rejected/pending quantities + reasons bind exact owning items and actual receipt portions; no foreign evidence, double allocation or accepted quantity not received.
- One item may be accepted and another rejected; one requested quantity3 may deliberately resolve2 accepted +1 rejected/withdrawn, without fake receipt for unreceived remainder.
- Final approved monetary amount derives ONLY from accepted quantities and stored accepted unit money facts, using existing supported currency policy. Never use today's price, browser amount/payment/destination, or silently refund all original requested units.
- No first money until reviewed resolution is complete and accepted coverage coherent. Freeze approved amount/plan/version before first attempt; after money started it cannot be changed. All-rejected means no refund owed, not “refunded”.
- Preserve full legacy approved cases as baseline full-amount/quantity evidence, no migration rewrite. Update snapshot/Contracts additively so original claim vs approved quantity/amount and current physical obligation are distinct.
- Settlement uses accepted usable portion ONLY for saleable restock after Inventory confirmation, stable per-item immutable approved quantity/key; accepted damaged never saleable. Rejected received portion remains customer-owned custody, never restocked because another portion was refunded.
- Add durable per-portion disposition/custody facts where one enum cannot express mixed quantities. Aggregate labels/counts/money/physical completion derive from those facts. No “all effects closed” while some received portion is still owed; history excludes active physical debt.
- Provide deliberate audited confirmation of ACTUAL handback to owning customer for rejected received goods (in-person or verified delivered proof). Quantity/condition/source/recipient/proof/actor/occurrence bound; tracking alone is not delivery. Partial handback remains partial; zero refund/Wallet/Inventory effect. This is a Returns custody fact, not a fake carrier booking/Fulfillment Delivered transition.
- Rejected/cancelled received-but-unresolved goods still occupy their physical quantity; do not release them to a second return while held. Unreceived/no-longer-claimed quantities may release capacity honestly.
- Unsupported courier-origin partial/correction cases fail with precise source/irreversible-effect blocker, never borrow ordinary receipt/hold authority. Existing accepted courier conservation/closure remains.

D — DELIVERY-SLICE CAPACITY / EXPIRY, ONE AUTHORITY
Fix evaluator's stillInWindow minus ALL returned bug with durable per-delivery allocation:
- Bind each NEW claimed quantity to positive owning delivered slices; add an additive Contracts identity/read if needed (current LineDeliverySlice has only line/quantity/time).
- Under existing per-seller-order serialization, allocate only available eligible quantities, persist Returns-owned source allocations with case/item ownership and exact quantities. Same source cannot fund sibling cases/reasons twice.
- Eligibility, creation, correction/capacity release and read deadlines use ONE allocation authority; no separate UI math.
- Old returns assigned to expired slice must not consume a newly delivered slice. Test two batches of same SKU with separate windows; last quantum/cutoff equality/partial returns/failed refund occupancy/rejection handback/correction all preserve quantity.
- No guessed backfill/reassignment of ambiguous legacy rows. An unambiguous one-slice legacy interpretation needs positive evidence; otherwise expose precise blocker/owed work and preserve history. No new browser “today”, manual timestamp rewrite or borrowing sibling parcel dates.

E — AMOUNT AND SHARED CHECKOUT REFUND BUDGET
Payment Contracts currently returns amount/currency/checkout/status, not remaining refundable balance. Development FakePaymentRefundGateway ignores amount/currency; never treat its success as budget protection.
Before each NEW first/retry attempt, serialize Returns-managed budget for that payment and atomically persist the attempt reservation:
- bind case to owning checkout/customer/payment/currency and immutable approved amount;
- count Pending+Succeeded obligations across Returns cases for that payment (including Wallet destination), with exact replay counted once; definite Failed releases only its failed attempt's reservation, not a Pending/unknown child;
- total reserved+confirmed cannot exceed the supported owning payment budget; no second first attempt per case, no double spend across sellers, no deadlock on differing seller orders sharing a checkout payment;
- external gateway/Wallet after commit; failure classification/outcome application remains273 monotonic same-attempt protocol. No lost reservation after unknown outcome or duplicate attempt from correction.
Prefer existing attempt rows + short owning budget lock over a second payment engine. No Payment/Wallet foreign schema SQL or cross-module transaction. If refunds outside this observed Returns authority are not represented, disclose that limit; don't claim universal PSP refundable-balance readiness.
Do not invent shipping/tax/coupon refund rules. Prove the accepted-item amount under current supported stored money facts and explicitly identify unsupported allocation facts instead of silently guessing.

F — ONE BOUNDED ADDITIVE MIGRATION / NO DATA FIX
Authorize the minimum Returns-owned additive schema for revisions/resolution/allocation/custody facts and local ownership/uniqueness/checks. Existing schemas/status numeric values/history not rewritten. Add fields/enums only additively, no renumbering/shipped member removal.
Before EACH actual apply attempt: verify canonical target; capture migration plan; create FRESH private pg_dump of tooba_alpha; verify checksum + pg_restore --list; then apply ONLY through Tooba.MigrationRunner for store-alpha. Record chronology, current/pending migrations and post-plan. No manual DDL, Down/restore/reset/reseed/backfill/cleanup/delete, no other module migration.
On an apply failure: report rollback state, correct source, make a NEW verified pre-apply backup before retry; no retroactive backup claims. Do not commit credentials/dumps/keys.
All existing user cases/orders/receipts/refunds remain untouched; additive migration/history only authorized durable live write.

G — FOCUSED CONNECTED PROOF / SERVED REVIEW
Named new/affected owning tests + real mounted workspace/customer component:
1 two-seller socks2of3, damaged-only, same-line two reasons shared ceiling; stable child payloads and template toast;
2 unknown -> fresh selection attempt cannot overwrite; corrupt/throwing/silent storage zero sends; close/remount/retarget/viewer switch/double click/late answer/finally fences and exact-only update;
3 correction history/version/exact replay, reject capacity reacquisition conflict, any money attempt/effect blocks reset; forced reopen-vs-first-start interleaving admits only the lawful winner;
4 legacy multi-item/partial accepted quantities, declined/unreceived remainder, correct approved amount, no stock on rejected goods; partial physical handback/proof replay and no false settled;
5 two same-line delivery batches, old returned batch vs new window, allocation/cumulative capacity/legacy ambiguity;
6 two seller cases sharing one payment budget, duplicate first and child retries, unknown Pending reservation, zero unreviewed extra money.
Controlled provider/Wallet/Inventory/Fulfillment ports ONLY. Essential PostgreSQL constraints/serialization proof uses validated canonical target and ROLLBACK-ONLY task-owned rows, zero committed fixtures/cleanup. Label simulated/serial/CAS proofs honestly; do not claim stress/two-writer commit proof without it.
No unfiltered Returns/Host/AMSC/Order/Shipping/parity/baseline/stress suites. Target route/catalog pins only when changed. Typecheck + FE build and backend build/publish after final code; fix actual failures within this task and rerun affected checks, not new per-test tasks.
ONE canonical tooba_alpha5432 SingleStore/store-alpha, Host5088/Next3000; no edition/connection/port/appsettings/keys/CapturedPreview change or second DB/Docker15432/15433.
Graceful stop before publish; refresh changed services once and verify loaded DLL/hash, health/ready + served BUILD_ID/manifest/chunks.
GET-only actual served customer checkout/admin queue/cases at390/1440; actual template toast/modal interaction only opening/closing without writes where legitimately offered. No screenshot. Settled history/empty eligibility is not active end-to-end Marketplace proof; controlled mounted proof clearly labelled.

USER DATA / ACCEPTANCE
Cases01a1e6b0-f33a-7000-80ee-beffb6344490 and01a1e9c5-c729-7000-b614-79daf3d0e812 remain READ-ONLY. No new live case/order/receipt/decision/refund/handback/stock/hold/provider/PSP/Wallet/waybill/notification/money action to demonstrate a feature. Do not reverse user test history.
No broad “all shipping/Returns/financial complete” claim. Goal is the requested connected internal/customer workflow; real provider readiness and user trial remain explicit limits.

HANDOFF
Evidence docs/architecture/evidence/TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279/:
TASK.md (exact issued bytes), RESULT.md, RECEIPTS.md, result-contract.txt,
decision-and-partial-resolution-proof.md, delivery-and-budget-proof.md,
customer-retention-proof.md, migration-and-backup-proof.md, user-review.md.
issuance-receipt.json already records original SHA256; preserve it. Update CURRENT AGENTS/START-HERE/RECOVERY and278–279 plan with actual completion and limitations. Preserve all prior worker/Architect/audit reports.
Normal code/evidence commit+push, exact Code/Evidence/FinalHEAD + fresh origin parity, served FE/Host identities. No force/amend/reset/stash/rebase/self-hash/poster loop/unrelated scratch scripts.
One result for this consolidated task. If an exact genuinely missing owning fact prevents a lawful part, state it and delivered scope; no fabricated PASS, scope disappearance or automatic follow-up.
One primary review URL with clear controlled-vs-live limits; user's later test separate. STOP after result. No further implementation task automatically.

STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK
