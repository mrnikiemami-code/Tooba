PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-FULL-OPERATIONS-076
Parent-Task: TB-SHIPPING-FULL-OPERATIONS-075
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-FULL-OPERATIONS
Mode: BOUNDED_OPERATIONAL_QUOTE_AND_CAPACITY_AUTHORITY
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 2c0b7e1adcd15c524ff3311334bd3df1a15e5b3b
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 minutes HARD

ARCHITECT RULING
Task 075 ACCEPT for Shipping-owned packaging and persisted Prepaid/CollectOnDelivery modes, but neither Checkout cutover nor COD financial settlement is implemented. This is task 6 of MAXIMUM 10 tasks (071–080); budget is not reset. Objective: make Shipping compute an explicit operationally eligible, auditable quote from existing validated service/option/destination, measured parcel weight and PHYSICAL shipment item count, weekly pickup/dispatch schedule, carrier freight, packaging, and payer mode. Do NOT call configured capacity enforced until an atomic claim is implemented. Respect separation of carrier dispatch availability vs customer delivery promise. No unapproved switch of legacy quote authority or money movement. User permits Development tooba_alpha disposable test data; use existing target only. HARD STOP 45 minutes. No repetitive build/restart/test loops or parallel DB.

PREFLIGHT — MANDATORY
BRIDGE-WAKE-V1: claim ONE Pending task on tooba-main, preserve received .task.md verbatim and UNSTAGED under docs/ai/tasks/. Read AGENTS.md; docs/ai/TOOBA-PIPELINE-PROTOCOL.md; Shipping 069–075 planning/evidence; current Shipping.Contracts Rates, Packaging, Scheduling; Shipping rate engine and IShippingRateQuoteLookup; IShippingShipmentCostLookup; Shipping Operating Schedule storage; read-only Fulfillment service/option and Geography seams. Inspect Order/Checkout/Payment code ONLY to understand existing ownership, do not edit them. Fetch origin/main: Expected-Base-SHA must equal current code base or be an ancestor through path-by-path proven nonoverlapping docs/bookkeeping commits. Conflicting source drift => RECOVERY_CONFLICT/STOP. Preserve unrelated dirty/untracked files, no reset/stash/rebase/force push. Positively establish Development host/store-alpha -> tenant-alpha/tooba_alpha before any write; no Production/staging.

MISSION A — EXPLICIT SHIPPING OPERATIONAL QUOTE (PRIMARY)

Compose existing Shipping quote/rate tier engine + packaging cost component + freight payer mode + explicit schedule availability evaluation into a NEW ADDITIVE Shipping.Contracts read-only seam. Do not rewrite existing IShippingRateQuoteLookup or IShippingShipmentCostLookup to change callers implicitly; reuse their logic and expose an auditable full response with distinct freight fee, packaging fee, total service expense, amount payable at Checkout, amount to collect on delivery, currency, matched rate policy/scope/tier IDs if available, schedule/window reason and configured/not-configured reason codes. Example: Prepaid => checkout portion includes freight+packaging; CollectOnDelivery => checkout portion is packaging only, freight is shown as a separate collect-on-delivery obligation. Zero missing packaging policy; free-freight waiver only applies to carrier freight. Fail closed on ambiguous policies, wrong currency, missing required measurements, missing/invalid service/option/destination; never return a success-shaped zero price on failure.
Input must include validated serviceCode, optional optionCode, canonical Geography destination IDs, optional measured WeightGrams long and physical ShipmentItemCount int, subtotal/currency ONLY when needed for existing threshold logic, and an explicit evaluation instant UTC. No fetching 'now' implicitly inside a deterministic domain evaluator. Do not derive shipping physical item count from cart lines or quantity. Existing tier specificity order option > destination depth > tier remains fixed; resolve coverage separately at service/destination.
Schedule semantics: evaluate an explicit requested carrier pickup/dispatch datetime in configured IANA timezone, ISO weekday, half-open [start,end) local time. Preserve no-schedule compatibility: no explicit schedule => label 'not configured' and do not reject legacy method. An INACTIVE configured schedule must have an explicit safe meaning (document and test). Never claim this is a promised delivery slot. Address DST invalid/ambiguous local times deterministically or fail closed with stable reason. If requested instant is outside active windows and scheduling is enabled for the new evaluator, report unavailable with stable code; do NOT alter old Checkout method listing/price.
Capacity: choose one bounded safe strategy after inspecting existing Order reservation lifecycle. If no atomic, idempotent shipment-slot reservation/cancellation can be implemented AND proven in 45 minutes, DO NOT FAKE ENFORCEMENT OR MARK IT COMPLETE. Keep capacity read-only 'configured only', expose unknown availability, document that commitment/booking cannot yet rely on the numeric cap. Never count Order rows by a naive non-atomic SELECT as enforcement. No cross-module DB reads and no distributed transaction simulation. Any capacity authority must be a separate later-approved design if this slice cannot deliver it.

MISSION B — FOCUSED API CONSUMER, IF SAFE
5. Prefer register a minimal read-only authorized ADMIN evaluation endpoint inside Shipping Endpoints and module-owned CQRS handler (ISender) that consumes the new seam with explicit DTO. No new public write endpoint, no customer price cutover; existing fulfillment.view permission for diagnostic read. Preserve Result/ApiResponseFactory, existing error handling, no Results.Json, no permission bypass. Implement only if A is robust, else deliver contracts/domain-focused work as PARTIAL. No front-end needed in 076; 077 owns UI/presentation integration.
6. Verify cases with smallest focused tests: Sunday 13:30 Asia/Tehran inside versus Sunday 14:00 outside; second window 16:30 inside; no schedule; inactive schedule; wrong timezone/DST edge if relevant; weight boundary 1999/2000 grams; count boundary 5/6 physical units; BOTH axes intersection; missing measurement distinct from unmatched tier; Prepaid fee split vs COD split; free freight + nonzero packaging; conflicting currency; zero packaging default; unknown/invalid identity; coverage rejection. Never invent a tariff or claim commercially valid price.
7. One authorized DEVELOPMENT HTTP read-only proof if endpoint exists and existing deployed Host can safely run new code. If a restart/build is genuinely needed, positively identify process and capture rollback; stop it intentionally once, rebuild once, restart once, test once. Do not probe an older running host then claim newly compiled route works. No scratch DB. Use existing synthetic rates only as clearly-labelled test data. Keep all new code Shipping-only.

ACCEPTANCE / STOP
PASS only when an additive operational quote seam composes the real owned inputs deterministically and exposes explicit freight/packaging/checkout/COD amounts, scoped schedule availability, test-proven compatibility and truthful capacity status, with focused tests and clean source changes; API live proof can be explicitly partial if endpoint was out of scope but not falsely claimed. If a primary computation or safe contract fails, PARTIAL. If capacity remains configuration-only, that is a KNOWN PROGRAM GAP for follow-up and must be called out, not concealed. No customer Checkout authority or COD settlement permitted. 45-minute HARD STOP, commit/push only authorized paths and publish exact Code-SHA, Evidence-SHA, elapsed, evidence and blockers. Worker PASS is not Architect ACCEPT.

ALLOWED PATHS
src/backend/Modules/Shipping/Tooba.Shipping.Contracts/**
src/backend/Modules/Shipping/Tooba.Shipping.Domain/**
src/backend/Modules/Shipping/Tooba.Shipping.Application/**
src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure/**
src/backend/Modules/Shipping/Tooba.Shipping.Endpoints/**
src/backend/Modules/Shipping/Tooba.Shipping.Tests/**
docs/planning/shipping/SHIPPING-FULL-OPERATIONS-076.md
docs/architecture/evidence/TB-SHIPPING-FULL-OPERATIONS-076/**
docs/ai/tasks/TB-SHIPPING-FULL-OPERATIONS-076.task.md (receipt only, NEVER stage)

FORBIDDEN
No Order/Payment/Settlement/Host or frontend changes, no direct cross-module database use, no raw Results.Json, no accidental Checkout cutover, no financial transaction/ledger, no falsely enforced capacity, no carrier schedule mislabelled customer delivery promise, no duplicate independent rate engine, no unapproved security/capability grant, no mass migration runner, no staging/production writes, no speculative broad tests or loops, no unrelated dirty edits, no automatic next implementation task.

RESULT CONTRACT — COMPLETE CANONICAL ENVELOPE ONLY
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-FULL-OPERATIONS-076
Parent-Task: TB-SHIPPING-FULL-OPERATIONS-075
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-FULL-OPERATIONS
Mode: BOUNDED_OPERATIONAL_QUOTE_AND_CAPACITY_AUTHORITY
Bridge-Task-Row: <actual Bridge row>
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 2c0b7e1adcd15c524ff3311334bd3df1a15e5b3b
Base-SHA: <actual>
Code-SHA: <actual or NONE>
Evidence-SHA: <actual or NONE>
automaticNextImplementationTask: NONE
<actual architecture, real component amounts, schedule/timezone rules, tier test outcomes, coverage/error behavior, capacity enforcement truth, Development target/API result, tests, modifications, elapsed, git push, blockers>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK