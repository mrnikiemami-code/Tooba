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
P02 — Identity / Authorization
```

Pipeline Mode:

```text
PIPELINE
```

Last Architect Accepted Task:

```text
TB-P02-T001
```

Last Architect Accepted Gate:

```text
TB-P01-GATE
```

Current Issued Task:

```text
TB-P02-T002
```

Current Gate:

```text
NONE
```

Gate State:

```text
TB-P01-GATE = ACCEPTED
```

Issued but not accepted:

```text
TB-P02-T001 = ACCEPTED
TB-P02-T002 = REPAIR IN PROGRESS / AWAITING_ARCHITECT_ACCEPT
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
IN_PROGRESS
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

5. Never invent the next task from memory. Do not execute TB-P02-T003 or a new Gate unless Architect issues that exact envelope.

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
```

Authorized local envelope path for this issued work:

```text
docs/ai/tasks/TB-P02-T002-REPAIR.task.md
```

Resume: complete TB-P02-T002 repair and wait in the same Architect chat for ACCEPT / next envelope. Do not start TB-P02-T003 without a new envelope. P00 = COMPLETE. P01 = COMPLETE. P02 IN_PROGRESS.
