PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-FULL-OPERATIONS-077
Parent-Task: TB-SHIPPING-FULL-OPERATIONS-076
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-FULL-OPERATIONS
Mode: BOUNDED_ADMIN_PACKAGING_EDITOR_AND_QUOTE_DIAGNOSTICS
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: d51ab7f574b765da787c3ca0ab181f92747fef44
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 minutes HARD

ARCHITECT RULING
076 is TECHNICALLY ACCEPTED for its additive Shipping operational-quote read seam, deterministic schedule and payment split, but the worker violated the HARD 45-minute timebox by 12 minutes. No extension is authorized. This is TASK 7 of MAXIMUM 10 (071–080); only three can follow. Our customer requested a USABLE fully configurable Shipping administration: fee packaging, prepaid/collect-on-delivery and transparent shipping conditions. 075 built backend packaging policy and payer mode, 076 exposed an ADMIN-only operational quote. Neither is visible in admin frontend. CLOSE THIS USER-FACING GAP, do not use 077 to create a premature public quote or order mutation. Preserve legacy Checkout pricing authority, paid order snapshots and monetary boundaries. Capacity remains CONFIGURATION ONLY, never remaining/reserved.

PREFLIGHT — STRICT STOP
Claim exactly one Pending task on tooba-main after BRIDGE-WAKE. Preserve received .task.md verbatim UNSTAGED under docs/ai/tasks. Read AGENTS.md and docs/ai/TOOBA-PIPELINE-PROTOCOL.md; planning/evidence 074–076; current admin shipping-services-screen.tsx and its row actions; existing shipping-rates-coverage-{api,model,dialog}.ts*, shipping-destination-picker and schedule editor patterns; backend ShippingPackagingContracts.cs, ShippingOperationalQuoteContracts.cs, ShippingPackagingPolicyEndpoints.cs, ShippingOperationalQuoteEndpoints.cs. Verify exact route templates, casing, request DTOs and response envelopes from SOURCE. Fetch origin/main; Expected-Base-SHA d51ab7f574b765da787c3ca0ab181f92747fef44 must be HEAD, or its ancestor only through independently verified documentation/result commits. Any overlapping source drift => RECOVERY_CONFLICT/STOP. Preserve all unrelated dirty/untracked paths; never reset/stash/rebase/force-push. Positively identify Development localhost:3000 / 5088 and store-alpha / tenant-alpha / tooba_alpha before mutations; no Production/staging or parallel/scratch database.

MISSION A — ADMIN PACKAGING AND PAYER EDITOR (PRIMARY)

Add an obvious per-service row action or appropriately labeled focused section within existing /admin/shipping-services management UI: «هزینه بسته‌بندی و نحوه پرداخت کرایه». No new page, no giant modal with rates/schedules merged. Real Fulfillment serviceCode/optionCode (including explicit service-level fallback) and Geography destination picker only; never invent IDs/options or fetch another module's DB directly.
Implement typed API client to the EXACT EXISTING Shipping 075 authorized admin packaging routes: GET list, POST create, PUT update, PATCH active. Read current backend before writing routes. No mock data and no local-only persistence. Display persisted fee/currency, destination, service option, payer mode (Prepaid «پیش‌کرایه», CollectOnDelivery «پس‌کرایه»), active flag and clear default «بدون سیاست بسته‌بندی = هزینه بسته‌بندی صفر / پیش‌کرایه پیش‌فرض». A selected existing option must not be accidentally changed by editing. State clearly that payer mode is a quotation fact and that real COD money collection/settlement has NOT been wired into Checkout/Payment.
Clear Persian validation: >=0 packaging amount with exact monetary decimal handling, ISO currency, existing Geography scoped identity, option identity, duplicate scope and ambiguity error mapping; field-adjacent backend errors, visible 401/403, no fake success. Show «ارسال رایگان، هزینه بسته‌بندی را رایگان نمی‌کند». Use READ AFTER WRITE for create/update/toggle, disable controls while request pending and on access denied. Avoid mutating rate tables or schedule fixtures.
Keep frontend components small and capability-oriented: new shipping-packaging-{api,model,dialog}.ts* files (and targeted tests) with only a minimal composition edit to existing admin shipping-services-screen.tsx. Reuse design system and destination picker; no dependencies invented.

MISSION B — ADMIN OPERATIONAL QUOTE INSPECTOR (SECONDARY, ONLY IF TIME)
5. Add a distinct clearly-marked ADMIN diagnostic view (not a customer-facing final quote) invoking existing authorized POST /v1/admin/shipping-services/{serviceCode}/operational-quote. Choose service option/destination from authentic Fulfillment/Geography; optional explicit parcel weight grams (human kg), physical shipment item count, cart subtotal, currency and explicit carrier pickup/dispatch instant UTC. Do NOT infer weight or item count from cart lines or display a customer delivery promise.
6. Present separate carrier freight, packaging, full expense, payable at Checkout, payable upon delivery, payer mode, rate condition, schedule availability and NotEnforced capacity. Distinguish missing price/ambiguous data and nonconfigured schedule without fabricating zero. State that this is a diagnostic from Shipping and NOT the live Checkout price. Read-only POST must not change DB or claim stock/slot reservation. If this cannot fit safely within 45 minutes, prioritize Mission A and mark Mission B NOT STARTED; do NOT overrun.

FOCUSED DEVELOPMENT PROOF
Use the EXISTING running Next/Host Development surfaces, no unnecessary process restarts. In real browser, create ONE uniquely scoped synthetic packaging policy with fee e.g. 12000 IRR, payer CollectOnDelivery; read/reopen/edit 15000; toggle off/on; observe field errors for invalid fee and duplicate; return task-owned row to INACTIVE by end, ledger exact policy id. Respect existing 075 test fixtures and rate/coverage/schedules. For operational inspector if implemented, test distinct prepaid/COD split and 13:30 vs 14:00 window using safe temporary fixture with exact restoration. Run only focused Vitest, npx tsc --noEmit, browser roundtrip and relevant frontend guards; no full tests or backend rebuild unless a real independent blocker makes it strictly necessary. Document pre-existing failures separately. NEVER invent rate approvals or commercial promises.

ALLOWED PATHS
src/frontend/app/admin/shipping-services-screen.tsx (minimal row actions/dialog composition ONLY)
src/frontend/app/admin/shipping-services/shipping-packaging-.ts
src/frontend/app/admin/shipping-services/shipping-packaging-.tsx
src/frontend/app/admin/shipping-services/shipping-operational-.ts
src/frontend/app/admin/shipping-services/shipping-operational-.tsx
src/frontend/app/admin/shipping-services/shipping-destination-picker.tsx (only if minimal reusable necessary fix)
src/frontend/app/admin/shipping-services/shipping-rates-coverage-api.ts (only for reused exact type, avoid edits)
docs/planning/shipping/SHIPPING-FULL-OPERATIONS-077.md
docs/architecture/evidence/TB-SHIPPING-FULL-OPERATIONS-077/**
docs/ai/tasks/TB-SHIPPING-FULL-OPERATIONS-077.task.md (UNSTAGED Bridge receipt only)

FORBIDDEN
No Shipping backend changes in 077; no new public API or pages; no Order, Checkout, Payment, Settlement, Host, security or permissions edits; no price authority cutover, no customer delivery guarantee, no claim that configured capacity is enforced, no Production/staging, no fresh database, no unrelated dirty edits, no broad tests, no fake result, no task 078 auto-start.

ACCEPTANCE / STOP
Mission A COMPLETE only when a real Development browser can create, edit, read, deactivate and explain packaging and payer mode via existing backend, focused tests and TypeScript pass, and task-created fixture is left INACTIVE with ledger. Mission B graded COMPLETE or NOT STARTED or PARTIAL with actual evidence. Overall PASS only when all scoped claims are true; be explicit if B deferred. HARD STOP at 45 minutes, do not extend. Return exact times and elapsed, real Code/Evidence SHAs, touched paths, checks, HTTP outcomes, data restoration and truthful blockers. Worker PASS does not equal Architect ACCEPT.

RESULT CONTRACT — CANONICAL BRIDGE-WAKE-V1
Return one completed Worker Result envelope with actual values; body strictly after automaticNextImplementationTask, end with END_TOOBA_TASK and no further prose. The following placeholder fields are only a template for the worker to replace:
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-FULL-OPERATIONS-077
Parent-Task: TB-SHIPPING-FULL-OPERATIONS-076
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-FULL-OPERATIONS
Mode: BOUNDED_ADMIN_PACKAGING_EDITOR_AND_QUOTE_DIAGNOSTICS
Bridge-Task-Row: <actual Bridge row id>
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: d51ab7f574b765da787c3ca0ab181f92747fef44
Base-SHA: <actual SHA>
Code-SHA: <actual or NONE>
Evidence-SHA: <actual or NONE>
automaticNextImplementationTask: NONE
<actual Mission A/B verdicts; UI paths; exact API identity; browser evidence; cleanup IDs; focused tests; capacity/checkout disclosures; timings; commit/push and any blockers>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK