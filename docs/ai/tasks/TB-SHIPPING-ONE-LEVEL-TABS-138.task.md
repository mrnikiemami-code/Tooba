PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-138
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-137
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-CHECKOUT-DELIVERY-CLOSURE
Mode: BOUNDED_ISOLATED_GROUPED_CHECKOUT_FINAL_SUBMIT_PAYMENT_ACCEPTANCE
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: f6c07a056524a5313ececce061d489609d1ca4bf
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 MINUTES HARD. By minute 30 decide if safe runtime acceptance can finish; reserve 15 minutes for evidence, cleanup and push. STOP PARTIAL/BLOCKED on limit; no loops.

ARCHITECT DECISION
TB-137 resolved the blank grouped shipping label and proved same-seller/two-method UI, draft, payment summary and total in a real isolated browser. Accept that narrow display result. The next customer-visible blocker is unproven FINAL-click transition from grouped Payment to durable accepted Order, Payment allocations and Fulfillment. Settlement shipping fee shares remain PendingAllocation until an approved commercial policy exists: NEVER invent shares or convert pending shipping charge into seller revenue. Do not branch into broad architecture work or CSS changes.

PREFLIGHT
Follow AGENTS.md, Bridge protocol and task receipt discipline. Fetch origin/main; validate expected ancestor, reject overlapping source drift, keep existing dirty admin screen/evidence files untouched. No reset/stash/rebase/force push. Read approved docs/planning/shipping/SHIPPING-CHECKOUT-DELIVERY-CLOSURE-PLAN.md and docs/architecture/checkout/SHIPPING-METHOD-GROUPING-UI-CONTRACT.md. Inspect only real Payment final-click path, Order Checkout submit, Payment Initiate/allocations, Fulfillment paid-order handoff and relevant Settlement guard. Do not repeat already PASS TB-132 through TB-137 browser steps beyond the one required acceptance run.

MISSION A — SECURITY/SIDE-EFFECT GATE (BEFORE ANY CLICK)
Identify exact storefront final payment action and whether it will initiate an external payment gateway, actual capture, charge a customer, send notifications or fulfill external carrier bookings. Authorize ONLY a pre-existing explicit Development-only isolated fake/sandbox payment method/provider with provably zero real financial or external-world effects. If such a mechanism does not exist, DO NOT call a real provider or bypass its guard: stop with PARTIAL, exact blocking boundary and next minimal owner-correct seam proposal. No fake payment-success event injection, direct DB writes, shadow settlement changes or auth bypass.
Use disposable isolated Host, frontend and Postgres/messaging databases with recorded unique ports and unique tenant reference; preflight fail-closed connection discriminator ensuring no shared :5088/:3000, tooba_alpha, shared containers or external payment credentials. Only Development fixture supported in TB-136: same seller, shirt plus scarf, two distinct real offers; gate enabled only in isolated overlay. Preserve all secrets outside evidence.

MISSION B — ONE E2E ACCEPTANCE (ONLY IF MISSION A SAFE)
In ONE browser journey: create guest cart using real storefront actions, two distinct lines of the SAME real seller; choose post:express and tipax:standard, provide destination, intersected server date and window; click Continue ONCE; verify real Payment summary has two methods, 200000 + 180000 = 380000 IRR shipping and server-derived payable (expected fixture 3977000 IRR only if source prices/tax unchanged). Verify NOTHING committed before final-click.
Then click real authorized final payment action ONCE ONLY via the approved isolated no-charge Development path. Inspect authorized API/DB readback for: exactly one accepted Checkout, one SellerOrder, two accepted ShippingGroups and disjoint OrderLine membership, amount sum once, two Payment StoreShippingGroup allocations by stable ShippingGroupId (not sentinel, no duplicate charge), no premature seller shipping payout, one FulfillmentUnit for the SellerOrder after an actual supported paid transition (if paid transition legally available), two scoped groups and no fabricated shipment bookings. Retry/idempotency may be proved using existing focused tests, NOT by a second customer click or accidental double payment. Carefully distinguish PAYMENT INITIATED vs PAYMENT PAID vs ORDER ACCEPTED; don't claim completion without proofs.
If any step fails, record exact HTTP/status/fault and the first owner module boundary; make at most one narrowly scoped, owner-correct code fix ONLY if safely reproducible and finishable inside timebox, otherwise PARTIAL and STOP. No bypassing domain invariants, fake group memberships, tax rules or policy. If payment provider cannot safely be exercised, prove the furthest safe submit boundary and stop.

MISSION C — TESTS, VISUAL SAFETY, CLEANUP
Only focused tests for paths actually edited; no broad backend or frontend suites. No frontend styling changes: preserve exact CSS, tokens, spacing, RTL, mobile behavior, current cards and labels. Prefer OBSERVATION-ONLY task, Code-SHA NONE unless one small authorized defect is actually fixed. Do not alter Settlement PendingAllocation or introduce made-up split policy. If no source edits, don't claim implementation.
Capture concise evidence of source provenance, final-click path, API/DB identities, group charge reconciliation, zero real-world charges, browser before/after, any blocker. Stop/clean only task-owned processes by verified PID and remove isolated disposable resources/secrets; shared environments remain untouched.
Commit small task planning/evidence/result; normal push, verify HEAD==origin/main. Report status PASS only if all safety and acceptance checks met; otherwise PARTIAL/BLOCKED, precise first blocker, elapsed time, exact SHA, what remains unproven, and STOP_FOR_ARCHITECT.
END_TOOBA_TASK
