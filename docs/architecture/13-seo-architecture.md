# Tooba — SEO Architecture

Status:

```text
P00 architecture design — candidate for later ADR; not an ADR lock
```

Task:

```text
TB-P00-T014
```

Documentation only. No routes, sitemap code, JSON-LD implementation, or frontend.

```text
SEO is not a metadata afterthought.
```

SEO influences routing, page ownership, content lifecycle, catalog identity, URL stability, localization, market handling, facet policy, composition, performance, rendering, internal linking, structured data, sitemap, robots, redirects.

Lighthouse score alone is not SEO architecture.

```text
Content provides semantic SEO inputs
Catalog provides product facts
Pricing/Inventory provide commercial facts
SEO Engine/Policy owns technical SEO composition
```

```text
Locale != Market != Currency
Backend/module boundary != UI boundary
```

## B. SEO Ownership Model

SEO policy owns: route-type policy, index/noindex, canonical, hreflang, metadata composition, structured-data composition, sitemap inclusion, robots, redirect coordination, diagnostics.

Modules supply inputs via contracts/projections. SEO does not own Catalog write model or CMS body.

## C. Route Ownership

| Route type | Route/policy owner | Semantic input | Index default | Canonical | Sitemap | Structured data |
| --- | --- | --- | --- | --- | --- | --- |
| Homepage | SEO + Composition | Content/theme | Index | Self | Yes | Organization/WebSite |
| Category | Catalog taxonomy + SEO | Catalog | Index | Self / primary | Yes | CollectionPage |
| Brand | Catalog Brand + SEO | Catalog/Content | Index | Self | Yes | Brand |
| Product | Catalog + SEO | Catalog | Index | Product canonical | Yes | Product |
| Variant | Catalog + SEO | Catalog | Follow G | Product or variant | Selective | Offer on Product |
| Seller | Seller + SEO | Seller | Marketplace selective | Seller page | Selective | Organization |
| Offer | Offer + SEO | Offer/Pricing | Usually noindex | Product | No default | Offer under Product |
| Search | Search + SEO | Query | noindex | none | No | none |
| Facet | SEO policy | Catalog+filters | Policy | Canonical or noindex | Rare | none |
| Tag | Content taxonomy + SEO | Content | Selective | Self | Selective | none |
| Landing | Composition + SEO | Content | Index if unique | Self | Yes if published | WebPage |
| Campaign | Composition + SEO | Content | Often noindex | Landing/product | Selective | none |
| Article / Guide / FAQ | Content + SEO | Content | Index if published | Self | Yes | Article/FAQPage |
| Account / Seller Admin / Cart / Checkout | App + SEO | — | noindex | — | No | none |

No route code.

## D. Locale-Prefixed Routing

Localized storefront URLs use an explicit locale prefix (or equivalent first-class locale segment). Locale is not Market and not currency query.

Exact prefix scheme (`/{locale}/...`) is `RECOMMENDED_FOR_ADR`; implementation later.

## E. hreflang

Alternate locale URLs for equivalent documents. Return x-default per later policy. Do not emit hreflang for noindex or unpublished locales. Market is not a language alternate.

## F. Canonical URLs

Every indexable page has one canonical. Parameters that do not change resource identity (sort, tracking) canonicalize away. Host is tenant-resolved; canonical host is the public storefront host, not an internal alias.

## G. Product Canonicalization

One canonical Product URL. Duplicate paths (IDs, trailing slugs) redirect. Offer URLs do not become competing product canonicals.

## H. Variant SEO

Default: canonical to parent Product unless a variant is a distinct crawl target (unique content/URL policy). Avoid indexing every combinable option as a thin page.

## I. Seller Offer SEO

Marketplace: customer SEO target is Product (and selected offer in structured data), not a forest of offer URLs. Seller storefront pages are separate route type.

## J. Category / Taxonomy SEO

Indexable category pages need unique editorial/content input where thin. Multiple-category membership: one primary canonical category path.

## K. Brand SEO

Brand landing is indexable composition + catalog facts. Not seller commercial page.

## L. Content SEO

Published Content items get stable slugs. Drafts unpublished = not in sitemap/index. Slug history → redirects (T013 AC).

## M. Landing / Campaign SEO

Unique landings index; duplicate campaign clones noindex or canonicalize to durable URL.

## N. Search Results SEO

Internal search result URLs: **noindex, follow** default. Do not sitemap. Query URLs are not product canonicals.

## O. Faceted Navigation

Facets are crawl-risk. Policy: index only allowlisted facet combinations with unique value; others noindex and/or canonical to category. Never infinite crawl of filter permutations.

## P. Filter / Sort / Pagination Parameters

Sort and tracking params: canonical to clean URL. Pagination: rel prev/next or equivalent; do not spawn duplicate full-index copies.

## Q. Programmatic SEO

High-volume pages allowed only with unique value, canonical, hreflang, sitemap budget, and thin-content gates. Not auto-index every attribute combo.

## R. Thin / Duplicate Content Control

Quality gate before index: unique title/description/body or substantial catalog+editorial. Duplicates canonicalize or noindex.

## S. Redirect Architecture

301 for permanent slug/id changes; preserve history. 302 only temporary. Redirect service/policy coordinates; Content/Catalog emit change events. No silent 200 duplicates.

## T. Sitemap Architecture

SEO owns sitemap composition from **published indexable** route feed. Split by type if large. Lastmod from owning module events. Exclude noindex, admin, cart, search.

## U. robots.txt

SEO policy owns robots.txt per public host. Disallow admin/checkout/account/search as needed. Do not block CSS/JS required to render.

## V. Robots Meta / X-Robots

Page-level index/noindex/nofollow from route policy + publish state. Preview/staging noindex.

## W. Structured Data

JSON-LD composed by SEO layer from module projections. Invalid/partial data omitted rather than invented.

## X. Product Structured Data

Product + Offer (price/availability from Pricing/Inventory **projections** at render). Never scrape live foreign tables. Marketplace: offers array / selected offer per later PDP policy.

## Y. Breadcrumbs

BreadcrumbList from primary taxonomy path. Matches visible UX hierarchy, not a random module tree.

## Z. Internal Linking

Category/product/content hubs. Composition sections may link by resource id. No orphan indexable landings by default.

## AA. Pagination / Infinite Scroll

SEO-accessible paginated URLs (or equivalent crawlable pages). Infinite scroll must not hide the only crawl path.

## AB. Rendering Strategy

Public indexable HTML must be crawlable (SSR or equivalent). Client-only shells are not the SEO document. Admin can be SPA.

## AC. Core Web Vitals

CWV is SEO-relevant. Media variants (T013/Media), bounded composition, no unbounded widgets. Details with frontend package.

## AD. UI/UX and SEO Compatibility

Professional storefront UX is mandatory. SEO must not force skeleton CRUD or ugly query URLs as the primary UX. Facet UX can be rich while crawl policy stays strict.

## AE. Mobile SEO / UX

Mobile-first crawl. Same canonical content as desktop; responsive, not cloaked.

## AF. Accessibility & Semantic HTML

Semantic headings, lang, alt (Media). a11y is production-grade, not optional.

## AG. Media SEO

Alt/filename via Media; Open Graph images from Media derivatives. Catalog does not store binaries.

## AH. Locale Fallback

Untranslated locale: fallback or noindex that locale version — do not serve wrong-language as if translated. Policy `NEEDS_LATER`.

## AI. Market-Specific SEO

Market may change offer/price/availability, not language. Do not use Market as hreflang. If market is in URL, it is commercial context, not locale.

## AJ. Currency Parameters

Currency switch is not a new indexable document. Canonicalize currency query away from product identity URL.

## AK. Tenant SEO

Each public Host has its own robots/sitemap/canonical host. No cross-tenant URL leakage.

## AL. Marketplace SEO

One marketplace host: product-centric index; seller pages selective. Single-Store: same engine, no fake seller URL forest.

## AM. Preview / Staging

Preview/staging: noindex, no sitemap, fail-closed public. Not production canonical.

## AN. Error Pages

404/410 for gone resources; 410 when permanently removed after redirect policy. Soft-404 (200 empty) forbidden for missing products.

## AO. SEO Observability

Indexability diagnostics, coverage feeds, structured-data validity signals. Not the same as first-party sales analytics.

## AP. Analytics Separation

SEO diagnostics ≠ GA/first-party funnels. Share page ids, not mixed SoT.

## AQ. Search Engine Integrations

Search Console etc. are ops, not domain. Domain must not couple to a vendor.

## AR. Data Ownership Matrix

| Concern | Owner |
| --- | --- |
| Technical index/canonical/hreflang/sitemap/robots/JSON-LD composition | SEO policy/engine |
| Product facts | Catalog |
| Price/availability in structured data | Pricing/Inventory projections |
| Editorial body/slug inputs | Content |
| Layout | Page Composition |
| Public HTML render | Frontend + SEO constraints |

## AS. Route Policy Matrix

See section C. Non-indexable: cart, checkout, account, admin, seller ops, internal search.

## AT. Failure Matrix

| Case | Direction | Fail closed? |
| --- | --- | --- |
| Unpublished content URL | 404/noindex | Yes |
| Unknown locale | Fail/404 | Yes |
| Facet explosion | noindex/canonical | Yes |
| Missing product | 404 not 200 | Yes |
| Staging host | noindex | Yes |
| Cross-tenant URL | Deny | Yes |

## AU. Testing Strategy — Architecture Level

Future: canonical uniqueness, hreflang pairs, sitemap membership, noindex search/facets, 404 vs 200, locale prefix, tenant host. No tests now.

## AV. Decision Summary

### RECOMMENDED_FOR_ADR

1. SEO is an architecture concern, not afterthought metadata.
2. SEO policy owns technical composition; modules supply inputs.
3. Locale-prefixed (or equivalent) public routing.
4. hreflang for locale equivalents; Market ≠ language.
5. One product canonical; offers do not compete.
6. Internal search noindex.
7. Facet crawl allowlist / canonicalize.
8. Redirect + slug history.
9. Sitemap of published indexable only.
10. Crawlable HTML for public pages (SSR or equivalent).
11. Structured data from projections, not joins.
12. Currency/sort params not new documents.
13. Tenant-isolated robots/sitemap/canonical host.
14. UX quality is not sacrificed for crawl hacks; module CRUD ≠ UI.

### NEEDS_LATER_P00_DETAIL

- Exact locale prefix pattern
- Variant vs product index rule
- Facet allowlist
- x-default
- Locale fallback vs noindex
- Auth/capture of JSON-LD offer selection on PDP

### DEFERRED

- Implementation, GSC, frontend, Shopeiva, final ADR
