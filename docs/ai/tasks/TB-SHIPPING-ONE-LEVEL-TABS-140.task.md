PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-140
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-139
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-CHECKOUT-DELIVERY-CLOSURE
Mode: BOUNDED_FULFILLMENT_PAID_EVENT_ROOT_CAUSE_AND_REPAIR
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: e5451961a3b0e21b59f81f721b5fb12ea65f9635
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 MINUTES HARD. Finish discovery by minute 17, narrowly scoped fix by minute 30, essential proof/report and stop by minute 45. If not feasible, PARTIAL with exact blocker. NO loops.

ARCHITECT DECISION / EVIDENCE
TB-138 confirmed one fake/sandbox payment succeeded and Order consumed payment.succeeded.v1, but Fulfillment had no inbox/unit. TB-139 repaired the Host transport consumer to invoke each event handler independently, preserve retry/dead-letter and log real handler exceptions. That fix has focused tests but NO runtime acceptance. Do not mark Fulfillment complete until durable runtime readback proves one FulfillmentUnit for the paid SellerOrder and its two stable ShippingGroupIds and exact OrderLine memberships. This is a delivery/owner fault, NOT permission to modify checkout UI or Settlement.
Read docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-138.md, SHIPPING-ONE-LEVEL-UI-139.md and docs/architecture/checkout/SHIPPING-METHOD-GROUPING-UI-CONTRACT.md; reuse the authorized isolated TB-138 environment scripts and payment fake-only safety gate.

PREFLIGHT
One pending Bridge task only; review AGENTS.md. Fetch origin/main, confirm SHA and preserve pre-existing dirty admin frontend screens, unrelated evidence logs and task receipt. Overlapping source drift: STOP RECOVERY_CONFLICT. No reset/stash/rebase/force-push. Do not touch shared :5088/:3000, tooba_alpha or shared Postgres/RabbitMQ.

MISSION A: SINGLE BOUNDED REAL REPRODUCTION + DECISIVE ERROR
On a disposable scratch tenant+messaging database, run the sanctioned two-line SAME-seller guest checkout with two shipping groups and the Development-only fake sandbox provider; absolutely no live PSP/charge. Verify checkout acceptance and payment Paid with exactly one final click/verification. Capture one payment.succeeded.v1 EventId and the new per-handler 'Integration handler failed' log (Handler + ErrorType + message) or the SQL transport error/dead-letter payload; collect read-only counts for Order and Fulfillment inboxes, units, items and shipping groups. Do not merely assume handler registration is the problem, or assume the outbox processed flag implies delivery. Capture exact first failing Fulfillment-owned stack/guard/EF write if available.
If Fulfillment is already healthy after TB-139, report runtime PASS with one unit, two groups and exact membership and STOP; do not make gratuitous source changes.

MISSION B: OWNER-CORRECT REPAIR, ONLY IF PROVEN NECESSARY AND WITHIN TIME
If concrete exception identifies a defect, repair the smallest actual owner seam (Fulfillment handler, bridge, domain group mapping, migrations or transport registration as proven), retaining module boundaries, tenant isolation, independent consumer retries and idempotency. Do NOT invoke Fulfillment directly from Order/Host, manually insert DB rows, inject success events, fabricate origin/carrier or alter the Payment amount. Keep one FulfillmentUnit per SellerOrder and two shipping groups under that same unit. Grouped shipment booking must remain explicit authorized seller action; no auto-shipment on payment.
Prefer focused regression test that fails on the actual observed fault and then passes. One test run and at most one focused rerun after a demonstrated fix. If bug cannot be safely repaired by minute 30, record specific exception and location and STOP PARTIAL rather than rush unverified code.

MISSION C: PROOF AND CLEANUP
If repair is made, verify the paid path once using the existing scratch runtime without restarting shared services. If run cannot fit, honestly mark the runtime invariant UNPROVEN. Capture: checkout count=1, seller order Paid, payment Succeeded, accepted order groups=2, Payment StoreShippingGroup allocations=2, Fulfillment inbox event count, FulfillmentUnit count=1, FulfillmentShippingGroups=2, items matching exact order lines, no duplicate charges, zero auto shipments. If Fulfillment still fails, capture real error with diagnostic identity and stop rather than speculate.
Keep Settlement PendingAllocation and missing schema as a separately tracked blocker, not this task. No commercial seller/platform/carrier split. No changes whatsoever to frontend, CSS, theme, tokens, layout, RTL, storefront/admin appearance.
Use disposable container, explicit isolated host/frontend port and PID-verified cleanup. Avoid repeated publish/startup or browser loops. Tests: only relevant owner and messaging focused cases; no full-solution sweeps.
Commit only changed owner files, focused tests and concise docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-140.md plus evidence under docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-140/. Normal push, confirm HEAD==origin/main; report status, code/evidence SHA, minutes, precise exception or recovered health, tests, scratch DB readback, non-interference, rollback and FIRST remaining blocker.

PASS CRITERIA
PASS only if real isolated payment.succeeded.v1 reaches Fulfillment, one durable FulfillmentUnit with exactly two stable groups and exact memberships is demonstrated, and no duplicated payment/order or premature shipment occurs. Otherwise PARTIAL/BLOCKED, STOP_FOR_ARCHITECT.
END_TOOBA_TASK
