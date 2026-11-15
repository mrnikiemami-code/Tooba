PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R3
Parent-Task: TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R2
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: febeb27e36829cdceb1a8c6a3dcf4a9b0f22f659
Mode: REPAIR_EXACT_CAPTURE_BINDING_LIVE_HOLD_AND_IMMUTABLE_PLAN_RECOVERY
Track: SHIPPING_OPERATIONAL_TIMING
Title: Repair actual captured-group lookup, dispatch binding, replacement readiness and planning recovery
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

ONE BOUNDED REPAIR
Repair concrete R2 defects below. Preserve the two-stage physical allocation -> Order-confirmed plan design and completed history. NO new architecture, 271 filters, screenshot work, demo dataset, cleanup/refactor or baseline campaign.
ONE worker/active task. No delegation/subagents, concurrent task, historical agent resume or automatic next implementation. Return one final canonical Result then IDLE and STOP.
No screenshot request/capture/generation/attachment. No polling/timer/retry loops. Focused checks once after final change; rerun only affected failures after a concrete correction.

ARCHITECT REVIEW — R2 PARTIAL, NOT ACCEPTED
Independent source/evidence review at febeb27e36829cdceb1a8c6a3dcf4a9b0f22f659; actual implementation ce5d7f998ce8a3f391c416881afcfa5f0a5c951f and documentation febeb27e36829cdceb1a8c6a3dcf4a9b0f22f659.
Useful implemented base: owning plan table/reader, scheduling preview/confirm, shared guard call sites, projection and modal. Worker test/build/deploy claims are not Architect runtime verification.
Source-confirmed defects requiring this repair:
1 AdminOrderOperationsOrchestrator.ShipmentPlan.ResolvePlanInputsAsync builds ShippingAttemptSchedulingLookupRequest with binding.ShippingGroupId as GroupId. That is the ORDER group id. R1 requires captured payload.Groups[].GroupId, which is binding.AreaGroupId. They are distinct namespaces/identities. This blocks valid preview with GroupUnavailable. Unit mocks did not prove the real R1->Order seam.
2 FulfillmentDirectory.EnsureLinkedAttemptsPlannedAsync checks plan dictionary key/ShipmentId + AttemptKind ONLY. It does not verify checkout/seller/fulfillment/group/source/decision/hold/method/exact item fingerprint, although all are stored. Wrongly bound records must not authorize dispatch or appear scheduled.
3 ResolveReadinessAsync replacement branch trusts a projected decision status and matching reservation GUID; it NEVER reads Inventory's current hold. It sums parcel quantity instead of checking each line against the decision/source line. It can call a released/consumed/wrong-line hold "ready". IFulfillmentInventoryGateway.FindReplacementReservationAsync already exists; expose any needed narrow owning Contracts fact, do not bypass ownership.
4 admin-shipment-plan-modal.confirm reads retained.occurrenceKey but overwrites retained selection with the currently selected slot. After unknown/timeout/refusal it still permits another slot; read/load clears selection if old slot is no longer offered. That is NOT an immutable reviewed envelope or a valid retry of a potentially committed intent.
5 onParcelException in admin-order-items-shipping-panel returns completedPostDispatchOutcome after create/resolve + refresh; it does not hand the exact created/recovered parcel to planning. Modal is opened only through the separate card onParcelPlan click. The claimed create/recover -> scheduling continuation is not implemented.
6 The 2 relational tests use one db/context/transaction at a time, sequential store calls and a raw SQL duplicate insert. They establish stored-row/index facts, NOT a race between two independent application writers. Both tests silently return if connection is absent, falsely appearing passed without exercising storage.
Additional evidence limits: plan confirmation performs mutable LocateParcelAsync BEFORE existing-plan lookup, so lost-response reconciliation can fail after lifecycle advancement. BlockedPreview discards confirmed despite receiving it. Reviewed request carries only key/date/open/close, not the reviewed binding/source/timezone/calculation context. Close/reopen resets inFlightRef while an earlier request may still be pending.
DO NOT certify these behaviors based on existing green mocks.

MIGRATION AUDIT — NONREPEATABLE FAILURE
The R2 required PRE-APPLY backup was NOT taken. pre-apply-plan.json is not a database backup; post-apply dump cannot be renamed/described as pre-apply protection. Preserve that disclosure and the actual post-apply backup.
Do not roll back/reapply/drop/recreate the existing table or manufacture retroactive proof. No schema change is expected for this repair. If a minimal additive migration is genuinely required, FIRST take and successfully validate a fresh private pre-change backup, then runner plan/apply once; never apply before backup. Report the actual scope; no restore.
No "zero data change" blanket wording: distinguish applied schema/migration metadata, rolled-back test writes and untouched live business rows.

RECOVERY / REQUIRED READS
Fetch main normally, verify safe tree and Expected-Base-SHA present/ancestor of origin/main. Inspect incoming delta; compatible docs-only advancement declared, unsafe code divergence -> RECOVERY_CONFLICT. No force/amend/reset/rebase/stash or deletion of unrelated scratch files.
Read current AGENTS/START-HERE, canonical BRIDGE-WAKE-V1 protocol/controller, backend feature skill, frontend feature skill/FRONTEND-ARCHITECTURE-STANDARD, and relevant Shipping Admin/UX decision text. User screenshot prohibition overrides screenshot/image-review workflow.
Read R2 actual targeted source/evidence, R1 Contracts and lookup; don't repeat broad discovery or deferred findings audit. R1 stays ACCEPT only within its original captured-input seam.

REPAIR A — EXACT BINDING + REAL SEAM PROOF
Use binding.AreaGroupId for captured GroupId and binding.RateRowId within that group. Keep binding.ShippingGroupId as ORDER group in the saved plan/locator, never replace one identity globally with another.
Validate AcceptedShipmentGroupBinding belongs to located checkout/seller order/seller and exact Order group/method. Validate R1 returned snapshot/draft/revision/format/service/option/group/row against that trusted binding; no browser owner or current/latest fallback.
Add one connected application test using actual R1 ShippingAttemptSchedulingLookup + real captured payload/directory seam and deliberately DIFFERENT Order ShippingGroupId vs captured AreaGroupId. Real expected row duration/calendar must reach Order preview/confirmation; a mock returning any requested group cannot prove this.
Use existing IIdGenerator for new plan ids where repository conventions require it, rather than adding ad hoc identity authority.

REPAIR B — SAME BINDING IN DISPATCH AND READ PROJECTIONS
One shared binding comparison, owned at the narrow appropriate Contracts/application seam, must validate at least ShipmentId, CheckoutId, SellerOrderId, FulfillmentId, Order ShippingGroupId, attempt kind, source parcel id, replacement decision/hold ids, method identity and exact canonical line->quantity fingerprint against the owning actual unit/parcel.
Where service/capture identity is not present in Fulfillment, validate it against Order's trusted accepted binding through a narrow read; do not join tables across modules or claim a field checked when it wasn't. No dispatch-time scheduling/current-setting recalculation.
Fingerprint must represent exact domain quantity values/canonical order; avoid silent rounding collisions or array-order identity. Reuse one definition across readers/guards; no duplicated subtly different formats.
Fail closed on missing/duplicate/conflicting facts. Keep all-member package preflight BEFORE any consume/state/event, preserve original first-dispatch and payment/source/package guards. If a public bulk operation processes several parcels, preflight its full affected set before the first effect; trace the actual path and test it rather than assuming helper inheritance.
Order actions, queue own-plan projection and detail use the same binding validity. A wrong-source plan cannot suppress scheduling, expose dispatch or lend a promise to a sibling/package.
No optional reader bypass; unavailable owning read is a truthful blocker, not authorization.

REPAIR C — INVENTORY-OWNED CURRENT REPLACEMENT READINESS
Use owning Fulfillment -> Inventory Contracts/port to validate current actual hold for the exact decision/source line/quantity. Existing gateway FindReplacementReservationAsync is an entry point; add only the minimal required read facts if its current DTO lacks purpose/binding evidence.
Order must consume a Fulfillment-owned eligibility/readiness contract or narrow existing adapter; no Order -> Fulfillment.Application/Infrastructure dependency, no Inventory DbContext access or re-reservation.
Require hold exists and is Held/not already released/consumed for NEW plan confirmation, bound to the decision's original supply and required exact line/quantity. Validate decision id/source receipt/order line, parcel allocations and ReplacementReservationId coherently; wrong-line sums are insufficient.
Read-only preview must not reserve/commit/release. No hold/parcel creation just to prove readiness, no fabricated availability or timestamp. Preserve actual readiness provenance; a timestamp is never substitute for lawfulness. Source evidence/review time anchors and one preparation/transfer allowance remain unchanged.
First authorize and distinguish new confirmation from authorized immutable-history replay. Replay of an already recorded exact plan returns stored fact without re-proposing its promise, while still respecting access; history read never re-enables dispatch or makes completed/cancelled parcel plannable.
No positive dispatch/stock action from a historical replay. No arbitrary bypass of payment/current-state checks for a new command.

REPAIR D — FROZEN REVIEW CONTEXT / UNCERTAINTY / LIFECYCLE RECONCILIATION
Persist a complete immutable reviewed envelope BEFORE sending: actual scope/parcel, key, selected customer date/window/timezone, server-issued reviewed binding/source context and calculation/policy context needed to detect changed meaning. The server revalidates trusted facts; client context is comparison evidence, not permission.
Use a narrow server-derived context fingerprint/version over the existing reviewed facts (or established repository review token pattern); don't invent a generic token framework. Confirmation must refuse if source/binding/timezone/calculation context changed since the user's review, even if date/open/close strings happen to match.
Same key changed intent -> exact typed conflict, zero write. Do not let the modal overwrite the previously submitted envelope under that key. Unknown/timeout/5xx retains original request byte-for-byte and retries/reconciles that EXACT request, even if old selection no longer appears in a freshly generated calendar.
Read stored exact outcome before NEW-plan feasibility/state gates after authorization and exact checkout/parcel ownership association. Support read-only recovery after dispatch/cancel without offering actions/recalculating/reconfirming. A failed fresh preview must not hide an already recorded confirmed plan.
If authoritative outcome proves no plan was written and operator wants a new slot after deterministic expiry, make a deliberate new-review transition with the previous outcome preserved; never automatically remint on unknown status or erase evidence. If outcome is unknown, no new intent.
A retained envelope from another viewer/parcel is not reusable. Clear only after exact authoritative resolution and only if it is still the same attempt (protect late responses).
Prevent same-tick, close/reopen, remount or changed target from releasing a pending single-flight guard or applying old response to another surface. No polling; bounded explicit retry/reconcile reads.
Preserve actor authorization on every replay, do not leak another checkout's plan. Typed refusals/history responses keep existing canonical API/error conventions.

REPAIR E — EXACT CREATE/RECOVER -> PLAN HANDOFF
Extend current shared post-dispatch outcome plumbing only enough to carry the actual new/recovered Created parcel id from owning result/reconciliation into planning. After the current exception review closes, open the existing planning modal for THAT parcel (no overlapping dialogs).
Resend success and replacement-with-real-stock success continue to review/confirm; pending-stock decision/referral/completed/cancelled replay do NOT open a fake calendar.
Lost create response must recover original exact parcel via existing occurrence reconciliation first. Never pick latest/first linked parcel, match by status alone, remint create key, create duplicate, or infer target from a collection after refresh.
If the owning response already carries required identity use it; otherwise a small owning result-contract enrichment is authorized, preserving replay semantics. No new generic workflow/page/wizard.
Retain existing active unplanned card entry, 268 tabs/ordinal/lineage, one action owner, Toast/MessageBox, readable Persian copy and shared date/accepted-zone/LTR format.
Avoid unrelated visual polish or broad file moves; no reopening 267/268/269.

FOCUSED PROOF — NO WIDE SUITES
Repair/add meaningful cases in existing task-specific tests:
1 Connected actual R1->Order lookup with distinct Order/captured-group ids and exact wrong seller/group/row refusals.
2 Plan has SAME ShipmentId/kind but wrong checkout/seller/unit/group/source/decision/hold/method/fingerprint -> actual dispatch command refuses with zero consume/state/event; projection agrees. Package includes a wrongly bound member -> whole package no effects. Trace/test real bulk all-member boundary if present.
3 Actual replacement hold read: Held exact positive, absent/released/consumed/wrong-purpose/line/quantity negative; preview/confirm zero inventory writes. Avoid mocks merely echoing decision ids.
4 Preview A then context changed but same date/time -> refusal; exact same stored replay after time/state advancement returns history only; actor-denied/cross-checkout refusal still enforced.
5 Mounted form: unknown response then user changes slot cannot overwrite/send a different envelope; expired old option still reconciles original intent; closed/reopened/different-target late response cannot clear/change a newer attempt; deterministic no-write -> deliberate new review.
6 Mounted ACTUAL panel integration: returned-source create/recovered create -> plan modal of exact child; replacement with stock -> exact child; stock pending/referral/completed replay -> no calendar; existing card still works. A standalone modal test is not integration proof.
7 Persistence: no silent success if relational connection absent; use explicit supported skip/not-run reporting or fail required proof. Existing sequential immutable/index tests remain labelled exactly that. Exercise store duplicate exception/re-read handling with the actual expected unique constraint, not "every 23505".
Do NOT claim true two-writer concurrency from sequential single-context/raw duplicate statements. If genuine two-context race can be proved with task-owned rolled-back writes under protected-data constraints, run it once with deterministic coordination; otherwise explicitly retain the narrow unproven claim, do not invent it or create durable demo rows.
Run only named affected task test classes / FE mount runner (tsx --tsconfig tsconfig.mount-tests.json --test), affected compile/typecheck/build and applicable targeted feature checks once after final code. No unfiltered Shipping/Order/Fulfillment/Composition suites, legacy DB matrix, repeated baseline/stash/guard sweeps or stress harness.
No independent runtime/tests by Architect claimed; worker must distinguish unit, connected-seam, relational store/index, actual command and mounted integration evidence.

ENVIRONMENT / DATA / DEPLOY
ONE canonical tooba_alpha5432 SingleStore/store-alpha, FE3000->Host5088. No extra DB/Docker/15432/15433, edition/port/connection/appsettings/key/CapturedPreview change.
Current user orders/draft and Delivered linked history read-only. No new durable live demo orders/parcels/plans/holds/settings, manual business SQL, reset/reseed/delete/restore/cleanup or provider/money/notification action.
Existing migration/table retained; no repeat apply or rollback. Any relational task-owned plan data must be inside rollback and proven absent afterward; not user records. No cleanup script to erase unrelated data.
Publish/restart only changed services once after checks; verify health/ready, actual FE BUILD_ID/chunk once and read-only current pages with DOM/text/network at390/1440 without screenshots. No new example needed; absence of eligible current parcel remains honest.
If future schema addition needed, PRE-change validated backup is mandatory before any apply; stopping before unsafe migration is correct. Never apply then retrospectively describe it as protected.
Preserve original paid-order hold retention and consumed-reservation history, quantity/case/payment/package laws, 255 parked/Tipax deferred.

HANDOFF / SOURCE OF TRUTH
Persist exact TASK docs/ai/tasks/TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R3/TASK.md.
Evidence docs/architecture/evidence/TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R3/: TASK.md, RESULT.md, RECEIPTS.md, result-contract.txt, exact-binding-proof.md, recovery-and-connected-workflow-proof.md, user-review.md.
Persist this R2 PARTIAL Architect review at R2/ARCHITECT-REVIEW.md, including missing pre-apply backup and exact relational proof limits. Preserve original worker reports as history, append corrections rather than rewrite them.
Top AGENTS/START-HERE and shipping plan pointer: R1 latest accepted seam; R2 PARTIAL; R3 actual result awaiting review; active/complete repair truthfully stated; 271 NOT ISSUED; next implementation NONE. No giant historical-document rewrite.
Record exact code/evidence/final SHAs, named checks/counts, current data/schema scope, unchanged business rows, real blockers and one existing read-only review URL explicitly marked history-only if no eligible parcel.
Normal commit/push/fetch parity; no force/amend/reset/rebase/stash/unrelated scratch files/secrets. Incoming compatible docs only normal merge with declared base; unsafe code divergence -> RECOVERY_CONFLICT.

RESULT CONTRACT / STOP
ONE complete final Result with canonical envelope:
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Actual Task-ID, Parent-Task, Channel, Worker-Id, Agent-Type, Program, Mode and Bridge-Task-Row.
Status: actual PASS / PARTIAL / BLOCKED / RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: febeb27e36829cdceb1a8c6a3dcf4a9b0f22f659
Base-SHA: actual verified work base
Code-SHA: actual implementation commit or NONE
Evidence-SHA: actual evidence commit or NONE
automaticNextImplementationTask: NONE
Body: each confirmed defect and repair; connected seams/actual guard/recovery/workflow proof; persistence limits and migration audit; named checks/deployment; data boundaries; changed paths/elapsed/final parity; review URL and remaining limitations.
Use real field values, no unresolved placeholders, no duplicate protocol marker at tail; END_TOOBA_WORKER_RESULT then END_TOOBA_TASK, nothing follows.
No placeholder/probe/extra Result POST, Worker PASS != Architect ACCEPT. No universal/user/provider/financial acceptance. PASS must not hide unimplemented listed repair.
Complete Bridge lifecycle then IDLE and STOP. No automatic repair/new task/271/wait/poll loop.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK