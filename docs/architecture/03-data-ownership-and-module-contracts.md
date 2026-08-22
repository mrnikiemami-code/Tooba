# Tooba — Data Ownership, Cross-Module Contracts & Transaction Boundaries

Status:

```text
P00 architecture design — candidate for later ADR; not an ADR lock
```

Task:

```text
TB-P00-T004
```

Documentation only. Modular Monolith does **not** license logical sharing of tables.

## A. Core Rule

```text
Each business module owns its operational write model and persistence.
```

```text
Cross-module SQL JOIN = FORBIDDEN
Cross-module ORM navigation = FORBIDDEN
Cross-module repository access = FORBIDDEN
Direct write into another module's tables = FORBIDDEN
```

Physical colocation in one process or even one database server does not imply logical ownership. Co-located tables remain module-private.

## B. Module Data Ownership Model

| Module | Owns | References | Must not own | Projection consumers |
| --- | --- | --- | --- | --- |
| Identity | Account lifecycle, typed login identifiers, credential references, method/MFA enrollment, sessions, external IdP bindings, identity security events (see `04-identity-authentication.md`) | Party subject id after explicit link | Party profile, prices, orders, SpiceDB graph | Authn traces / security audit |
| Party | Person/org business identity, membership lifecycle, current addresses/contacts (see `06-party-organization-b2b-foundation.md`) | Optional IdentityId | Passwords, catalog, TenantId, Seller commercial status | Order snapshots, Pricing context, Authorization |
| Authorization | Authorization-relevant relationships/permissions and check/lookup contracts (SpiceDB behind `IAuthorizationService`; see `05-spicedb-authorization.md`) | Party, Identity, resource ids | Catalog/prices/order lines; Identity role matrix | All gated operations |
| Catalog | Canonical product identity & descriptive attributes | Media ids, content ids | Seller terms, prices, stock | Search, AI, Offer, PDP RM |
| Seller / Marketplace | Seller participation/profile | Party | Canonical product | Offer, Admin |
| Offer | Listing binding seller↔catalog, listing flags | Catalog id, Seller id | Canonical description, global price books | Pricing, Inventory, Search |
| Pricing | Price books, authored market/currency prices, quote service | Offer/Catalog ids, Market, Party/contract | Locale, catalog copy | Cart, Checkout, Order snapshot |
| Market / Commercial Context | Market definition & policies | Currency codes (kernel) | Translations, tenant Host | Pricing, Catalog availability |
| Inventory | Availability/reservations write model | Offer/SKU ids | Titles, prices | Cart, Checkout |
| Cart | Cart lines & session commercial context | Offer ids, market/currency | Catalog truth, payment | Checkout |
| Checkout | Checkout process state | Cart, quotes, reservations | Long-term order after confirm | Order |
| Order | Confirmed order + commercial snapshots | Payment refs, fulfillment refs | Live price books, PSP internals | Payment, Fulfillment, Analytics |
| Payment | Payment attempts/results, PSP references | Order id | Order lines as catalog | Order, Admin |
| Fulfillment | Shipments/fulfillment units (see `docs/architecture/21-fulfillment.md`) | Order id | Payments, catalog, Inventory reservations as write model | Order, Notifications, Customer timeline |
| Promotion | Promotion definitions, campaigns, coupon constructs (see `docs/architecture/22-promotion-discount.md`) | Offer/catalog/market refs | Base price books | Pricing quotes |
| Content | Semantic/editorial write model | Opaque product/brand ids | Layout trees as only model | Search, AI, Page Composition |
| Page Composition | Layout/section instances | Content ids | Article body source | Storefront |
| Search | Index/query over feeds | None as business truth | Catalog writes | Storefront, Admin |
| Media | Assets, originals, variants, delivery refs (see `docs/architecture/15-media-image-pipeline.md`) | None as product identity | Product identity, placement roles, vendor URLs as domain data | Catalog, Content, Page Composition, SEO, Search, Theme |
| Reviews | Review records | Product/offer/order ids | Catalog | PDP RM |
| Notifications | Dispatch requests | Tenant/order/party ids | Order truth | — |
| Support | Tickets if confirmed later | Party/order ids | Catalog | Admin |
| Analytics | First-party observations, event contracts, rebuildable aggregates (see `docs/architecture/16-first-party-analytics.md`) | Opaque ids from owning modules | Order/Payment/Catalog/Inventory truth; audit; authn sessions | Dashboards, Search ranking feed, Recommendation/AI (controlled) |
| AI Knowledge / Assistant | Retrieval orchestration/policy, grounded RAG (see `docs/architecture/17-ai-assistant-rag.md`) | Approved knowledge ids, live commerce contracts | Source DBs; SpiceDB graph; unrestricted analytics/media dumps | Storefront assistant |

## C. Cross-Module Read Patterns

| Pattern | Use when |
| --- | --- |
| Synchronous Query Contract | Need current authoritative data; coupling acceptable (e.g. price quote at checkout) |
| Materialized Projection / Read Model | Read-heavy, composite, repeated, extractable |
| Event-fed Local Projection | Consumer can tolerate lag; needs local copy |
| Reference by Opaque ID | Identity only; resolve via contract when needed |
| Batch Export / Feed | Search, analytics, AI, bulk index |

Prefer projections for search, analytics, dashboards, storefront composites, low-latency repeats, extraction.

Never: query another module’s table.

## D. Cross-Module Write Patterns

Approved: Command Contract, Application Service Contract, Domain Event Reaction, Integration Event Reaction, Workflow / Process Manager.

A module changes **only its own** state. Commands = intent. Events = facts that already occurred. No direct foreign persistence writes.

## E. Domain Events vs Integration Events

```text
Domain Event = internal business fact inside a module boundary
Integration Event = stable externalized fact for other modules/services
```

Not every domain event is published. Mapping/versioning at the module edge.

Address without choosing a broker: versioning, backward compatibility, event metadata, correlation/causation, tenant/store context, idempotency.

## F. Transaction Boundary Policy

```text
One business transaction should normally commit inside one module's ownership boundary.
```

No distributed ACID as the default. Tools (not implemented here): local ACID, eventual consistency, outbox, inbox/idempotency, process manager/saga, compensation, reservation.

Synchronous contracts **before** local commit when a decision requires current authority (e.g. inventory reservation check, price quote) without opening the other module’s tables.

## G. Critical Commerce Workflows

**Add to Cart:** Catalog/Offer identity by opaque id; Pricing quote (sync contract); Inventory availability check (sync, not a join); Market/Currency from request/tenant context. Cart stores references + ephemeral quote metadata; revalidate before order.

**Checkout:** Re-quote, revalidate promotion, reserve inventory via Inventory command, bind Party/address by id. No catalog table access.

**Place Order:** Order **snapshots** commercial facts (display, price, currency, tax/fees, discount, buyer, market, channel, seller). Exact fields deferred.

**Payment:** Order references Payment; Payment owns PSP persistence. Order does not store provider internals.

**Fulfillment:** Events/commands between Order and Fulfillment. No table sharing.

**Search / AI:** Projections/feeds only. Never business truth. Never source DB joins.

## H. Reference vs Snapshot Policy

| Store | Typical |
| --- | --- |
| Order | Immutable commercial snapshot + opaque ids |
| Cart | References + temporary pricing; revalidate |
| Search | Denormalized projection, not authority |
| Analytics | Event facts/projections |
| Content→Product | Opaque ProductId, display via projection |

## I. Identifier Policy Across Boundaries

- Opaque IDs; no business meaning in format.
- No FK-driven ownership across modules.
- Globally unique IDs useful for extraction.
- Avoid leaking internal DB surrogates in contracts.

UUID v7 or similar: `NEEDS_ADR` (not locked in SoT).

## J. Shared Kernel Boundary

Allowed candidates: Money primitive, CurrencyCode, LocaleCode, MarketId/typed IDs, TenantId/StoreId, time, correlation/causation, Result/Error.

Forbidden: Product/Customer/Order entities, pricing rules, authorization rules, mutable shared models.

Keep intentionally small.

## K. Reporting / Admin / Composite Screens

Admin may show integrated UX. **Must not** join five modules.

Approved: BFF / application composition, dedicated operational read model, reporting projection, search-backed view, analytics store.

Bad: one screen ⇒ one SQL join across owners.

## L. Search / SEO / Landing Composition

PLP, PDP, brand/category/landing/tag/best-seller pages compose Catalog, Offer, Pricing, Inventory, Content, Reviews, Media, Promotion, SEO metadata via **composite read models**, not joins. Frontend/SEO routes deferred.

## M. Marketplace-Specific Boundary Rules

Catalog Product, Seller, Offer, Pricing, Inventory stay independent. Seller does not own canonical Product. Catalog does not own seller commercial terms. Order lines may attribute seller by id/snapshot. Settlement/accounting is a future financial boundary, not implemented.

## N. Single-Store Edition Boundary Rules

Disable marketplace-only behavior via composition/policy. **Do not** collapse Catalog Product, Offer, Pricing, Inventory into one entity because there is one seller. Implicit commercial actor/policy is allowed; seams remain.

## O. B2B Readiness

Future Organization, membership, contract, price agreement, credit, approvals, buyer vs actor, channel, quantity tier must not steal Catalog/Order/Pricing write ownership.

## P. External Integration Boundary

Adapters/ACL for Payment, SMS/Email, object storage, CDN/images, search engine, IdP/Keycloak, SpiceDB, AI, FX, tax, shipping. Domains consume **internal** contracts, not vendor SDKs. Providers not chosen here.

## Q. Microservice Extraction Readiness

In-process contract → remote API/event without exposing tables.

Indicators: clear ownership, stable contracts, local transactions, idempotent integration, event versioning, local projections, no shared mutable entities, no cross-module joins.

Not every module should become a service.

## R. Boundary Violation Examples

**BAD:** Order query joins Product + Price + Seller + Inventory tables.  
**GOOD:** Order uses owned snapshots/references and contracts/projections.

**BAD:** Content FK + ORM navigation to Catalog Product table.  
**GOOD:** Opaque ProductId + projection/contract for display.

**BAD:** Search updates Product table.  
**GOOD:** Search owns index/projection; consumes feeds/events.

**BAD:** Admin endpoint joins every module schema.  
**GOOD:** Admin reads operational projection/composition service.

## S. Decision Classification

### RECOMMENDED_FOR_ADR

1. Module-owned write models  
2. No cross-module joins  
3. No cross-module ORM navigation  
4. No direct foreign-module writes  
5. Explicit contracts/events/projections  
6. Local transaction default  
7. Eventual consistency for cross-module workflows where appropriate  
8. Outbox/inbox readiness  
9. Opaque references across boundaries  
10. Historical snapshot policy for Orders  
11. Composition/read models for Admin/public aggregates  
12. Vendor integrations behind internal adapters  

### NEEDS_LATER_P00_DETAIL

- Event taxonomy and versioning scheme  
- Outbox/inbox technology  
- Process manager catalog for checkout/payment  
- ID scheme ADR (e.g. UUID v7)  
- Read-model topology for PDP/PLP/Admin  

### DEFERRED

- Final ADR document  
- Schemas, aggregates, APIs  
- Message broker  
- Reservation algorithm, pricing formulas, checkout steps  
- SpiceDB schema, providers, Shopeiva  
