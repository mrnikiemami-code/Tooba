# Tooba — Catalog, Product, Variant, Seller & Offer Architecture

Status:

```text
P00 architecture design — candidate for later ADR; not an ADR lock
```

Task:

```text
TB-P00-T008
```

Documentation only. Locale != Market != Currency. Catalog Product != Seller Offer. Price and inventory are not Product fields.

## A. Core Separation

```text
Catalog Product != Seller Offer
```

```text
Catalog Product = canonical/shared product identity and descriptive truth
Seller Offer = seller/store-specific commercial offer for a sellable product/variant
```

Catalog must **not** own: seller-specific price, inventory, fulfillment promise, commercial status, settlement terms.

Offer must **not** own canonical description/specifications.

## B. Product Identity

Canonical Product conceptually includes: ProductId; Product Type; title/name; description **references** (not CMS body ownership); Brand; category placement; specifications; variant model; media **references**; SEO/search projection inputs; lifecycle/publishability.

Distinguish: canonical identity; editorial/descriptive truth; commercial availability (owned by Offer/Inventory/Pricing, not Catalog).

No schema.

## C. Product vs Variant

```text
Product: iPhone 17
Variant: 256GB / Black
```

```text
Product: T-Shirt Model X
Variant: Size L / Red
```

Concepts: Product, Variant, Option, Option Value, Specification Attribute, Variant-defining Attribute, Non-variant Specification.

Not every attribute creates a variant. Do not model variants as duplicated free-form products without justification.

## D. Attribute Architecture

Separate: Attribute Definition; Attribute Value; Attribute Set / Product Type Schema; Variant Axis; Specification; Filterable Facet; Comparable Attribute; Searchable Attribute; Localized Label; Unit / Measurement metadata.

Avoid `Dictionary<string,string>` as the whole model. Category-specific attributes remain extensible. Persistence not finalized.

## E. Product Type / Schema

Product Type examples (not locked): Mobile Phone, Laptop, Perfume, Clothing, Food.

Each may define required/optional attributes, variant axes, filterability, comparability, units, validation, display grouping — for later professional Admin authoring.

## F. Category Architecture

Category: taxonomy, hierarchy, navigation, product classification, attribute-schema defaults, SEO/content reference, merchandising hooks.

Category does **not** own price, seller, or inventory.

Multiple-category membership vs primary category: cardinality `NEEDS_LATER_P00_DETAIL`.

## G. Brand

Brand is catalog/editorial: identity, localized names, slug/SEO reference, logo/media reference, editorial content reference.

Brand does not own seller commercial data. Brand landings compose via Content/Page Composition contracts.

## H. Seller vs Offer

Seller/Marketplace owns seller **lifecycle**. Offer owns:

```text
Seller offers Product/Variant
```

Offer conceptual facts: OfferId, SellerId, ProductId/VariantId, commercial status, market/channel eligibility, **pricing reference**, **inventory reference**, fulfillment policy/reference, seller SKU, condition, warranty/service reference, validity.

Offer does not store price books or stock quantities as truth.

## I. Marketplace Multiple Sellers

```text
1 canonical Product / Variant
→ many Seller Offers
→ different prices / availability / fulfillment / service
```

PDP may show selected/buy-box offer, other offers, seller info, price, inventory, delivery **from projections/contracts**, not Catalog tables.

Buy Box ranking: later detailed design; not implemented.

## J. Single-Store Edition

Not multi-vendor, but **do not** collapse Product/Offer/Pricing/Inventory into one entity.

Candidate: one implicit/default commercial seller/store actor. UX may hide seller entirely. Same core architecture reusable; no mandatory marketplace screens on every page.

## K. Product Lifecycle

Candidate (not locked enum): Draft, Review, Active, Inactive, Archived, Discontinued.

```text
Catalog publishability != Offer sellability
```

A catalog product may exist with no active offer.

## L. Offer Lifecycle

Independent candidate: Draft, Active, Paused, Rejected/ModerationRequired, commercial OutOfStock **signal** (not inventory SoT), Expired, Archived.

Inventory remains Inventory’s write model.

## M. Seller SKU vs Canonical Identifiers

Seller SKU is offer-local (seller’s code). It is **not** canonical Product/Variant identity.

GTIN/EAN/ISBN and similar may attach to canonical Product/Variant as **identifiers**, not as seller SKUs. Matching later (N).

## N. Product Matching / Marketplace Onboarding

Sellers may propose listing against an existing canonical product or submit a candidate for matching/moderation.

Catalog owns match **decision** against canonical identity. Offer is created only after bind. No import implementation.

## O. Catalog Governance / Moderation

Marketplace may require review of new products, attribute quality, prohibited content, brand claims.

Moderation is catalog/ops capability; not Identity roles (`IsAdmin`). SpiceDB gates who may moderate.

## P. Product Merge / Duplicate Handling

Preserve merge, redirect, and SEO/reference migration. Canonical identity may survive; losers redirect. Offers re-bind to survivor. No implementation.

## Q. Content Separation

Catalog holds structured product truth. Content module holds articles, guides, FAQ, landings knowledge.

PDP copy blocks may **reference** Content ids. Content does not own Product tables. Semantic Content != Page Composition (T002).

## R. Media Separation

Media owns asset lifecycle and derivatives. Catalog stores **media ids**, not binaries or CDN internals. See `docs/architecture/15-media-image-pipeline.md`. Product authoring UX may compose Catalog + Media without merging modules.

## S. Pricing Separation

Price is never a Product scalar and never Offer’s write-model of “the price.”

Pricing owns price books / quotes. Offer holds a **reference** that Pricing can resolve with Market, Currency, Channel, Party/contract context.

## T. Inventory Separation

Inventory owns availability/reservations. Offer references sellable identity; does not own stock counts.

## U. Market / Availability

Offer/catalog **eligibility** per Market is commercial context, not Locale. A product may be catalog-visible in a market without being sellable (no offer/inventory).

## V. Multi-Language Catalog

Localized titles/attributes are Locale. Not Market, not Currency. Translation is not a price and not a market policy.

## W. Search Projection

Search consumes feeds/events. Search owns no catalog write model. Initial PostgreSQL FTS later; engine swappable.

## X. PDP / PLP Read Composition

Public pages compose from **read models/projections**: catalog facts + selected offer + price quote + availability + content/media + reviews.

**Forbidden:** SQL/ORM joins across Catalog, Offer, Pricing, Inventory, Seller, Content tables.

## Y. SEO Implications

Canonical product URLs, variant/canonical policy, brand/category landings, hreflang — later SEO package. Catalog supplies identity/slug **inputs**; Page Composition/SEO own routing composition. Do not lock routes here.

## Z. Reviews / Ratings

Reviews module owns review records. Catalog/PDP consume projections. Reviews are not product identity.

## AA. Promotion / Merchandising

Promotions modify commercial terms; merchandising ranks/collections. Neither owns Product identity or base authored price (Pricing).

## AB. B2B Readiness

B2B reuses canonical Catalog + Offer. Contract/quantity prices are Pricing context (T007), not a second product tree.

## AC. Admin UX Implications

Admin composes operational projections. Staff do not query every module schema. Least privilege via SpiceDB (catalog moderation vs offer ops vs inventory).

## AD. Data Ownership Matrix

| Concept | Owner | Must not own |
| --- | --- | --- |
| Canonical Product / Variant / attributes | Catalog | Price, stock, seller terms |
| Category / Brand | Catalog (editorial) | Seller, inventory |
| Seller lifecycle | Seller / Marketplace | Canonical description |
| Offer bind seller↔variant | Offer | Price books, stock qty, CMS body |
| Price | Pricing | Catalog copy |
| Availability | Inventory | Titles |
| Media binaries | Media | Product identity |
| Search index | Search | Catalog writes |
| PDP composition | Read model / BFF | Write models |

## AE. Critical Invariants

1. Catalog Product != Seller Offer.
2. Product != Variant (when variation exists).
3. Structured attributes, not only string bags.
4. Seller SKU != canonical identity.
5. Price not owned by Product or Offer write model.
6. Inventory not owned by Product or Offer write model.
7. Single-Store keeps the same four seams (Product, Offer, Pricing, Inventory).
8. Catalog != Content CMS.
9. Catalog != Media processing.
10. Search is projection-only.
11. PDP/PLP without cross-module joins.
12. Locale != Market != Currency.
13. Merge/redirect capability preserved.
14. B2B does not fork the catalog.

Do not implement invariants as code yet.

## AF. Decision Summary

### RECOMMENDED_FOR_ADR

1. Catalog Product != Seller Offer.
2. Product != Variant.
3. Structured Attribute Definitions / Product Types.
4. Seller lifecycle separate from Party identity.
5. Offer owns seller–product commercial relationship, not price/inventory truth.
6. Pricing separate from Offer.
7. Inventory separate from Offer.
8. Single-Store preserves Product/Offer/Pricing/Inventory seams.
9. Catalog structured truth separated from Content CMS.
10. Media asset lifecycle separate from Catalog.
11. Search consumes projections and owns no catalog truth.
12. Public PDP/PLP use composition/read models, not cross-domain joins.
13. Marketplace product matching/moderation supported conceptually.
14. Canonical product merge/redirect capability preserved.
15. Locale/Market/Currency remain separate.
16. B2B reuses canonical Catalog/Offer architecture.

### NEEDS_LATER_P00_DETAIL

- Variant option model cardinality
- Primary vs multi-category
- Buy Box ranking
- Identifier schemes (GTIN vs internal id)
- Catalog vs Offer publish state machines
- SEO URL/canonical/hreflang ownership details

### DEFERRED

- Entities, schemas, APIs, UI, import, Buy Box, search indexes, Shopeiva, final ADR
