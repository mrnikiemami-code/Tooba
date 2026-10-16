> **SUPERCEDING DELIVERY-SCHEDULE AUTHORITY (2026-10-16):** [Independent seller shipping commitments and per-group dates/windows](../shipping/SHIPPING-GROUP-INDEPENDENT-DELIVERY-COMMITMENT.md). Every operational ShippingGroupId has its own eligible dates, time window and seller commitment. Two sellers using identical method codes NEVER become one operational group. The legacy global cart-level date/time and cross-method horizon intersection are expressly NOT COMPLIANT and must be replaced. This directive supersedes contradictory timing/grouping clauses below.

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


## Durable multi-seller / multi-method order and fulfillment contract (MANDATORY)

An order MUST support multiple shipping groups, regardless of whether its items originate from **one seller or multiple sellers**. A seller may have multiple shipping groups in the same order when the customer selects different eligible methods or when fulfillment constraints require separation. Conversely, two sellers must never be merged into one execution group solely because their method codes are equal.

### Durable ownership and relationships

- Persist a stable **ShippingGroupId** per shipping group, tied to the OrderId and tenant; each group references SellerId, authorized fulfillment origin/context, canonical MethodCode plus distinct ServiceCode and optional OptionCode, destination/scheduling snapshot, monetary breakdown, and lifecycle status.
- Persist item membership with a group-to-order-item relation (and assigned quantity where splitting an order line is supported). No order item/quantity can be silently omitted, assigned twice, or shipped twice. Do not rely on serialized UI grouping or transient cart/session state as the system of record.
- Snapshot the effective selection, relevant delivery constraints, shipping quote/currency and tax/pricing references at the authoritative order-conversion boundary. The source catalogs can evolve later without rewriting historical shipping decisions. Preserve stable IDs for traceability and correct updates.
- Checkout draft selections and groups must survive reload/back navigation and convert consistently to the durable order group/items representation. Transactional persistence and idempotent conversion prevent partial grouping, duplicate shipments or duplicate shipping fees.
- Fulfillment execution is per group (or finer authorized shipment if business rules require splitting): state transitions, dispatch, tracking, cancellation/returns and delivery evidence reference the group and affected order items. The Order remains one commercial order, while execution may include many shipping groups/shipments. Aggregate order state is derived from group/item states, not assumed to be one parcel.
- Grouping is produced/validated by domain-owned eligibility and fulfillment policy including seller, origin, item restrictions, supported methods, capacity, delivery region/time window and compatibility. Customer choice is honored only when eligible; otherwise show an actionable correction before placement. An identical method code does not override incompatible logistics rules.
- Shipping fee and applicable tax are calculated/recorded with the authoritative financial owners at group/order boundaries. Display per-group fee and correctly reconciled aggregate totals; no accidental fee multiplication or omission.

### Mandatory schema/contract acceptance

Before coding, inspect actual Order and Fulfillment entities, database mappings, migrations and interfaces. Write an owner-correct minimal schema evolution plan; do NOT assert the existing database already supports this model without proof. Any changes must use module-owned migrations (no Host tables, no manual SQL), explicit tenant isolation and correct FK/uniqueness/check constraints, indexes for OrderId/SellerId/ShippingGroupId lookup, and migration/backward compatibility strategy for existing single-method orders. Avoid speculative extra tables and large-scale refactors; prefer the smallest model consistent with the established module boundaries.

Essential acceptance cases: (1) one seller + two methods => two persisted and executable groups, (2) two sellers + same method => separate groups, (3) same seller + same method but incompatible origin => separate groups, (4) compatible items => one group with correct membership and quantities, (5) a persisted Order checkout/roundtrip retains selected methods, grouping, fees and schedule, (6) partial shipment or cancellation updates the correct group/items without corrupting the other groups. Confirm database readback through authorized module APIs, plus only necessary targeted integration tests. No UI-only mock is accepted as evidence of durable support.

Architectural status: this is a binding requirement, NOT an assertion that migrations/implementation currently exist. Implementation requires a separately bounded task and architect review before any shared or production rollout.


## Shipping-fee ledger, allocation and settlement (MANDATORY)

Each persisted shipping group must have its OWN authoritative shipping-fee financial record, separate from merchandise amount and from any other shipping group. The charged amount is not synonymous with carrier cost or seller revenue. Persist explicit money components in one currency with an immutable order-placement snapshot and referenceable group/order/tenant identity:

- Customer shipping charge, before/after shipping discounts, with discount funder where applicable.
- Carrier/fulfillment payable and any handling/packaging surcharge where applicable.
- Seller shipping share (seller receivable or seller-borne payable, direction explicitly typed).
- Marketplace shipping share (marketplace revenue/subsidy/expense, direction explicitly typed).
- Other allocated parts only when justified by business policy (e.g. platform-funded promotion, insurance, or applicable shipping tax); do not invent a fixed commission or arbitrary percentage.
- Currency, taxation treatment, pricing/rate/policy version, calculation timestamp, payer/payee and any adjustment/refund identifiers.

Never infer allocation from a displayed total or reconstruct it after the order from mutable current tariffs. Define a deterministic, versioned allocation policy owned by the appropriate pricing/settlement capability. Require a documented reconciliation invariant for customer charge, subsidy, carrier cost, seller/platform shares, tax and discount; avoid double counting or ambiguous sign conventions. A marketplace subsidy can produce a negative marketplace contribution, so do not force all shares positive. Every amount must use existing canonical Money/precision/rounding rules.

Persistence ownership: Order owns the accepted customer quote and per-group order snapshot; Settlement/Wallet/Payment own settlement ledger entries, allocations, receivables/payables and money movement under their existing contracts; Fulfillment owns execution and actual carrier charges/fulfillment adjustments, not financial settlement authority. Changes and reimbursements must be auditable with linked events/adjustments rather than silent overwrite. Do not put financial business authority or settlement writes in Host, browser code, or a global shipping table without a module owner.

Key scenarios: a single seller with multiple shipping methods incurs independently computed fee and allocations per group; multiple sellers sharing a method never merge allocation; seller-funded vs platform-funded free shipping has explicit funder and balanced settlement; partial shipping/cancellation/refund affects the relevant groups and balances only; any difference between quoted and actual delivery cost follows an explicit adjustment policy. Preserve idempotency across retries and prohibit paying or refunding the same shipping share twice.

The schema/contract design must be reviewed against existing Order, Pricing, Settlement, Wallet and Fulfillment models BEFORE migration. Specify the minimum required normalized columns/relations, tenant-scoped referential integrity, transaction/ledger linkage and compatibility path for legacy single-shipping orders. Acceptance needs focused financial reconciliation and persistence tests, not UI-only assertions. This is a REQUIRED architectural invariant, not a claim that the current system already implements it.

## Current proof boundary

TB-121 showed 7/7 one-level catalog labels in the shipping projection. TB-122 showed 7/7 on the real /fa/shipping DOM. TB-123 showed the cart-level shipping choice was saved before navigating to payment, but payment draft was blocked by scratch tax rule availability (checkout.tax.unavailable). None proves per-item Checkout grouping. This requirement is pending implementation/proof, not a claim of a completed feature.

Implementation tasks: bounded, maximum 45 minutes each; only relevant tests; no repetitive fix/retest loop; STOP_FOR_ARCHITECT after each result.
