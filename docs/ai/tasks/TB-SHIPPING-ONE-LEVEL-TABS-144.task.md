PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-144
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-143
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-CHECKOUT-DELIVERY-CLOSURE
Mode: BOUNDED_FINAL_OPERATIONAL_ACCEPTANCE_AND_COMMERCIAL_POLICY_DECISION_GATE
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 5ff8b546512730effb2a72f21195051493f4c8e4
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Hard-Timebox: 45 minutes, stop with PASS/PARTIAL/BLOCKED; no repeated launch/test loops.

ACCEPTED FACTS
TB-143 PASS: real isolated SingleStore Marketplace-sales-channel paid journey, with explicit opt-in Settlement ledger capability and scratch-only module migration. One paid seller order, two durable shipping groups for same seller, two payment StoreShippingGroup allocations (200000+180000 IRR), one FulfillmentUnit with two linked groups, two Settlement shipping charge records PendingAllocation with unknown commercial portions NULL; no shipping seller credit or payout. Source committed at ca0f98982 and evidence at 5ff8b5465. Default capability OFF. No UI source changed.
Critical commercial limitation: PendingAllocation is intentionally non-payable; actual carrier costs, signed seller/platform shares, funder, shipping tax and discounts are NOT specified by business. Never invent a split or mark settled.

MISSION A — MINIMAL FINAL DELIVERY REVIEW (do not reopen completed work)
Read docs/planning/shipping/SHIPPING-CHECKOUT-DELIVERY-CLOSURE-PLAN.md, locked docs/architecture/checkout/SHIPPING-METHOD-GROUPING-UI-CONTRACT.md, and TB-136/137/138/140/143 proof. Produce a concise acceptance matrix of what is genuinely runtime proven and remaining commercial/production blockers. Inspect precise current code owner seams for shipping group allocation policy and its invocation; do not repeat architecture inventory, migrations or full browser runs. Check the critical practical edge: Settlement capability default OFF and explicit module schema activation remain required, and per-event sale-channel gate cannot silently credit non-marketplace retail or missing-schema tenants. Spot-check exactly one focused guard test if required, no broad suite.

MISSION B — VERSIONED BUSINESS POLICY INPUT CONTRACT, NOT A GUESS
Define an actionable architect-owned policy decision record or specification under docs/architecture/checkout/SHIPPING-GROUP-FEE-ALLOCATION-POLICY-DECISION.md:

per group: customer charge, discount/funder and timing, carrier quoted/actual payable, signed seller share, signed marketplace share, shipping tax jurisdiction/tax authority, currency/rounding, policy version and effective-date;
explicit accounting equation and treatment of zero-charge/free shipping, seller-funded vs platform-funded promotion, carrier overage, cancellation/partial shipment, return/refund and compensating entries, idempotency and audit;
responsible module/service for each value; no duplicate seller accrual, no residual percentage inference;
mark ALL commercially undecided values as BUSINESS APPROVAL REQUIRED, NOT as proposed numerical defaults.
Write short, decision-ready choices for the owner rather than implementing fictional numbers. Do not activate payouts or commercial settlement until approved. A policy interface may be described, but do not modify payout code or live business logic in this task.

MISSION C — FINAL UI/PAYMENT RELEASE CHECKLIST
Build a tight handoff checklist for user testing (single-line legacy, same seller 2 methods, two sellers same/different methods, mobile RTL, shipping back-refresh, final sandbox button side-effect timing, Order/Fulfillment stable groups, fee sums, pending Settlement no payout). Use already-proven acceptance evidence, identify remaining unexplored production deployment/auth/migration concerns explicitly. If a source defect is unambiguously found that blocks the main customer journey and is repairable within minutes, STOP and report rather than mix unapproved source changes into this decision task.
Maintain original storefront look: NO frontend, CSS, theme, tokens, card markup, layout, RTL, responsive, admin or shared setting modifications.
No use of live PSP, shared :5088/:3000 or tooba_alpha; no production activation, no global schema migration order changes. Do not alter existing settlement capability default OFF. Avoid new runtime launch unless absolutely necessary.
Commit concise plan/policy/checklist to docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-144.md and the decision record above; minimal evidence in docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-144/. Normal push; verify HEAD/origin/main; report exact decisions awaiting user/business approval and whether technical delivery is accepted. No more speculative technical tasks beyond this checkpoint without identified blocker. STOP_FOR_ARCHITECT.
END_TOOBA_TASK
