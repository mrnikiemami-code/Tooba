PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-130
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-129
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-CHECKOUT-DELIVERY-CLOSURE
Mode: BOUNDED_SETTLEMENT_SHIPPING_GROUP_LEDGER_M4
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 3446b751545d76ef66797203975272d327454bac
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 MINUTES HARD. Stop new implementation by minute 32; reserve the remainder for only focused tests, evidence, commit and report. If blocked or late, return PARTIAL/BLOCKED, not loops.

ARCHITECT RULING
TB-129 PASS: Order group identity and Fulfillment seller-scoped execution exist. TB-128 Payment has per-group customer charge allocations, NOT seller revenue. Implement the smallest Settlement-owned durable per-group accounting representation without inventing business percentages, carrier tariffs, subsidies, tax rates or payout entitlements. Read and follow:

docs/architecture/checkout/SHIPPING-METHOD-GROUPING-UI-CONTRACT.md
docs/planning/shipping/SHIPPING-CHECKOUT-DELIVERY-CLOSURE-PLAN.md
docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-125.md
TB-128/TB-129 owner/contract reports.
No CSS, theme, components, visual changes or unrelated checkout refactor. Speed and safe delivery take priority over extensive tests/documentation.

PREFLIGHT — FAIL CLOSED
Claim exactly one Pending Bridge row on tooba-main. Read AGENTS.md and pipeline. Fetch origin/main; expected SHA must be an ancestor, reject overlapping drift, preserve pre-existing dirty admin screens and evidence. No reset/stash/rebase/force push; Bridge task receipt remains UNSTAGED. Trace the existing Payment StoreShippingGroup allocations, existing Settlement entry/idempotency paths and Order per-group accepted quote contracts before coding. Do not rewrite unrelated settled modules.

MISSION A — OWNER-CORRECT M4 FINANCIAL PERSISTENCE
Create a minimal Settlement-owned additive table/model/migration for shipping fee accounting keyed by stable ShippingGroupId and payment/order/seller identities through existing Contracts boundaries. Capture customer shipping charge and currency, quote/policy version/reference, plus separate explicit carrier-payable, seller-share, marketplace-share, shipping-discount/subsidy, shipping-tax and funder/adjustment fields only as authorized values. Explicitly distinguish KNOWN values from UNSPECIFIED values: do NOT set every missing share to zero or assume the marketplace keeps a residual. Never fabricate percentages or financial allocation policy.
Implement the smallest idempotent reception/recording path for the known authoritative per-group customer charge from the accepted Order/Payment allocation. Ensure group-target identity never posts as a SellerOrder merchandise entry, never falsely credits seller/marketplace balances, never enters payout as an invented share. Preserve legacy StoreShipping sentinel route. For unavailable commercial split policy, represent record as PENDING_ALLOCATION (or existing equivalent) and fail closed on posting/settlement of unknown carrier/seller/platform shares, while still allowing authorized checkout customer-charge bookkeeping. If actual canonical ledger architecture forbids even that partial recording without a full balanced allocation, STOP BLOCKED_ALLOCATION_POLICY with exact source proof; do not graft a parallel ledger.
Document the explicit reconciliation invariant and deterministic versioned policy seam that must eventually supply carrier cost, signed seller/platform share, discount funding and tax, but DO NOT invent policy content. Preserve immutable accepted quote and use adjustment/reversal entries rather than rewriting history. Minimum migration/backward compatibility only. No Host business authority, no manual SQL writes, no new public endpoint or forced production activation.

MISSION B — MINIMAL EVIDENCE
Prefer 4–6 targeted Settlement tests: two shipping groups for one seller with distinct charge; same method different sellers kept separate; stable group/idempotency and no duplicate credit; legacy untouched; missing allocation policy cannot post a fake payout; correction/refund scoped to one group if existing capability allows without broad redesign. Build only changed projects and run targeted tests ONCE; at most one bounded rerun for a concrete defect. No broad suites or runtime startup loop. If database integration cannot safely run within budget, report migration/runtime UNPROVEN.

MISSION C — REPORT AND DELIVERY
Commit only Settlement/Contracts owner-correct changes, focused tests, minimal module-owned migration and concise docs/evidence under docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-130.md and docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-130/. Push origin/main normally, verify HEAD == origin/main, include measured minutes and exact paths/SHA. Shared :5088/:3000 and tooba_alpha READ-ONLY, no process stop/restart, no shared DB changes. Keep UI identical to current storefront.

ACCEPTANCE
PASS only if persisted per-group known charges are authoritative, separately identifiable, idempotent, backward compatible and cannot become invented seller/marketplace revenue; all missing commercial allocation content remains explicit and blocked from settlement, with focused tests/build passing. Otherwise PARTIAL/BLOCKED with the precise next blocker. Do not claim completed commercial settlement, UI checkout grouping, or production readiness.

RESULT CONTRACT
Report Task-ID, Parent, Worker, Bridge row, Expected/Actual Base SHA, Code/Evidence SHA, drift, changed paths, tests, time, substantive result, allocation unknowns, migration/runtime caveat, shared parity, rollback and next blocker. STOP_FOR_ARCHITECT; Automatic-Next-Implementation-Task NONE.
END_TOOBA_TASK
