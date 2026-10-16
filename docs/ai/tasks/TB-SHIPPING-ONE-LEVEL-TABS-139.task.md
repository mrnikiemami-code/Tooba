PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-139
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-138
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-CHECKOUT-DELIVERY-CLOSURE
Mode: BOUNDED_PAID_ORDER_FULFILLMENT_EVENT_DELIVERY_REPAIR
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: ca4ee6e97298420f60614634fde2dbfc70ec944f
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 MINUTES HARD; source investigation + fix target by minute 27, focused proof + commit by minute 45. No loops or repeated broad test suites.

ACCEPTANCE CONTEXT
TB-138 real isolated sandbox payment succeeded: exactly one Checkout, one SellerOrder, two accepted OrderShippingGroups, two shipping payment allocations, SellerOrder Paid. Outbox payment.succeeded.v1 processed by Order (order.payment_inbox=1), but Fulfillment payment_inbox=0 and fulfillments=0. Settlement schema absent; that is SEPARATE and MUST NOT be worked around in this task. Existing M3 code for Fulfillment groups and shipments passed module tests, but live integration-event handoff did not occur. TB-138 reported Fulfillment registered in Host, so investigate exact binding/dispatch failure and do not assume registration alone means delivery.
Binding source: docs/architecture/checkout/SHIPPING-METHOD-GROUPING-UI-CONTRACT.md
Delivery closure: docs/planning/shipping/SHIPPING-CHECKOUT-DELIVERY-CLOSURE-PLAN.md
Evidence: docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-138.md and docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-138/

PREFLIGHT
Claim one pending task, read AGENTS.md and Bridge protocol, fetch main and confirm expected base; preserve unrelated dirty admin files/evidence and task receipt. If overlapping drift: RECOVERY_CONFLICT STOP. No reset/stash/rebase/force push. Check module ownership and Contracts-only event binding before edits.

MISSION 1 — PROVE ROOT CAUSE AND FIX JUST THAT
Trace persisted PaymentSucceededIntegrationEvent publisher/outbox, subscriber discovery + DI registration in Host, dispatcher semantics (delivery to multiple modules, consumer idempotency/inbox ordering, whether one consumer success removes an event), FulfillmentPaymentSucceededHandler invocation, and Fulfillment CreateFromPaidSellerOrdersAsync. Compare event id observed in Order inbox to zero Fulfillment inbox. Identify the FIRST failing boundary from concrete source + focused integration proof; do not speculate about MassTransit or bypass event dispatcher.
Make smallest owner-correct change to ensure EACH intended module receives the same payment.succeeded.v1 exactly once, without changing successful Order handling and without duplicate Fulfillment units. If the issue is that event is consumed competitively, fix proper fan-out/routing/subscriptions instead of manually calling Fulfillment from Order or creating units in Host. If handler is reached but errors, preserve failure observability/retry and fix exactly the actual fault.
Keep one FulfillmentUnit per SellerOrder, with its two stable ShippingGroupIds and exact line membership; no automatic shipment creation/booking; no seller/carrier spoofing.

MISSION 2 — ESSENTIAL PROOF
Focused event-delivery/inbox tests that would have FAILED before the fix: one payment.succeeded.v1 reaches both Order and Fulfillment; duplicate delivery does not duplicate fulfillment; two same-seller order shipping groups reach one paid FulfillmentUnit with two groups and correct item membership. Build/test only touched modules and integration seam; no broad sweeps or guard-baseline changes. If time allows ONE isolated disposable Postgres/Host sandbox re-run of the TB-138 paid path with proven fake-only gateway; count units and group membership by authorized read-only APIs/DB. If runtime not completed within timebox, report as UNPROVEN. Do not simulate a delivery success with direct DB inserts or manual handler calls and claim E2E.
No shared :5088/:3000/tooba_alpha change; bounded setup and PID-verified cleanup; no external PSP, customer charges, or actual carrier actions.

MISSION 3 — REPORT AND STOP
Commit source/test change with minimal evidence under docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-139.md and docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-139/. Normal push; confirm origin/main and report SHA, elapsed minutes, exact before/after event subscriptions/inboxes, Fulfillment group count, acceptance proved/unproved, rollback, first remaining blocker.
NEVER claim Settlement/payout closure: commercial share policy unknown and TB-138 scratch Settlement schema missing; these require a later task.
NO FRONTEND, CSS, TOKENS, COMPONENTS, THEME, RTL OR STORE APPEARANCE CHANGES. Do not alter unrelated user experience.
PASS only with demonstrated multi-consumer event delivery plus Fulfillment unit/group result (at minimum focused integration tests; mark browser/runtime acceptance pending if not executed). Otherwise PARTIAL/BLOCKED and STOP_FOR_ARCHITECT.
END_TOOBA_TASK
