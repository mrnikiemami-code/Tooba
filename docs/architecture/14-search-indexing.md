# Tooba — Search & Indexing Architecture

Status:

```text
P00 architecture design — candidate for later ADR; not an ADR lock
```

Task:

```text
TB-P00-T015
```

Documentation only. No FTS/ES implementation, APIs, or search UI code.

```text
Search Engine implementation must not leak into business domains.
Search owns no catalog/price/inventory/content write models.
Search consumes projections/feeds/events.
```

```text
Locale != Market != Currency
Backend/module boundary != UI boundary
```

Initial engine: PostgreSQL Full Text Search. Future: Elasticsearch / OpenSearch behind the same internal search contract.

## B. Initial and Future Engines

`ISearchIndex` / `ISearchQuery` (conceptual) hide engine. Domains never import pg_trgm or ES client. Swap later without rewriting Catalog/Content.

## C. Search Boundary

In: projection documents, query + context (tenant, locale, market, currency, authz). Out: ranked ids + display projection fields. Not SoT for sellability.

## D. Search Document

Documents are **assembled projections**: identity ids, searchable text, filter facets, sort keys (price, date), flags (in stock), locale, tenant. Not ORM entities. Thumbnail/primary image is a stable Media **delivery reference** (projection), not a vendor URL and not Media SoT. See `docs/architecture/15-media-image-pipeline.md`.

## E. Document Ownership

Owning modules emit facts. Search assembly service (platform/search module) builds documents. Catalog does not write ES mappings.

## F. Projection Assembly

Join-equivalent assembly happens **inside Search** from feeds, never SQL across module tables. Stale fields allowed; checkout revalidates price/stock.

## G. Indexing Flow

```text
domain event → outbox → search consumer → upsert/delete document
```

Idempotent by document id + version.

## H. Rebuild / Backfill

Full rebuild from source feeds. Versioned index alias/swap. No dual-write from Admin into engine.

## I. Index Versioning

Schema/version on documents. Reindex job for mapping changes.

## J. Multilingual Search

Analyzer/pipeline per locale. Query uses request locale. Not Market.

## K. Persian Search Quality

Preserve stemming/normalization/ZWNJ/Arabic-Persian Yeh/Kaf later. Engine-specific; behind analyzer strategy. `NEEDS_LATER_P00_DETAIL`.

## L. Query Model

Full-text + filters + sort. Query parser must not become a second catalog language.

## M. Facets / Filters

Facets from indexed attributes/category/brand/price buckets/availability. Facet counts are search, not Catalog SoT.

## N. SEO and Facets

Align T014: search result URLs noindex; facet crawl allowlist is SEO policy, not Search’s job to invent URLs.

## O. Sorting

Relevance, price, newest — sort keys from projections. Price sort uses projected display price for **a context**, not live Pricing table.

## P. Ranking

Text relevance + business boosts (stock, rating) as **explicit ranking inputs**, not hidden Catalog queries.

## Q. Business Rules vs Relevance

Must-match filters (market eligibility) are filters, not score hacks. Unsellable: filter or demote per policy; checkout still authoritative.

## R. Price in Search

Projected price for default/context key. Not Pricing write model. Currency in context.

## S. Availability in Search

Projected in-stock. Recheck at cart/checkout (T010).

## T. Marketplace Search

Documents may include seller/offer dimensions. Result cards compose via PDP/PLP RM. Search does not pick Buy Box as SoT.

## U. Single-Store Search

Same engine/contract. Implicit seller. Tenant isolation still required.

## V. Content Search

Published content documents. Drafts excluded (T013).

## W. Unified vs Vertical Search

Unified query box with typed results (product/content) is UX. Internally vertical indexes/queries allowed behind one API.

## X. Autocomplete / Suggestions

Prefix/completion index. Professional UX required later: suggestions, chips, keyboard, RTL.

## Y. Typo Tolerance

Engine capability; do not encode in Catalog.

## Z. Synonyms

Managed synonym lists per locale; not hardcoded in modules.

## AA. Query Analytics

First-party search terms/zero results — Analytics, not Search SoT. Privacy: no raw PII in query logs by default.

## AB. Zero-Result Handling

UX recovery (relax filters, suggestions). Architecture: return empty + diagnostics, not fake products.

## AC. Search UI/UX

Future: autocomplete, filter chips, mobile sheet, sort, counts, skeletons, a11y, RTL. Not CRUD of Search documents.

## AD. Performance

Latency budget later. Pagination/cursors. Do not N+1 hydrate from every module on each hit — use document fields + batched contracts.

## AE. Pagination

Offset or cursor; stable enough for UX. SEO pagination is T014.

## AF. Cache

Query cache keyed by tenant+locale+market+currency+query+filters. Short TTL. No cross-tenant. Redis optional later.

## AG. Authorization-Aware Search

Public catalog search vs Admin. Do not leak unpublished/admin docs. SpiceDB may filter ids or constrain document visibility flags at index time for public.

## AH. Tenant Isolation

Every document tagged with tenant/store. Query always scoped. Fail closed if context missing.

## AI. Market / Locale / Currency

Locale → analyzer/text. Market → eligibility filter. Currency → projected price field. Independent.

## AJ. Eventual Consistency

Search lags events. Acceptable for browse; not for charge/stock commit.

## AK. Delete / Unpublish

Delete/tombstone documents on unpublish/delete events. Fail closed if still returned after confirmed unpublish (reindex).

## AL. Reconciliation

Compare source feed vs index counts. Repair jobs.

## AM. Search Observability

Query latency, zero-result rate, index lag. Separate from SEO GSC.

## AN. Engine Abstraction

Internal ports only. Tests use fakes.

## AO. PostgreSQL FTS Initial Direction

tsvector/tsquery (or equivalent) in Search’s own store/schema, not Catalog tables. Good enough for first sellable; not an excuse to skip abstraction.

## AP. Elasticsearch/OpenSearch Migration Seam

Same documents/events. Change adapter + mapping. No domain rewrite.

## AQ. Recommendation / Personalization Boundary

Recommendations are not Search SoT. May consume similar documents; ranking personalization later.

## AR. AI Integration

AI retrieval uses approved knowledge (T013), not raw Search admin index. Search may feed product ids to assistant via contract.

## AS. Data Ownership Matrix

| Concept | Owner |
| --- | --- |
| Write models | Catalog, Content, Offer, Pricing, Inventory |
| Search documents/index | Search |
| Query API | Search |
| SEO of search URLs | SEO policy |
| UI | Frontend |

## AT. Failure Matrix

| Case | Direction | Fail closed? |
| --- | --- | --- |
| Engine down | Degraded empty/error UX | Yes for wrong-tenant data |
| Missing tenant | No results | Yes |
| Unpublished still in index | Repair; do not show as published if flag says no | Yes for drafts |
| Stale price | OK for browse; checkout requotes | No for browse |
| Cross-tenant hit | Impossible by query scope | Yes |

## AU. Testing Strategy — Architecture Level

Future: tenant isolation, locale analyzers, unpublished exclusion, facet filters, engine-swap contract tests. No tests now.

## AV. Decision Summary

### RECOMMENDED_FOR_ADR

1. Search is projection-only; no write-model ownership.
2. Engine behind internal contract (PG FTS now, ES later).
3. Event-driven upsert/delete + rebuild.
4. Tenant-scoped documents/queries.
5. Locale vs Market vs Currency in document/query independently.
6. Price/availability in index are projections.
7. Public search URLs noindex (SEO).
8. Draft/unpublished content excluded.
9. Unified UX, possible vertical indexes.
10. Authz-aware public vs admin indexes.
11. Eventual consistency accepted for browse.
12. UI quality for search is mandatory later; not module CRUD.

### NEEDS_LATER_P00_DETAIL

- Persian analyzer
- Ranking formula
- Unified vs strictly vertical indexes
- Cache TTL
- Buy Box field in document

### DEFERRED

- Implementation, ES cluster, UI, Shopeiva, ADR
