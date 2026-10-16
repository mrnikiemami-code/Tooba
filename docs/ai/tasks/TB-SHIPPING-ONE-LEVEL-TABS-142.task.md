PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-142
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-141
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-CHECKOUT-DELIVERY-CLOSURE
Mode: BOUNDED_ISOLATED_MARKETPLACE_SETTLEMENT_PENDING_LEDGER_ACCEPTANCE
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 0a771866e85057470a2f7d8fee4764a6f84093a9
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
HARD TIMEBOX: 45 minutes. By minute 25 choose a single bounded acceptance attempt; by minute 40 begin cleanup/report. No loops or repeated startup attempts; report PARTIAL on the deadline.

ARCHITECT DECISION — APPROVED ONLY FOR DISPOSABLE SCRATCH
Choose Option 1 of TB-141: KEEP Settlement schema activation explicit/CLI-owned; DO NOT add Settlement to the Development startup migrator chain, DO NOT add a migration-order constant, and DO NOT change shared/production module activation. Authorize an isolated, disposable Marketplace-edition overlay ONLY if the canonical existing Marketplace configuration/connection-reference authority and fail-closed tenant binding can be established without fabricated production settings. Explicitly apply the existing SettlementDbContext migrations by its own approved ToobaNpgsql/ModuleMigrationRegistry mechanism to the SCRATCH database. No legacy SingleStore shortcut or manual SQL inserts. If Marketplace is not safely bootable using existing authority, STOP PARTIAL with the exact prerequisite and avoid modifying architecture to force a green result.

PROVEN BASELINE
TB-138: a real same-seller/two-method customer journey accepted one Checkout, one SellerOrder, two shipping groups, separate Payment allocations. TB-140: sandbox success reached one FulfillmentUnit with two stable group ids and matching item membership. TB-130: Settlement-owned shipping_group_fees ledger is PendingAllocation only; seller/platform/carrier amounts unknown, never revenue or payout. TB-141: Settlement schema migration applies twice safely through the explicit module-owned seam, but SingleStore does not register SettlementPaymentSucceededHandler; Marketplace does. Review docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-141.md and docs/architecture/checkout/SHIPPING-METHOD-GROUPING-UI-CONTRACT.md before work.

PREFLIGHT AND ISOLATION
Claim one pending Bridge task and follow AGENTS.md. Fetch origin/main, verify SHA and no overlapping source drift; do not reset/stash/rebase/force-push, stage unrelated pre-existing admin screen changes, or change historical evidence. Inspect canonical Tooba:Edition=Marketplace registry path, Tooba:Marketplace:ConnectionReference, seed/offer/readiness and guest/auth policy, Settlement handler registration, Settlement migration runner. Before any launch require a guard proving NO tooba_alpha/tenant-alpha, shared host ports :5088/:3000, shared PostgreSQL :5432, or shared environment reference. Both tenant/data and MassTransit messaging DBs must be disposable. Use fake no-charge sandbox Payment exclusively (non-Production, Mode Sandbox, PSP endpoints/secrets empty); no live payments, no external carrier, no invented organization/tenant ids or prices.

MISSION A — SAFE MARKETPLACE ACTIVATION
Use existing canonical Marketplace connection-ref authority on one disposable Postgres container (and messaging DB), with minimal approved overlay. Read the actual edition requirements before deciding feasibility. Apply Settlement migrations through the explicit, owner-owned migrator (as proved in TB-141), never by adding automatic Host startup registration. Prove settlement.__ef_migrations_history includes the two existing migrations and shipping_group_fees exists. Do not relax edition validation or make Settlement run in SingleStore merely for this test.

MISSION B — ONE PAID EVENT ACCEPTANCE IF SAFE
With a real sanctioned scratch fixture and genuine customer/canonical API path, attempt exactly one same-seller two-lines/two-methods final checkout + fake sandbox payment completion, if Marketplace supports the fixture and auth legitimately. Assert from read-only DB evidence: payment Succeeded, one seller Paid, two accepted order groups with stable IDs and 200000/180000 IRR charges, two StoreShippingGroup payment allocations, one FulfillmentUnit with two groups, and exactly two Settlement shipping charge records keyed to those same ids/payment, SUM=380000. Verify allocation_state PendingAllocation, allocation policy/carrier payable/seller share/marketplace share/discount/tax/funding values NULL; no shipping seller credit or payout. If the module errors, use the existing Host per-handler error logs and transport failure evidence to identify the first owner-correct blocker. Do not inject payment events, call SettlementDirectory manually, synthesize ledger rows or claim end-to-end success from isolated unit tests. Repeated deliveries must not duplicate charge rows; verify from existing idempotency evidence or one bounded targeted test, NOT extra customer payments.

SCOPE CONTROL / NON-REGRESSION
Observation/proof first. Modify source ONLY to fix a PROVEN owner-correct blocker within the hard timebox and with one focused test; otherwise PARTIAL with reason. No startup migration registration, no generic DbContext, no invented financial policy, no granting PendingAllocation payout. Do not touch frontend, CSS, components, typography, theme, RTL, responsive layout or storefront appearance. No broad test suites, polling loops or repeated publishes. PID-verified cleanup and disposal mandatory.

DELIVERY
Minimal plan/evidence: docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-142.md and docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-142/. Report exact edition/connection authority and configuration isolation, migration history, payment/event/settlement readback, unknown NULLs, tests, minutes, source/evidence SHA, rollback, and any remaining commercial policy decision. Commit/push normally; confirm HEAD==origin/main; STOP_FOR_ARCHITECT. PASS only if the Marketplace paid path actually records the two pending fee rows (or an owner-correct fix with appropriately qualified proof); else PARTIAL/BLOCKED. No claim of final commercial settlement until the business defines signed seller/platform portions, carrier cost, tax and funding.
END_TOOBA_TASK