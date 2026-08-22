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
P00 — Architecture / Discovery
```

Pipeline Mode:

```text
PIPELINE
```

Last Architect Accepted Task:

```text
TB-P00-T002
```

Current Issued Task:

```text
TB-P00-T003
```

Issued but not accepted:

```text
TB-P00-T003 = ISSUED / AWAITING_ARCHITECT_ACCEPT
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
- Never model product price as one scalar. Locale != Market != Currency.
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
- SpiceDB relationship model;
- Catalog Product vs Seller Offer design;
- Pricing/Market/Currency model details (precision, rounding, FX provenance, history);
- Inventory, Cart/Checkout/Order, Payment designs;
- Content + Page Composition designs;
- Search/indexing feed documents;
- Media pipeline provider choices;
- First-party analytics implementation;
- AI/RAG retrieval contracts;
- Caching/infrastructure abstractions;
- frontend/template adaptation strategy;
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

5. Never invent the next task from memory. Do not execute `TB-P00-T004` unless Architect issues that exact envelope.

P00 discovery inputs (not locked architecture):

```text
docs/architecture/00-technical-inventory.md
docs/architecture/01-capability-domain-map.md
docs/architecture/02-edition-tenant-deployment.md
```

Authorized local envelope path for this issued task:

```text
docs/ai/tasks/TB-P00-T003.task.md
```

Resume: await Architect review of TB-P00-T003. Do not execute `TB-P00-T004` unless Architect issues that envelope.
