PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-124
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-123
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_CHECKOUT_PER_ITEM_SHIPPING_GROUPING_AND_TAX_BLOCKER_TRACE
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: b6e5639b1c8d29241c75d16edaf6b3668cbcaaae
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 minutes HARD. Aim to finish by 30 minutes; stop investigation/implementation by minute 35 and reserve time for evidence/cleanup. No loops, no broad tests, no repeated attempts.

ARCHITECT RULING
TB-123 proved one real browser click to payment PUT /v1/storefront/shipping/selection first, retained Order-owned draft tipax:standard and Active cart, but payment-draft GET returned 409 checkout.tax.unavailable on scratch without applicable Tax rule. Independently, product acceptance MUST address user's precise requirement: Checkout must group items by the shipping method selected FOR EACH ITEM, not automatically assume one shipping method applies to the entire cart. Do not claim this requirement is implemented. This task is a bounded source/contract audit and minimal next-proof design, NOT a speculative architecture change or another full runtime attempt.

PREFLIGHT — FAIL CLOSED

Claim exactly one Pending Bridge task on tooba-main; Working heartbeat; preserve docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-124.task.md UNSTAGED. Read AGENTS.md and relevant pipeline rules plus TB-118/121/122/123 evidence.
Fetch origin/main. Expected-Base-SHA must be ancestor; inspect overlapping source drift and existing dirty paths. On conflict STOP RECOVERY_CONFLICT. No reset/stash/rebase/force push or adoption of unrelated frontend/admin edits.
Shared :5088/:3000, tooba_alpha and existing containers are read-only. No process start/stop/restart, no DB changes.

MISSION A — TRACE CHECKOUT GROUPING CONTRACT (PRIMARY)

Trace the REAL data model across Cart line, Shipping projection and selection/draft, Payment draft, Checkout command, resulting Order lines and Fulfillment shipment/group creation, plus storefront shipping/payment UI. Identify the precise authoritative ownership for a per-line method selection, its persistence and presentation. Use concrete files/symbols and distinguish existing behavior from intended behavior.
Answer with evidence: (a) Is methodCode stored once per cart/draft or separately per cart line / seller group? (b) Can different cart items carry different methodCodes concurrently? (c) Where, if anywhere, are items grouped by shipping method in Checkout UI, order persistence and Fulfillment? (d) Must grouping also partition by seller/origin/fulfillment constraints? (e) What is the narrowest capability/contract gap to meet the user's per-item shipping grouping rule without compromising module ownership or creating a second source of truth?
Use a minimal 4-line example: item A and C = post:express; B = tipax:standard; D = store_courier. Show expected groups and whether the current implementation can represent them; account for seller/origin boundaries. Never claim this is working from tests covering a single item or single method.
If the current flow is cart-wide, document the precise blocking contract and propose one bounded implementation slice and a minimal acceptance scenario. Do not implement a whole redesign or modify business contracts without Architect review.

MISSION B — TAX 409 FIRST-BLOCKER TRACE (SECONDARY)

Trace GET /v1/storefront/cart/{cartId}/payment-draft through Tax resolver and rule source; identify whether a sanctioned existing Development seed/API can create an applicable tax rule in scratch (no manual SQL or synthetic bypass). Distinguish missing test fixture from a real code defect; include rule applicability conditions, with authoritative references.
If a small authorized scratch fixture already exists, document an exact bounded recipe for a later isolated acceptance. Do NOT start Host/frontend or run an end-to-end session in this task. If none exists, identify the smallest module-owned fixture gap and fail closed instead of fabricating Tax authority.

MISSION C — MINIMAL VERIFICATION / EVIDENCE

Prefer read-only source trace and at most ONE existing focused test command if it materially resolves ambiguity; no full test suites, no repeatedly running failed tests, no new architectural guard sweep. Observation-only: zero source/test/frontend/config/migration changes and no DB/network mutations.
Commit ONLY docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-124.md and docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-124/**, with concise source map, exact supported/unproven claims, tax finding and a SINGLE proposed next task boundary. Never include credentials/customer data/raw connections. Preserve the Bridge task receipt UNSTAGED.
Push normally; verify HEAD == origin/main; STOP_FOR_ARCHITECT. No automatic implementation task.

ACCEPTANCE
PASS only if the per-item grouping capability is definitively classified from source/contract evidence, including its limitation (if any), and tax 409 is traced to an authoritative actionable fixture/owner with no speculative fixes. PARTIAL if either remains uncertain; BLOCKED for source/conflict. This is NOT a certification of per-item grouping, payment readiness, production Shipping or Tax. Do not activate flags or run checkout.

RESULT CONTRACT — BRIDGE-WAKE-V1
Return Task-ID, Parent-Task, Channel, Worker-Id, Agent-Type, Bridge-Task-Row, Expected/Actual Base-SHA and drift, Code-SHA NONE, Evidence-SHA, Status PASS/PARTIAL/BLOCKED, Stop-State STOP_FOR_ARCHITECT, Automatic-Next-Implementation-Task NONE, measured minutes, source-backed per-item grouping verdict and concrete ownership/map, Tax 409 cause/fixture, any targeted test result, changed paths, shared non-interference, deviations, rollback, first remaining blocker, and one bounded recommended next slice.
END_TOOBA_TASK
