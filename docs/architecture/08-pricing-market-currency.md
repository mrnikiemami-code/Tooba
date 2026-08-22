# Tooba — Pricing, Market & Currency Architecture

Status:

```text
P00 architecture design — candidate for later ADR; not an ADR lock
```

Task:

```text
TB-P00-T009
```

Documentation only. No formulas, FX vendor, tax engine, or checkout code.

```text
Locale != Market != Currency
Product != Price
Offer != Price
Price != Promotion
Authored Price != FX-Derived Price
```

## A. Core Invariants

Pricing is contextual commercial logic, not a scalar on Product or Offer. Offer may **reference** a price book/list; it does not own the quote.

## B. Pricing Context

Conceptual quote inputs (not all required every time):

```text
ProductId / VariantId
OfferId
SellerId
Market
Sales Channel
Currency
BuyerPartyId
OrganizationId
CommercialAccountId
ContractId
Quantity
Promotion Context
Tax/Fee Context
Requested At / Effective At
```

Pricing consumes these via **contracts/projections**, never Party/Catalog table joins.

## C. Market

Market is commercial geography/policy (who can be sold to, which price list, tax region hints), **not** UI language and **not** money code.

One Market may support multiple Currencies. One Currency may appear in many Markets.

## D. Market Ownership

Market / Commercial Context module owns Market definition and policies (T002/T004). Pricing **consumes** MarketId. Catalog availability per market is eligibility, not a price.

## E. Currency

Currency is an ISO-style money code plus precision/rounding **policy** owned as kernel/commercial config, not Locale.

Display currency may differ from charge currency; that is a conversion/checkout policy (T), not “the product’s currency.”

## F. Money Value Object Direction

Recommend a **decimal/integer-minor-units** money value object with explicit currency and scale.

Do **not** recommend IEEE floating-point as money storage or arithmetic.

Exact type/library: later. Rounding policy: `NEEDS_LATER_P00_DETAIL` (half-even vs half-up per currency).

## G. Authored Price

A human- or system-authored amount in a **specific Market + Currency + book/scope** (list, seller offer list, contract). Provenance: who authored, when, source book.

Authored prices are Pricing write-model truth for that scope.

## H. FX-Derived Price

Display or conversion using an FX rate from authored currency to another currency.

Must record: rate, provider, timestamp, from/to currency. Never silently overwrite authored price with a converted number as if it were authored.

## I. FX Provider Boundary

FX is an **adapter**: internal rate contract, not a vendor SDK in Pricing domain. Provider not chosen. Stale/unavailable rates fail closed for **charge** conversion; display may degrade per later policy.

## J. Price Book

Price Book groups authored prices: list/book id, market, currency, channel, validity, seller or contract scope.

Offer and B2B contracts **reference** books; they do not copy every cell into Offer.

## K. Offer Pricing

Marketplace: each Offer may bind to seller-authored prices in Pricing for that Offer/Seller/Market.

Offer != Price: changing stock does not change the book; changing the book does not rewrite Catalog.

## L. B2B / Organization Pricing

Quote context may include Organization, CommercialAccount, Contract. Contract prices are Pricing facts keyed by those ids (T007). Party module does not store list prices.

## M. Quantity Pricing

Tiers/breaks are Pricing rules on a book/contract, not Catalog attributes. Quote must pass Quantity.

## N. Promotion Separation

Promotions modify a quote; they are not base authored price. Promotion module owns definitions; Pricing applies them as a **modifier step** in the quote pipeline, with snapshot of applied promotions on Order.

## O. Effective Price / Quote

A Quote is a **deterministic** result of context + books + promotions + tax/fee policy (if in quote) at Effective At.

Public PDP/PLP show quote or projection of quote, not a Product.Price column.

## P. Quote Validity

Quotes have validity windows / version ids. Checkout **revalidates** before capture. Stale quote must not silently become Order truth.

## Q. Order Snapshot

Order persists commercial snapshot: amounts, currency, market, channel, applied books/contracts/promotions, FX if used, quote id/version. Later book changes do not rewrite history.

## R. Price History / Audit

Pricing records authored changes and quote provenance for audit. Not a substitute for Order snapshots.

## S. Tax / Fee Context

Tax/fee may be quote-time or order-time. Do not bury tax inside Product. Exact tax engine: `NEEDS_LATER_P00_DETAIL`. Pricing may accept tax context; it does not become a tax SoT.

## T. Currency Conversion & Checkout

Charge currency vs display currency: policy later. Conversion at checkout uses explicit FX snapshot on the order. Fail closed if required rate missing.

## U. Seller Settlement Implications

Seller payout currency/terms are **not** customer quote. Settlement belongs to marketplace finance later; do not store settlement in Catalog.

## V. Market / Locale / Domain

Host/tenant (T003) is not Market. Locale is not Market. Domain is not Currency. Storefront may **select** Market/Currency under policy after tenant resolve.

## W. Cache Key Dimensions

Price cache keys must include quote-relevant dimensions (market, currency, offer, channel, party/contract, qty bucket, promotion set version) and never leak across tenants. Redis not required initially.

## X. Search / PLP / PDP Projection

Search may index **display price projections** for a default context. Search is not pricing truth; sort-by-price is a projection. PDP uses a fresh or short-ttl quote/contract.

## Y. SEO Implications

Indexable prices (if any) are contextual; do not pretend one global price in structured data without market/currency. Details: later SEO package.

## Z. Admin UX Implications

Admin authors books/markets via Pricing contracts, composed in Admin RM. Not `Product.Price` form field as SoT.

## AA. Pricing Authority Hierarchy

Candidate (not locked): contract/B2B override → seller offer authored → market list book → FX display fallback.

Exact stack: `NEEDS_LATER_P00_DETAIL`. Must be **deterministic**.

## AB. Conflict Resolution

Two matching rules: later precedence policy. Never pick arbitrarily. If unresolved, fail closed (no quote / no sell) rather than a guessed amount.

## AC. Data Ownership Matrix

| Concept | Owner |
| --- | --- |
| Authored price / price book | Pricing |
| FX rate / conversion snapshot | Pricing (via FX adapter) |
| Market definition | Market / Commercial Context |
| Currency metadata | Kernel / commercial config |
| Offer bind | Offer |
| Promotion definition | Promotion |
| Quantity on quote | Cart/Checkout input; Pricing applies tiers |
| Order paid amounts | Order snapshot |
| Search display price | Search projection |
| Analytics revenue/GMV | Analytics observation/projection only; original Amount+Currency; Order remains money truth (see `docs/architecture/16-first-party-analytics.md`) |

## AD. Failure Matrix

| Case | Direction | Fail closed? |
| --- | --- | --- |
| No matching authored price | Do not invent; optional FX-only if policy allows display-only | Yes for charge |
| FX unavailable | No charge conversion | Yes for charge |
| Ambiguous overlapping books | No arbitrary pick | Yes |
| Stale quote at checkout | Revalidate; block capture | Yes |
| Tenant mismatch | No quote | Yes |
| Unknown currency | No quote | Yes |

## AE. Testing Strategy — Architecture Level

Future tests: context matrix quotes; authored vs FX; B2B contract override; quantity tiers; promotion not mutating book; snapshot immutability; cache isolation; conflict fail-closed. No tests now.

## AF. Decision Summary

### RECOMMENDED_FOR_ADR

1. Locale != Market != Currency.
2. Product != Price; Offer != Price.
3. Authored price != FX-derived price.
4. Money as decimal/minor-units, not float.
5. Price books + deterministic quote service.
6. Quote revalidation before order capture.
7. Order snapshots commercial outcome.
8. Promotions separate from base authored price.
9. FX behind internal adapter.
10. B2B via quote context (org/account/contract), not Party table joins.
11. Search/SEO consume price projections, not pricing write model.
12. Fail closed on missing/ambiguous price for charge.

### NEEDS_LATER_P00_DETAIL

- Rounding mode per currency
- Authority hierarchy ranking
- Tax engine boundary vs quote
- Display vs charge currency UX
- FX vendor
- Default PLP price context

### DEFERRED

- Implementation, schemas, vendors, promotion engine, tax, checkout, settlement, ADR, Shopeiva
