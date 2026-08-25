# Tooba — Recovery Context

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
PIPELINE
```

Last Architect Accepted Task:

```text
TB-P04-T010
```

Last Implementation Task:

```text
TB-P05-T001
```

Issued but not accepted:

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
TB-P05-T001 = ACCEPTED
P00 = COMPLETE
P01 = COMPLETE
P02 = COMPLETE
P03 = COMPLETE
P04 = COMPLETE
P05 = IN_PROGRESS
TB-P05-T001/T002/T003/T004/T005/T006/T007/T008 = ACCEPTED
TB-P05-T009 = AWAITING_ARCHITECT_ACCEPT
TB-P05-T009-REPAIR-01 = COMPLETED_BY_CURSOR
Next Task = return TB-P05-T009-REPAIR-01 RESULT and wait in the same Architect chat
Identity & Authentication Foundation = COMPLETE
SpiceDB Authorization Foundation = COMPLETE
Party / Organization / Membership Foundation = COMPLETE
Session / Token / Credential Lifecycle = COMPLETE
Authentication HTTP Boundary = COMPLETE
Catalog Product & Variant Foundation = COMPLETE
Seller Offer & Listing Foundation = COMPLETE
Pricing Foundation = COMPLETE
Inventory Foundation = COMPLETE
Cart Foundation = COMPLETE
Checkout & Order Foundation = COMPLETE
Tax Calculation Foundation = COMPLETE
Payment Foundation = COMPLETE
Promotion & Discount Foundation = COMPLETE
P03 Commerce Core Gate = COMPLETE
P04 Experience Foundation = COMPLETE (TB-P04-GATE ACCEPTED)
P05 Operational Surface Integration = IN_PROGRESS (TB-P05-T001 ACCEPTED; waiting Fast Connect envelope)
```

## Recovered Architect procedure

Run:

```bash
git rev-parse --show-toplevel
git fetch origin
git branch --show-current
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch
```

Then read:

```text
AGENTS.md
docs/PROJECT-STATE.md
docs/ROADMAP.md
docs/ai/TOOBA-PIPELINE-PROTOCOL.md
docs/ai/TOOBA-PIPELINE-CONTROLLER.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md
```

Then determine from the repository, not from chat memory:

- current phase;
- last accepted task;
- issued-but-unaccepted task;
- blockers;
- locked / confirmed requirements;
- unresolved decisions;
- exact resume rule.

Never invent the next task from memory.

## Confirmed (not implemented)

- Modular Monolith with microservice-readiness rules.
- Commercial multilingual e-commerce; SEO non-negotiable; production-grade UI/UX.
- Locale != Market != Currency.
- SpiceDB authorization direction; Keycloak-extensible identity without Keycloak coupling.
- Catalog Product vs Seller Offer must be analyzed, not prematurely merged.
- Purchased template `shopeiva.zip` is not in the repository; Architect-verified archive facts are recorded in `docs/architecture/00-technical-inventory.md`.

## Resume rule

1. fetch origin and compare HEAD with origin/main;
2. inspect working tree; do not destroy unknown work;
3. read Project State / Roadmap / Pipeline docs;
4. recover latest accepted/issued task from the repository;
5. execute only a complete authorized envelope;
6. TB-P04-GATE is Architect-accepted. P04 = COMPLETE. P05 = IN_PROGRESS. TB-P05-T001/T002/T003/T004/T005/T006/T007/T008 ACCEPTED. TB-P05-T009 is AWAITING_ARCHITECT_ACCEPT after honest live public merchandising, brand and seller routes. TB-P05-T009-REPAIR-01 is COMPLETED_BY_CURSOR: a Development-only demo seed (`src/backend/Host/Tooba.Host/Storefront/StorefrontDemoCatalogBootstrap.cs` plus `StorefrontDemoCatalogMatrix.cs`) writes 8 top-level categories, 24 child categories, 72 published products, 8 published brands and 96 offers with Pricing and Inventory truth through owning-module directory contracts, and `ICatalogDirectory` gained `PublishCategoryAsync`/`PublishBrandAsync` because Storefront reads only Published rows. Do not invent TB-P05-T010; send RESULT and wait in the same Architect chat. Locked follow-up: PDP completeness must place real backend capability in the correct Shopeiva PDP section — variants/options, short/summary description, full/detailed description, media/gallery, specifications/attributes, seller/offers, pricing, availability, reviews/ratings when capability exists, and related products. Adding the minimum Shopeiva-compatible section is an approved exception to strict no-structure-change, but only when an important real backend capability has no section. Deferred: Payment missing IdempotencyKey → 500/NRE; Cart replace-hold release→reserve window; storefront product card star rating is fixed template decoration, not a backend rating signal.
7. P00 architecture docs remain `docs/architecture/00` through `27`. Bootstrap layout: `docs/architecture/28-platform-foundation-bootstrap.md`. Observability/error foundation: `docs/architecture/29-observability-error-foundation.md`. Tenant/edition/database foundation: `docs/architecture/30-tenant-edition-database-foundation.md`. PostgreSQL persistence foundation: `docs/architecture/31-postgresql-persistence-foundation.md`. Persian documentation standard: `docs/architecture/32-persian-code-documentation-standard.md`. Outbox/events/background foundation: `docs/architecture/33-outbox-domain-events-background-foundation.md`. MassTransit PostgreSQL SQL Transport: `docs/architecture/34-masstransit-postgresql-sql-transport.md`. Cache abstraction foundation: `docs/architecture/35-cache-abstraction-foundation.md`. Module composition and boundary enforcement: `docs/architecture/36-module-composition-boundary-enforcement.md`. P01 gate evidence: `docs/evidence/TB-P01-GATE.md`. Identity authentication foundation: `docs/architecture/37-identity-authentication-foundation.md`. SpiceDB authorization foundation: `docs/architecture/38-spicedb-authorization-foundation.md`. Party organization membership foundation: `docs/architecture/39-party-organization-membership-foundation.md`. Session/token/credential lifecycle: `docs/architecture/40-session-token-credential-lifecycle.md`. Authentication HTTP boundary: `docs/architecture/41-authentication-http-boundary.md`. Catalog product/variant foundation: `docs/architecture/42-catalog-product-variant-foundation.md`. Seller offer/listing foundation: `docs/architecture/43-seller-offer-listing-foundation.md`. Pricing foundation: `docs/architecture/44-pricing-foundation.md`. Deep Shopeiva Study and Professional Data Grid remain mandatory before serious UI.
