PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-082
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-081
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-TABS
Mode: BOUNDED_FULFILLMENT_ONE_LEVEL_CATALOG_FOUNDATION
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 0d83659238f9a1b668c6f61bb7d505192a2effc7
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD

ARCHITECT DECISIONS — LOCKED FOR THIS TASK
B1 = Option B: Fulfillment owns a new additive one-level shipping-method catalog/table. Each merchant-visible method is independently named/activatable and has a STABLE public method code. Existing Fulfillment service and service-option rows remain intact; provider grouping is hidden internal metadata, NEVER another merchant configuration level. Existing persisted method codes remain byte-identical: post:standard; post:express (inactive); tipax:standard; tipax:express; store_courier; in_person; snapp_courier (inactive). The legacy (serviceCode, optionCode?) binding is retained internally for compatibility, including nullable option fallback; do NOT interpret a service-level fallback policy as another sellable method. Existing option IDs are unstable across UpdateAsync; NEVER key new identity on option UUID.
B2 = new method codes must fit <=32 characters (narrowest known paid checkout shipping_method_code varchar(32)); DO NOT widen/alter order/checkout tables or rewrite historical cart/order/shipment codes in 082. Existing codes retained unchanged. Prevent case-insensitive collisions, forbidden/ambiguous separators and duplicate mappings; preserve legacy composite colon codes already in use. Validate against concrete legacy code semantics.
No commercial pricing cutover: legacy Checkout and paid snapshots remain untouched, and Tooba:ShippingRateAuthority / Tooba:ShippingOperationalShadow remain OFF. New Tooba:ShippingOneLevelCatalog gate is DEFAULT OFF by absence, scoped solely to future catalog-read/UI path; this task may define configuration/contract, but MUST NOT activate it or change current storefront behavior. Neither configured capacity nor COD quotation is a reservation or settlement.

PREFLIGHT — HARD STOP ON CONFLICT

Claim exactly one Pending task on tooba-main under BRIDGE-WAKE-V1; keep received docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-082.task.md UNSTAGED. Read AGENTS.md, docs/ai/TOOBA-PIPELINE-PROTOCOL.md, docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-081.md, latest master recovery checkpoint, existing Fulfillment shipping catalog entities, DbContext, EF migration conventions, contracts, and their focused tests.
Fetch origin/main and verify Expected-Base-SHA. Proven nonoverlapping bookkeeping drift can be documented, but any overlapping source drift => RECOVERY_CONFLICT/STOP. Preserve ALL existing dirty/untracked paths; no reset/stash/rebase/force push. No unexpected Host/database restart or production/staging access.
Before any database interaction verify target and provenance; prefer isolated test DB/EF migration generation and focused tests. NO mutation of live Development tooba_alpha under this task. Do not seed live rates or grants.

MISSION — FULFILLMENT ONLY, ADDITIVE FOUNDATION
A. Implement the smallest coherent Fulfillment-owned persistent one-level method catalog schema/entity/EF mapping and additive migration. Each method carries: stable methodCode (<=32 chars), editable Persian/English-capable display name using existing localization conventions, active state, hidden provider/grouping kind, immutable legacy serviceCode + nullable legacy optionCode binding, audit fields only insofar as existing module conventions require. Enforce database and application-level uniqueness/case normalization, stable mapping identity, and referential/scope integrity consistent with the existing Fulfillment schema. Do not duplicate geography, tariffs, schedules, or commercial state.
B. Provide an explicitly idempotent, preservation-safe mapping/backfill of current legacy methods, including option-less services, option-dependent methods, inactive legacy options and inactive services. Preserve existing active/inactive accurately. Do not repurpose the post bare service identity into a second customer-selectable row when post:standard is the real method. No accidental activation. Do NOT call destructive UpdateAsync routines as a backfill mechanism. If no safe backfill without a live database write, implement migration/backfill logic and prove against isolated fixtures only; document the live rollout procedure for later authorization.
C. Introduce minimal Fulfillment-owned read contract for one-level catalog only if genuinely needed by the following UI task; contracts must not expose EF models. Avoid changing Shipping’s existing internal service/option key or Order projection in 082. Existing legacy read APIs/paths must remain backward compatible.
D. Introduce a default-OFF one-level read gate, in correct configuration owner/namespace with no enabled appsettings entry. Prove OFF means exactly the pre-task catalog response; do not enable or wire into legacy Checkout. If safest to defer wiring, define a precise follow-up seam; no pretend pass.
E. Add only focused tests for identity stability, idempotent mapping, active/inactive cases (post express, snapp_courier), option-less rows, collisions, max-32 boundary, migration up/down or isolated DB constraints where practical, and existing catalog regression. Run minimal relevant builds/tests once; do not run broad suites or continuous loops. Bound the work; stop on timebox with honest PARTIAL.
F. Document implementation, precise state of Development (UNMIGRATED unless independently proven), rollout/rollback plan and guardrails at docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-082.md; evidence at docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-082/. Include exact changed paths/SHAs and 083 dependency.

ALLOWED PATHS

src/backend/Modules/Fulfillment/** (shipping catalog Domain/Application/Infrastructure/Contracts and corresponding migration files, only essential narrow changes)
tests/** or src/backend/Modules/Fulfillment//Tests/ (focused Fulfillment tests only; use actual project layout)
docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-082.md
docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-082/**
docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-082.task.md (receipt only, DO NOT STAGE)
Do not touch unrelated code or master recovery unless separately authorized.

FORBIDDEN
No frontend changes in this task; no Host, Order, Shipping, Geography, Payment, Settlement, Checkout, or pricing configuration code changes. No edits to paid snapshots, shipment historical method codes, current Checkout rate authority, capacity reservation, real COD settlement, delivery promise, storefront prices, active commercial grants/rates, production/staging DBs or running services. No removal/renaming of legacy service/option rows. No invented weight or item-count. No blanket test suite. No second-level merchant UI.

ACCEPTANCE / STOP
PASS only for bounded additive Fulfillment foundation when: actual committed schema/code + reversible migration exist, historical code compatibility is maintained, default OFF safety proven, idempotent mapping and inactive/option-less handling are focused-test proven, and isolated migration safety evidence is present. Otherwise PARTIAL/BLOCKED with exact limitation. Timebox 30 minutes HARD; state elapsed, tests run (actual only), changed paths, commit and evidence SHAs, and rollback. Do not automatically launch 083. STOP_FOR_ARCHITECT.

RESULT CONTRACT — BRIDGE-WAKE-V1
Return exactly one complete result using the canonical envelope, with no extra text after END_TOOBA_TASK:
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-082
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-081
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-TABS
Mode: BOUNDED_FULFILLMENT_ONE_LEVEL_CATALOG_FOUNDATION
Bridge-Task-Row: <actual>
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 0d83659238f9a1b668c6f61bb7d505192a2effc7
Base-SHA: <actual>
Code-SHA: <actual or NONE>
Evidence-SHA: <actual or NONE>
automaticNextImplementationTask: NONE
<actual details: decisions B1/B2, real migration tests, backfill idempotency, compatibility, gate OFF, dirty-path safety, changed files, exact elapsed, remaining gaps, safe rollback>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK