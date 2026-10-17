PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-127
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-126
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-CHECKOUT-DELIVERY-CLOSURE
Mode: BOUNDED_ORDER_ACCEPTED_SHIPPING_GROUPS_M2
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 7dfac497c2ca337f90746d662a2d32fd89cc7c98
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 MINUTES HARD. Stop new implementation by minute 32; remaining time for focused proof, commit and report. No loops, no repeated broad test suites; PARTIAL/BLOCKED on deadline.

ARCHITECT RULING
TB-126 delivered M1 persistent draft groups and a deliberate fail-closed guard against collapsing >1 groups during order conversion. ACCEPT M1 with explicit limitation: runtime DB migration and transaction safety have not been proven; draft replacement is two-phase and must not be assumed atomic. Advance the order-owned M2 conversion only within bounded, owner-correct scope. Preserve the existing Shipping/Checkout frontend and exact CSS. Mandatory architecture:
docs/architecture/checkout/SHIPPING-METHOD-GROUPING-UI-CONTRACT.md
docs/planning/shipping/SHIPPING-CHECKOUT-DELIVERY-CLOSURE-PLAN.md
docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-125.md

PREFLIGHT — FAIL CLOSED
Claim exactly one Pending Bridge task, respect AGENTS.md and pipeline protocol, preserve task receipt UNSTAGED. Fetch origin/main and inspect SHA drift/dirty paths; permit only non-overlapping documentary drift; overlapping code -> RECOVERY_CONFLICT / STOP. No reset, stash, force push or unintended merge. Do not touch the two pre-existing dirty admin frontend screens or unrelated evidence.
Inspect the exact current Order draft, CheckoutGroup, Checkout/SellerOrder/OrderLine persistence and conversion transaction, StorefrontCheckoutService, StorefrontShippingService, M1 migration, and Order->Fulfillment ports. Do NOT spend time on broad exploratory inventory already completed by TB-124/125/126.

ARCHITECT DECISIONS FOR THIS SLICE

Shipping-group origin is OPTIONAL and NULL when no trusted origin authority exists. Never invent warehouse/origin, and never merge groups based on an unverified origin. Record BLOCKED_ORIGIN_AUTHORITY for a future capability; do not add fake origin.
Version 1 assigns a complete cart/order line to exactly ONE shipping group; no quantity splitting. Maintain explicit bijective membership; no duplicate and no omitted item.
Per-seller shipping method eligibility is NOT currently provided by authoritative service. Do not claim eligibility verified per seller. Preserve current canonical eligibility checks and reject unsafe selections; document the limitation rather than making up permission. Do not broaden to Fulfillment eligibility in this task.
Keep legacy checkout scalar columns for historical read compatibility; for genuine multi-group accepted orders derive aggregate ShippingAmount from group snapshots and never project one arbitrary method as though it applied to all lines.
Allocation POLICY CONTENT (seller/platform/carrier share) is reserved for Settlement slice; do NOT invent percentages or persist fabricated allocations in M2.

MISSION A — DURABLE ORDER GROUPS M2
Add the smallest Order-owned migration, EF mappings and domain entities for order_shipping_groups and order_shipping_group_lines: immutable/stable ShippingGroupId carried from draft, CheckoutId, SellerOrderId, SellerPartyId, canonical MethodCode/ServiceCode/OptionCode, label/promise/date/window, currency and authoritative group shipping-charge snapshot, lifecycle state, and membership referencing OrderLineId through Order-owned FK. Scope uniqueness/index/foreign keys to the existing module schema/house rules. Seller order/checkout ownership and full coverage of lines are hard invariants.
At the existing Order-owned conversion boundary, atomically materialize groups from M1 drafts and freeze them once. Keep SellerOrder merchandise totals unchanged. Compute the shipping payable as the sum of committed group charges, exactly once; preserve existing single-method orders and idempotency keys; retries must not create extra groups/charges. Detect any missing/duplicate/wrong-seller/order mapping and abort with a stable fail-closed code BEFORE unsafe side effects.
Remove the TB-126 multi-group conversion guard ONLY once actual M2 persistence and payable are proven; otherwise leave guard and return PARTIAL. Do not extend to Fulfillment M3, Settlement M4, Payment allocation refactoring, shipping UI or tax fixture changes. Do not claim payment acceptance if the unchanged Payment module still expects one StoreShipping allocation; document exact boundary and maintain fail-closed behavior until its owner is updated.
Investigate M1's two-phase draft group replacement only insofar as needed to avoid silent partial persistence; if fixing requires substantial scope expansion, document the exact transactional gap and stop rather than conceal it.

MISSION B — ESSENTIAL PROOF ONLY
Focused Order tests (prefer four to six cases) for: single-seller two methods -> two durable groups; two sellers same method -> separate seller-scoped groups; stable group/line IDs and exactly-once membership; repeat idempotent conversion and summed shipping charge; legacy one-method path unchanged; invalid mapping aborts without silent collapse.
Build only changed projects and run focused tests once, maximum one bounded rerun after a precise code fix. Minimal migration/EF verification on disposable isolated DB ONLY if feasible within 45 min; do not start shared systems. If no runtime migration proof, say so plainly. Existing TB-126 tests need not be rerun wholesale.
No frontend, CSS, globals, theme, components or shared config edits. Strict preservation of visual identity.

MISSION C — DELIVERY
Commit bounded Order source/migration/tests and concise evidence in docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-127.md and docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-127/. Push normal origin/main and verify HEAD==origin/main. No shared :5088/:3000 or tooba_alpha mutation, no process-name termination, no production gate activation.
Report task/bridge row/status/time/SHA/drift, exact tables/files, tests, financial and ownership invariants, rollback, unproven claims and FIRST next blocker. STOP_FOR_ARCHITECT; automaticNextImplementationTask NONE.

ACCEPTANCE
PASS only when M2 stores separate stable accepted-order shipping groups and full item membership, preserves seller boundaries and single-method backward compatibility, totals shipping charges exactly once, and guards failures and idempotency, with focused passing evidence. Otherwise PARTIAL/BLOCKED; never weaken guards to obtain green tests.

END_TOOBA_TASK
