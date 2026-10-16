> **Admin Shipping UI authority (LOCKED):** [docs/architecture/shipping/SHIPPING-ADMIN-FLAT-METHOD-CONTRACT.md](../../architecture/shipping/SHIPPING-ADMIN-FLAT-METHOD-CONTRACT.md). Each selectable method is an independent flat managed service; per-method detail has non-nested tabs. Phase 1 visual shell needs user approval before scattered forms move. Do not revive provider -> service-type nested Admin UX.

# TOOBA Shipping-to-Checkout Delivery Closure Plan

Status: ACTIVE — architect delivery plan, 2026-10-16.
Purpose: finish the work already started, prevent scope jumping and deliver a usable storefront quickly while preserving its existing visual identity.

## Non-negotiable delivery rules

- Focus on shipped customer behavior, not endless investigations, evidence collections or task churn. Each Cursor task has a **45-minute HARD limit**, with implementation/verification bounded; only minimum essential targeted tests, no broad suites, retry loops, repeated full rebuilds or process restarts without reason. Stop with PASS/PARTIAL/BLOCKED and STOP_FOR_ARCHITECT; no automatic follow-up.
- Make tasks outcome-oriented, with explicit acceptance and an owner. Reuse accepted proofs and don't repeat TB-117 through TB-123 checks unless a material code change invalidates them. Record the minimum evidence, affected files, SHA, regressions and unresolved blockers.
- **Never damage or redesign the current storefront appearance.** Reuse exact storefront components, current CSS/classes/tokens, cards, buttons, type, color, spacing, RTL, responsive breakpoints and Persian formatting. Prefer tiny scoped additions or composition of existing primitives. No new design system, global CSS overrides, broad page refactoring, theme changes, or visual regressions. Before accepting UI change, compare actual existing Shipping/Checkout layouts and mobile/desktop DOM against the updated pages; verify no unintended change to unrelated storefront routes. Do not adopt existing dirty admin frontend files.
- Preserve TMAR boundaries: Order owns checkout drafts and order persistence; Fulfillment owns method/group execution; Pricing/Tax own calculation; Settlement owns allocation/ledger; Host composition only; contract-only cross-module traffic. Tenant-safe and fail-closed.
- Shared :5088/:3000 and tooba_alpha remain unchanged unless expressly authorized. Use isolated disposable setups for runtime proof; no invented authority, manual SQL bypass or hidden feature activation.

## TRACK A — Finish previously started Shipping-to-Payment functionality FIRST

The old work is NOT closed merely because startup and seven labels were demonstrated.

| Acceptance | Grounded current status | Remaining action |
| --- | --- | --- |
| Host startup and legacy seed | PASS TB-117 | Do not repeat |
| One-level catalog, seven labels in API | PASS TB-121 | Do not repeat |
| Shipping seven cards and labels in real browser | PASS TB-122 | Preserve appearance and do not repeat |
| Shipping selection saved on explicit Continue, cart remains active | PASS for cart-wide selection, TB-123 | Preserve before-payment semantics |
| Payment draft and customer-visible selected shipping/price | BLOCKED in TB-123 scratch: checkout.tax.unavailable | One authorized isolated proof using tax-applicable workspace-live-shirt fixture from TB-124; verify payment-draft and price/selection round-trip, no premature order/commit; fix only proven scope-limited blocker |
| Customer acceptance / checkout handoff | NOT CERTIFIED | Minimal final browser acceptance after other blockers resolved; hand off precise URL and checklist to user |

For Track A, do not assume the existing cart-wide shipping selection solves per-item grouping. Do not claim full final Checkout release while Track B is mandatory for the intended product. If a narrow Track A verification needs no code change, finish rapidly, report and close it, avoiding a new long series of exploratory tasks.

## TRACK B — Mandatory multi-method, multi-seller Checkout capability

Source of truth: docs/architecture/checkout/SHIPPING-METHOD-GROUPING-UI-CONTRACT.md (including durable order-group relationships and per-group shipping financial allocation).

Current code is **NOT capable** of this: TB-124 traced one CartShippingDraft.ShippingMethodCode per cart, one CheckoutGroup shipping code and one FulfillmentUnit per SellerOrder. This cannot represent multiple methods for items of the same seller. No UI-only simulation.

Delivery increments, each end-to-end and owner-correct where feasible:
1. Review TB-125's architecture result against the approved contract; choose the smallest migration and contract design grounded in actual Order/Fulfillment/Pricing/Settlement mappings. Avoid extra research-only cycles. Coordinate code changes against the new docs commits and current origin/main.
2. Persist item-to-shipping-group membership and canonical method/option identity for multiple groups in a **single order** (same seller may have different methods; different sellers must remain segregated; origin/eligibility constraints hold). Implement module-owned migrations, conversion from checkout draft and backward-compatible handling of legacy single-method orders.
3. Persist separately per group the customer charge, shipping quote, carrier payable, seller share, marketplace share, discounts/subsidy, applicable taxes and audit/settlement references, in the appropriate Order/Settlement owners. Reconciliation, idempotency and cancellation/refund treatment must be authoritative, with no invented fixed percentages or duplicate money.
4. Fulfillment execution must reference durable group/item IDs, preserve quantity membership and permit independent status/dispatch/tracking per group; avoid duplicating items, shipping charges or seller ownership.
5. Render grouped items, individual method/fee and correct totals in Checkout by **faithfully extending the actual existing storefront look and CSS**. Preserve the Shipping hero, controls, typography, card designs, RTL, responsiveness and behavior; do not rebuild the UI from scratch. One minimal real-browser acceptance with multiple sellers and one seller having two methods; compare DOM against Order/Fulfillment/financial readback.

No claim of PASS for any step until persistence, execution and customer-visible behavior are proved. No arbitrary implementation of pricing/seller shares without explicit approved policy.

## Work ordering and checkpoint discipline

- **Current TB-125 is already issued:** await and evaluate its design result; do not issue another conflicting implementation task before review. This plan is documentation-only and must be treated as non-overlapping SHA drift if TB-125 runs concurrently.
- After TB-125 review: close Track A's payment-draft tax-fixture proof in a tightly scoped step, while implementing Track B in a small number of substantial delivery slices. Do not forget either track.
- Each architect review states: done, incomplete, next blocking user-visible outcome, and expected time/essential tests. Any uncovered dependency is logged in this plan rather than silently changing direction.
- **Definition of Done:** customer can use Shipping and Payment without premature cart/order mutation; Checkout represents multiple shipping methods for items within a seller and across sellers; group membership and financial splits are durable and executable in Fulfillment/Settlement; UI matches the current storefront at mobile and desktop; user is explicitly invited to verify, with outstanding issues disclosed.

This document tracks architectural requirements and delivery intent, not a declaration that any pending implementation is completed.
