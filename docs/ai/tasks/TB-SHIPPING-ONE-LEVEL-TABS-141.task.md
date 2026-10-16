PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-141
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-140
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-CHECKOUT-DELIVERY-CLOSURE
Mode: BOUNDED_SETTLEMENT_SCHEMA_RUNTIME_AND_PENDING_ALLOCATION_ACCEPTANCE
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 509010d792ee9fc9ee0e7729f92011d46205d423
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Hard-Timebox: 45 minutes. Target work done by minute 30, evidence and cleanup by 45; PARTIAL/STOP at deadline. No retry loops or repeated broad suites.

ACCEPTED BASELINE
TB-140 PASS: real sandbox paid event created 1 FulfillmentUnit with 2 stable ShippingGroupIds, exactly 2 FulfillmentItems and no automatic shipment, after owner-correct EF shadow-FK repair. TB-130: Settlement ShippingGroupFeeAllocation records customer charge PendingAllocation only, with unknown commercial shares NULL and payout prohibited. TB-138 found no Settlement schema in isolated tenant DB. This is today's NEXT BLOCKER. Complete runtime schema and truthful pending-ledger verification, NOT invented commercial allocation.

PREFLIGHT / SAFETY
Claim exactly one Bridge task; inspect AGENTS.md, origin/main, current worktree and the sanctioned isolated TB-138/TB-140 launch/config scripts. Do not reset/stash/rebase/force push or touch dirty admin screens, prior evidence, shared :5088/:3000, tooba_alpha, production flags/credentials. Stop on overlapping source drift. Track tenant/module schema-migration composition, SettlementModule DI/registered migrator and the actual settlement.shipping_group_fees migration. Inspect precisely why Settlement schema was absent despite the successful Order/Payment/Fulfillment migrations, before changing source.

MISSION A — ISOLATED MIGRATION PROOF
Determine whether the missing Settlement schema is an intentional disabled optional module, a composition/migrator registration defect, migration gating/order, or scratch fixture omission. Use concrete file/line evidence. If a source defect exists, make the smallest owning-module/Host-composition correction with focused regression test. Do NOT globally enable a module or alter shared configuration merely to force schema creation. If module requires a documented explicit activation, use it in a DISPOSABLE overlay only and record its semantics. Do not assume migration is safe merely because raw DDL looks idempotent.
Use one bounded scratch Postgres with BOTH tenant + messaging databases, trusted isolated development Host and the already-sanctioned fake no-charge Payment gateway; prefer existing TB-140 setup scripts. Read-only verify that settlement.shipping_group_fees exists and the migration version is applied. If boot/policy setup cannot be safely completed in time, report PARTIAL with the first concrete blocker, no speculation.

MISSION B — PAYMENT-TO-SETTLEMENT CUSTOMER CHARGE READBACK
On isolated scratch only, run at most one complete paid same-seller/two-method journey if safe/time allows (the real Development fixture, group fees 200000+180000 IRR, no live PSP). Query via authorized read-only DB checks:

exactly one Checkout, one Paid SellerOrder, one payment Succeeded, TWO OrderShippingGroups and TWO StoreShippingGroup Payment allocations with matching stable ShippingGroupIds;
one FulfillmentUnit, two linked FulfillmentShippingGroups and exact item membership, no auto-shipment;
TWO positive shipping fee charge rows in Settlement if both group fees positive, amounts 200000+180000, matched to stable IDs, currency and payment/checkout; once per group/payment, never seller merchandise ledger, never seller payout;
AllocationState PendingAllocation with policy/carrier cost/seller share/marketplace share/funding/tax fields NULL (NOT silently zero). No commercial allocation or payout transition.
If payment event fails for a different module, use TB-139 per-handler fault logs and transport error details; report exact first cause, and fix only if clearly owner-correct within timebox, otherwise PARTIAL.
No direct injection of integration event, no manual commercial rows, no real PSP, no unbounded resubmission. Preserve previous verified Fulfillment success.

MISSION C — MINIMAL DELIVERABLE
If defect is fixed: commit narrowly scoped source/test changes. If no defect and runtime proves all: docs/evidence only PASS. Evidence: docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-141.md and docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-141/. Record module/schema provenance, exact before/after counts, accepted fees, states, NULL fields, one test/run, PID-verified cleanup, SHAs, elapsed time and explicit unproven items. Normal push and HEAD/origin/main check.
Architect constraints: no fake commercial policy (percentages, carrier payable, seller/platform shares, subsidies, tax funding). Do not turn PendingAllocation into Settled, Revenue, Credit or Payable. Do not modify UI, CSS, tokens, theme, typography, RTL, shipping cards, checkout visual structure or admin. No business logic in Host.
PASS only when Settlement-owned schema and pending per-group fee readback are genuinely proven, or an owner-correct source fix plus focused tests demonstrate that capability with runtime state caveat explicit. Otherwise PARTIAL/BLOCKED; STOP_FOR_ARCHITECT.
END_TOOBA_TASK