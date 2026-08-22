# Tooba — Promotion & Discount Architecture

Status:

```text
P00 architecture design — candidate for later ADR; not an ADR lock
```

Task:

```text
TB-P00-T023
```

Documentation only. No promotion engine, coupon endpoints, rule DSL, schemas, Admin/Seller UI, Tax, Returns, or Shopeiva.

```text
Promotion != Pricing
Promotion != Price Book
Promotion != Coupon Code
Promotion != Campaign Content
Promotion != Order
Promotion != Payment
```

```text
Locale != Market != Currency
Product != Price (price is not a scalar)
Backend/module boundary != UI boundary
Modular monolith; no cross-module DB joins
```

Template-only concepts (`gift card`, `wallet`, `referral`) remain `TEMPLATE_PRESENT / PRODUCT_DECISION_PENDING` and are **not** Promotion architecture.

## A. Core Separation

**Pricing** owns base/contextual commercial **price formation** (price books, authored/contract/quantity prices, quote inputs). See `docs/architecture/08-pricing-market-currency.md`.

**Promotion** owns conditional commercial **incentives and adjustments**: eligibility, benefits, combinability, funding, coupons as triggers, redemption authority, evaluation evidence.

**Coupon** is one trigger/claim mechanism, not the Promotion domain.

**Content / Page Composition** may describe a campaign; they do not calculate discount truth.

**Order** snapshots accepted commercial outcomes; it does not re-run current promotion rules.

**Payment** receives a final payable amount; it does not apply coupons or promotions.

Do not merge Promotion into Pricing write models. Do not treat “sale price on Product” as architecture.

## B. Promotion Scope

Conceptual capabilities (not a schema):

```text
Promotion
Promotion Rule / Eligibility
Promotion Benefit
Coupon / Code
Redemption / Usage
Funding Source
Combinability Policy
Validity
Targeting
Evaluation Result
```

A Promotion has identity, version, validity, targeting, rules, benefits, combinability, funding, and optional coupon bindings. Evaluation Result is a structured, explainable outcome — not a mutated price book.

## C. Benefit Types

Preserve typed extensibility. Do **not** lock all types into one generic formula.

Candidates:

```text
percentage discount
fixed amount discount
fixed target price
buy X get Y
quantity discount
free shipping
shipping discount
bundle benefit
gift item future
seller/platform subsidy
```

Exact first-release subset:

```text
NEEDS_LATER_P00_DETAIL
```

Gift-item, wallet credit, and referral benefits stay out of first architecture lock unless a later USER/Architect decision promotes them.

## D. Eligibility Dimensions

Eligibility **may** depend on (not all required every time):

```text
Tenant / Deployment
Market
SalesChannel
Currency
Seller
Offer
Product
Variant
Category
Brand
Customer / Party
Organization / Commercial Account
Contract
Quantity
Cart subtotal
Order subtotal
Shipping method
Payment method where business policy permits
First purchase / segment future
Campaign source
Date/time
Coupon
```

Promotion consumes these via **contracts/projections**, never Catalog/Party/Offer table joins.

## E. Locale vs Market

```text
Locale != Market != Currency
```

Promotion eligibility is **commercial context** (Market, Channel, Currency, Seller, Offer, Party/Org, time). UI language is not an eligibility key.

Localized campaign copy (banner text, coupon explanation) is Content. Do not infer Market from Locale or Currency from Market.

## F. Promotion Evaluation Boundary

Conceptual internal contract (name not locked):

```text
IPromotionEvaluator
```

**Input:** normalized commerce context — Tenant, Market, SalesChannel, Currency, EffectiveAt (explicit clock), Pricing quote lines (offer/seller/qty/base amounts), cart/order subtotals, shipping method/rate **inputs**, optional payment-method code, Party/Org/Contract refs, coupon claims, PromotionVersion set or “evaluate current published set”.

**Output:** deterministic structured adjustments and evidence — applied promotions (id/version), line/order/shipping adjustments with Money+Currency, funding attribution, rejected claims with customer-safe reasons, combinability outcome.

No engine-specific DSL, SQL, or Admin JSON leaks into Cart, Checkout, or Order. Checkout/Order consume the **result contract**.

## G. Pricing Integration

```text
Pricing establishes eligible/base price
Promotion applies conditional adjustments
```

Promotion must **not** mutate authored Price Books.

Candidate pipeline (composition, not locked tax placement):

```text
Base/Contextual Price (Pricing)
→ Promotion Evaluation (Promotion)
→ Tax/Fee/Shipping composition as defined later
→ Final Quote (Checkout/Pricing composition)
```

T011’s “promotions via Pricing quote step” means **quote pipeline composition**, not Promotion living inside Pricing tables.

Unresolved:

```text
Exact tax/fee ordering vs discount: NEEDS_LATER_P00_DETAIL
Exact shipping-charge composition authority: see 21-fulfillment.md (NEEDS_LATER_P00_DETAIL)
```

## H. Quote Determinism

Given the same:

```text
PromotionVersion
PricingVersion
Context
Items
Time
```

evaluation must be reproducible.

**Time is an explicit input** (`EffectiveAt`), not hidden ambient `DateTime.Now` inside the engine.

No random selection, unversioned “best guess,” or row-scan order as commercial outcome.

## I. Promotion Versioning

Promotions may change after a customer views PLP/PDP/Cart.

Preserve conceptually:

```text
PromotionId
PromotionVersion / Revision
Validity
Rule version
Benefit version
```

Quote/Order snapshot **applied** promotion evidence (ids, versions, amounts, funding, coupon ref where safe). Historical Order must not recompute against the current mutable promotion.

## J. Coupon Model

Coupon/code is a **trigger/claim**, not a price calculator.

Need conceptual support for:

```text
single shared code
unique generated codes
customer-bound code
organization-bound code
seller-bound code
campaign-bound code
limited redemption
```

Coupon binds to Promotion(s); Promotion owns benefit calculation. Coupon itself does not own Pricing.

## K. Coupon Security

Must consider:

```text
brute-force guessing
enumeration
case normalization
rate limiting
usage race
customer binding
tenant isolation
expiration
revocation
```

Do not log full sensitive/private coupon codes unnecessarily. Prefer hashed/redacted operational logs.

Exact entropy / format policy:

```text
NEEDS_LATER_P00_DETAIL
```

Security-driven UX may **intentionally** collapse some failure reasons (see AL / BC).

## L. Automatic Promotion

Promotions may apply with **no code** (category sale, seller campaign, cart threshold, quantity break, market campaign, free-shipping threshold).

Evaluation must still explain **which** promotion applied and **why**. Automatic ≠ invisible.

## M. Manual / Admin Adjustment

Do **not** conflate ad-hoc Admin price override with standard Promotion.

If allowed later, require: authorization, reason, audit, limits.

Exact manual-adjustment capability:

```text
NEEDS_LATER_P00_DETAIL
```

## N. Stacking / Combinability

Explicit policies, not accidental overlap:

```text
stackable
exclusive
best-of
priority/order
group-exclusive
coupon + automatic compatibility
seller promotion + platform promotion compatibility
```

Database row order must **not** determine discount outcome. Combinability is versioned with the Promotion.

## O. Conflict Resolution

When promotions conflict, evaluation is deterministic.

Possible concepts (not locked): priority, promotion group, exclusive flag, benefit comparison, policy strategy.

Exact strategy:

```text
NEEDS_LATER_P00_DETAIL
```

Unresolved conflict must **fail closed** to a defined policy result (no apply / exclusive winner / reject coupon) — never a guessed stack.

## P. Maximum Discount / Guardrails

Preserve future:

```text
max benefit
max quantity
max order discount
minimum price floor
seller subsidy limit
campaign budget
```

Combined promotions must not create **negative line totals**. Final payable Money must respect Pricing/Money invariants (no float money).

## Q. Funding Source

Marketplace requires funding **attribution**, distinct from customer-facing benefit.

Potential sources:

```text
Platform
Seller
Shared
Manufacturer/Partner future
```

Promotion preserves funding evidence on evaluation and Order snapshot for later settlement/accounting. Promotion does not execute settlement.

## R. Marketplace

Marketplace promotions may target: one seller, many sellers, one offer, category, platform-wide, seller-funded offer campaign, platform-funded campaign, shared-funding campaign.

```text
Seller-scoped promotion cannot apply to another seller's Offer.
```

Seller-funded benefits must not leak onto other sellers’ lines.

## S. Single-Store

Same Promotion architecture. Seller may be **implicit**. No forked discount engine. UX need not show seller or funding internals.

## T. B2B

Future B2B may require: organization-targeted promotion, contract exclusion, account-specific incentive, volume incentive, channel campaign, sales-rep code future.

Promotions must **not** override negotiated contract pricing unless an explicit later policy says so.

Pricing/Contract vs Promotion precedence:

```text
NEEDS_LATER_P00_DETAIL
```

## U. Quantity Pricing Boundary

Distinguish:

```text
authored quantity / contract price   → Pricing
promotion quantity discount         → Promotion
```

Both can change payable price. Different semantics, ownership, audit, and validity. Do not collapse quantity breaks in a price book into “a promotion.”

## V. Shipping Promotion

Free/discounted shipping is a **Promotion benefit**.

Fulfillment provides shipping **service/rate facts**. Checkout/Pricing composition applies customer-facing shipping charge and promotion.

```text
Do not mutate carrier raw rate.
```

See `docs/architecture/21-fulfillment.md` L.

## W. Payment-Method Promotion

Architecture may preserve payment-method as an **eligibility input**. Payment provider/PSP must **not** own promotion rules.

Legal/commercial permission for payment-method incentives:

```text
NEEDS_LATER_P00_DETAIL (product policy)
```

## X. Cart Boundary

Cart may show **estimated** promotion benefits. Cart is not Promotion authority.

Changes to items, quantity, seller, market, currency, or coupon trigger re-evaluation.

Long-lived cart display may go stale. Cart stores coupon/promotion **refs**, not live engine truth. See `docs/architecture/10-cart-checkout-order.md`.

## Y. Checkout Boundary

Checkout performs **authoritative** re-evaluation before Order placement.

If promotion expired, usage exhausted, or eligibility changed: clear recoverable UX. Do **not** silently preserve an invalid discount.

If Promotion authority is unavailable at this step: fail closed (see BC).

## Z. Order Snapshot

Order must snapshot:

```text
PromotionId
PromotionVersion
Benefit/Adjustment
Funding attribution
Coupon reference where safe
Eligibility/evaluation evidence where needed
```

Historical Order must not recompute current promotion rules. Later campaign edits do not rewrite sold orders.

## AA. Payment Boundary

Payment receives **final payable** from the accepted commercial quote/Order.

Payment must not independently apply coupon or promotion. Refunds may later allocate using snapshot adjustments; Promotion does not execute refunds. See `docs/architecture/11-payment.md`.

## AB. Refund / Cancellation Readiness

Promotions complicate partial cancellation, partial refund, and return.

Need preserved **per-line/order adjustment allocation** so later financial workflows can compute refundable amounts. Returns consume that snapshot; they do not re-run current Promotion rules (`docs/architecture/25-returns-rma.md`).

Exact refund allocation policy:

```text
NEEDS_LATER_P00_DETAIL
```

Full Returns/RMA is designed in `docs/architecture/25-returns-rma.md` (awaiting Architect ACCEPT of TB-P00-T026).

## AC. Redemption / Usage

First-class usage tracking for restricted promotions.

Potential constraints: total redemption limit, per customer, per organization, per seller, per coupon, per period.

Usage must handle **concurrency**. Do **not** use Analytics counters as eligibility truth.

## AD. Reservation of Limited Promotion

Scarce promotions/coupons race. Analyze (do not lock universal behavior):

```text
checked at checkout
reserved before order
consumed on order
released on cancellation/payment failure
```

Exact limited-redemption reservation strategy:

```text
NEEDS_LATER_P00_DETAIL
```

## AE. Time

Validity uses authoritative time abstraction (NodaTime/time direction where applicable).

Need: `StartsAt`, `EndsAt`, timezone / business-calendar semantics.

Do **not** depend on client clock. Evaluation uses `EffectiveAt` supplied by the application clock/policy.

## AF. Scheduling

Activation/deactivation may be scheduled. Background jobs/events **must** carry tenant context.

Even without a scheduler, evaluation respects validity timestamps at `EffectiveAt`.

## AG. Content / Campaign Integration

Content/Page Composition may surface: campaign landing, banner, promo strip, coupon explanation.

Content references Promotion by **opaque ID/contract**. Content does not calculate discount. See `docs/architecture/12-content-page-composition.md`.

## AH. SEO

Promotion/campaign routes may be indexable or noindex per SEO policy.

Promotion engine does **not** decide canonical/indexation. Expired campaign handling belongs to SEO/Content route policy. See `docs/architecture/13-seo-architecture.md`.

## AI. Search

Search may consume a **stale projection**: has promotion, discount range, campaign badge.

Search is not discount authority. Checkout re-evaluates. Do not sell from a search badge. See `docs/architecture/14-search-indexing.md`.

## AJ. Display Price / Badge

Storefront may show: original price, discounted price, discount percent, campaign badge, coupon-required label.

UI must distinguish: automatic discount, coupon-only benefit, starting-from offer discount, seller-specific discount.

Avoid a **global** sale badge when only one seller/condition qualifies.

```text
Backend/module boundary != UI boundary
Build PASS != UI ACCEPT
Functional PASS != Visual ACCEPT
Desktop PASS != Mobile PASS
LTR PASS != RTL PASS
```

## AK. Promotion Explainability

Evaluation should produce explainable results: applied promotions, rejected coupon reason, conflict reason, benefit amount, affected lines, funding source.

Do **not** expose internal abuse-prevention details (rate-limit internals, entropy, sibling-code existence).

Needed for customer support and Admin UX.

## AL. Customer UX

Professional UX must cover: coupon apply/remove, success, invalid, expired, not eligible, usage exhausted, minimum threshold not reached, seller/offer mismatch, stacking conflict, promotion changed at checkout.

Do not use a single generic “invalid code” for every case **unless** security requires ambiguity (enumeration/brute-force).

Loading, empty, error, Desktop, Mobile, RTL, LTR, and accessibility are product requirements for future UI — not this document’s implementation.

## AM. Admin UX

Promotions are **not** raw CRUD.

Workflow direction: Promotion Library, Draft, Scheduled, Active, Paused/Disabled, Expired, Eligibility, Benefits, Coupon configuration, Combinability, Funding, Usage, Preview/Test, Audit, Performance link to Analytics.

Guardrails against dangerous configs (negative totals, unbounded stack, cross-seller leak, currency mismatch).

## AN. Seller UX

Marketplace Seller panel may create **seller-scoped** promotions where policy permits.

Need: seller-owned offers only, funding clarity, date window, budget/usage, approval workflow future, scoped analytics.

Sellers must not affect other sellers or platform-funded rules they do not own.

## AO. Preview / Simulation

Future Admin tooling: simulate before activation.

Inputs: sample cart, market, seller, customer/account, coupon, time.

Output explains eligibility/benefit. No production implementation now.

## AP. Approval / Governance

High-impact promotions may require future approval (large discount, platform-funded campaign, seller subsidy above threshold).

SpiceDB governs who can create/approve/activate. Do **not** hardcode role names.

## AQ. Authorization

Potential permissions (names not locked): view promotion, create, edit draft, approve, activate, pause, manage coupon, view usage, view funding, seller-create-own.

SpiceDB remains authorization authority. UI hiding is not the security boundary. See `docs/architecture/05-spicedb-authorization.md`.

## AR. Audit

Durable business audit for: rule change, benefit change, activation, pause, coupon creation/revocation, funding change, limit change, manual override.

Technical logs are insufficient. See `docs/architecture/18-observability-logging-audit.md`.

## AS. Analytics

Analytics may observe: promotion impression, coupon attempt, coupon success/failure **category**, promotion conversion, revenue, seller/platform funding impact.

Analytics does **not** own eligibility or redemption truth. See `docs/architecture/16-first-party-analytics.md`.

## AT. Budget

Future campaign budget may constrain promotions.

Do not treat Analytics spend counters as budget authority.

If budget-limited promotions are supported later, consumption needs durable concurrency-safe authority.

Detailed budget subsystem:

```text
DEFERRED / NEEDS_LATER_DETAIL
```

## AU. Multi-Currency

Monetary benefits **must** carry Currency.

Do not apply a fixed USD amount as the same number in EUR/IRR.

Promotion may be: market/currency-specific, percentage, currency-specific fixed amount.

FX-derived promotion amounts require **explicit** policy; do not silently convert. Currency mismatch at evaluation: fail that benefit (see BC).

## AV. Rounding

Promotion allocation may require rounding. Must be deterministic and compatible with Pricing/Money rules. No floating-point money.

Exact allocation/rounding algorithm:

```text
NEEDS_LATER_P00_DETAIL
```

## AW. Tax Interaction

Tax ordering can materially affect totals. Do **not** guess jurisdictional rules. Tax is a separate module (`docs/architecture/26-tax-architecture.md`). Promotion is not a tax rate.

Architecture must **allow** policies such as: discount before tax, discount allocation across taxable lines, shipping-discount tax treatment.

```text
Tax ordering vs promotion: NEEDS_LATER_P00_DETAIL
```

## AX. Rule Representation

Do not prematurely build end-user scripting.

Later candidates: typed rule model, composable conditions/actions, limited DSL, policy objects.

Hard rule:

```text
No arbitrary executable code from Admin/DB
```

Rules must be validated and versioned.

## AY. Engine Complexity

Architecture-ready does **not** mean implementing every theoretical promotion type now.

Recommend incremental **typed** extensibility. Avoid a universal enterprise rule engine as the first implementation.

## AZ. Promotion Events

Candidate events (names not locked): `PromotionActivated`, `PromotionChanged`, `PromotionPaused`, `PromotionExpired`, `CouponRedeemed`, `RedemptionReleased`.

Consumers: Search projections, cache invalidation, Analytics, Audit, Content/Campaign projections.

Outbox readiness. Tenant context on every event. No cross-module table writes.

## BA. Cache

Published/active promotion **configuration** may be cacheable.

Cache key dimensions may include: Tenant, Market, SalesChannel, Seller, PromotionVersion.

Authoritative checkout evaluation must **not** trust stale cached **eligibility** when limits/redemptions are scarce.

```text
Cache != promotion usage authority
```

See `docs/architecture/19-caching-infrastructure-abstractions.md`.

## BB. Observability

Need: evaluation latency, coupon failure categories, redemption conflicts, promotion application rate, engine errors, stale projection mismatch, future budget/usage anomalies.

Integrate with OpenTelemetry. Telemetry ≠ business audit ≠ Analytics KPIs.

## BC. Failure Matrix

| Case | Fail closed? | Retry? | Re-evaluate? | Remove benefit? | Customer message? | Admin alert? |
| --- | --- | --- | --- | --- | --- | --- |
| Coupon invalid | Yes (no apply) | No | N/A | Yes if previously shown | Invalid / not recognized (may be ambiguous if anti-enumeration) | Low |
| Coupon expired | Yes | No | N/A | Yes | Expired | Low |
| Coupon exhausted | Yes | No | N/A | Yes | Usage exhausted | Medium if unexpected spike |
| Promotion expired during checkout | Yes | No | Yes | Yes; do not keep stale discount | Promotion changed / no longer available | Low |
| Seller offer changed | Yes if ineligible | N/A | Yes | Yes for that seller’s lines | Offer/seller mismatch or line updated | Low |
| Price changed | Re-quote | N/A | Yes | Recalculate; do not keep old amount | Price/promotion updated | Low |
| Usage race | Winner consumes; loser closed | Idempotent consume | Yes for loser | Loser: remove | Usage exhausted | Medium |
| Stacking conflict | Policy result, not guess | No | Yes | Per combinability | Stacking conflict / not combinable | Low |
| Promotion engine unavailable at **authoritative checkout** | **Yes** — do not invent/retain unverified discount | Bounded retry then fail | After recovery | Remove unverified | Temporary unavailability; cannot apply discount | **Yes** |
| Promotion engine unavailable on Cart estimate | No (degraded estimate) | Optional | Later at checkout | Hide estimated discount | Optional “estimate unavailable” | Medium |
| Invalid rule config | Yes (skip/fail that promotion) | After config fix | Yes | Do not apply broken rule | Generic unavailability if customer-facing | **Yes** |
| Currency mismatch | Yes for that benefit | No | N/A | Yes | Not available in this currency | Medium |
| Cross-tenant promotion leak | **Yes** deny | No | N/A | N/A | Denied / not found | **Yes** |
| Search shows stale badge | No (search is projection) | N/A | Checkout yes | Checkout removes if invalid | Checkout explains change | Optional freshness |

Critical: if Promotion authority is unavailable during **authoritative checkout evaluation**, do not silently invent or retain a discount.

## BD. Data Ownership Matrix

Marks: `OWNER` | `SOURCE` | `REFERENCE` | `CONSUMER` | `PROJECTION` | `NOT_OWNER`

| Fact | Promotion | Pricing | Cart | Checkout | Order | Payment | Fulfillment | Seller | Content | Search | Analytics | Authorization |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Base / contextual price | NOT_OWNER | OWNER | CONSUMER | CONSUMER | OWNER (snapshot) | CONSUMER | NOT_OWNER | REFERENCE | NOT_OWNER | PROJECTION | CONSUMER | NOT_OWNER |
| Promotion rule | OWNER | NOT_OWNER | REFERENCE | CONSUMER | REFERENCE (snapshot) | NOT_OWNER | NOT_OWNER | REFERENCE (seller-scoped) | REFERENCE | NOT_OWNER | NOT_OWNER | CONSUMER |
| Coupon | OWNER | NOT_OWNER | REFERENCE (claim) | CONSUMER | REFERENCE (safe) | NOT_OWNER | NOT_OWNER | REFERENCE (seller-bound) | REFERENCE | NOT_OWNER | OBSERVATION via events | CONSUMER |
| Discount adjustment | OWNER (evaluation) | CONSUMER (compose) | PROJECTION (estimate) | CONSUMER | OWNER (accepted snapshot) | CONSUMER | NOT_OWNER | CONSUMER | NOT_OWNER | PROJECTION | CONSUMER | NOT_OWNER |
| Shipping rate (carrier/raw) | NOT_OWNER | CONSUMER | CONSUMER | CONSUMER | REFERENCE | NOT_OWNER | OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | NOT_OWNER |
| Shipping discount | OWNER (benefit) | CONSUMER (compose) | PROJECTION | CONSUMER | OWNER (snapshot) | CONSUMER | NOT_OWNER (raw rate unchanged) | REFERENCE | NOT_OWNER | PROJECTION | CONSUMER | NOT_OWNER |
| Redemption count | OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | REFERENCE | NOT_OWNER | NOT_OWNER | CONSUMER (own) | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER |
| Final payable | NOT_OWNER | SOURCE (quote compose) | NOT_OWNER | CONSUMER | OWNER | CONSUMER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | NOT_OWNER |
| Funding source | OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | OWNER (snapshot) | NOT_OWNER | NOT_OWNER | CONSUMER | NOT_OWNER | NOT_OWNER | CONSUMER | CONSUMER |
| Campaign copy | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | OWNER | PROJECTION | CONSUMER | CONSUMER |
| Discount badge projection | SOURCE (ids/flags) | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | OWNER (index) | CONSUMER | NOT_OWNER |
| Conversion metric | NOT_OWNER | NOT_OWNER | NOT_OWNER | SOURCE (facts) | SOURCE | SOURCE | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | OWNER (observation) | NOT_OWNER |
| Activation permission | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | OWNER |

Coupon on Cart is a **claim reference**, not redemption authority. Order snapshot is commercial truth after accept. Analytics conversion remains observation of Order/Payment facts.

## BE. Testing Strategy — Architecture Level

Future implementation must test: automatic promotion; coupon promotion; stacking; exclusive promotion; seller-scoped promotion; platform-funded promotion; multi-currency; quantity threshold; cart threshold; usage-limit race; expiration at checkout; coupon normalization; tenant isolation; B2B target; shipping discount; order snapshot; later partial refund allocation; Admin simulation; RTL/mobile promotion UX.

No tests now.

## BF. Decision Summary

### RECOMMENDED_FOR_ADR

1. Promotion is separate from Pricing, Coupon, Content, Order and Payment.
2. Coupon is a trigger/claim mechanism, not the Promotion domain itself.
3. Pricing provides contextual/base quote inputs; Promotion returns conditional adjustments.
4. Promotion evaluation is deterministic and versioned.
5. Order snapshots applied promotion evidence; historical orders do not re-evaluate current rules.
6. Locale/Market/Currency remain separate.
7. Marketplace seller/platform funding attribution is first-class.
8. Seller-scoped promotion cannot leak across seller Offers.
9. Authored quantity/contract price is distinct from promotional quantity discount.
10. Shipping promotion modifies customer shipping charge, not carrier raw rate.
11. Cart display is estimate; Checkout re-evaluates authoritatively.
12. Stacking/combinability is explicit and deterministic.
13. Scarce redemption/usage is concurrency-safe and not analytics-based.
14. Fixed monetary benefits are currency-aware.
15. Promotion rule configuration never executes arbitrary code.
16. Content/Search/SEO consume promotion references/projections only.
17. SpiceDB governs create/approve/activate/pause scopes.
18. Significant promotion changes are audited.
19. Admin/Seller Promotion UX is workflow-oriented with simulation/preview readiness, not CRUD.
20. Promotion failure at authoritative checkout must not silently retain an unverified benefit.

Do not create final ADR yet.

### NEEDS_LATER_P00_DETAIL

- Exact first-release benefit-type subset
- Combinability / conflict-resolution strategy
- Pricing/Contract vs Promotion precedence (B2B)
- Limited-redemption reservation (check vs reserve vs consume vs release)
- Coupon entropy / format policy
- Manual Admin adjustment capability
- Tax ordering vs discount (and shipping-discount tax treatment)
- Money allocation / rounding algorithm
- Refund/partial-cancellation allocation policy
- Payment-method incentive product/legal policy
- Exact shipping-charge composition authority (shared with T022)

### DEFERRED

- Implementation, schemas, coupon APIs, rule engine/DSL, Admin/Seller UI
- Campaign budget subsystem (`DEFERRED / NEEDS_LATER_DETAIL`)
- Gift item / wallet / referral as Promotion benefits (`TEMPLATE_PRESENT / PRODUCT_DECISION_PENDING`)
- Manufacturer/partner funding, sales-rep codes
- Full Returns/RMA, Tax engine, Notifications implementation
- Shopeiva, P00 Gate, ADR

---

## Remaining P00 Capability Gaps After T023

Architect-review input only. This subsection does **not** design these capabilities.

| Capability | Classification | Reasoning |
| --- | --- | --- |
| Reviews / Ratings | `DEDICATED_TASK_REQUIRED` | Listed as SUPPORTING in the capability map; storefront/AI already assume reviews. No dedicated architecture package (moderation, order-verified purchase, seller scope, aggregation). T002 boundary is not enough for P00 Gate completeness. |
| Notifications | `DEDICATED_TASK_REQUIRED` | Many domains emit facts “for Notifications,” but there is no channel/template/tenant/consent/dispatch architecture. Order, Fulfillment, Promotion, and Identity will otherwise improvise outbound messaging. |
| Support | `NEEDS_USER_PRODUCT_DECISION` | Capability map marks Customer Service / Support as SUPPORTING with tickets “if confirmed later.” Template tickets are not a USER-confirmed first-release requirement. Confirm whether Support is in first sellable scope before a dedicated P00 task. |
| Returns / RMA | `DEFER_POST_P00` | T022 already treats full Returns/RMA as a separate future capability. Promotion refund **allocation** is later P00 money detail, not a substitute for RMA. Reclassify to dedicated task if USER requires returns in first sellable release. |
| Tax | `DEDICATED_TASK_REQUIRED` | Quote/Order payable money is incomplete without tax policy. T009 and this document leave tax engine and discount-vs-tax ordering unresolved. Guessing jurisdiction in Promotion would be incorrect. |
| Fraud / Risk | `BOUNDARY_SUFFICIENT_FOR_P00` | Coupon brute-force, enumeration, rate-limit, tenant isolation, and checkout fail-closed are covered here; Payment already bounds PSP/risk at the payment edge. A dedicated fraud-scoring/risk platform is not required to close Promotion architecture; treat full Fraud/Risk as `DEFER_POST_P00` unless USER requires it before first sellable. |
