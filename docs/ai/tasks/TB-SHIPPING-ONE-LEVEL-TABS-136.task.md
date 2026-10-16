PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-136
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-135
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-CHECKOUT-DELIVERY-CLOSURE
Mode: BOUNDED_SAME_SELLER_TWO_LINES_REAL_BROWSER_ACCEPTANCE
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 2b0f4a4ae67fb63aeebacf3ec3c4961cd08d34a2
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Hard-Timebox: 45 minutes maximum. Stop exploratory work early; one bounded environment, one browser journey, focused tests only. On deadline report PARTIAL/BLOCKED, do not overrun.

ARCHITECT ACCEPTANCE OF TB-135
Code commit 9040eb11c212563c2cd892b92581d57fafc4f840 is accepted as source-level delivery of per-cart-line method selection. Runtime same-seller/two-method acceptance is NOT certified; absence of second live offer is a fixture gap, not a reason to claim completion. This task closes that gap using ONLY disposable Development fixtures and observes the actual checkout flow. Preserve previous Shipping/Payment work and docs/planning/shipping/SHIPPING-CHECKOUT-DELIVERY-CLOSURE-PLAN.md.

SCOPE AND SAFETY

Preflight fetch origin/main, verify expected ancestry and no overlapping source drift. No reset, stash, rebase or force push. Preserve two existing dirty admin frontend screens and unrelated evidence. Task receipt per Bridge convention.
Read TB-135 fixture-gap and existing WorkspaceDemoMarketplaceSeed/product/offer Development paths. Use sanctioned app-owned Development seeding/API capabilities; if no safe application-owned fixture extension exists, implement the smallest DEVELOPMENT-ONLY additive second live product/offer for the EXISTING seller, behind the already existing explicit disposable Development fixture gate. No production seed, no new user-facing fixture, no manual SQL, no DB bypass, no global seed reset. Ensure two genuinely distinct sellable cart lines of same SellerPartyId. Do not fake IDs or rates. Prefer a truly minimal fixture extension with bounded focused proof.
Scratch only: own ephemeral Host, frontend and PostgreSQL with both tenant+messaging DB created up front; ports and deployment identity must discriminate from shared :5088/:3000 and tooba_alpha. Never restart, mutate or stop shared services. Keep secrets out of commits; PID-verified cleanup.
Existing storefront appearance is LOCKED: no CSS/tokens/layout/typography/theme/global selector changes; no redesign; do not touch unrelated frontend. Existing item-level tiles should be used unchanged.

MISSION A: REAL SAME-SELLER CUSTOMER FLOW

Prove via authorized API readback the two active offers/products belong to the same REAL seller, have distinct cart-line identities and canonical rates; if tax fixture is missing, stop with exact evidence, no tax bypass.
In one real browser session add the two items from the same seller through storefront customer actions (not direct DB manipulation), confirm cart line count=2, seller count=1. Visit /fa/shipping; verify both item rows and independent method tiles appear, choose two different authoritative methods (e.g. post:express and tipax:standard only if genuinely available).
Verify allowed shared delivery dates are server-derived intersection; complete address and window; Continue becomes enabled only after all fields valid. One click saves exactly one shipping selection draft. Verify through authorized API/isolated DB read-back TWO persisted shipping groups associated to the SAME seller, correct disjoint CartLineIds, chosen method codes and authoritative fee sum. No automatic checkout submit, payment start, capture, reservation or cart clear.
Visit /fa/payment and verify per-group method/charge and grand total from the Order-owned projection. Return or cold reload /fa/shipping ONCE and verify address, both per-line methods and date/window restored. Validate desktop and one mobile viewport against pre-existing storefront component classes/RTL: no redesign. Report any mismatch, do not pretend visual parity if unverified.
If specific source defect blocks the journey, prefer STOP_WITH_ROOT_CAUSE and propose a narrow follow-up rather than unrelated fixes. A small bounded fix is allowed ONLY if essential and achievable inside the timebox, then focused test and single repeat of the affected step; no investigation loops.

MISSION B: SPEED / EVIDENCE
Run minimal targeted fixture test if fixture source was changed; do not rerun entire solutions. One publish, one Host launch, one frontend launch wherever prerequisites hold; absolute-path/port/tenant guards and cleanup evidence. Record precisely observed browser selectors, APIs, persistence, fee reconciliation, payment reachability, refresh restoration, CSS parity, failed steps and remaining policy blockers (Settlement PendingAllocation, origin/eligibility). Do not claim end-to-end payment/production acceptance.

COMMIT/REPORT
Commit only task-owned Development fixture code/tests (if needed), minimal docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-136.md and docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-136/. Push normally, verify HEAD==origin/main. Report PASS only for fully proven same-seller/two-method customer case; otherwise PARTIAL/BLOCKED with the first exact blocker. State measured minutes, Code-SHA, Evidence-SHA, deployment provenance, runtime cleanup and visual non-regression. STOP_FOR_ARCHITECT.
END_TOOBA_TASK
