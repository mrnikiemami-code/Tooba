# Tooba — Tax Architecture

Status:

```text
P00 architecture design — candidate for later ADR; not an ADR lock; not a Gate
```

Task:

```text
TB-P00-T027
```

Documentation only. No tax engine code, tables, Admin UI, schemas, Iranian law defaults, UK VAT implementation, B2B VAT/invoices, T028, or P00 Gate.

```text
Tax != Pricing
Tax != Promotion
Tax != Order
Tax != Payment
Tax != Invoice
Tax != Jurisdiction Law Source
Locale != Market != Currency != Tax Jurisdiction
Backend/module boundary != UI boundary
```

USER example `10% from 1/1/1405 to 10/10/1406` is a **configurable rule illustration**, not Iranian tax law, and MUST NOT be encoded as a legal default.

## A. Locked Product Decisions

```text
First commercial emphasis = Iran
Near-future architecture readiness = UK + additional markets
Base/displayable commercial price source = tax-exclusive
Tax is calculated separately
Tax policy is configurable by market/context
Admin can define date-effective percentage rules
Admin may enter an allowed context-specific tax percentage only if policy/configuration permits
Tax-exempt treatment exists
B2B tax/VAT/invoice-specific functionality is out of initial scope
```

Do not infer legal rates or legal deadlines.

## B. Core Separation

**Pricing** owns tax-exclusive commercial/base pricing. See `docs/architecture/08-pricing-market-currency.md`.

**Tax** owns tax determination/calculation policy and tax calculation result.

**Promotion** owns incentives/adjustments; it is not a tax engine.

**Order** snapshots accepted tax calculation results.

**Payment** executes collection/refund of the payable total; it does not calculate tax.

**Future Invoice** may consume tax evidence; it is not Tax authority.

## C. Tax-Exclusive Pricing

```text
Commercial/Base Price = Tax Exclusive
```

Tax is a separate component. Conceptual composition (ordering **versioned**):

```text
Tax-Exclusive Price
+/- Promotion Adjustments according to policy
+ Tax
+ Fees/Shipping according to ordering policy
= Final Payable
```

Do not bury tax inside Product.Price.

Exact promotion-vs-tax-vs-shipping order: `NEEDS_LATER_P00_DETAIL` as a versioned policy, not ad-hoc per screen.

## D. Configurable Tax Rule

First-class Tax Rule (not a schema): TaxRuleId; Market/Jurisdiction context; Tax Category; Rate; StartsAt; EndsAt; Priority; Status; Version; Exemption policy; Override policy.

```text
Tax rate is data/configuration, not hard-coded source logic.
```

## E. Effective-Dated Rules

Validity periods: Rate X valid from Date A until Date B (pattern only).

Need: non-overlap validation where policy requires; deterministic resolution; version/history; future scheduled activation.

Do not hard-code Persian-calendar dates internally.

## F. Time Representation

Domain/system time uses Tooba canonical instant/date. Iran UI may display/input Jalali.

Do not use Jalali/Persian date strings as domain keys.

## G. Market / Jurisdiction

```text
Locale != Market != Currency != Tax Jurisdiction
```

Iran first does **not** mean `fa-IR => Iranian Tax Rule`.

Architecture remains ready for UK and other jurisdictions. Jurisdiction resolution policy later.

## H. Tax Category

Catalog may reference TaxCategoryId via contract. Catalog does not calculate tax.

Candidate labels (not legal lock): Standard; Reduced; Zero-rated; Exempt; special later.

## I. Tax Exempt

Explicit exempt treatment with reason/evidence.

Potential: product/category exemption; market/jurisdiction exemption; authorized manual exception; future customer exemption (seam only).

Distinguish:

```text
TAX_EXEMPT
ZERO_RATE
NO_APPLICABLE_RULE
CALCULATION_ERROR
```

No tax because exempt ≠ missing configuration.

## J. Manual / Context-Specific Rate Override

Allowed only if configuration/policy **explicitly** enables it.

Require: feature enabled; SpiceDB; reason/context; valid range; percentage semantics independent of currency; effective scope; audit; versioning.

Do not allow silent per-order tax editing by default. Client-submitted tax rate is not trusted.

## K. Override Precedence

Deterministic conceptual order:

```text
Explicit authorized Tax Exemption
→ Explicit authorized Context Tax Override
→ Matching configured Tax Rule
→ Fallback policy / Fail-closed behavior
```

Nondeterminism forbidden. DB row order must not decide tax.

## L. Rule Matching Dimensions

Initial/future: Tenant; Market; Tax Jurisdiction; Tax Category; Product/Service type; Shipping taxability; Sales Channel later; Transaction date; Customer context later.

Do not overbuild all dimensions in first implementation. Must extend without rewriting Checkout.

## M. Tax Calculation Contract

Conceptual `ITaxCalculator` (name not locked).

Inputs: Tenant; Market; Jurisdiction; Currency; Tax date/time; line items; tax-exclusive amounts; promotion-adjusted taxable basis; tax categories; shipping amount/category; authorized override/exemption context.

Output: structured, explainable. No provider-specific types.

## N. Tax Calculation Result

Preserve: calculation version; rule IDs/versions; taxable basis; rate; tax amount; exemption/zero-rate marker; line allocation; shipping tax if any; rounding evidence; jurisdiction context; timestamp/effective date.

## O. Determinism

Same TaxRuleVersion + TaxCalculationPolicyVersion + commerce context + amounts + effective date ⇒ same result.

No hidden ambient clock. Historical Order never recomputes from “today’s rule”.

## P. Tax Rule Versioning

Immutable/versioned evidence for historical transactions. Order stores applied tax evidence.

## Q. Pricing Boundary

Pricing returns tax-exclusive commercial price. Tax consumes taxable monetary basis.

```text
Pricing does not own jurisdictional tax rules.
```

## R. Promotion Boundary

Promotions adjust taxable basis according to versioned ordering policy. Promotion is not tax. Do not encode tax as a “promotion of 10%”.

## S. Shipping / Fulfillment Boundary

Shipping taxability is a Tax hook (shipping category / amount). Fulfillment does not calculate tax. Fulfillment may supply shipping amount/context via contract.

## T. Cart Boundary

Cart may **estimate** tax via Tax contract for display. Estimate ≠ Order snapshot. Stale estimate must be revalidated at checkout.

## U. Checkout Boundary

Checkout must call Tax (or consume a just-issued calculation) before placement. Fail closed on `NO_APPLICABLE_RULE` or `CALCULATION_ERROR` for taxable items.

Do not place an order with invented zero tax.

## V. Order Snapshot

Order stores tax result evidence: amounts, rates, rule versions, basis, rounding. Later rule changes do not rewrite history.

## W. Payment Boundary

Payment charges/refunds **payable total**. Payment does not calculate tax. Refund tax allocation uses Order/Returns snapshots.

## X. Refund / Returns Boundary

Returns instruct refund using **historical tax snapshot**. See `docs/architecture/25-returns-rma.md`. Tax module may re-explain snapshot; it does not apply current rates to old lines unless policy later defines a documented exception (not initial).

## Y. Marketplace

Platform Tax policy is market/jurisdiction scoped. Seller does not own jurisdiction law.

Seller-funded vs platform-funded commercial amounts remain Promotion/Order; tax calculation still uses Tax rules + line basis.

No cross-seller tax config leakage.

## Z. Single-Store

Same Tax architecture. Tenant-specific Tax configuration in one shared publish. Tenant isolation mandatory.

## AA. Tenant Isolation

No cross-tenant rule or calculation leakage. Cache keys include tenant.

## AB. UK / Multi-Market Readiness

Do not implement UK VAT now. Preserve: multiple jurisdictions; multiple components later; tax-exclusive base; category classification; effective dating.

Iran-first UX must not hard-wire one jurisdiction into Checkout.

## AC. Multiple Tax Components

Initial UX may be one percentage rule. Architecture must allow multiple components later (e.g. future VAT breakdown) without rewriting Order snapshot shape conceptually (list of tax lines).

## AD. Rounding

Need explicit rounding policy per calculation version (line vs document). Exact mode: `NEEDS_LATER_P00_DETAIL`. Evidence of rounding is part of the result.

## AE. Currency

Tax amount is in the transaction currency of the quote/order. Do not tax after an undocumented FX. FX remains Pricing/Order snapshot concern.

## AF. Tax Configuration

Admin configures rules, categories, enablement of overrides, market/jurisdiction binding. Configuration is Tax-owned data, not Catalog fields.

## AG. Admin UX

**Tax Configuration Workspace**, not raw CRUD: markets/jurisdictions, categories, effective-dated rules, preview/simulation, overlap validation, override policy, audit.

```text
Backend/module boundary != UI boundary
Build PASS != UI ACCEPT
Functional PASS != Visual ACCEPT
Desktop PASS != Mobile PASS
LTR PASS != RTL PASS
```

Persian date input is UI; canonical date is domain.

## AH. Manual Tax Override UX

Only when enabled: authorized context, reason required, visible audit, cannot look like a hidden checkout cheat. Disabled by default.

## AI. Storefront UX

Show tax-exclusive base and tax and payable according to market presentation policy. Must not mislead that exclusive price is the final payable.

Loading/error if tax cannot be calculated. RTL/LTR, mobile.

## AJ. SEO / Structured Data

Offer price in structured data must match **visible** commercial policy. Do not emit a tax-inclusive price if the page shows exclusive + tax, or vice versa. See `docs/architecture/13-seo-architecture.md`. Do not invent tax.

## AK. Search

Search may project display price. If ranking/display uses payable, it consumes Tax/Pricing projections, not Catalog tax columns. Search does not own tax.

## AL. Authorization

SpiceDB: who may configure rules; enable overrides; apply authorized override; view tax audit; simulate.

UI hide ≠ security.

## AM. Audit

Durable audit: rule create/change/activate/expire; override apply; exemption apply; calculation policy version change.

Actor, reason, target, before/after, correlation. Technical logs insufficient.

## AN. Validation

Detect: invalid percentage; invalid date range; overlapping conflicts; missing category; cross-tenant ref; unsupported currency/context; unauthorized override.

No silent ambiguity.

## AO. Rule Conflict

Explicit precedence/priority. Never TOP 1 / first row / latest insert / DB order.

## AP. Missing Rule

Taxable item + no applicable rule ⇒ `NO_APPLICABLE_RULE`: fail closed; configuration error; admin alert; customer-recoverable checkout error.

Do not assume zero. Explicit `TAX_EXEMPT` may yield zero with evidence.

## AQ. Cache

Published rules may be cached. Keys: Tenant; Market/Jurisdiction; TaxCategory; RuleVersion; effective-date bucket where safe.

Checkout must use correct effective version. Cache ≠ tax truth.

## AR. Invalidation

On rule activation/change: tax config cache; cart estimates; price display projections; search projections if final-price aware.

Historical Orders unaffected.

## AS. Events

Candidates (names not locked): TaxRuleActivated; TaxRuleChanged; TaxRuleExpired; TaxConfigurationChanged; TaxCalculationCompleted; TaxOverrideApplied.

Consumers: cache; Search/display; Analytics; Audit; ops alerts.

## AT. Analytics

May observe tax amount, exempt count, override usage, calculation failure. Analytics does not own rules/results. Legal reporting ≠ Analytics.

## AU. Observability

Metrics: latency; resolution failure; missing rule; conflict; override rate; cache staleness; checkout tax failure. OpenTelemetry. See `docs/architecture/18-observability-logging-audit.md`.

## AV. Reconciliation

Readiness: missing Order tax snapshot; calculated tax ≠ composed total; override without audit; expired rule still active; wrong tenant rule; refund tax allocation mismatch.

No implementation now.

## AW. Testing Strategy — Architecture Level

Future tests: percentage rule; effective date boundaries; scheduled transition; exempt; zero rate; override on/off; unauthorized override; conflict; missing rule; tenant/market isolation; promotion interaction; shipping hook; partial refund snapshot; historical order after rate change; Persian UI date → canonical date; RTL/mobile tax presentation.

No tests in this task.

## AX. Data Ownership Matrix

| Fact | Tax | Pricing | Promotion | Catalog | Cart | Checkout | Order | Payment | Returns | Fulfillment | Search | SEO | Analytics | Authorization | Audit |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| tax-exclusive base price | CONSUMER | OWNER | CONSUMER | NOT_OWNER | CONSUMER | CONSUMER | SNAPSHOT | NOT_OWNER | CONSUMER | NOT_OWNER | PROJECTION | PROJECTION | CONSUMER | CONSUMER | CONSUMER |
| tax rule config | OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | CONSUMER |
| tax calculation result | OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | ESTIMATE | CONSUMER | SNAPSHOT | CONSUMER (total) | CONSUMER (snapshot) | NOT_OWNER | PROJECTION | PROJECTION | CONSUMER | CONSUMER | CONSUMER |
| tax category ref | SOURCE | NOT_OWNER | NOT_OWNER | REFERENCE | CONSUMER | CONSUMER | SNAPSHOT | NOT_OWNER | CONSUMER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | CONSUMER |
| promotion adjustment | CONSUMER (basis) | CONSUMER | OWNER | NOT_OWNER | CONSUMER | CONSUMER | SNAPSHOT | NOT_OWNER | CONSUMER | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | CONSUMER | CONSUMER |
| payable total | CONSUMER | CONSUMER | CONSUMER | NOT_OWNER | ESTIMATE | CONSUMER | OWNER (snapshot) | CONSUMER | CONSUMER | NOT_OWNER | PROJECTION | PROJECTION | CONSUMER | CONSUMER | CONSUMER |
| refund tax | SOURCE (explain snapshot) | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | SOURCE | OWNER (tx) | INSTRUCTION | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | CONSUMER | CONSUMER |

## AY. Failure Matrix

| Failure | fail closed? | retry? | customer-visible | admin alert? |
| --- | --- | --- | --- | --- |
| Missing applicable rule | Yes (checkout/place) | After config | Recoverable error | Yes |
| Calculation error | Yes | Yes | Error | Yes |
| Rule conflict unresolved | Yes | After config | Error | Yes |
| Unauthorized override | Yes deny | No | Denied | Yes |
| Override feature disabled | Ignore/deny client rate | N/A | N/A | No |
| Cache stale vs new rule | Checkout uses versioned authority | Invalidate | Estimate may lag | If SLA |
| Cross-tenant rule | Yes deny | No | Deny | Yes |
| Persian date parse UI error | Yes (config save) | User correct | Field error | No |
| Refund without tax snapshot | Hold refund tax portion | Rebuild from Order if possible | Pending | Yes |

## AZ. Decision Summary

| # | Decision | Classification |
| --- | --- | --- |
| 1 | Tax separate from Pricing, Promotion, Order, Payment | RECOMMENDED_FOR_ADR |
| 2 | Initial commercial prices tax-exclusive | RECOMMENDED_FOR_ADR |
| 3 | Tooba calculates Tax separately in first release | RECOMMENDED_FOR_ADR |
| 4 | Rules are configurable data, never hard-coded jurisdiction/rate | RECOMMENDED_FOR_ADR |
| 5 | Rules effective-dated and versioned | RECOMMENDED_FOR_ADR |
| 6 | Iran first-market emphasis; jurisdiction-neutral; UK/multi-market ready | RECOMMENDED_FOR_ADR |
| 7 | Locale ≠ Market ≠ Currency ≠ Tax Jurisdiction | RECOMMENDED_FOR_ADR |
| 8 | Catalog references Tax Category; never calculates Tax | RECOMMENDED_FOR_ADR |
| 9 | TAX_EXEMPT, ZERO_RATE, NO_APPLICABLE_RULE, CALCULATION_ERROR distinct | RECOMMENDED_FOR_ADR |
| 10 | Manual/context override disabled unless policy enables | RECOMMENDED_FOR_ADR |
| 11 | Overrides require SpiceDB, reason, audit | RECOMMENDED_FOR_ADR |
| 12 | Calculation deterministic and explainable | RECOMMENDED_FOR_ADR |
| 13 | Order snapshots tax evidence; never “today’s rule” | RECOMMENDED_FOR_ADR |
| 14 | Payment consumes final total; does not calculate Tax | RECOMMENDED_FOR_ADR |
| 15 | Returns uses historical Tax snapshot | RECOMMENDED_FOR_ADR |
| 16 | Missing rule does not silently become zero | RECOMMENDED_FOR_ADR |
| 17 | Conflicts deterministic; never row order | RECOMMENDED_FOR_ADR |
| 18 | One-percentage UX does not block multiple components later | RECOMMENDED_FOR_ADR |
| 19 | Single-Store tenant-specific Tax in one shared publish | RECOMMENDED_FOR_ADR |
| 20 | B2B Tax/VAT/invoice out of initial phase | RECOMMENDED_FOR_ADR |
| 21 | Admin: Tax Configuration Workspace, not CRUD | RECOMMENDED_FOR_ADR |
| 22 | Storefront must not mislead final payable | RECOMMENDED_FOR_ADR |
| 23 | Backend/module ≠ UI | RECOMMENDED_FOR_ADR |
| 24 | Tax UI requires visual evidence and Architect visual ACCEPT | RECOMMENDED_FOR_ADR |
| — | Exact promotion/tax/shipping composition order | NEEDS_LATER_P00_DETAIL |
| — | Rounding mode | NEEDS_LATER_P00_DETAIL |
| — | Jurisdiction resolution algorithm | NEEDS_LATER_P00_DETAIL |
| — | UK VAT implementation | DEFERRED |
| — | B2B VAT number / tax invoice | DEFERRED |
| — | Legal compliance engine | DEFERRED |

Do not create a final ADR in this task.

## B2B Scope Record

```text
B2B tax/VAT-number/tax-invoice functionality = OUT OF INITIAL PHASE
```

Keep generic seams (customer context later, exemption reason types) without designing B2B VAT now.

## P00 Gap Status After Tax

```text
Reviews / Ratings = COMPLETE
Returns / RMA = COMPLETE
Tax = current task pending Architect acceptance
Notifications = BOUNDARY_SUFFICIENT_FOR_P00
Fraud / Risk = BOUNDARY_SUFFICIENT_FOR_P00
Support = DEFER_POST_P00 unless USER later promotes it
```

Assessment:

```text
P00 Gate candidate after Architect accepts T027
```

Cursor does not issue P00-GATE or TB-P00-T028.
