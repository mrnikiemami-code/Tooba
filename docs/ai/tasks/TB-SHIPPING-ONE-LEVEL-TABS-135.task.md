PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-135
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-134
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-CHECKOUT-DELIVERY-CLOSURE
Mode: BOUNDED_SAME_SELLER_PER_ITEM_SHIPPING_METHOD_UI
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: fb512dc732b7c6e5e94b5f01d1d29918f4ce2335
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 MINUTES HARD. Target source completion <=30 minutes; reserve remaining time for targeted tests, push, report. No speculative work or repeated loops; stop PARTIAL if blocked.

ARCHITECT ACCEPTANCE
TB-134 PASS accepted for restoring persisted multi-seller shipping drafts on cold refresh (real browser proof). Current critical product gap: TB-131's UI groups ONLY by SellerPartyId and uses the scalar legacy path for any single-seller cart, thus one seller with two different shipping methods cannot be chosen by customers even though Order M1/M2, Payment, Fulfillment, Settlement now recognize stable groups. Deliver this missing user-visible case, grounded in real cart lines and authoritative shipping methods. Do not derail into Tax, carrier pricing, commercial settlement policy, general redesign or another design-only task. Keep docs/planning/shipping/SHIPPING-CHECKOUT-DELIVERY-CLOSURE-PLAN.md and docs/architecture/checkout/SHIPPING-METHOD-GROUPING-UI-CONTRACT.md authoritative.

PREFLIGHT
Claim exactly one bridge task; confirm origin/main matches Expected-Base-SHA or analyze drift; overlapping source drift -> stop RECOVERY_CONFLICT. Preserve pre-existing dirty admin frontend and unrelated evidence. No reset/stash/rebase/force push. Read existing shipping group UI, real cart line identity, StorefrontShippingGroupSelection and StorefrontShippingGroupPlanner invariants, grouped delivery horizon and persisted-draft hydration. Do not invent seller/eligible method/origin or prices.

IMPLEMENTATION — BOUNDED VERTICAL

Add a small, unobtrusive PER-ITEM shipping-method selection affordance within existing seller/item cards so one seller with two cart lines can explicitly assign DIFFERENT method codes; support two sellers and multiple methods simultaneously. Use real CartLineIds and server-returned eligible method codes. Group selected lines by (seller, canonical method identity), not seller alone; merge same-seller/same-method lines into one persisted group; NEVER merge sellers. Exactly-one assignment per actual cart line. Preserve legacy single-item/single-method cart layout and current behavior without churn.
Serialize Groups[] to existing /v1/storefront/shipping/selection when required (including same-seller multiple methods); client grouping must obey Order planner constraints and never fabricate amounts, service options or line IDs. Restore per-item selections deterministically from saved draft shippingGroups and real cart membership on refresh/back navigation. Any missing/changed line or ineligible saved method fails closed rather than silently choosing a different method.
Feed distinct selected method codes into the existing server-based shared delivery-date horizon hook (TB-133); intersection must remain authoritative and Continue disabled for incomplete/invalid assignment, address or time window. Keep payment draft and payable sourced from Order, not calculated in client. Continue only saves shipping draft; no order creation on navigation.
If the existing Order M1 planner enforces an overstrict one-group-per-(seller,method) invariant, adapt only the minimal grouping structure to satisfy it; no new pricing algorithm, origin assertion, seller-specific eligibility fiction or migration. If a missing trusted API makes correct implementation impossible, stop and report the precise contract gap rather than ship mock behavior.

STRICT VISUAL LOCK
Do not change global CSS, theme, design tokens, spacing scale, typography, responsive breakpoints, RTL, unrelated storefront pages or admin screens. Reuse EXACT existing shipping cards, method tiles, item rows and button styles; add only minimal local affordance. No broad rewrite of already oversized storefront-shipping.tsx; extract a small helper/component if appropriate. Preserve single-item legacy UI markup where feasible.

PROOF (MINIMUM)
Focused frontend tests: one seller/two cart lines/two methods -> distinct server-bound groups; same seller/two lines/same method -> one group; two sellers/same method -> seller isolation; persisted draft restore; no duplicate/missing line; shared horizon and legacy single-item path. Frontend typecheck and only targeted storefront tests. One test run and <=1 targeted rerun after specific fix. If time permits, one disposable isolated browser test of the key same-seller/two-method case, plus checkout readback. Runtime proof must use real authoritative cart/catalog actions and scratch databases, never shared :5088/:3000/tooba_alpha. If no two distinct real same-seller offers can be added from the authorized fixture, document exactly which fixture is absent and report runtime unproven; do not fabricate data, SQL inserts, mock seller identity or unsupported selections. Do not spend time in launch retries.

DELIVERY
Commit minimal implementation/tests and concise docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-135.md; evidence only where material. Push normally and verify origin/main. Report exact SHA, elapsed time, focused tests, verified UI/browser cases, backward compatibility, remaining blockers. Status PASS only for actually implemented + tested capabilities; label runtime proof separately if not performed. STOP_FOR_ARCHITECT.
END_TOOBA_TASK
