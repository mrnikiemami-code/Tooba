PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-131
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-130
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-CHECKOUT-DELIVERY-CLOSURE
Mode: BOUNDED_GROUPED_CHECKOUT_UI_AND_PAYMENT_DRAFT_DELIVERY
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 010a7d18a4e04397685e1ffea2578df54fdb4711
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 MINUTES HARD; finish implementation by minute 32, allow 13 minutes for essential focused tests, evidence and push; stop PARTIAL if impossible. No retry loops, broad test suites or repeated builds.

ARCHITECT RULING
TB-126/127/128/129/130 delivered draft and accepted Order groups, per-group Payment charge allocations, Fulfillment shipment group identity and Settlement PendingAllocation records. Commercial seller/platform/carrier split is NOT defined: never invent percentages or credit seller payout. Now prioritize customer-facing grouped Checkout/Payment UI using the existing storefront appearance; do not leave this track unfinished. The legacy cart-wide selection remains supported. Read docs/planning/shipping/SHIPPING-CHECKOUT-DELIVERY-CLOSURE-PLAN.md and docs/architecture/checkout/SHIPPING-METHOD-GROUPING-UI-CONTRACT.md.

PREFLIGHT
Claim one Pending Bridge task, heartbeat, AGENTS.md, task receipt unstaged. Fetch origin/main; inspect source drift and dirty frontend files. Non-overlapping docs drift permissible, overlapping source drift => RECOVERY_CONFLICT STOP. Preserve pre-existing dirty admin-fulfillment-work-queue-screen.tsx and admin-order-detail-screen.tsx. No reset/stash/rebase/force push.
Trace only customer storefront shipping form, shipping API/projection, payment handoff and current Order Contracts. Inspect reusable components/CSS and the exact grouped data shape; don't rewrite existing UI or create parallel source of financial truth.

MISSION A — SMALL, REAL CUSTOMER-FACING VERTICAL

Render customer shipping items grouped by authorized SellerPartyId + selected MethodCode, with item identity/quantities from real cart projection and per-group shipping fees from backend. Prefer existing shipping cards and list-row styles. Multi-method choice must be explicit at the appropriate item/group scope. Never fabricate seller/eligibility/warehouse or client fees. If API lacks essential per-line/seller metadata, add the smallest owner-correct read contract at Order/Cart seam and report the limits.
Submit Groups[] through the existing Order-owned /shipping/selection action using real CartLineIds and server-authoritative amounts; retain the legacy scalar for single-group carts. Preserve customer-selected groups when navigating back and forth, refresh or payment route transition, without automatic conversion on navigation.
Payment/Checkout summary displays actual selected groups, member items, localized method name, scheduled delivery, group shipping fee and reconciled grand total, using existing StorefrontCheckoutPage.ShippingGroups and Order-owned checkout draft projection. If the read model lacks group items, extend through an owner-correct Contracts read seam rather than guessing.
Fix no unrelated tax logic. For one bounded real proof, use the authorized workspace-live-shirt tax-applicable fixture (TB-124/125) in disposable scratch. If relevant tax/eligibility or backend prerequisites block rendering, fail closed, record exact gap and stop; no fake/mock checkout acceptance.

VISUAL NON-REGRESSION LOCK
Reuse current storefront CSS/classes, components, typography, colors, spacing, border/radius/shadow tokens, buttons, item cards, hero steps, Persian currency/number formatting, RTL and responsive behavior. Small scoped modifications only. NO global CSS or design system changes, no unrelated frontend files, no wholesale rewrite. Compare before/after shipping and payment on desktop and mobile (bounded screenshots/DOM only where practical); current storefront identity must remain unchanged. Do not touch admin UI.

MISSION B — MINIMUM TESTS
Run only critical-storefront targeted tests associated with changed files and at most one precise backend contract test if changed. One initial run and no more than ONE targeted rerun after a concrete failure. No full solution tests or broad architecture sweeps.
Optional single isolated browser launch only if implementation completes by minute 32 and safe scratch fixture ready; otherwise defer runtime proof to next authorized task with clear readiness checklist. Do NOT mutate shared :5088, :3000, tooba_alpha or flags. No direct SQL, fabricated customer identity or synthetic price data. Keep Payment/Settlement PendingAllocation fail-closed intact.

MISSION C — COMMIT / STOP
Commit minimal source changes and docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-131.md + docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-131/. Normal push, verify HEAD==origin/main. Record exact files, focused test count, API/UI provenance, visual nonregression, known blockers, time, Code-SHA and Evidence-SHA. Preserve task receipt unstaged.
PASS only if grouped selections and checkout summary are grounded in authoritative group data, no legacy regression, and storefront visual design is intact. Otherwise PARTIAL/BLOCKED; never claim release readiness from unit tests alone.
END_TOOBA_TASK
