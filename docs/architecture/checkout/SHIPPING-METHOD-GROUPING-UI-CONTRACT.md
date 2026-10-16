# Checkout — Shipping-method grouping and storefront visual continuity

Status: ARCHITECTURAL REQUIREMENT (user-approved intent); implementation and runtime acceptance pending.
Scope: customer-facing Checkout/Payment review and corresponding Order/Fulfillment handoff.
Date: 2026-10-16.

## Mandatory customer behavior

Checkout must present items grouped by their **effective selected shipping method per item**, rather than assuming one shipping method for the entire cart/order. A group displays its member items (image, title, variant, quantity), shipping method's canonical identity and localized display label, delivery information when available, shipping fee, and group subtotal/summary as supported by authoritative pricing. Totals must not double-count a shipping fee, discount, or tax.

Example: items A and C select post:express; B selects tipax:standard; D selects store_courier. Checkout shows the corresponding shipping groups. However, equal method codes alone are **not** sufficient to merge items across distinct seller, fulfillment origin, fulfillment eligibility, shipment constraints, or other authoritative fulfillment boundaries. Group identity must be stable and derived from authoritative item assignment and fulfillment rules, not a frontend string comparison alone.

A customer's selection is a distinct per-item (or explicitly defined fulfillment-group) choice and must survive Shipping -> Checkout/Payment -> back-navigation and order submission without silently reverting to a cart-wide default. Grouping must match the persisted Order-owned shipping selection; Fulfillment consumes the resulting authoritative grouping rather than inferring it independently from Persian display labels. Preserve serviceCode, optionCode, and canonical methodCode as distinct fields (e.g., tipax:standard), while labels remain presentation only.

**Compatibility checkpoint:** TB-118/TB-123 proved only a cart-level method selection/draft path. They did NOT establish per-item shipping selection, group persistence, or order-level grouping. Before implementation, trace the existing Cart, Order checkout draft and Fulfillment contracts; if they are cart-wide only, explicitly design the smallest owner-correct contract/data evolution and obtain architect approval. Never pretend UI-only grouping solves a missing backend assignment.

## Visual implementation — reuse the current storefront design

The Checkout shipping groups MUST look native to the **existing storefront** and appear to be a careful extension of its Shipping/Checkout design, not a newly invented design system. Reuse actual existing storefront React components, CSS classes, design tokens, typography, spacing scale, card surfaces, radii, borders, shadows, icons, button styles, item rows, Persian number/currency formatting, RTL direction, responsive breakpoints, focus/hover/selected/error states, and step-header/summary conventions after tracing their real owners.

Prefer composition/extraction of existing presentation primitives and CSS rules over copying large stylesheets or replacing the page's CSS. Introduce the smallest scoped styling necessary for group separation and per-group header/summary; avoid global selectors, broad overrides, duplicate CSS, hard-coded colors or dimensions conflicting with the store theme, UI-library swaps, wholesale page rewrites, and destructive redesign. Keep storefront layout, brand identity, existing components, mobile responsiveness, and accessibility intact. Match the current appearance with high visual fidelity and fine detail.

The design must gracefully handle: one group, multiple groups, long item titles, duplicate localized labels with different method identities, empty/loading/unauthorized/error states, and narrow/mobile screens. RTL ordering must remain correct; a user should never confuse one group's shipping cost or selected method with another's.

## Architecture and authority guardrails

- Cart owns cart lines; Order owns checkout draft/confirmed shipping selections and order conversion; Fulfillment owns method identity/binding, capability constraints and fulfillment grouping authority; Pricing/Tax own financial calculations. Host composes only. Cross-module access through established Contracts ports.
- No shipping choice may be committed merely by opening Checkout/Payment. Persist only at the intended explicit action and prevent unintended cart clearing/order creation.
- Never derive logistics or financial rules from UI labels, client-only arrays, or CSS grouping.
- Respect tenant and edition isolation, authenticated/guest policy, feature flags, and fail-closed configuration. No production/shared-environment toggles or silent activation.
- Do not expand work into unrelated architecture refactoring or replace certified module boundaries.

## Acceptance gates (essential tests only)

1. Source/contract proof that every item is assigned an authoritative shipping method/group, with seller/origin/capability boundaries respected; absent support must be reported as BLOCKED rather than simulated.
2. Focused data/behavior checks: two items sharing compatible method and seller/origin group correctly; distinct methods stay separate; same method with incompatible seller/origin stays separate; persistence round trip and totals correct, including return navigation.
3. One bounded real browser acceptance using authorized disposable scratch setup (only after backend readiness): compare item/group identities, per-group cost and selected method between API/Order draft and rendered DOM; inspect RTL, desktop and mobile styles against the existing storefront. No expansive screenshot/test loops.
4. Confirm no checkout/payment/order mutation from page navigation alone, no shared DB/config changes, and no visual regressions of existing Shipping and Checkout surfaces.

## Current proof boundary

TB-121 showed 7/7 one-level catalog labels in the shipping projection. TB-122 showed 7/7 on the real /fa/shipping DOM. TB-123 showed the cart-level shipping choice was saved before navigating to payment, but payment draft was blocked by scratch tax rule availability (checkout.tax.unavailable). None proves per-item Checkout grouping. This requirement is pending implementation/proof, not a claim of a completed feature.

Implementation tasks: bounded, maximum 45 minutes each; only relevant tests; no repetitive fix/retest loop; STOP_FOR_ARCHITECT after each result.
