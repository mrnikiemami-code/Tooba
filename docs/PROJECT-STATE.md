# Tooba — Project State

Project:

```text
Tooba
```

Canonical repository:

```text
https://github.com/mrnikiemami-code/Tooba
```

Branch:

```text
main
```

Current Phase:

```text
P05 — Operational Surface Integration
```

Pipeline Mode:

```text
BRIDGE-WAKE-V1
Channel: tooba-main
```

Last Architect Accepted Task:

```text
TB-P05-T018-UNBLOCK-01
```

Last Implementation Task:

```text
TB-P05-T019
```

Last Architect Accepted Gate:

```text
TB-P04-GATE
```

Current Issued Task:

```text
TB-P05-T017 = ACCEPTED
TB-P05-T017-R1 = ACCEPTED
TB-P05-T017-UNBLOCK-01 = ACCEPTED
TB-P05-T018 = ACCEPTED
TB-P05-T018-UNBLOCK-01 = ACCEPTED
TB-P05-T019 = AWAITING_ARCHITECT_ACCEPT
```

Current Gate:

```text
NONE
```

Next Phase:

```text
P05 (in progress)
```

Gate State:

```text
TB-P01-GATE = ACCEPTED
TB-P02-GATE = ACCEPTED
TB-P03-GATE = ACCEPTED
TB-P04-GATE = ACCEPTED
```

Issued but not accepted:

```text
TB-P05-T019 = AWAITING_ARCHITECT_ACCEPT
```

Accepted ledger (selected):

```text
TB-P02-T001 = ACCEPTED
TB-P02-T002 = ACCEPTED
TB-P02-T003 = ACCEPTED
TB-P02-T004 = ACCEPTED
TB-P02-T005 = ACCEPTED
TB-P02-GATE = ACCEPTED
TB-P03-T001 = ACCEPTED
TB-P03-T002 = ACCEPTED
TB-P03-T003 = ACCEPTED
TB-P03-T004 = ACCEPTED
TB-P03-T005 = ACCEPTED
TB-P03-T006 = ACCEPTED
TB-P03-T007 = ACCEPTED
TB-P03-T008 = ACCEPTED
TB-P03-T009 = ACCEPTED
TB-P03-GATE = ACCEPTED
TB-P04-T001 = ACCEPTED
TB-P04-T002 = ACCEPTED
TB-P04-T003 = ACCEPTED
TB-P04-T004 = ACCEPTED
TB-P04-T005 = ACCEPTED
TB-P04-T006 = ACCEPTED
TB-P04-T007 = ACCEPTED
TB-P04-T008 = ACCEPTED
TB-P04-T009 = ACCEPTED
TB-P04-T010 = ACCEPTED
TB-P04-GATE = ACCEPTED
```

Observability / Error Handling Foundation:

```text
COMPLETE (Architect accepted TB-P01-T002)
```

Tenant / Edition / Database Resolution Foundation:

```text
COMPLETE (Architect accepted TB-P01-T003)
```

PostgreSQL Persistence Foundation:

```text
COMPLETE (Architect accepted TB-P01-T004)
```

Persian Code Documentation Standard:

```text
COMPLETE (Architect accepted TB-P01-T005)
```

Outbox / Domain Events / Background Foundation:

```text
COMPLETE (Architect accepted TB-P01-T006)
```

MassTransit PostgreSQL SQL Transport Alignment:

```text
COMPLETE (Architect accepted TB-P01-T007)
```

Cache Abstraction Foundation:

```text
COMPLETE (Architect accepted TB-P01-T008)
```

Module Composition & Boundary Enforcement:

```text
COMPLETE (Architect accepted TB-P01-T009)
```

P01 Platform Foundation Gate:

```text
COMPLETE (Architect accepted TB-P01-GATE)
```

Identity & Authentication Foundation:

```text
COMPLETE (Architect accepted TB-P02-T001)
```

SpiceDB Authorization Foundation:

```text
COMPLETE (Architect accepted TB-P02-T002)
```

Party / Organization / Membership Foundation:

```text
COMPLETE (Architect accepted TB-P02-T003)
```

Session / Token / Credential Lifecycle:

```text
COMPLETE (Architect accepted TB-P02-T004)
```

Authentication HTTP Boundary:

```text
COMPLETE (Architect accepted TB-P02-T005)
```

P02 Identity / Authorization Gate:

```text
COMPLETE (Architect accepted TB-P02-GATE)
```

Catalog Product & Variant Foundation:

```text
COMPLETE (Architect accepted TB-P03-T001)
```

Seller Offer & Listing Foundation:

```text
COMPLETE (Architect accepted TB-P03-T002)
```

Pricing Foundation:

```text
COMPLETE (Architect accepted TB-P03-T003)
```

Inventory Foundation:

```text
COMPLETE (Architect accepted TB-P03-T004)
```

Cart Foundation:

```text
COMPLETE (Architect accepted TB-P03-T005)
```

Checkout & Order Foundation:

```text
COMPLETE (Architect accepted TB-P03-T006)
```

Tax Calculation Foundation:

```text
COMPLETE (Architect accepted TB-P03-T007)
```

Payment Foundation:

```text
COMPLETE (Architect accepted TB-P03-T008)
```

Promotion & Discount Foundation:

```text
COMPLETE (Architect accepted TB-P03-T009)
```

P03 Commerce Core Gate:

```text
COMPLETE (Architect accepted TB-P03-GATE)
```

P04 Experience Foundation:

```text
COMPLETE (Architect accepted TB-P04-GATE)
```

P05 Operational Surface Integration:

```text
IN_PROGRESS (TB-P05-T001 through T018 / T018-UNBLOCK-01 ACCEPTED; TB-P05-T019 = AWAITING_ARCHITECT_ACCEPT)
```

Design System Foundation:

```text
COMPLETE (Architect accepted TB-P04-T002)
```

Professional Data Grid Foundation:

```text
COMPLETE (Architect accepted TB-P04-T003)
```

Workspace Interaction Patterns:

```text
COMPLETE (Architect accepted TB-P04-T004)
```

Admin Product Workspace:

```text
COMPLETE (TB-P04-T005 Architect ACCEPTED as live functional/interaction foundation; custom Admin visual language is not the final Tooba target)
```

ErrorState retry label i18n gap:

```text
RESOLVED (bounded retryLabel on ErrorState in TB-P04-T004)
```

Grid virtualization:

```text
DEFERRED_NON_BLOCKING
```

Project-wide documentation rule:

```text
All required Tooba-owned Classes / Interfaces / Methods / Properties
must have strong Persian documentation.
```

Known Blockers:

```text
NONE
```

Architecture Status:

```text
CONFIRMED: Modular Monolith with mandatory microservice-readiness
UNRESOLVED: exact bounded contexts, tenant implementation code/ADR lock, locale list, and other P00 design details listed below
```

## Confirmed Requirements

Recorded from Architect-authorized TB-P00-T000. These are durable requirements, not implemented product.

### Product / Quality

- Commercial multilingual e-commerce product.
- Must reach a sellable state quickly without sacrificing production quality.
- SEO is top-tier and non-negotiable.
- UI/UX is commercially critical, production-grade, mobile-intentional, accessible, multilingual, and must not degrade into developer-skeleton screens.
- Digikala/Amazon are references, not architecture truth, and must not be copied.
- Purchased template is an adaptation/reference input, not architecture truth.

### Editions / Deployment

Marketplace edition (currently stated model):

```text
one dedicated marketplace publish/deployment
one marketplace database
multi-seller marketplace behavior
```

Single-store commercial edition (currently stated requirement):

```text
one shared publish/deployment for all single-store customers
many domains
incoming domain -> store/tenant resolution -> correct database
one database per customer/store
theme per store
not multi-vendor
```

Tenant implementation is not finalized.

### Architecture

```text
Modular Monolith
```

Mandatory microservice-readiness:

- module/domain data ownership;
- no direct cross-module DB joins;
- no cross-module table/repository access;
- collaboration through explicit contracts/interfaces/gateways/events;
- future in-process gateway replacement by remote integration without rewriting consuming business logic.

### Other confirmed directions

- Content is broad Content, not Blog-only.
- Semantic Content != Page Composition; landing pages must be reusable/composable.
- Identity identifiers: username, phone, email, national ID, future identifiers.
- Authentication: password, OTP login, optional 2FA/MFA, future external identity providers; extensible to Keycloak without coupling core identity to Keycloak.
- Authorization direction: SpiceDB; relationship-based; do not collapse into fixed role columns.
- Full B2B is after first sellable release; P00 must preserve Party/Organization foundations.
- Never model product price as one scalar. Locale != Market != Currency != Tax Jurisdiction.
- USER Tax policy (architecture input, not implemented law): Iran first-market emphasis; UK/other markets readiness; tax-exclusive base; Tooba calculates tax; configurable effective-dated percentage rules; context override only if enabled; tax-exempt supported; no hard-coded rate/date/law; B2B VAT/invoice out of initial phase. See `docs/architecture/26-tax-architecture.md`.
- P00 must analyze Catalog Product vs Seller Offer / Listing; do not prematurely merge them.
- Search: initial PostgreSQL Full Text Search; future Elasticsearch / OpenSearch; domain logic must not couple to PostgreSQL search internals.
- Caching abstracted so Redis can be added later without redesign. Initial hosting may be public/shared; later dedicated.
- Observability: OpenTelemetry, advanced technical logging, metrics, traces, audit logging. Technical logs and audit/business events remain conceptually separate.
- First-party analytics planned in addition to third-party analytics.
- Media: original + transformed/cached variants; swappable storage/CDN.
- Customer-facing AI agent required; grounded/RAG; no unrestricted direct AI access to internal DBs.
- Product is multilingual. Do not bind Locale to Market or Currency.

## Unresolved P00 Decisions

- exact bounded contexts;
- tenant implementation code, control-plane storage, and ADR lock of the T003 candidate (see `docs/architecture/02-edition-tenant-deployment.md`);
- initial locale list;
- indexation / canonical / hreflang / structured data / sitemap ownership details;
- Identity uniqueness policy per identifier type/edition and whether one Identity may span Single-Store tenants (see `docs/architecture/04-identity-authentication.md`);
- SpiceDB relationship model;
- Catalog Product vs Seller Offer design;
- Pricing/Market/Currency model details (precision, rounding, FX provenance, history);
- Inventory, Cart/Checkout/Order, Payment designs;
- Content + Page Composition designs;
- Media pipeline provider choices (see `docs/architecture/15-media-image-pipeline.md`);
- First-party analytics implementation (see `docs/architecture/16-first-party-analytics.md`);
- AI/RAG retrieval contracts (see `docs/architecture/17-ai-assistant-rag.md`);
- Observability/audit implementation (see `docs/architecture/18-observability-logging-audit.md`);
- Caching/infrastructure abstractions (see `docs/architecture/19-caching-infrastructure-abstractions.md` and P01 foundation `docs/architecture/35-cache-abstraction-foundation.md`);
- frontend/template adaptation strategy (see `docs/architecture/20-frontend-ux-template-adaptation.md`);
- whether/how `shopeiva.zip` is present as a later inventory input.

## Purchased Template

Architect has received `shopeiva.zip`.

Repository presence at TB-P00-T000 execution:

```text
NOT_PRESENT_IN_REPOSITORY
```

Do not copy, unzip, vendor, or modify it until an authorized later P00 inventory task.

## Repository State

```text
Primary branch: main
Required after each task: HEAD == origin/main
```

Do not record a recursive commit SHA in this file.

## Exact Resume Rule

1. Run:

```bash
git rev-parse --show-toplevel
git fetch origin
git branch --show-current
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch
```

2. Read:

```text
AGENTS.md
docs/PROJECT-STATE.md
docs/ROADMAP.md
docs/ai/TOOBA-PIPELINE-PROTOCOL.md
docs/ai/TOOBA-PIPELINE-CONTROLLER.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md
```

3. Determine current phase, last accepted task, issued-but-unaccepted task, blockers, locked requirements, unresolved decisions, and this resume rule from the repository.

4. Execute only a complete Architect-authorized envelope (`BEGIN_TOOBA_CURSOR_TASK_V1` / `BEGIN_TOOBA_CURSOR_GATE_V1`).

5. Never invent the next task from memory. Do not execute TB-P03-T002 or a P03 Gate unless Architect issues that exact envelope.

P00 discovery inputs (not locked architecture):

```text
docs/architecture/00-technical-inventory.md
docs/architecture/01-capability-domain-map.md
docs/architecture/02-edition-tenant-deployment.md
docs/architecture/03-data-ownership-and-module-contracts.md
docs/architecture/04-identity-authentication.md
docs/architecture/05-spicedb-authorization.md
docs/architecture/06-party-organization-b2b-foundation.md
docs/architecture/07-catalog-product-offer.md
docs/architecture/08-pricing-market-currency.md
docs/architecture/09-inventory-availability-reservation.md
docs/architecture/10-cart-checkout-order.md
docs/architecture/11-payment.md
docs/architecture/12-content-page-composition.md
docs/architecture/13-seo-architecture.md
docs/architecture/14-search-indexing.md
docs/architecture/15-media-image-pipeline.md
docs/architecture/16-first-party-analytics.md
docs/architecture/17-ai-assistant-rag.md
docs/architecture/18-observability-logging-audit.md
docs/architecture/19-caching-infrastructure-abstractions.md
docs/architecture/20-frontend-ux-template-adaptation.md
docs/architecture/21-fulfillment.md
docs/architecture/22-promotion-discount.md
docs/architecture/23-p00-capability-gap-review.md
docs/architecture/24-reviews-ratings.md
docs/architecture/25-returns-rma.md
docs/architecture/26-tax-architecture.md
docs/architecture/27-p00-gate-review.md
docs/architecture/28-platform-foundation-bootstrap.md
docs/architecture/29-observability-error-foundation.md
docs/architecture/30-tenant-edition-database-foundation.md
docs/architecture/31-postgresql-persistence-foundation.md
docs/architecture/32-persian-code-documentation-standard.md
docs/architecture/33-outbox-domain-events-background-foundation.md
docs/architecture/34-masstransit-postgresql-sql-transport.md
docs/architecture/35-cache-abstraction-foundation.md
docs/architecture/36-module-composition-boundary-enforcement.md
docs/architecture/37-identity-authentication-foundation.md
docs/architecture/38-spicedb-authorization-foundation.md
docs/architecture/39-party-organization-membership-foundation.md
docs/architecture/40-session-token-credential-lifecycle.md
docs/architecture/41-authentication-http-boundary.md
docs/architecture/42-catalog-product-variant-foundation.md
docs/architecture/43-seller-offer-listing-foundation.md
docs/architecture/44-pricing-foundation.md
docs/architecture/45-inventory-foundation.md
docs/architecture/46-cart-foundation.md
docs/architecture/47-checkout-order-foundation.md
docs/architecture/48-tax-calculation-foundation.md
docs/architecture/49-payment-foundation.md
docs/architecture/50-promotion-discount-foundation.md
docs/architecture/51-shopeiva-study-reuse-map.md
docs/architecture/52-design-system-foundation.md
docs/architecture/53-professional-data-grid-foundation.md
docs/architecture/54-workspace-interaction-patterns.md
docs/architecture/55-admin-product-workspace.md
docs/architecture/56-storefront-live-slice.md
```

Bridge-Wake-V1 task audit artifact for the current governance work:

```text
docs/ai/tasks/TB-P05-GOV-MIGRATION-BRIDGE-WAKE-V1.task.md
```

Historical Bridge-V2 governance migration artifact (evidence only):

```text
docs/ai/tasks/TB-P05-GOV-MIGRATION-BRIDGE-V2.task.md
```

Recorded principle: Seller authorization must bind authenticated actor to Seller Party; requested SellerPartyId is context, never authority.

Resume: `PIPELINE-PROTOCOL: BRIDGE-WAKE-V1`; channel `tooba-main`; Worker is normally IDLE/OFFLINE between Tasks; External Watchdog sends `BRIDGE-WAKE` when a Pending Task appears; no continuous polling while idle. P04 = COMPLETE and P05 = IN_PROGRESS. TB-P05-T017 / T017-R1 / T017-UNBLOCK-01 / T018 / T018-UNBLOCK-01 are Architect-accepted. TB-P05-T019 = AWAITING_ARCHITECT_ACCEPT (Home/PDP visual regression guards). Evidence under `docs/evidence/TB-P05-T019/` and contracts under `docs/visual-baselines/`. Worker PASS is not Architect ACCEPT. Historical task/result artifacts may contain retired pipeline syntax and remain evidence only. P00–P04 = COMPLETE.
