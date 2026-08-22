# Tooba — Product Capability Map & Candidate Domain Boundaries

Status:

```text
P00 discovery / candidate model — not locked ADRs
```

Task:

```text
TB-P00-T002
```

This document is architecture discovery. It does not implement product, lock schemas, or promote Shopeiva demo routes into USER requirements.

## A. Mapping Method

| Term | Meaning |
| --- | --- |
| Business Capability | What the business must be able to do (sell, price, fulfill, authorize). |
| Candidate Bounded Context / Module | A proposed ownership boundary for operational data and write rules. Not an ADR lock. |
| Cross-cutting Platform Capability | Technical substrate consumed by many modules (telemetry, cache, storage). Not a sales domain. |
| Projection / Read Model | Derived view fed from owning modules for search, AI, admin, analytics. Not source of truth. |
| External Integration | Payment PSP, Keycloak, CDN, email/SMS, FX source. Owned at the contract edge. |

Examples that are **not** business domains: OpenTelemetry, Redis, Elasticsearch, Docker.

## B. Top-Level Business Capability Map

Classifications: `CORE` | `SUPPORTING` | `GENERIC` | `PLATFORM` | `FUTURE`

| Capability | Class | Rationale |
| --- | --- | --- |
| Commerce Experience | CORE | Sellable storefront/customer journeys; UX is commercially critical |
| Catalog | CORE | Canonical sellable identity and descriptive product truth |
| Merchandising | SUPPORTING | Presentation/ranking/collections; does not own product identity |
| Seller / Marketplace | CORE | Required for marketplace edition; overlay-disabled in single-store |
| Pricing | CORE | Contextual prices; never a product scalar |
| Market / Commercial Context | CORE | Market is not Locale and not Currency |
| Inventory / Availability | CORE | Sellability depends on availability, not catalog copy |
| Cart | CORE | Pre-order commercial session |
| Checkout | CORE | Capture of order intent under a pricing/market snapshot |
| Order Management | CORE | Post-checkout commercial record |
| Payment | CORE | Capture/settlement against order; PSP is integration |
| Fulfillment / Delivery | CORE | Physical/digital completion of an order |
| Customer / Party | CORE | People and organizations as business parties |
| Identity / Authentication | CORE | Login credentials; must stay separable from Party |
| Authorization | CORE | SpiceDB relationship model; not role columns |
| Content | CORE | Broad Content (articles, guides, FAQ, landings knowledge), not Blog-only |
| Page Composition / Landing | CORE | Composable pages; not semantic content itself |
| Search / Discovery | CORE | Findability; consumes projections |
| Media | SUPPORTING | Assets/variants for catalog/content/UI |
| Promotion / Campaign | SUPPORTING | Temporary commercial modifiers; not base authored price |
| Review / Rating | SUPPORTING | Social proof; not catalog identity |
| Notification / Communication | SUPPORTING | Outbound messages; not order truth |
| Customer Service / Support | SUPPORTING | Tickets/help; may exist in template as UX only |
| First-Party Analytics | SUPPORTING | First-party events besides third-party tags |
| AI Assistance / Knowledge | CORE | Grounded customer agent is a confirmed requirement |
| Administration / Operations | SUPPORTING | Staff operations across modules via contracts |
| B2B / Organization Commerce | FUTURE | After first sellable release; boundaries must not block it |
| Localization / Internationalization | CORE | Multilingual product; Locale != Market != Currency |

Template-only concepts (`premium`, `referral`, `wallet`, `gift card`, `site survey`) remain `TEMPLATE_PRESENT / PRODUCT_DECISION_PENDING` and are **not** classified as confirmed Tooba capabilities here.

## C. Candidate Module / Bounded Context Set

All statuses are `CANDIDATE` or `NEEDS_FURTHER_P00_ANALYSIS`. Not ADR-locked.

### Identity

- Purpose: authenticate actors (password, OTP, optional MFA, future IdP/Keycloak).
- Owns: credentials, typed login identifiers, method enrollments, sessions, external `issuer+subject` bindings, identity security events (see `docs/architecture/04-identity-authentication.md`).
- Must NOT own: Party profile, prices, orders, SpiceDB relationship tuples as business graph (authorization is separate). Login Identifier is not an Authentication Method.
- Inbound: none as source of Party truth.
- Outbound: authenticated subject IDs to Party/Authorization.
- Editions: SHARED_CORE. B2B: one human Identity may act for multiple organizations via Party/SpiceDB, not via Identity roles. Extraction: high (Keycloak-extensible).

### Party

- Purpose: human and organization business identity, membership, buyer vs actor (see `docs/architecture/06-party-organization-b2b-foundation.md`).
- Owns: Party/Organization, membership lifecycle, party addresses/contacts as current facts (not order historical snapshots).
- Must NOT own: passwords, catalog, prices, Tenant/Store platform identity, Seller commercial lifecycle, SpiceDB as profile store.
- Inbound: optional Identity link. Outbound: Authorization (membership facts), Pricing context ids, Order buyer/payer refs.
- Editions: SHARED_CORE. Actor != Buyer; Buyer != necessarily Payer. B2B overlay on same Party module.
- Status: CANDIDATE (keep separate from Identity).

### Authorization

- Purpose: relationship-based access (SpiceDB direction). See `docs/architecture/05-spicedb-authorization.md`.
- Owns: authorization decision graph / relationship tuples needed for checks; not business write models.
- Must NOT own: product catalog, prices, order lines, Identity credentials. UI hiding is not the security boundary.
- Inbound: Party, Identity, Seller, Order, Admin, Organization (future). Outbound: allow/deny at use-case boundaries; bulk/lookup for lists.
- Editions: SHARED_CORE. Isolation: tenant/store and seller-scoped relations. Extraction: high (SDK hidden behind internal contract).

### Catalog

- Purpose: canonical product identity and descriptive/merchandisable attributes.
- Owns: Catalog Product (and later variant/attribute design — see `docs/architecture/07-catalog-product-offer.md`).
- Must NOT own: seller commercial terms, authored market prices, inventory quantities, search index internals.
- Inbound: Media refs, Content refs by contract. Outbound: Search/AI/Media projections, Offer identity refs.
- Editions: SHARED_CORE. Status: CANDIDATE.

### Offer (Seller Listing)

- Purpose: seller-specific listing/commercial presentation of a catalog product.
- Owns: listing identity, seller binding, listing-level commercial flags (not full price books).
- Must NOT own: canonical product description as source of truth, global catalog identity.
- Inbound: Catalog ID, Seller. Outbound: Pricing, Inventory refs, Search feed.
- Marketplace: REQUIRED. Single-store: may collapse to a single implicit seller **without merging Catalog and Offer data models**.
- Status: CANDIDATE (preserve Product vs Offer).

### Seller / Marketplace

- Purpose: seller participation, seller profile, marketplace policies.
- Owns: seller account/participation (not catalog product).
- Must NOT own: Catalog Product, platform Identity.
- Single-store: DISABLED/overlay. Marketplace: REQUIRED.
- Status: CANDIDATE.

### Pricing

- Purpose: contextual authored and derived prices.
- Owns: price books, market/currency authored prices, promotion hooks, contract price refs; effective-price **calculation service** (not Cart persistence).
- Must NOT own: Locale, Catalog descriptive text, FX market data source as business truth (FX provenance is a pricing concern but source is integration).
- Inbound: Offer/Catalog IDs, Market, Currency, Party/Organization, Quantity, Contract, Promotion.
- Outbound: Cart, Checkout, Order snapshot.
- Status: CANDIDATE.

### Market

- Purpose: commercial market context distinct from locale and currency.
- Owns: market definition and market↔channel policies.
- Must NOT own: translations (Locale) or currency tables as “the market”.
- Standalone vs shared: **candidate standalone commercial context** consumed by Pricing, Catalog availability, Offer. Not merged into Identity or Content.
- Status: NEEDS_FURTHER_P00_ANALYSIS for whether it is its own module vs Pricing-owned context service. Direction: do not bury Market inside Catalog.

### Inventory

- Purpose: availability and reservation toward checkout/order.
- Owns: stock/availability write model.
- Must NOT own: product titles or prices.
- Inbound: Offer/SKU refs. Outbound: Cart/Checkout.
- Status: CANDIDATE.

### Cart

- Purpose: shopper commercial session before order.
- Owns: cart lines, selected context (market/currency/channel) as session.
- Must NOT own: catalog truth, payment capture, inventory as source.
- Inbound: Pricing quotes, Inventory checks. Outbound: Checkout.
- Status: CANDIDATE.

### Checkout

- Purpose: convert cart to an order under snapshotted commercial context.
- Owns: checkout process state.
- Must NOT own: long-term order after confirmation (Order does).
- Status: CANDIDATE (keep adjacent to Order; do not merge blindly — workflow vs record).

### Order

- Purpose: confirmed commercial record including buyer party, acting user, channel, pricing snapshot, currency, market.
- Owns: order write model and lifecycle.
- Must NOT own: live catalog, live price books, payment provider internals.
- Status: CANDIDATE.

### Payment

- Purpose: payment attempts/captures against orders.
- Owns: payment intents/results and PSP references.
- Must NOT own: order lines as catalog.
- Status: CANDIDATE.

### Fulfillment

- Purpose: delivering the order.
- Owns: shipments/fulfillment units.
- Must NOT own: payment or catalog.
- Status: CANDIDATE.

### Promotion

- Purpose: campaigns and promotional modifiers.
- Owns: promotion definitions.
- Must NOT own: base authored price books (Pricing).
- Status: CANDIDATE (may later sit next to Pricing; keep named boundary).

### Content

- Purpose: semantic/editorial knowledge: articles, guides, FAQ, category/brand copy, campaign copy, multilingual metadata, publishing lifecycle, approved AI knowledge.
- Owns: content write model and publish state.
- Must NOT own: page layout trees as the only model; must not be Blog-only.
- Status: CANDIDATE.

### Page Composition

- Purpose: reusable composable landings/sections.
- Owns: composition/layout instances.
- Must NOT own: article body as source of editorial truth.
- Status: CANDIDATE (separate from Content; contract between them).

### Search

- Purpose: discovery over projections.
- Owns: index/feed consumption and query API, not catalog writes.
- Must NOT own: Catalog business truth or SQL joins into Catalog tables.
- Initial tech (confirmed elsewhere): PostgreSQL FTS with abstraction toward OpenSearch.
- Status: CANDIDATE (platform-ish but commercially CORE).

### Media

- Purpose: original + transformed variants, swappable storage/CDN.
- Owns: media assets and derivative records.
- Must NOT own: product identity.
- Status: CANDIDATE.

### Reviews

- Purpose: ratings/reviews bound to product/offer/order eligibility (later).
- Owns: review records.
- Must NOT own: catalog.
- Status: CANDIDATE.

### Notifications

- Purpose: outbound communication.
- Owns: notification requests/templates dispatch, not order.
- Status: CANDIDATE.

### Support

- Purpose: tickets/customer service.
- Owns: ticket records.
- Template tickets are not a confirmed product requirement; keep FUTURE/SUPPORTING until USER confirms.
- Status: NEEDS_FURTHER_P00_ANALYSIS.

### Analytics

- Purpose: first-party PageView, Session, ProductView, Search, AddToCart, Checkout, Purchase.
- Owns: event collection pipeline, not blocking storefront writes.
- Must NOT own: Catalog.
- Status: CANDIDATE (platform+product).

### AI Knowledge / Assistant

- Purpose: grounded answers via approved knowledge and authorization-aware retrieval.
- Owns: assistant orchestration and retrieval policy, not source DBs.
- Must NOT bypass Authorization or query internal tables unrestricted.
- Inbound: Content, Catalog projections, contracts.
- Status: CANDIDATE.

## D. Critical Ownership Questions

### Product / Offer

| Concern | Direction | Later ADR? |
| --- | --- | --- |
| Canonical product identity | Catalog | YES |
| Product descriptive attributes | Catalog | YES (variant/attribute model) |
| Seller-specific listing | Offer | YES |
| Seller-specific commercial terms | Offer + Pricing (split later) | YES |
| Seller-specific availability reference | Inventory keyed by offer/SKU, not Catalog copy | YES |

### Pricing

| Concern | Direction | Later ADR? |
| --- | --- | --- |
| Price books | Pricing | YES |
| Market-specific prices | Pricing + Market context | YES |
| Currency-specific authored prices | Pricing | YES |
| FX-derived display prices | Pricing (derived); FX source is integration | YES |
| Promotions | Promotion modifying Pricing quote | YES |
| B2B/contract pricing | Pricing + Party/Contract FUTURE | YES |
| Effective-price calculation | Pricing service; Cart/Checkout consume quotes | YES |

### Market

Direction: **commercial context, not Locale, not Currency**. Candidate standalone context service; may sit beside Pricing. Later P00 must decide standalone module vs Pricing-owned. Do not conflate.

### Party / Identity

Keep **business Party/Organization** separate from **authentication identity**. A company is not a User row.

### Order

Preserve Buyer Party, Acting User, Sales Channel, Pricing Context Snapshot, Currency, Market. No tables in this task.

### Content / Page Composition

Separate modules (or hard contract sub-capabilities). Semantic content ≠ layout composition.

### Search

Consumes projections/index documents. Does not own Catalog truth.

### AI

Consumes approved knowledge/projections/contracts. Does not own source domains.

## E. Cross-Module Interaction Matrix

Direct DB Join = FORBIDDEN for all rows. Admin/public composites use projections/BFF, not joins.

| Flow | Preferred style |
| --- | --- |
| Catalog -> Search | Projection / Feed |
| Catalog -> Media | Reference by ID |
| Seller/Offer -> Pricing | Synchronous Contract / Query |
| Pricing -> Cart | Synchronous Contract / Query |
| Pricing -> Checkout | Synchronous Contract / Query + snapshot |
| Inventory -> Cart/Checkout | Synchronous Contract / Query + later Command for reserve |
| Checkout -> Order | Command |
| Order -> Payment | Command |
| Order -> Fulfillment | Domain Event / Command |
| Content -> Page Composition | Reference by ID / Contract |
| Content -> Search | Projection / Feed |
| Content -> AI Knowledge | Projection / Feed |
| Catalog -> AI Knowledge | Projection / Feed |
| Identity -> Party | Reference by ID / Policy Context |
| Party -> Authorization | Command / tuples |
| Authorization -> Admin/Customer/Seller operations | Synchronous Contract / Query |
| Analytics <- storefront | Integration Event / fire-and-forget |

## F. Shared Kernel Policy

```text
Shared Kernel must stay intentionally small.
```

May include: strong IDs, Money primitive (amount+currency code, not price policy), currency code, locale code, time primitives, result/error primitives, event metadata.

Must **not** include: Product, Offer, PriceBook, User-as-Party, Market policy, or other mutable business entities.

No code in this task.

## G. Cross-Cutting Platform Concerns

| Concern | Consumed by | Business owner? |
| --- | --- | --- |
| OpenTelemetry | All | No |
| Technical Logging | All | No |
| Audit Infrastructure | Identity, Authorization, Order, Admin | No (audit sink is platform; audit *events* raised by modules) |
| Caching | Search, Catalog reads, Media | No |
| Tenant / Host Resolution | Single-Store storefront (Marketplace uses Deployment Context, not Host→store DB) | No — PLATFORM; see `docs/architecture/02-edition-tenant-deployment.md` |
| Database Connection Resolution | All modules | No — infrastructure; business modules must not pick connections |
| Feature / Edition Configuration | Seller, Pricing policies | No |
| Localization Infrastructure | Content, Experience | No (Locale codes may be kernel) |
| Security Infrastructure | Identity, edge | No |
| Background Jobs / Messaging | Order, Search feeds, Notifications | No |
| File/Object Storage | Media | No |
| CDN / Media Delivery | Media, Experience | No |
| Configuration / Secrets | All | No |

## H. Edition Overlay

| Capability | Marketplace | Single-Store | Future B2B |
| --- | --- | --- | --- |
| Catalog | SHARED_CORE | SHARED_CORE | SHARED_CORE |
| Offer | REQUIRED | SHARED_CORE (single implicit seller; do not delete Offer concept) | SHARED_CORE |
| Seller / Marketplace | REQUIRED | DISABLED | OPTIONAL |
| Pricing | REQUIRED / SHARED_CORE | REQUIRED / SHARED_CORE | REQUIRED (contract overlay FUTURE) |
| Market | REQUIRED | REQUIRED | REQUIRED |
| Party | SHARED_CORE | SHARED_CORE | REQUIRED (org overlay FUTURE) |
| Identity / Authorization | SHARED_CORE | SHARED_CORE | SHARED_CORE |
| Inventory / Cart / Checkout / Order / Payment | REQUIRED | REQUIRED | REQUIRED |
| Content / Page Composition / Search / Media | SHARED_CORE | SHARED_CORE | SHARED_CORE |
| AI Assistance | REQUIRED | REQUIRED | REQUIRED |
| Organization commerce | FUTURE | FUTURE | FUTURE |

Do not fork into two product architectures.

## I. Data Ownership Guardrails

1. Each module owns its write model.
2. External modules reference opaque IDs/contracts, not foreign tables.
3. No cross-domain ORM navigation.
4. No cross-domain SQL joins.
5. Reporting/search/admin aggregation uses projections/read models/contracts.
6. Transaction boundaries normally remain inside one module.
7. Cross-module consistency uses events/workflows, not distributed ACID by default.
8. Future extraction must not expose another module’s internal schema.

## J. Deferred Decisions

```text
exact bounded-context locks
database/schema topology
tenant resolution mechanics (candidate in 02-edition-tenant-deployment.md; not ADR-locked)
catalog aggregate model
variant/attribute model
seller/offer ownership details
pricing calculation model
inventory reservation model
checkout/order workflow
payment abstraction
content schema
page-composition schema
search document schema
media storage/transformation provider
SpiceDB schema
identity credential model
analytics event schema
AI retrieval architecture
Support/tickets as confirmed product vs template-only
Market as standalone module vs Pricing-owned context
Checkout vs Order merge vs split lock
Promotion vs Pricing merge vs split lock
```
