PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-MVP-DESTINATION-066
Parent-Task: TB-SHIPPING-MVP-DEV-ADMIN-UI-065
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-MVP-OPERATIONAL-CLOSURE
Mode: BOUNDED_REAL_CHECKOUT_DESTINATION_IDENTITY_PROPAGATION_NO_CUTOVER
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 9f297f43fb8b7faa9290bfb39dd85aef71095a99
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 minutes HARD

ARCHITECT AUTHORITY
One bounded task. User authorizes use of existing disposable Development database to speed completion, NOT Production or staging. Do not create scratch/parallel databases, run broad suites, or enter retry loops. Do not restart services unless demonstrably required. A genuine conflict, unsafe migration, or unresolvable design uncertainty: report PARTIAL/BLOCKED rather than inventing semantics. Follow AGENTS.md, docs/ai/TOOBA-PIPELINE-PROTOCOL.md, .cursor/skills/tooba-backend-feature/SKILL.md. Worker PASS != Architect ACCEPT.

PREFLIGHT
On BRIDGE-WAKE claim exactly one Pending task; preserve receipt verbatim and do not stage it. Fetch origin/main; compare the exact base. Permit only explicitly proved, nonoverlapping evidence/bookkeeping advance; overlapping source drift -> RECOVERY_CONFLICT STOP. Preserve unrelated dirty files; no reset/stash/rebase/force-push. Read the Shipping recovery section, 057 projection/shadow design, 065 result, and inspect the exact existing storefront projection request/handler, Checkout destination selection, geography Contracts and front-end API seam BEFORE changing code. Identify the real source of selected country/area/city/service-area IDs and which are actually available to the projection request.

MISSION — IMPLEMENT A REAL USEFUL CUSTOMER DESTINATION IDENTITY PATH
A. Repair the missing city-capable destination identity flow between existing storefront destination/checkout selection, the storefront shipping projection HTTP request, Order query/handler, and shadow observation input, ONLY where real validated IDs are available. Use existing Geography Contracts and canonical validation, not hard-coded IDs or province-name guessing. Preserve the existing legacy ProvinceName for backward compatibility. If the UI does not possess a stable selected city ID, integrate the existing Geography lookup/selection surface minimally; do not invent city identification from raw text. Keep nullable/optional input backward-compatible so existing province-only clients continue to work.
B. The existing legacy ShippingMethodsOptions quote, available methods, selected amount, cart state, persisted order/payment snapshot and Checkout behaviour MUST remain unchanged. Destination IDs are observational metadata until explicitly validated for future authority; never enable ShadowEnabled/PrerequisitesConfirmed, never change customer price/eligibility/promise, never activate a cutover. Preserve zero query overhead when shadow is off; avoid introducing a new mandatory lookup on the legacy checkout path.
C. Ensure Order shadow observer uses validated geographic identity at the highest proven precision, when available, and fail-closed NotComparable when missing/inconsistent/unresolved. If geography Contracts cannot resolve city/service-area identity safely, report the specific gap instead of faking support. Do not cross module DbContexts, imports of foreign infrastructure, or relax the Contracts-only boundary.
D. Close the 065 real UI residual fetch race at the existing admin shipping Geography cascading picker: when administrative area changes, stale country-wide or old-area city responses must never appear/select; cancel/ignore stale responses and clear stale descendants, using the existing component pattern. No new screen or broad UI rewrite.
E. Prove just the necessary behavior: focused tests for province-only compatibility, optional correct/incorrect city hierarchy identity, shadow-off unchanged projection/zero extra geography+shipping calls, cancellation where relevant, admin picker stale-response race; run targeted backend module tests and frontend relevant tests/typecheck. One real browser/HTTP check against the existing Development Host only if needed and without restarting a healthy service merely for convenience. No synthetic real tariffs or grants; no database writes unless indispensable. Do not rerun 063–065 long CRUD suites.

SCOPE / ARCHITECTURE
Allowed source scope: existing Order-owned storefront shipping projection request/query/observer and their focused tests; existing storefront checkout shipping destination selection/API wiring and focused tests; existing admin shipping Geography picker component and focused test. Discover canonical paths, report every one exactly; no Host/Admin/Security, Order checkout paid snapshot/persistence, Payments, schema/migrations, Shipping price engine, Fulfillment commercial catalog, appsettings or permission changes. No new page, no Results.Json, no Host business authority; CQRS ISender, Contracts-only Geography lookup. Do not widen architecture guard baselines.
Allowed docs: docs/planning/shipping/SHIPPING-MVP-DESTINATION-066.md and docs/architecture/evidence/TB-SHIPPING-MVP-DESTINATION-066/**, plus task receipt unstaged.

ACCEPTANCE
PASS only if validated city-capable identity travels from an actual existing customer selection through the projection boundary into observation without changing the legacy customer quote, province-only clients still work, invalid/stale identity cannot silently become trusted, admin cascading picker no longer shows stale cities, focused tests pass, and no unauthorized data/process changes. If real frontend source lacks sufficient city selection to finish safely in 45 minutes, deliver strongest bounded verified portion with PARTIAL and exact next blocker. Do not claim shadow parity or Checkout cutover.

RESULT CONTRACT — BRIDGE-WAKE-V1
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-MVP-DESTINATION-066
Parent-Task: TB-SHIPPING-MVP-DEV-ADMIN-UI-065
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-MVP-OPERATIONAL-CLOSURE
Mode: BOUNDED_REAL_CHECKOUT_DESTINATION_IDENTITY_PROPAGATION_NO_CUTOVER
Bridge-Task-Row: <actual>
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 9f297f43fb8b7faa9290bfb39dd85aef71095a99
Base-SHA: <actual>
Code-SHA: <actual or NONE>
Evidence-SHA: <actual or NONE>
automaticNextImplementationTask: NONE
<actual destination flow before/after; legacy quote invariance; picker race proof; targeted tests; process and data safety; changed paths; elapsed; commit and push; blockers>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
