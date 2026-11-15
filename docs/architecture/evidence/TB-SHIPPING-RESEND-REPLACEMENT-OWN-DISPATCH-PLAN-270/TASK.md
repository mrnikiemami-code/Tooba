PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270
Parent-Task: TB-SHIPPING-QUEUE-PERSIAN-DATES-AND-CONSISTENT-TIME-DISPLAY-269
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 8e9f42cf72af54101c102a60d6fdfce7be01e473
Mode: RESEND_REPLACEMENT_OWN_DURABLE_DISPATCH_PLAN
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

USER INTENT / SINGLE TASK
An employee must know when a returned-source resend or healthy-stock replacement can be prepared, leave the origin, reach the carrier, and reach the customer. A NEW physical attempt needs its OWN feasible durable commitment; the original shipment's expired dates cannot be reused as its plan.
Implement ONLY this timing closure through existing owners and the existing operational workflow. Employee job-stage filters are reserved for 271 and are NOT issued here.
ONE worker, ONE active task. No delegation/subagents, no historical-agent resume, no parallel task. Return ONE complete Result and STOP. Do not self-issue 271 or any repair.
Do NOT request, capture, generate or deliver screenshots. Use focused tests, text/network/DOM evidence and one review URL. No polling, repeated baseline/build/test loops or speculative polish. Rerun only an affected check after a concrete correction; disclose the reason.

ARCHITECT CHECKPOINT — 269
Architect decision: ACCEPT WITHIN PRESENTATION-ONLY SOURCE/EVIDENCE SCOPE.
Reviewed main 8e9f42cf72af54101c102a60d6fdfce7be01e473, implementation commit 463609685d797c2ea21e424ba66116aa026834e4, current queue formatter/shared helper source, issued TASK, RESULT, RECEIPTS, result-contract and date-display-proof. Compare from f62bd7564d5394dc8e36df4193b065d17be38682 confirms only three frontend code/test paths plus documentation/evidence, not backend changes. Normal DateOnly labels retain their calendar day, operational deadline formatting validates the accepted zone, ranges are LTR isolated, and stored/query values are unchanged.
13/13 focused tests, fulfillment 6/6, typecheck/build and served/DOM observations are WORKER evidence; Architect did not independently run the user's runtime/tests. No user visual acceptance, whole-shipping acceptance, backend/provider/financial certification, or approval of every possible malformed off-contract payload.
Expected-base absence in the 269 worker clone remains disclosed; fetched history now preserves the actual work base and implementation through the merge. Do not rewrite receipts or call that historical expected base verified.
268 remains awaiting user visual acceptance; preserve its tabs and the user's positive functional feedback. Do not repeat 269 or reopen its scope.
As part of THIS task's evidence, persist this checkpoint at the 269 evidence folder's ARCHITECT-REVIEW.md; do not invent an extra task for documentation.

RECOVERY / REQUIRED READS
Fetch origin normally; verify main and a known safe worktree. Verify Expected-Base-SHA exists and is an ancestor of actual origin/main. If remote has advanced only with compatible docs, inspect that delta and declare actual base. Missing expected object or unsafe/unrelated code divergence -> RECOVERY_CONFLICT, not silent substitution. Do not overwrite unrelated untracked scripts.
Read current AGENTS.md top, docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md, docs/ai/TOOBA-PIPELINE-PROTOCOL.md and CONTROLLER.
Read docs/planning/shipping/SHIPPING-RESEND-REPLACEMENT-TIMING-AND-EMPLOYEE-WORK-PLAN-270-271.md. Its "269 result absent" statement is historical and superseded by the checkpoint above.
Before backend code read .cursor/skills/tooba-backend-feature/SKILL.md; before frontend code read .cursor/skills/tooba-frontend-feature/SKILL.md and docs/architecture/FRONTEND-ARCHITECTURE-STANDARD.md. Follow repository feature/AMSC ownership rules without starting an architecture audit or recertification.
Read docs/architecture/shipping/SHIPPING-ADMIN-RULES-DECISION.md and SHIPPING-STOREFRONT-UX-REFERENCE.md for timing/owner constraints. User override: no screenshot deliverable or screenshot request. Do not scan the deferred findings register.
Inspect only the relevant existing resend/replacement/recovery and scheduling seams; old task files are evidence, not executable instructions.

SOURCE-CONFIRMED GAP / ENTRY POINTS

src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Queries/AdminFulfillmentWorkUnitProjection.cs, BuildParcelRow: ResendOfShipmentId != null selects an EMPTY plan list; first dispatch reads the original accepted-group plan. Linked damaged replacements also use resend lineage. Merely removing this branch would falsely lend the original plan.
src/backend/Modules/Order/Tooba.Order.Application/Admin/Operations/Services/AdminOrderOperationsOrchestrator.Fulfillment.cs, ResendShipmentCoreAsync: existing reviewed request passes originalShipmentId, selected lines and occurrence dispatch key to CreateResendShipmentAsync; it carries no new accepted scheduling intent.
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Directories/FulfillmentDirectory.Returns.cs, CreateResendShipmentAsync: payment authority, exact occurrence replay, returned-capacity allocation and serializable transaction already exist. Preserve them.
FulfillmentDirectory.Damaged.cs, ResolveDamagedGoodsAsync: durable damaged decision -> healthy-stock reservation/retained hold -> linked replacement parcel. Stock unavailable preserves decision without a parcel. Preserve decision-first and hold-retention semantics.
src/backend/Modules/Order/Tooba.Order.Contracts/Shipping/OrderDispatchPlanFacts.cs: IOrderDispatchPlanFactsReader currently exposes immutable ORIGINAL group plans. Add a narrow attempt-specific owning fact/reader only where needed; do not relabel historical group evidence.
src/backend/Modules/Shipping/Tooba.Shipping.Contracts/Scheduling/ShippingDispatchSchedulingAuthority.cs: existing ResolveFirstHandoff, BuildReceivingCalendar, ResolveLatestHandoffForReceivingDate, ResolvePreparationDeadlines / policy3. Reuse their actual current contracts; no second scheduling engine.
Existing captured timing inputs flow through StorefrontCapturedGroupCompositionCore, StorefrontCapturedSelectionWriter and CheckoutDirectory.CapturedAcceptance. Reuse owner Contracts and authoritative preparation/transfer/carrier/customer weeks; do not bypass module boundaries to join another module's tables.
Existing frontend features/order-operational-groups post-dispatch exception dialog, outcome and attempt store own reviewed envelopes and recovery. Extend that SAME action/dialog authority; do not create a parallel wizard or second action owner.

IMPLEMENTATION CONTRACT
A — Ownership and binding, decide narrowly before coding
Write a short ownership-and-plan.md inside this task evidence: current call path; exact facts already stored; genuinely missing facts; chosen minimal owner/persistence/Contracts seam; write ordering and recovery strategy.
Order owns customer delivery commitments; Fulfillment owns physical receipt/readiness, attempt/parcel and lineage; Inventory owns healthy-stock hold/readiness evidence; Shipping owns pure scheduling policy/settings. No Host business logic, cross-module DbContext/table access, endpoint arithmetic or frontend clock authority.
Bind each new plan to exact checkout/seller order/accepted group, source parcel or damaged decision, selected source lines/quantities, owner/method scope, attempt occurrence and resulting parcel. Read original captured binding and authoritative current eligibility through owning contracts. Explicitly record which policy snapshot/revision and current validity were used for the NEW commitment.
Never silently treat changed current settings as historical acceptance, select a sibling's seller/method, or edit the original accepted promise. If original capture/binding is missing, return a precise localized blocker; do not fabricate it.
Minimal additive schema/migration is authorized ONLY if required for durable new-plan ownership; no destructive migration, broad framework or legacy backfill.

B — Real readiness and feasible schedule
For same-goods resend, readiness cannot precede actual usable physical origin receipt and required processing. For replacement, it cannot precede lawful healthy-stock availability/reservation and preparation. A damaged receipt is not healthy resend capacity.
Use owning server facts and clock, not original order creation/dispatch date or browser now. If an operational input truly has no authoritative source, state the exact missing input; do not invent timestamps, durations or schedules.
Use existing scheduling authority forward for feasible receiving dates/windows and backward for preparation START, transfer DEPARTURE and carrier HANDOFF deadline. Preserve half-open cutoff/closed-day rules and both customer/carrier timezones.
Do not double-count preparation: distinguish source-ready time from the scheduling policy's preparation allowance; document input/output meaning. Never label PreparationStartUtc as packing completion; do not invent separate pick/pack durations.
Original customer date/window may be proposed only if still feasible for the NEW readiness. Otherwise show genuinely feasible alternatives in the existing review. Operator deliberately confirms the new date/window; do not claim customer consent/notification/carrier booking without an existing recorded fact.

C — Durable review, submit, replay and replacement stock pending
Review displays exact source/quantity, healthy resend versus replacement, chosen customer date/window/timezone, preparation start, departure and handoff deadline with readable Persian copy. Shared formatting from 267/269, RTL/mobile and existing Toast/MessageBox.
Persist immutable plan inputs/outputs and binding, calculation/policy version, readiness provenance, reviewed selection and actor/confirmation evidence. No GET/review side-effect writes.
Revalidate authoritative eligibility/stock/scope/feasibility at submit. A selection no longer feasible returns a localized refusal retaining the reviewed envelope; never silently clamp, replace dates or recompute-and-save a different intent.
Same occurrence after a lost response must resolve to the SAME parcel AND plan. Changed method/date/window/source/quantity under that key must conflict with zero new allocation/commitment. Protect concurrent submissions and crash/partial-write boundaries using the existing owners' mechanisms; no new successful parcel may escape with a missing/orphan/mismatched actionable plan.
Reconciliation of an already committed exact intent must return its recorded facts, even when time/settings later change; new-intent feasibility gates must not erase a committed outcome or mint a second parcel.
Replacement without stock retains the existing durable decision and truthful pending state. It MUST NOT acquire an actionable dispatch promise based on nonexistent stock. When stock is lawfully available, explicitly review/confirm the feasible plan before creating/activating the replacement attempt. Keep any existing paid-order hold retained; do not release/re-reserve it merely to refresh a plan.
A retained damaged decision/hold may be visible as pending, but may not masquerade as a fully scheduled parcel. Document the exact recovery route for each interruption boundary.

D — Existing rows and queue/detail projection
Read the new plan by exact physical attempt/parcel identity. First-dispatch parcels retain original group plans; resend/replacement parcels use ONLY their own newly accepted plan. No sibling lending or seller-order earliest-date fallback, no row/count inflation.
Existing ACTIVE unplanned resend/replacement parcels: provide explicit authorized review/confirmation on the same operational surface where lawful. Do not auto-backfill, reset lifecycle or mint a duplicate parcel merely to obtain a plan. Same-key replay and changed-intent protection apply to attaching the initial plan.
Completed/cancelled history remains unchanged and must not become actionable because a future plan exists. Existing legacy uncaptured rows remain an honest exception with a clear blocker.
Wire the durable facts into current queue timing and operational detail through narrow existing readers; no current-settings recalculation on every read. Keep original customer promise distinguishable from the new attempt commitment.
Do NOT implement 271 job-stage filters or redesign queue layout; retain current paging/full-set count authority, exact locators, 64px rows and 268 tabs/recovery.

E — Preserved business laws
Preserve original consumed reservation, quantity conservation, partial/mixed source allocations, one decision/case branch per unit, payment-authorized physical supply, package/member locks, held stock until lawful order-obligation closure, retained replacement hold after parcel cancellation, exact occurrence uncertainty handling, courier-return linkage and existing localized errors.
No refund/payment/wallet/settlement behavior change, new notification dispatch, carrier/provider call, real money or fabricated provider waybill. 255 remains parked and Tipax deferred. Parent common-package return support is not part of this task.
No architecture cleanup/refactor of existing oversized files; use focused feature-owned additions only where needed and keep existing dependency direction. No baseline resets/guard weakening.

ENVIRONMENT / DATA BOUNDARY
ONE canonical tooba_alpha @5432, SingleStore/store-alpha, FE3000 -> Host5088. No extra/disposable DB or Docker clone, port/edition/connection/appsettings change. Preserve keys/DataProtection/CapturedPreview and configured languages from the DB registry; do not hardcode FA/EN.
Current user orders, completed Returned-source/Delivered-resend trial history and shipping draft remain READ-ONLY during worker live checks. No new live demonstration orders, stock creation, settings edits, fixtures, manual business SQL, reset/reseed/delete/restore or repeat of 266 cleanup.
Feature implementation authorizes future operator-confirmed plan writes through the owning application; it does NOT authorize the worker to execute new business mutations on user orders for proof.
If an additive migration is required, use repository MigrationRunner with a private backup and actual plan once; no manual DDL or destructive change. Report migration IDs, backup reference, pending status and deployed code compatibility without exposing secrets. Do not restore backup.
Use existing test conventions with controlled owning ports for behavioral proof; any required relational test writes must be task-owned and rolled back, not a durable demo dataset, and must not alter user rows/settings/stock. Do not call a SQL-seeded Paid row a paid checkout-flow proof. If the protected environment cannot support a necessary proof safely, report the exact unproven item, not a substitute PASS.

FOCUSED PROOF ONCE
Use one focused new/affected suite per owner touched; group cases rather than repeatedly invoking whole modules. Cover:

Usable physical return readiness versus healthy-stock replacement readiness; no early promise, no duplicate preparation allowance; stock pending has no actionable plan.
Carrier cutoff, processing/carrier/customer closed hours/days, accepted timezones, expired original customer date -> newly feasible deliberate selection; no original-deadline borrowing.
Stored new plan/lineage survives reload; first dispatch unchanged; partial/mixed quantities remain conserved; unrelated sibling and completed parcel cannot inherit this plan.
Review-to-submit expiry/settings revision change refuses truthfully, retains envelope and writes no unintended parcel/commitment.
Exact replay and lost response before/after relevant durable boundaries -> same plan+parcel; changed scheduling/source intent -> conflict; concurrent attempts cannot double-allocate.
Replacement pending-stock -> later explicit schedule review -> one retained hold/one linked parcel; parcel cancellation does not release paid-order hold.
Existing active unplanned parcel explicit confirmation, legacy capture blocker, completed history unchanged.
Queue/detail read stored own plan and correct next timing without recomputation; Persian DateOnly/instant/range display and existing retained-dialog recovery remain intact.
Relevant frontend mounted/contract suite, typecheck and production build once after the final change; relevant backend focused tests/build once. No full-solution test sweep, certification pass, historical baseline comparison or performance/stress loop. Apply directly applicable repository checks; no unrelated findings task.
Publish/restart only changed services once as necessary, verify served FE BUILD_ID and Host health/ready. No user business actions. Read-only current operational page/queue at 390/1440: no new overflow, controls/readable Persian labels and truthful existing-row state. Use DOM/text/network only; NO screenshots.
If no live eligible unplanned parcel exists, say so. Controlled tests may prove the write path; read-only UI/bundle checks do not establish a live successful write or user acceptance. Never manufacture a new example just to obtain a handoff URL.
Record what is independently tested, what is worker runtime observation and what remains for user's later trial. No universal shipping/provider/customer-consent claim.

DELIVERY / EVIDENCE
Persist exact received file at docs/ai/tasks/TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270/TASK.md.
Evidence folder docs/architecture/evidence/TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270/:
TASK.md, RESULT.md, RECEIPTS.md, result-contract.txt, ownership-and-plan.md, timing-and-readiness-proof.md, replay-and-write-boundaries.md, user-review.md.
Keep evidence compact: actual changed paths/seams, plan schema/Contracts/migration if any, focused commands/counts, readiness/policy binding, stored-vs-projected values, replay/refusal facts, deployed services/BUILD_ID, Git ancestry/parity, actual limits and elapsed time. No screenshot files, credentials, cookies or giant copied logs.
Persist the 269 Architect checkpoint above as docs/architecture/evidence/TB-SHIPPING-QUEUE-PERSIAN-DATES-AND-CONSISTENT-TIME-DISPLAY-269/ARCHITECT-REVIEW.md.
Update top AGENTS/START-HERE with 270 result and limits; update the plan pointer truthfully: 270 issued/result awaiting review, 271 NOT ISSUED. Preserve old receipts/audit corrections; do not erase history. No broad documentation rewrite.
One review URL pointing to the real existing operational surface; explain whether it only shows current read-only state or actually contains an eligible unplanned parcel. Do not promise an actionable example that does not exist.
Commit/push normally with actual code/evidence/final SHA and fetch parity. Expected base must remain the issued value; actual base/merge facts are separate. No force, amend, reset, rebase, stash, unrelated file commit or credential artifact. Integrate compatible incoming docs by normal merge only if safe; code conflict or unsafe divergence -> precise RECOVERY_CONFLICT.

RESULT CONTRACT
Send ONE final complete Result through Bridge using the canonical envelope. No test/probe/placeholder POST and no partial progress Result. Begin with:
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Then actual Task-ID, Parent-Task, Channel, Worker-Id, Agent-Type, Program, Mode and Bridge-Task-Row.
Status must be PASS, PARTIAL, BLOCKED or RECOVERY_CONFLICT; Stop-State is STOP_FOR_ARCHITECT.
Fixed ordered tail fields before the body:
Expected-Base-SHA: 8e9f42cf72af54101c102a60d6fdfce7be01e473
Base-SHA: actual verified work base
Code-SHA: actual code commit or NONE
Evidence-SHA: actual evidence commit or NONE
automaticNextImplementationTask: NONE
Replace descriptive values above with real values; no unresolved placeholders in transmitted Result.
Body: root; owner/binding/readiness design; implemented behavior; exact durable/replay/crash proofs; migration/data boundaries; tests/build/deployment; changed files; final HEAD/origin parity; one review URL; unproven items.
Close exactly END_TOOBA_WORKER_RESULT then END_TOOBA_TASK; nothing follows.
Worker PASS != Architect ACCEPT. No user visual/functional acceptance or all-shipping completion claimed.
After final Result delivery and task completion return IDLE and STOP. No 271, no self-repair task, no wait/poll loop.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK
