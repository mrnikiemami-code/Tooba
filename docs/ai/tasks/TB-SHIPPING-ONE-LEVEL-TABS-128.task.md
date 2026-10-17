PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-128
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-127
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-CHECKOUT-DELIVERY-CLOSURE
Mode: BOUNDED_MULTIGROUP_PAYMENT_DRAFT_ALLOCATION_CLOSURE
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: ef412bd0b8d9de132275a25cef9624bf61ce2f35
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 MINUTES HARD. Stop implementation by minute 32; essential checks, commit and report by minute 45. No loops; max one targeted rerun after a precise fix. STOP PARTIAL/BLOCKED on deadline.

ARCHITECT RULING
TB-127 M2 PASS: Order-owned group snapshots and summed shipping charges are implemented, but Payment draft still fails closed on >1 group because Payment assumes one StoreShipping allocation. Next user-facing goal is to render a correct Payment draft for a multi-method checkout, not another design-only loop. Fix only the true owner boundary. Preserve exact existing storefront CSS/components and all legacy behaviors. Fulfillment M3 and Settlement financial allocation remain future tasks: never claim them completed here.

LOCKED SOURCES
Read AGENTS.md and pipeline instructions; docs/planning/shipping/SHIPPING-CHECKOUT-DELIVERY-CLOSURE-PLAN.md; docs/architecture/checkout/SHIPPING-METHOD-GROUPING-UI-CONTRACT.md; TB-125 through TB-127 reports. Work from current GitHub source, not assumptions. Existing PaymentAllocation kinds SellerOrder and StoreShipping and Order snapshots must be inspected before editing.

PREFLIGHT
Claim one Pending Bridge job on tooba-main; Working heartbeat. Keep task receipt UNSTAGED. git fetch origin main; verify Expected-Base-SHA ancestor, inspect remote drift and dirty files. Only unrelated docs bookkeeping drift acceptable; overlapping source conflict -> RECOVERY_CONFLICT STOP. Never reset/stash/rebase/force push. Preserve pre-existing dirty admin screens/evidence.

MISSION A — BOUNDED PAYMENT GROUP ALLOCATION

Trace exact Order ProjectPaymentDraftAsync fail-closed guard, accepted Checkout's ShippingGroups snapshots, PaymentDirectory's allocation creation/idempotency, payment-draft response and amount composition. Record concrete cross-module Contracts handoff.
Implement the smallest OWNER-CORRECT, additive representation of separate StoreShipping payment allocations for every persisted Order shipping group, keyed by stable ShippingGroupId and associated with the correct checkout/order context. Preserve SellerOrder merchandise allocations. Preserve legacy single StoreShipping allocation for legacy orders, including the current sentinel identity if required for historical compatibility. Avoid arbitrary changes to existing payment public contracts if the owner has a compatible extension.
Verify customer payable = merchandise payable + SUM(group shipping charges) (+ authoritative Tax when applicable) EXACTLY ONCE. Do not mistake customer shipping charge for seller/platform/carrier revenue; Settlement allocations/commission and actual carrier cost are not to be invented here. If Payment architecture fundamentally cannot support per-group allocations without a separate authorized schema redesign, STOP BLOCKED_PAYMENT_AUTHORITY and present minimal concrete change; do not fake group IDs or misuse SellerOrder allocations.
Remove Order's multi-group Payment-draft guard ONLY after Payment safely consumes all groups and idempotently returns a coherent readback. Ensure no payment capture, order conversion, checkout commit or cart clear occurs by merely opening Payment. Keep all existing auth/tenant/edition/Tax fail-closed behavior. Do not bypass checkout.tax.unavailable.
Keep M1/M2 Order grouping and fulfillment origin limitations unchanged. No Fulfillment M3, no Settlement M4 business ledger, no carrier pricing policy, no unrelated modules.

MISSION B — ESSENTIAL PROOF
Run only focused Payment/Order tests for: one seller two methods -> two group shipping allocations, two sellers same method -> separate group allocations, exact total/no duplicate shipping charge, idempotent repeated draft, and legacy one-method compatibility. Fail-closed for missing/duplicate/mismatched group IDs. Build changed projects. Prefer one focused test run, at most one bounded rerun. Do not run broad solution suites. If a real isolated DB check is needed, keep disposable and within timebox; NEVER touch shared :5088/:3000/tooba_alpha. State honestly if migrations/EF have not been executed.

MISSION C — NON-INTERFERENCE / REPORT
Zero frontend/CSS/theme/template edits: the storefront visual design is LOCKED. No shared config, feature gate or database changes; no unauthorised process restarts. Commit only scoped Payment/Order/contracts/tests/migrations if unavoidable, and concise docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-128.md plus docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-128/. Push normally, verify HEAD == origin/main. Report exact changed paths, status, SHA, time, tests, payment accounting invariant, unresolved boundary, rollback and next first blocker.

ACCEPTANCE
PASS only if multi-group payment-draft readback represents each group and correct combined payable without inventing financial allocation policy, legacy path survives, and focused owner tests/build pass. If Payment draft still blocked, return PARTIAL/BLOCKED with precise first failure. NO claim of Settlement split, Fulfillment group execution, UI parity, or production readiness.
STOP_FOR_ARCHITECT. Automatic next task NONE.
END_TOOBA_TASK
