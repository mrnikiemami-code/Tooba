PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R2
Parent-Task: TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 15b0d91488637631dbbc7e466212dd226b26f919
Mode: OWN_ATTEMPT_PLAN_REVIEW_CONFIRM_AND_GATED_DISPATCH
Track: SHIPPING_OPERATIONAL_TIMING
Title: Durable own plan for resend and replacement on the existing operator workflow
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

USER INTENT / EXECUTION
Finish one coherent operator path: an actually allocated returned-source resend or healthy-stock replacement has its OWN deliberately confirmed feasible customer window and preparation/departure/handoff plan. The employee sees when to work on that exact parcel.
One worker / one active task. No subagents, delegation, concurrent task, historical-agent resume or automatic follow-up. 271 job-stage filters are NOT ISSUED.
NO screenshots: do not request, capture, generate or deliver them. Use focused tests and text/network/DOM measurements. No baseline comparisons, idle polling, repeated broad builds/tests, speculative polishing or unrelated repair. Run the applicable focused checks once after final code; rerun affected checks only after a concrete correction.
This is implementation, not another design-only stop for an internal missing adapter/table/command already authorized below.

ARCHITECT CHECKPOINT — 270-R1
ACCEPT within additive read-only captured-input seam scope, based on independent source/evidence review at 15b0d91488637631dbbc7e466212dd226b26f919.
Compare from 1b1f8b68f0a1fa0278293f2ca91998d477943e2b confirms four implementation paths: Contracts, adapter, scoped DI registration and focused test. Reviewed request/result/lookup source, DI, focused test source and RECEIPTS. Exact group AND row resolution recovers stored durations; no quote, destination, measurements, provider or current-draft dependency. Typed refusals have no payload; historical missing inputs remain explicit. Cancellation/unexpected exceptions propagate. No live caller/plan/UI outcome is accepted.
7/7 and build success are worker evidence; Architect did not independently execute local tests/runtime. No relational/hosted/user acceptance claimed.
Process deviation: the whole Shipping suite and Composition subset were OUTSIDE the issued focused validation scope. Their failures are disclosed, but "sampled" failures do not prove all 88 have one cause; catalog failure being unrelated is not independent baseline certification. Do NOT repeat those suites, create their disposable DBs, fix the catalog opportunistically, or claim all unrelated baselines certified.
Persist this checkpoint as R1/ARCHITECT-REVIEW.md during THIS task, preserving prior receipts.

ARCHITECTURE DECISION — AVOID CROSS-MODULE PARTIAL-COMMIT FICTION
Use TWO explicit durable stages in the EXISTING workflow:
1 Fulfillment creates/reconciles the physical parcel and its original source allocation through its EXISTING owning occurrence mechanism.
2 Order deliberately confirms an OWN shipment plan for that exact existing Created parcel.
Between stages the parcel is legitimately "نیازمند زمان‌بندی": it is NOT dispatch-authorized. Response loss leaves one recoverable allocated parcel, not a successful scheduled attempt. The UI resumes that same parcel's planning.
This supersedes 270's unapproved "Order supplies parcel id, insert Pending plan, create parcel, flip Committed" proposal. DO NOT introduce Order-generated Fulfillment ids, change owner identity policy, or claim separate module commits atomic.
The enforceable invariant is: NO resend/replacement may be dispatched or consume stock through dispatch without its own validly bound confirmed plan. Unplanned Created parcels can exist visibly and recoverably; they cannot masquerade as scheduled.
No distributed transaction, scheduler job, compensation daemon, polling or new generic state engine. Plan confirmation writes only its owning Order fact in one transaction. Fulfillment's existing parcel/hold creation and replay stay authoritative.
The existing resend/replacement dialog follows the recovered/newly-created parcel into explicit scheduling review/confirm. Existing active Created unplanned parcels get the SAME action. This is one workflow, not a second wizard/action owner.

RECOVERY / MANDATORY READS
Fetch main; verify known safe worktree, Expected-Base-SHA present and ancestor of actual origin/main. Inspect incoming changes: compatible docs advancement may be integrated and declared, unsafe code divergence/missing base -> RECOVERY_CONFLICT. No silent replacement, reset/rebase/stash or force. Preserve unrelated untracked scripts.
Read current AGENTS/START-HERE, docs/ai/TOOBA-PIPELINE-PROTOCOL.md, CONTROLLER, .cursor/skills/tooba-backend-feature/SKILL.md; for frontend read .cursor/skills/tooba-frontend-feature/SKILL.md and FRONTEND-ARCHITECTURE-STANDARD.md.
Read SHIPPING-ADMIN-RULES-DECISION.md and SHIPPING-STOREFRONT-UX-REFERENCE.md under docs/architecture/shipping; preserve existing approved groups, configured language registry, scope, RTL and customer window semantics. Do not read linked screenshot artifacts: user instruction explicitly forbids screenshot work.
Read 270/270-R1 evidence as history with the architecture decision above overriding the unimplemented two-owner Pending/Committed proposal. No deferred findings scan, module recertification or broad audit.

SOURCE ANCHORS

Shipping.Contracts/Scheduling/ShippingAttemptSchedulingContracts.cs and Infrastructure/Scheduling/ShippingAttemptSchedulingLookup.cs now supply captured inputs with HasInputs and MissingInputs. HasCompleteInputs is presence only, NOT feasibility/eligibility.
Shipping.Contracts/Scheduling/ShippingDispatchSchedulingAuthority.cs owns forward receiving calendar, latest feasible handoff and preparation/departure/handoff policy3. Use its actual contracts, don't copy calendar arithmetic.
Order.Domain/Checkout/ShippingGroupAcceptedEvidence.cs owns immutable original binding and original plan outputs; Order.Contracts/Shipping/OrderDispatchPlanFacts.cs and Infrastructure/Integrations/DispatchPlan/OrderDispatchPlanFactsBridge.cs read ORIGINAL group plans.
Fulfillment.Contracts/Operations/FulfillmentAdminOperationsContracts.cs ShipmentSnapshot already exposes exact parcel/group, CreatedAt, ResendOfShipmentId, ReturnedAt, ReturnReceipt, IsReplacement, DamagedResolutionId, ReplacementReservationId and item quantities.
FulfillmentDirectory.Returns.cs and .Damaged.cs own parcel creation, source capacity, replacement hold and recovery; do not move them into Order.
Fulfillment.Infrastructure/Queries/AdminFulfillmentWorkUnitProjection.BuildParcelRow deliberately uses no original plan when ResendOfShipmentId != null. Replace only that gap with exact own-plan lookup.
Order.Application/Admin/Operations existing MediatR commands/orchestrator/projection/authorization and AdminOrderOperationRequest; existing order operations endpoints are the action boundary.
Frontend features/order-operational-groups/ui/post-dispatch-exception-dialog.tsx, its outcome and retained-attempt/session modules own current reviewed actions/reconciliation. Existing shared operational group/parcel surface owns action entry points and 268 tabs.

IMPLEMENTATION A — OWNER FACTS / SCHEDULING INPUTS
Add minimal Order-owned immutable confirmed shipment-plan entity/EF configuration and additive migration. Suggested table order.shipment_dispatch_plans keyed by exact ShipmentId; exact occurrence uniqueness scoped to owning context and payload binding. Do not reuse plan of another attempt or edit original accepted evidence.
Persist checkout/seller order/fulfillment/group/source/parcel identities, attempt kind, source item quantities/fingerprint, damaged decision/hold identity when relevant, accepted owner/method/service/option and captured SnapshotId/DraftId/SourceRevision/FormatVersion/GroupId/RateRowId, readiness provenance, server review/confirmation time/actor, selected customer date/window/timezone, handoff local date/window/zone, preparation START/departure/handoff UTC triple, used prep/transfer minutes, transport-days rule and scheduling policy version. Store immutable calculation inputs or an exact durable captured reference plus readiness/selection sufficient to audit/reproduce that version; do not fabricate a mutable snapshot.
A narrow Order Contracts plan reader returns exact confirmed records for requested shipment ids. No cross-module table joins/DbContext access, Domain type leakage, Host business logic, raw-json bridge or new scheduling framework.
Use R1 LookupAsync from trusted original accepted binding, with exact group/rate row and ExpectedSnapshotId. Validate returned provenance; never trust browser-provided owner/revision/row/method. No re-quote/current draft/destination/measurements or first-item/seller shortcuts.
Current actor/checkout-owner access and owning Fulfillment context remain mandatory. Captured historical presence is NOT current activation/eligibility; apply existing authoritative validity checks where applicable without silently rebinding to a newer revision. If validity facts truly are absent, report the narrow limitation rather than imply checked.
Document the exact binding policy in ownership-and-plan.md, distinguishing immutable captured scheduling inputs from present physical/actor/payment eligibility.

IMPLEMENTATION B — PHYSICAL READINESS / FEASIBLE PREVIEW
Plan only an exact live Created linked resend/replacement parcel; never first dispatch, dispatched/completed/cancelled history, another seller, or a parent common package passed as a child.
For healthy resend require source's actual usable return receipt/ReturnedAt and this parcel's owning allocation/quantities; damaged goods cannot fund it.
For replacement require this parcel's durable damaged decision and its OWN purpose-bound retained healthy-stock hold, still lawful for these units. Stock-pending decision with no actual parcel/hold yields an honest blocker and NO actionable promise. Read narrow owning Contracts/ports if a necessary existing fact is not projected; adding such a seam is authorized, not reason to abandon implementation.
Use server evidence: physical source ready time cannot precede actual receipt/hold/allocation. A parcel's CreatedAt may be a conservative evidence bound ONLY when the owning successful creation proves the relevant source was already lawfully available; verify and document that fact, never use a timestamp as substitute for stock validation.
Effective scheduling anchor cannot be before present server review/submit time or source readiness. Preparation and transfer allowances are applied ONCE by existing Shipping authority; don't add them to readiness and then subtract/add again. Preserve explicit WorkingWeek/continuous mode, independent weeks/timezones and half-open cutoff rules.
Build feasible receiving options using ShippingDispatchSchedulingAuthority and complete required inputs; incomplete historical capture, refused timezone or unprovable source yields localized blocker, never invented weekdays/date/slot/minutes. Nullable legacy slot duration keeps its established full-day meaning.
Keep customer receiving date separate from carrier handoff. Original date/window is a candidate only if still feasible for the new attempt; elapsed/unreachable promise -> new feasible choices deliberately selected by operator. Do not claim customer consent, notification, carrier appointment or new money fact.
The read-only preview returns exact source/binding/version/review context and feasible options plus computed deadline meanings. Never writes, reserves stock, creates parcel or commits a plan.

IMPLEMENTATION C — CONFIRM / EXACT REPLAY / CONCURRENCY
Use existing admin operations boundary and MediatR/Result conventions for a precise plan_shipment_dispatch command (repository-equivalent naming allowed), with validator, stable localized error codes/resources and same actor gate as existing physical operations.
The immutable reviewed envelope binds exact parcel/source/binding/selection. Before any new write recheck authorization, payment/supply eligibility, parcel live Created state, quantities/hold, binding and feasibility with authoritative current server time. Selection expired since review -> truthful refusal retaining envelope; never clamp, silently choose another date or recompute-and-save different intent.
Only then write one confirmed Order plan transactionally. No stock/parcel/decision creation or modification in this command. Unique constraints / appropriate owning concurrency mechanism arbitrate two independent confirmations:

identical same key and intent -> same stored plan;
same key changed date/window/parcel/source/binding/quantities -> conflict, zero extra write;
two different keys for one parcel -> winner's plan is immutable, second must resolve an exact-identical fact or typed conflict, never overwrite;
lost response after commit -> read/reconcile exact stored plan, no recalculation from now;
actor authorization is checked even on replay; committed fact can be read as history after time passes, but does not re-enable completed/cancelled parcel.
One immutable initial plan per physical parcel in this scope. Do not auto-replan an elapsed confirmed promise or overwrite it. Display a truthful elapsed-plan state; any new attempt uses existing lawful lifecycle and separate identity, never automatic cancellation/recreate.
A concurrent cancellation or other lifecycle change must not make an Order record authorize a stale parcel: loaded Fulfillment state and exact plan binding are required at dispatch; saved plan for a concurrently cancelled parcel is inert history. No claim of atomic transactions across modules.
Reconciliation before and after confirm must be explicit, bounded request/response, not timers or endless retries.

IMPLEMENTATION D — DISPATCH GUARD + QUEUE / DETAIL
Apply one shared authoritative own-plan requirement to EVERY existing actual dispatch path for linked resend/replacement parcels, including single, bulk selection and consolidated-package dispatch. Check all affected members before any consume/state/event, then preserve existing quantity/payment/package authorization. Legacy first-dispatch behavior unchanged.
No plan / wrong source/group/parcel/quantity/binding -> typed "ابتدا زمان‌بندی مرسوله را تأیید کنید" (appropriate catalog wording), zero stock consume, zero DispatchedAt/event/partial package dispatch. No optional bypass, client-only disabled button, or default reader that always authorizes.
Read plan facts through a narrow Order Contracts seam which ONLY reads Order data; avoid recursive Order->Fulfillment->Order read chains and DI cycles. Pure reads must not call scheduling or current settings.
Server capabilities project scheduling action/blocker on exact eligible Created parcels and suppress dispatch until confirmed plan. Make the real command guards agree with projection; cancelled/completed history never gains a planning action.
Queue: first dispatch uses original group plan; linked resend/replacement uses ONLY its own confirmed plan by ShipmentId. Keep full-set timing/count/paging authority, exact locator and one row per physical unit; no earliest sibling/group-plan lending.
Detail: display new customer commitment and own deadlines alongside clearly distinguished original history, not overwrite. PreparationStartUtc means last time to START preparation, not preparation complete/packing finish; departure and handoff close keep their distinct meaning. Use shared Persian/Jalali/accepted-zone/LTR formatting from 267/269.
No 271 job-stage filters, unrelated layout redesign, status engine or new financial surface.

IMPLEMENTATION E — SAME EXISTING OPERATOR WORKFLOW
After existing resend/replacement create returns/reconciles one Created parcel, existing dialog continues to its preview/selection/review/confirm step. Creation successful != scheduled; show truthful stage label until confirm. Lost create response first reconciles its original parcel; lost plan response reconciles same plan; never remint either occurrence just to proceed.
Existing Created unplanned linked parcel exposes the same planning step on its current card. No duplicate dialog/action owner, separate app/page, parallel direct API workflow or nested duplicate CTA.
Retain reviewed plan identity/selection through error, reload/remount/tab changes using existing durable-envelope/recovery patterns. Distinguish original create key from this parcel's schedule-confirm occurrence; never change already-recorded create intent to add scheduling.
After success refresh owning read models to show stored plan, never local-only optimistic invented dates. If source is stock-pending without parcel, retain existing damaged decision/retry route; planning begins only once real allocation is reconciled.
Existing 268 tab/ordinal/lineage behavior, user positive functional feedback, existing Toast/MessageBox, read-only completed history, mobile44px controls and finance-free shipping page remain intact.
Configured languages from DB registry, no FA/EN registry hardcode; localized text and existing RTL/LTR conventions.

DATA / DEPLOYMENT BOUNDARIES
ONE canonical tooba_alpha5432 SingleStore/store-alpha FE3000->Host5088. No extra/disposable DB, Docker instance, 15432/15433, connection/edition/port/appsettings/keys/CapturedPreview changes.
No new durable live demo orders/parcels/plans/reservations/settings or inventory mutations by worker. Current user orders, completed Returned-source/Delivered-resend trial and shipping draft GET/read-only. No reset/reseed/delete/restore, cleanup, manual business SQL or prior266 cleanup repetition. Preserve prior audit gaps/backups.
Minimal additive Order migration authorized: private backup, repository MigrationRunner plan/apply once, verify migration id/applied/pending state; no manual DDL/destructive change. Restart/publish only changed services once after successful checks, verify health/ready and served FE BUILD_ID.
Behavioral proof uses real owning code with controlled ports; relational plan persistence/concurrency proof uses task-owned transactionally rolled-back test data on the canonical nonproduction DB through authorized existing test/owning creation seams. Do not modify user rows or leave a demo dataset. No cross-process live write disguised as rollback; if rollback cannot encompass a proof, report the exact limitation and do not perform that business mutation.
No provider/waybill booking, real money, refund/wallet/settlement or notification send. Internal tracking remains internal only. Do not repeat payment/quantity/Returns architecture audit. Preserve retained healthy hold on parcel cancellation and original consumed reservation untouched.

FOCUSED VERIFICATION — PLAN ONCE, RUN ONLY THESE AFFECTED CHECKS
Before execution list precise new/affected test classes; do NOT invoke unfiltered Shipping/Order/Fulfillment/Composition suites or legacy disposable-DB matrices.
Use grouped targeted cases proving:
1 Source receipt versus replacement-hold readiness; stock pending no plan; no duplicate preparation allowance, closed processing/carrier/customer day, cutoff and independent zones; elapsed original date offers new feasible deliberate selection.
2 Same actual created parcel becomes scheduled; stored plan survives new context/reload; original commitment/first dispatch untouched; partial/mixed source binding exact; missing legacy capture localized.
3 Review-to-submit expiry, wrong actor/checkout, wrong source/quantity/binding, missing/released hold refuse with no plan/consume; stored exact replay survives elapsed time without changing promise.
4 Same/different key and same/changed intent behavior; two independent relational contexts race one parcel -> one immutable winner; response loss before/after Order commit; no duplicate parcel/hold or plan; concurrent cancel remains inert.
5 Actual single/bulk/consolidated dispatch guard cases prove zero stock consume/state/event on any unplanned linked member and lawful planned path, retaining payment/package guards. Test guarded command behavior, not just helper/source text.
6 Queue own-plan projection/full-set timing and missing-state counts, first-dispatch unchanged, no sibling lending, no completed-history planning action.
7 Mounted existing dialog: creation/recovery->plan review->confirm, retained envelope on failure/remount/tab switch, lost confirm response reconciliation, same Created card entry, no stock-pending fake calendar. Typecheck/build and affected route/feature suite once; no unrelated UI sweep.
Run targeted backend tests/build once after final code plus required targeted feature checks. Rerun only failing affected checks after correction; no broad baselines or certification. Test doubles/InMemory do not establish relational durability; mark evidence honestly.
Read-only served real operations/queue at390/1440 with DOM/network/text (no screenshots), confirm BUILD_ID/health once. Don't mutate current orders to prove buttons. If no eligible current unplanned parcel exists say so; controlled tests prove write path, current live page may be history-only.
Do not label SQL-seeded Paid test rows coherent end-to-end checkout payment. No all-shipping/real-carrier/user acceptance claim.

HANDOFF / RECOVERY SOT
Persist exact received file docs/ai/tasks/TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R2/TASK.md.
Evidence docs/architecture/evidence/TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R2/: TASK.md, RESULT.md, RECEIPTS.md, result-contract.txt, ownership-and-plan.md, readiness-and-feasibility-proof.md, replay-and-dispatch-guard-proof.md, user-review.md.
Record chosen two-stage invariant, exact owning types/requests/error mapping, readiness provenance, captured-vs-current eligibility scope, schema/migration/backup refs, transaction/race/replay facts, focused commands/counts, actual deployment/build, changed paths and limits. Compact text, no huge logs/screenshots/secrets.
Persist R1 Architect checkpoint above at its ARCHITECT-REVIEW.md. Update top AGENTS/START-HERE and plan pointer: R2 outcome awaiting review; 271 NOT ISSUED. Clearly state latest accepted R1, this implementation/repair, active or completed lifecycle, remaining blockers/next task NONE. Preserve old evidence/audit history.
ONE real existing review URL; state truthfully whether it is an eligible unplanned example or read-only completed history. No manufactured URL claim. No new live example creation to satisfy handoff.
Normal commit/push/fetch; exact Expected/actual base, Code/Evidence/final HEAD SHAs and final parity. No force/amend/reset/rebase/stash, unrelated scripts or credential artifacts. Compatible incoming docs normal merge only, unsafe code divergence -> RECOVERY_CONFLICT.

SUCCESS / RESULT CONTRACT
PASS only if preview + durable confirmation/replay + server dispatch guards + own-plan queue/detail + existing workflow are implemented with the stated focused proof. A table/helper/source-only design is PARTIAL, never successful completion.
Canonical ONE final Result per protocol; no test/probe/placeholder posts:
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Actual Task-ID, Parent-Task, Channel, Worker-Id, Agent-Type, Program, Mode, Bridge-Task-Row.
Status: actual PASS / PARTIAL / BLOCKED / RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 15b0d91488637631dbbc7e466212dd226b26f919
Base-SHA: actual verified work base
Code-SHA: actual implementation commit or NONE
Evidence-SHA: actual evidence commit or NONE
automaticNextImplementationTask: NONE
Body: root/decision; two-stage allocation-plan invariant; readiness/binding; implementation; stored-vs-read facts; replay/concurrency/dispatch guard proof; migration/deploy/data boundaries; checks/results; actual changed files/elapsed/finalHEAD/origin parity; one review URL and limitations.
Use actual values, no unresolved placeholders. End exactly END_TOOBA_WORKER_RESULT then END_TOOBA_TASK, nothing follows.
Worker PASS != Architect ACCEPT. No user visual/functional, provider/financial/customer-consent or universal shipping acceptance.
After final Result and completion return IDLE and STOP; no next task/repair/271, no idle heartbeat/poll/wait loop.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK