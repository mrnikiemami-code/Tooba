PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R4
Parent-Task: TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R3
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — repair exact package execution and prove relational delivery
Mode: CLOSE_PACKAGE_IDENTITY_OWNER_REPLAY_AND_EXECUTE_POSTGRES_MATRIX
Expected-Base-SHA: 0bb72adf972097eb50d846b43ded1e50f4b14863
Status: ISSUED
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

1. Binding delivery and priority

Continue same delivery, no scope reset. User authorizes sequential corrections until usable handoff; one worker at a time then independent Architect review. Read AGENTS/module instructions and full parent tasks. Preserve this task verbatim.

Canonical SarvNewVer FE3000→Host5088 / SingleStore/store-alpha/tooba_alpha only. Preserve Marketplace owners, current UI layout, user data/cart/orders/tabs/unsaved work and DataProtection keys. No screenshots, credentials request, Tipax HTTP, real shipment/order fulfillment/payment/cancellation.
Authorized Host API read in R3 proves pending-payment/manual capability; preserve that result. Browser acceptance still requires normal BFF rendering, not only401/public200.

Priority is correct existing bridge plus an EXECUTED PostgreSQL working path. Do not add another interface-only layer, rebuild repeatedly or stop merely after provisioning a container.

2. Source-confirmed defects in R3 CreateAsync

File ShippingCarrierShipmentDirectory.cs at239e40a0:
A. openCreate lookup by checkout/group happens before ShippingCarrierOwnerScopeMatching/readiness. It returns Pending/Unknown operation without proving facts owner matches caller. Check owner before EVERY replay/read result; include exact owner in operation query and deny mismatch without leaking receipt/config/tracking.
B. CreateShipmentAsync passes actorUserId:Guid.Empty. Propagate authenticated actor through the authorized command context; never invent a constant actor. Audit must name real principal.
C. Allocated shipment discovered by partial membership matching and fallback to ANY latest Created shipment; null ShippingGroupId accepted. Return/expose exact created shipment identity from Fulfillment owning contract. Verify exact group + exact item quantities; no First/Last/latest or foreign fallback.
D. Fulfillment allocation occurs BEFORE Shipping idempotent intent. Crash/config change/DB failure can strand quantity without operation linkage; two concurrent requests may allocate different packages and bypass one-operation retry semantics.
E. Pending saved separately before execution input snapshot. Crash can leave operation reserved with missing durable input; test this failure boundary.
F. resolvedPackage is still group-level accepted/completed data despite partial selectedLines. Multiple partial packages must not repeat full group weight/value/collect.
G. catch-all around fulfillment allocation hides cancellation, unexpected failures and exact refusal. Preserve caller cancellation and typed business failures, do not turn storage/unexpected failure into misleading not_eligible.

3. Repair within existing owners

Use existing Fulfillment shipment/item quantity authority. Do not create a parallel allocation engine.

Stable explicit package/dispatch identity with exact accepted group, item quantities and physical package facts. Client locator is untrusted; server validates own group/owner.
Establish durable idempotent execution intent BEFORE contested allocation/provider mutation. Link exact Fulfillment shipment and Shipping operation; retry/recovery after any local failure resumes/reconciles same intent.
Implement atomic local transaction when lawful under existing architecture, or explicit durable recoverable state transitions if cross-module transaction not available. Never hold DB transaction over provider HTTP.
Pending intent + complete immutable execution snapshot persisted atomically BEFORE external call. Include exact package lines, sender AND recipient, provider/account/config/service, package weight/value/collect/currency and routing; no credentials.
Provider response ambiguity remains UNKNOWN, reconcile without blind resend. Same request identity+different payload typed conflict.
Concurrency control at actual Fulfillment owner prevents manual/carrier double allocation; do not rely on read-before-write sum.
Separate per-package real physical weight/dimensions from per-line financial value. Use validated declared-value/collect allocation consistent with accepted facts; no guessed proportional dimensions or repeating whole-group collect on each package.
Shipment replay after owner/config change must not leak or mutate foreign data. Operation state and account permission gates remain uniform for capabilities/list/get/mutations.
Keep customer method/account/service/financial history immutable. No recapture or pricing change to repair an old committed order.
Keep UI compact in same group/shipment area; readable package identities/products, no raw IDs/numeric service codes or detached form. Only genuinely missing physical facts editable; use repo frontend skill/design components.
4. PostgreSQL proof FIRST, not optional cleanup report

Provision ONE task-owned postgres16/17 container on verified free loopback15433 (or free alternate), fresh DB/user and private password. Assert identity/ownership before migrations. Use existing test infrastructure, extend narrowly where needed.
Normal migration chains for Order/Shipping/Fulfillment; this is required. R3 proved Docker provisioning works: unset env is not a blocker. Set actual test env privately. No real tooba_alpha in tests, no engine restart or user process disruption.

Build one real integration fixture: real contexts/stores/facts reader/readiness/directory/Fulfillment adapter; only provider boundary CONTROLLED. Add missing fixture prerequisites through owning test/domain paths, not production seed changes. RUN the tests before cleanup.
Required executable cases:

Real accepted group→physical package completion→sender completion→capability ready→exact package execution; immutable financial evidence unchanged.
Package completion with unpacked/exhausted/cancelled facts stays denied; foreign owner replay denied.
TWO packages of SAME group with distinct line quantities/weight/value; provider receives each package's own data exactly once, remaining quantities correct.
Concurrent same-intent request→one allocation/operation/outbound; concurrent competing manual/carrier allocation→no overship.
Provider callback queries separate DB context and sees full pending snapshot BEFORE HTTP.
Inject local failure after intent/before allocation, after allocation/before link and before provider. Retry/recovery uses same exact package, no stranded/double allocations and no unintended HTTP.
Timeout→UNKNOWN; replay no second create; lawful track reconciles same intent.
Wrong shipment/group IDs, mismatched quantity and payload fail closed. No arbitrary latest fallback.
Real endpoint handler/validator body with authenticated principal through execution; audit actor matches actual test actor.
Unauthorized/disabled account/state refusals cause zero external calls.

Unit tests/source checks/InMemory do not replace this matrix. If real migration/test fails fix owning defect without widening scope or weakening assertions. Record concrete error if bounded correction cannot resolve.
Stop/remove ONLY own container after executed proof; verify port closed.

5. Component and canonical checks

Use actual shared component interactions (not decoder-only):

Completion→ready→execution→tracked/unknown state.
Typed network error/retry preserves input and does not trigger status-driven refetch cascade.
Same-tick lock and stale-response guard.
Original Manual+PendingPayment order shows existing normal UI/payment block, no carrier form.
No screenshot. Use existing sanctioned admin login/BFF in independent task session, no fake identity/role change or credential search.
UI must preserve page hierarchy, shared design system and minimal concise texts.

Deploy only final corrected build to canonical3000/5088. Necessary additive migration authorized after private backup via official runner/readback. No reset/reseed/hand business SQL. Preserve existing local gates/Manual/Free/captured-preview behavior and keys. Verify authorized read-only current order plus runtime/source parity; do not mutate user order.

6. Completion discipline and recovery

Checkpoint within10 focused minutes: PostgreSQL fixture/migration status plus exact repair plan/estimate; checkpoint25: executed matrix counts and first real failure if any. One cumulative clock. Prioritize working integration before repeated publish/doc-polish.
R3 PARTIAL stays historical. Update AGENTS.md, both recovery TOPs and carrier plan with real current state; no all-complete claim from compile/unit counts.
Task:
docs/ai/tasks/TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R4/TASK.md
Evidence:
docs/architecture/evidence/TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R4/{TASK.md,RESULT.md,RECEIPTS.md}
Normal commit/push, no force/reset/amend/discard of user work. Reconcile advanced main safely.
Return full BRIDGE-WAKE-V1 envelope with actual Base/Code/Evidence SHA, exact stage, relational case ledger, migration/runtime identity, cleanup, actual elapsed and unmet items.
PASS requires executed relational mandatory paths + canonical deployment; otherwise PARTIAL with exact issue. Live Tipax remains deferred only, no account needed.
STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE
END_TOOBA_TASK