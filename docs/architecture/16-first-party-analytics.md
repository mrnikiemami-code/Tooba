# Tooba — First-Party Analytics Architecture

Status:

```text
P00 architecture design — candidate for later ADR; not an ADR lock
```

Task:

```text
TB-P00-T017
```

Documentation only. No SDK, tracker, cookies, consent UI, warehouse, dashboards, or Shopeiva.

```text
Analytics != Business Source of Truth
Analytics != Audit Log
Analytics != Technical Logging
Analytics != Authorization
```

```text
Locale != Market != Currency
Backend/module boundary != UI boundary
Analytics Session != Authentication Session
```

## A. Core Separation

Analytics owns behavioral/derived **observation** data. Order, Payment, Pricing, Inventory, Catalog, Party, Authorization remain authoritative. Analytics consumes facts; it does not decide them.

## B. Analytics Event Model

Conceptual envelope (not a schema): EventId, EventName, OccurredAt, ReceivedAt, Tenant/Deployment, VisitorId, SessionId, IdentityId/PartyId where appropriate, anonymous/pseudonymous subject, page/route, Locale, Market, Currency, SalesChannel, device/UA summary, referrer, campaign attribution, entity references, properties, CorrelationId, TraceId where safe, SchemaVersion.

Events are **versioned**. Do not silently change historical meaning.

## C. Event Taxonomy

Preserve families (exact names later): PageView, SessionStarted, ProductViewed, CategoryViewed, BrandViewed, SearchPerformed, SearchResultClicked, SearchZeroResult, FilterApplied, SortChanged, AddToCart, RemoveFromCart, CartViewed, CheckoutStarted, CheckoutStepCompleted, CheckoutFailed, OrderPlaced, PaymentSucceeded, PurchaseCompleted, ContentViewed, LandingViewed, CTAClicked, SectionImpression, PromotionViewed, PromotionClicked.

Avoid uncontrolled arbitrary event naming. Governed catalog of events.

## D. Client vs Server Events

| Class | Example | Authority |
| --- | --- | --- |
| Client-observed UX | button click, impression | UX observation only |
| Server-confirmed business | OrderPlaced, PaymentSucceeded | Owning domain events |

Do not trust client events as proof of revenue or business completion.

## E. Purchase / Revenue Truth

```text
Analytics purchase event is derived from accepted Order/Payment business facts.
```

Not from thank-you-page pixels alone. Conversion events require **idempotency/deduplication**.

## F. Visitor

Analytics/experience observation identity — not Authentication Identity. May be anonymous, authenticated, returning, cross-session. Do not automatically merge all devices into one legal person.

## G. Session

Analytics Session = bounded browsing activity. Authentication Session = security lifecycle. Do **not** reuse auth session tokens as analytics identifiers.

## H. Anonymous to Authenticated Transition

Anonymous visitor → login/register → authenticated identity. Associate **future** analytics per privacy/product policy. Do not automatically rewrite all historical anonymous data onto a known person unless policy allows. Stitching: `NEEDS_LATER_P00_DETAIL`.

## I. Tenant Isolation

Single-Store: every event carries resolved TenantId (or equivalent). No cross-store leakage.

Marketplace: deployment/marketplace context plus seller/entity dimensions. Do not invent fake tenant semantics.

## J. Locale / Market / Currency

Events may carry all three independently. Locale = presentation language; Market = commercial context; Currency = displayed/order currency. Do not infer one from another.

## K. Sales Channel

Preserve SalesChannel where commercially meaningful. Future candidates: DIRECT, MARKETPLACE, B2B, CORPORATE, AFFILIATE, API. Exact values deferred.

## L. Attribution

Normalized dimensions: utm_source/medium/campaign/content/term, referrer, landing page, affiliate/campaign id, internal campaign. Do not persist arbitrary raw query parameters as unbounded schema.

## M. First-Touch / Last-Touch

Store enough normalized evidence for first-touch, last-touch, and future multi-touch. Do not lock one methodology now.

## N. Search Analytics

Support: query, normalized query, result count, zero-result, filters, sort, clicked result/position, entity type, conversion after search. Privacy-aware. Later may feed ranking, synonyms, typo, recommendations. Search executes search; Analytics **observes** outcomes.

## O. Product Analytics

Preserve ProductView, OfferView where relevant, AddToCart, Purchase, revenue attribution. Wishlist/Compare only as future extension points — do not promote template-only features.

## P. Content Analytics

Content/Page Composition may emit ContentView, LandingView, SectionImpression, CTA click, collection interaction, future scroll-depth. Avoid noisy default tracking. Stable opaque refs: ContentId, RevisionId, PageDefinitionId, SectionInstanceId.

## Q. Marketplace Seller Analytics

Seller dashboards: views, offer impressions, clicks, add-to-cart, orders, conversion, revenue **projection**, stockout effect, search visibility — **seller-authorized scope only**. No other-seller canonical leakage.

## R. Admin Analytics

Cross-business views via analytics/read models: GMV/revenue, orders, conversion, traffic, search, category, seller, tenant/store, content, campaign. Not via cross-module transactional joins.

## S. Analytics Ingestion

Customer-facing synchronous requests must not unnecessarily block on analytics persistence.

```text
use-case → emit observation/fact → async buffer → analytics storage/projection
```

Server-confirmed facts may flow via outbox/integration events.

## T. Delivery Reliability

Conceptual classes (names not locked): BEST_EFFORT (lossy UI hover/click OK), IMPORTANT, BUSINESS_CONFIRMED (PurchaseCompleted durable).

## U. Idempotency / Deduplication

EventId, producer identity, deduplication, business-event correlation. Purchases must not double-count on retry. Client events may use looser duplicate policy.

## V. Event Ordering

No perfect global order. Handle late, out-of-order, retry, clock skew, offline client send via OccurredAt vs ReceivedAt. Domain event timestamps/versions for business-confirmed facts.

## W. Event Schema Versioning

SchemaVersion, backward-compatible consumers, transform/migration, contract governance. Do not edit historical meaning in place.

## X. Analytics Storage Boundary

No vendor lock. Future may include PostgreSQL analytical tables, ClickHouse, warehouse, lake, stream, BI. Keep event contracts stable. Transactional modules must **not** query analytics tables for business decisions.

## Y. Operational vs Analytical Read Models

Order detail = operational. Orders-per-day / search conversion by query = analytical. Admin BFF may compose both. Analytics aggregates are not Order SoT.

## Z. Real-Time vs Batch

Near-real-time counters, hourly/daily aggregates, batch history. Not everything must be real-time. UI must communicate **freshness** when delayed.

## AA. Funnel Model

Support funnels across PageView → ProductView → AddToCart → CheckoutStarted → OrderPlaced → PaymentSucceeded/PurchaseCompleted. Stable correlation dimensions without coupling domains to analytics impl. Do not hardcode one funnel.

## AB. Conversion

From authoritative server facts. Dimensions: OrderId, pseudonymous buyer ref, Tenant, Seller, Market, SalesChannel, Currency, Amount, ItemCount, campaign/search attribution. Minimize PII.

## AC. Money in Analytics

Amount + Currency always. No bare revenue. Do not blindly sum mixed currencies. Cross-currency reporting needs explicit FX policy later. Analytics does not change Order money truth.

## AD. Privacy / Data Minimization

Collect only needed data; prefer pseudonymize/hash; separate security/audit; never passwords/tokens/OTP/payment secrets/PAN/CVV. No legal-compliance claims here. Consent/legal policy later.

## AE. PII Boundary

Do not stuff email, phone, national ID, full name, address into general analytics by default. Opaque/pseudonymous refs. PII use-cases need explicit policy and narrow handling.

## AF. Cookies / Client Storage

Do not lock cookie strategy. Future distinguish: essential/security storage; analytics visitor/session storage; consent-driven optional tracking. Exact consent/cookie: `NEEDS_LATER_P00_DETAIL`.

## AG. External Analytics Integration

Coexist with optional GA, Tag Manager, Meta Pixel, etc. via adapter boundary. External tools are **not** SoT. First-party events remain independently available.

## AH. Analytics Event Bus Boundary

No required broker initially. Allow in-process buffer, DB outbox/queue, future broker/stream. Consumer request path stays decoupled.

## AI. Recommendation Boundary

Future Recommendation consumes views/clicks/cart/purchases/search/content signals. Analytics provides signals. Recommendation owns model/output. Analytics does not decide recommendations.

## AJ. Personalization Boundary

May consume analytics signals with tenant, authorization, privacy, market, locale, customer context. Search/Content/Pricing remain separate owners.

## AK. AI Boundary

Future AI may consume **aggregated/approved** signals (popular queries, zero-result patterns, engagement, product interest trends). Do not expose raw sensitive behavioral history indiscriminately. AI authz/privacy remain separate. See `docs/architecture/17-ai-assistant-rag.md`.

## AL. Ranking Feedback

Search ranking may consume controlled features (popularity, CTR, conversion, zero-result patterns). Analytics events must not directly mutate ranking weights without a versioned ranking pipeline.

## AM. Bot / Internal Traffic

Distinguish bots, health checks, admin/internal, test automation, preview/staging from customer analytics. Do not count known system traffic as commerce behavior by default. Detection policy later.

## AN. Event Quality

Controls: missing required dimensions, invalid currency, unknown version, duplicate IDs, impossible timestamps, cross-tenant refs, malformed entity refs. Invalid events must not silently poison analytics.

## AO. Late / Offline Client Events

Buffered late send: rules around OccurredAt, ReceivedAt, session/campaign attribution, retention. No implementation now.

## AP. Analytics Retention

Different classes may differ. Preserve raw retention, aggregate retention, deletion/anonymization support. Periods not locked.

## AQ. Tenant Deletion / Data Lifecycle

Single-Store may require analytics export, retention, deletion/anonymization. Tenant deletion must not leave uncontrolled identifiable remnants. Legal/business rules later.

## AR. Dashboards

Decision-oriented, not raw event tables.

Admin: revenue trend, conversion funnel, top categories/products, search zero-result, seller, campaign, content, stockout impact.

Seller: offer views, conversion, orders, revenue, search visibility, stockout impact — scoped.

## AS. UI / UX for Analytics

KPI hierarchy, date range, comparison period, filters, drill-down, loading/empty/error, data freshness, export, mobile, RTL/LTR, accessible charts **and** tabular/text alternatives. Do not expose backend event schema as dashboard IA.

## AT. Dashboard Authorization

SpiceDB scopes: tenant, seller, finance, content, security-sensitive reports. Prefer scope-aware projections / bulk authorization over N+1 checks.

## AU. Analytics Caching

Cache aggregates with Tenant, Seller, date range, Market, reporting currency, SalesChannel, filters, authz scope. No cross-tenant leakage. Cache must not affect business transaction truth.

## AV. Reporting Currency

Do not sum `100 USD + 100 EUR` as 200 generic. Preserve original amount/currency; optional normalized reporting amount/currency; FX source/timestamp/version. Exact policy: `NEEDS_LATER_P00_DETAIL`.

## AW. Event-to-Aggregate Flow

```text
events → validated ingestion → raw/normalized event store → aggregates/projections → dashboards/exports/models
```

Do not force dashboards to scan raw event logs.

## AX. Rebuild / Recompute

Replay/rebuild, aggregate version, backfill, late-event correction. Irreversible counters must not be the only analytical truth.

## AY. Reconciliation

Observable differences among Analytics Purchase count, Order count, Payment count. Analytics does **not** overwrite Order/Payment to “fix” gaps.

## AZ. Observability of Analytics Pipeline

Ingestion lag, drop rate, invalid events, duplicate rate, queue/backlog, consumer failures, aggregate freshness, event volume, tenant volume anomalies. Technical telemetry ≠ product analytics metrics. See `docs/architecture/18-observability-logging-audit.md`.

## BA. Export / BI Readiness

Future CSV, scheduled reports, BI, warehouse export, data science — via analytical datasets, not transactional DB joins.

## BB. Data Ownership Matrix

Marks: `OWNER` | `SOURCE` | `OBSERVATION` | `CONSUMER` | `PROJECTION` | `NOT_OWNER`

| Fact | Analytics | Catalog | Search | Cart | Checkout | Order | Payment | Inventory | Content | Page Composition | Authorization | Recommendation (future) | AI (future) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| PageView | OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | OBSERVATION | NOT_OWNER | CONSUMER | CONSUMER |
| ProductView | OWNER | SOURCE | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | CONSUMER |
| SearchPerformed | OWNER | NOT_OWNER | SOURCE | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | CONSUMER |
| AddToCart | OWNER | NOT_OWNER | NOT_OWNER | SOURCE | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | NOT_OWNER |
| CheckoutStarted | OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | SOURCE | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | NOT_OWNER |
| Purchase fact | OBSERVATION | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | SOURCE | SOURCE | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | CONSUMER |
| Revenue truth | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | OWNER | OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER |
| Inventory truth | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER |
| Content truth | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER |
| Permission | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | OWNER | NOT_OWNER | NOT_OWNER |
| Behavioral aggregate | OWNER | NOT_OWNER | CONSUMER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | CONSUMER | NOT_OWNER | CONSUMER | CONSUMER |
| Recommendation output | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | OWNER | NOT_OWNER |
| AI knowledge | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | SOURCE | NOT_OWNER | NOT_OWNER | NOT_OWNER | OWNER |

Purchase **fact** in analytics is derived OBSERVATION; Order/Payment remain SOURCE of conversion.

## BC. Failure Matrix

| Case | Block customer request? | Retry? | Drop? | Durable? | Reconcile? | Alert? | Freshness visible? |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Client event lost | No | Optional client buffer | Yes if expired | BEST_EFFORT | No | Low | No |
| Duplicate event | No | N/A | Deduped | Yes (id) | No | Low | No |
| Invalid schema | No | After producer fix | Quarantine | Quarantine store | No | Medium | No |
| Ingestion unavailable | No | Buffer/outbox | Last resort | Buffer | Later | Yes | Admin |
| Queue backlog | No | Drain | No | Yes | Later | Yes | Stale dashboards |
| Late event | No | Apply with OccurredAt | If beyond retention | If in window | Yes if conversion | Low | Possible restatement |
| Cross-tenant event | No | No | Reject | No | No | Yes | N/A |
| Purchase event missing | No | From domain outbox | No | BUSINESS_CONFIRMED | Yes vs Order | Yes | Admin |
| Analytics store unavailable | No | Queue | No | Queue | Later | Yes | Error/stale |
| Aggregate build failure | No | Rebuild | No | Raw retained | Yes | Yes | Stale |
| Dashboard stale | No | Refresh | N/A | Aggregates | N/A | Optional | Yes |
| External vendor down | No | Adapter retry | First-party continues | First-party | N/A | Medium | Optional |
| Authz scope error | Deny dashboard | N/A | N/A | N/A | N/A | Medium | Empty/denied |

Most analytics failures must **not** break purchase paths. BUSINESS_CONFIRMED conversions need stronger delivery and reconciliation than UI observations.

## BD. Testing Strategy — Architecture Level

Future: schema versioning, dedupe, tenant isolation, anonymous/auth transition, client vs server authority, purchase dedupe, multi-currency, late events, search/campaign attribution, seller scope, dashboard authz, aggregate rebuild, pipeline degradation, PII exclusion. No tests now.

## BE. Decision Summary

### RECOMMENDED_FOR_ADR

1. First-party Analytics is a dedicated capability.
2. Analytics is not business truth, audit, or technical logging.
3. Client-observed and server-confirmed events are distinct.
4. Purchases/conversions derive from authoritative server business facts.
5. Analytics Visitor/Session are separate from Identity/Auth Session.
6. Analytics event contracts are versioned.
7. Tenant/Marketplace scope is explicit in every event.
8. Locale/Market/Currency remain separate.
9. Analytics ingestion does not unnecessarily block customer request paths.
10. Event reliability class may vary by business importance.
11. Critical conversion events are idempotent/deduplicated.
12. PII is minimized; secrets/payment credentials never enter analytics.
13. External analytics vendors remain optional adapters, not truth.
14. Search/Recommendation/AI may consume controlled analytics signals, not raw unrestricted data.
15. Multi-currency reporting preserves original money and explicit FX provenance.
16. Aggregates are rebuildable/recomputable.
17. Analytics pipeline has reconciliation against Order/Payment truth.
18. Professional Admin/Seller analytics UX is KPI/workflow-oriented, not raw-event CRUD.
19. Dashboard authorization is scope-aware and SpiceDB-governed.
20. Analytics pipeline health/freshness is observable.

### NEEDS_LATER_P00_DETAIL

- Identity stitching policy
- Consent/cookie strategy
- Reporting FX policy
- Exact event names and reliability class names
- Retention periods
- Bot detection policy

### DEFERRED

- Implementation, vendors, GA/GTM, consent UI, warehouse, recommendation/personalization/AI ingest, Shopeiva, ADR
