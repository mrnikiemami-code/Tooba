PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-FULL-OPERATIONS-079
Parent-Task: TB-SHIPPING-FULL-OPERATIONS-078
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-FULL-OPERATIONS
Mode: BOUNDED_REAL_CHECKOUT_INTEGRATION_READINESS_AND_CAPACITY_DECISION
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 4c7c565d1dca40ca22418e42a35828dcb0028749
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 minutes HARD

ARCHITECT RULING
078 ACCEPT. This is implementation task 9 of the user's STRICT MAXIMUM 10 tasks (071–080). The
program was requested to COMPLETE practical shipping, not merely build an admin simulation. Existing
capabilities: Geography destination identities; Fulfillment service/option identities; Shipping
coverage, service/option/geography rate policies with half-open weight/count tiers, Sunday/hour
operating windows, packaging fee, persisted prepaid vs collect-on-delivery modes, diagnostic
operational quote; Order retains legacy checkout pricing authority; capacity is only configured. This
task must establish the real, safe integration path and a credible decision about remaining commitment
constraints before final 080. DO NOT claim commercial launch or switch paid-order prices
automatically. Do not pretend a capacity number is enforceable. Only one task, no automatic
continuation.

PREFLIGHT
Claim ONE pending BRIDGE-WAKE-V1 task on tooba-main and preserve the exact receipt UNSTAGED at
docs/ai/tasks/TB-SHIPPING-FULL-OPERATIONS-079.task.md. Read AGENTS.md,
docs/ai/TOOBA-PIPELINE-PROTOCOL.md, architecture/Shipping 068–078 evidence and decisions, current
ShippingOperationalQuote Contracts/application/infra, Order storefront shipping
projection/quote/checkout/payment handoff, and Fulfillment packing/quantity information. Read-only
inspection outside allowed paths is OK; edits ONLY permitted paths. Fetch origin/main: expected SHA
must match or be ancestor through explicitly proved nonoverlapping result/evidence-only commits;
overlapping source drift => RECOVERY_CONFLICT/STOP. Preserve unrelated dirty/untracked paths and
stashes; no reset/stash/rebase/force-push. Before writes identify Development GET
/__platform-commerce dev-singlestore store-alpha tenant-alpha -> tooba_alpha; NEVER
production/staging. No scratch databases, migration runner or uncontrolled restarts.

MISSION A — REAL STOREFRONT READ-ONLY SHADOW OF OPERATIONAL QUOTE, PRIMARY

Trace actual existing storefront shipping projection and Order ownership; add only a read-only,
fail-isolated operational shadow observation that can compare the real legacy customer quote to the
new Shipping operational quote when a request has verified destination, service+option, currency,
subtotal and REAL parcel measurements. Reuse existing ShadowEnabled/PrerequisitesConfirmed feature
gate where feasible; if the existing gate is scoped to the earlier simple-rate shadow, introduce an
explicitly separately disabled new gate rather than broadening its meaning. Default OFF. Preserve
100% identical legacy storefront response shape and bytes when gate OFF and when ON. No paid order
mutation and no consumer-facing monetary or delivery promise change. An operational
quote/availability failure must not change legacy checkout behavior. Prefer integration through
existing Contracts-only seam and MediatR ISender, no direct foreign DbContext, Host business logic,
Results.Json or privilege bypass.
Physical shipment weight/count MUST have a trusted provenance. NEVER manufacture kilograms from cart
subtotal, line count, product count alone or user-submitted arbitrary values; if shipment packing
aggregation/trusted weight is unavailable, report MeasurementUnavailable explicitly and SKIP
tier-conditioned comparison, not fabricated parity. Don't claim comparison for tiered policy if input
cannot be obtained reliably. Distinguish carrier pickup availability from customer delivery promise.
Payer split is a FACT; no COD Payment/Settlement entry is authorized. Never insert packaging into
live checkout amount, even for Prepaid, without human pricing cutover approval.
Capture an actionable structured observation: service/option/canonical destination; legacy
freight/eligibility; new freight/packaging/payer/checkout-due/COD-due, schedule result, missing
measurement and reason codes, WITHOUT leaking sensitive personal data or flooding logs. Ensure no
persisted customer PII, no production logging enabled by default, no globally activated shadow, no
duplicate public evaluation query with invented shipping identities.
If the existing Order read path cannot consume the contract without larger changes than 45 minutes,
implement a bounded Contracts-only read adapter or focused integration harness and mark PARTIAL with
exact owner and missing source path. Stop rather than building a parallel shipping price path.

MISSION B — CAPACITY & COMMERCIAL CUTOFF DECISION, REQUIRED
5. Inspect authoritative existing Order placement/cancellation state transitions and the allowed
shipment promise lifecycle. Determine whether an atomic, idempotent per-window claim/release (unique
order/window relation, transactional bound, expiration and replay strategy) can be safely added. If
yes ONLY when objectively bounded and appropriate to the owner, implement a minimal complete mechanism
with concurrency proof and cancellation/replay. If not, DO NOT partially fake capacity enforcement;
document explicit FAIL gate: capacity remains CONFIGURATION ONLY, cannot sell guaranteed booking
windows. Current shipping schedules describe carrier collection/dispatch windows, NOT customer slot
reservations. Do not turn a carrier operating schedule into promised delivery availability.
6. Document a precise cutover checklist: destination/option/weight-count data provenance,
fee+currency parity, unpaid versus COD ledgers and Payment/Settlement owner, customer-facing pickup vs
promise, merchant-approved tariffs/destinations, schedule gating/absence/inactive behavior, capacity
enforcement or explicit out-of-scope decision, and flag safe rollback. Explicitly distinguish
functional technical readiness from commercial acceptance.

MISSION C — FOCUSED DEVELOPMENT PARITY, NO DATA DAMAGE
7. On existing positively verified Development host and tooba_alpha, use a real stored service/option
and cart projection for OFF/ON byte-identical read-only shadow when A implemented. Get at least one
charged packaging + CollectOnDelivery case and one active tier boundary 1999g/2000g through a BOUNDED
disposable synthetic fixture only if safely possible using authorized admin routes. Mark any
measurement or input gap as NOT PROVEN. Ledger and deactivate every task-owned fixture. Preserve
current 068/069 legacy parity fixture and 071–078 records. No output changes to user checkout and no
live money collection.
8. Run focused tests for only changed code; one real Development probe when safe. No full-repo test
loops, no blind Host restarts, no 12-minute overrun. At minute 45 HARD STOP with PASS only for
objectively completed primary shadow+relevant matrix, PARTIAL for unproven/blocked. Do not exaggerate
scope. Document actual elapsed and Git SHA. Task 080 remains reserved for final reconciliation and
release gate and is NOT preauthorized.

ALLOWED PATHS
src/backend/Modules/Shipping/Tooba.Shipping.Contracts/**
src/backend/Modules/Shipping/Tooba.Shipping.Application/**
src/backend/Modules/Shipping/Tooba.Shipping.Domain/**
src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure/**
src/backend/Modules/Shipping/Tooba.Shipping.Tests/**
src/backend/Modules/Order/** (ONLY existing storefront SHIPPING observation/projection and focused
tests, NEVER checkout-paid-price/order mutation)
docs/planning/shipping/SHIPPING-FULL-OPERATIONS-079.md
docs/architecture/evidence/TB-SHIPPING-FULL-OPERATIONS-079/**
docs/ai/tasks/TB-SHIPPING-FULL-OPERATIONS-079.task.md (Bridge receipt only, NEVER stage)

FORBIDDEN
No customer shipping price/checkout cutover, no arbitrary carrier rate/packaging fee as approved
truth, no Order/Payment/Settlement financial transactions, no public admin routes, no new privileges,
no direct cross-module DB access, no Host business authority, no Results.Json, no fake weight or item
count or capacity enforcement, no guessed delivery promises, no staging/production writes, no parallel
database, no broad restart or tests, no unauthorized other module edits, no automatic 080.

ACCEPTANCE / STOP
Report specifically: legacy Checkout bytes OFF/ON; condition-tier measurement provenance and explicit
missing cases; payer split and packaging observation, no price change; schedule/operational result
behavior; whether atomic capacity is implemented or NOT; commercial gates with owner; tests; actual
elapsed <=45 minutes; changed files, git push, verified SHA and fixture cleanup. PASS/ PARTIAL/
BLOCKED/ RECOVERY_CONFLICT as truthful. Worker PASS is NOT Architect ACCEPT.

RESULT CONTRACT — CANONICAL BRIDGE-WAKE-V1
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-FULL-OPERATIONS-079
Parent-Task: TB-SHIPPING-FULL-OPERATIONS-078
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-FULL-OPERATIONS
Mode: BOUNDED_REAL_CHECKOUT_INTEGRATION_READINESS_AND_CAPACITY_DECISION
Bridge-Task-Row: <actual Bridge row>
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 4c7c565d1dca40ca22418e42a35828dcb0028749
Base-SHA: <actual>
Code-SHA: <actual or NONE>
Evidence-SHA: <actual or NONE>
automaticNextImplementationTask: NONE
<actual implementation and authority; parity bytes; measurement provenance; tier/packaging/COD
evidence; schedule/capacity truth; commercial gates; Development target and cleanup; tests; actual
elapsed; touched paths; commit/push; blockers>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
