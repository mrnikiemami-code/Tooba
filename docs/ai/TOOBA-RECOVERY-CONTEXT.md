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
P03 — Commerce Core
```

Pipeline Mode:

```text
PIPELINE
```

Last Architect Accepted Task:

```text
TB-P03-T004
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
TB-P03-T005 = ISSUED / AWAITING_ARCHITECT_ACCEPT
P00 = COMPLETE
P01 = COMPLETE
P02 = COMPLETE
P03 = IN_PROGRESS
Identity & Authentication Foundation = COMPLETE
SpiceDB Authorization Foundation = COMPLETE
Party / Organization / Membership Foundation = COMPLETE
Session / Token / Credential Lifecycle = COMPLETE
Authentication HTTP Boundary = COMPLETE
Catalog Product & Variant Foundation = COMPLETE
Seller Offer & Listing Foundation = COMPLETE
Pricing Foundation = COMPLETE
Inventory Foundation = COMPLETE
Cart Foundation = IN_PROGRESS
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
6. execute `TB-P03-T005` then await Architect review; do not start `TB-P03-T006` unless Architect issues that envelope.
7. P00 architecture docs remain `docs/architecture/00` through `27`. Bootstrap layout: `docs/architecture/28-platform-foundation-bootstrap.md`. Observability/error foundation: `docs/architecture/29-observability-error-foundation.md`. Tenant/edition/database foundation: `docs/architecture/30-tenant-edition-database-foundation.md`. PostgreSQL persistence foundation: `docs/architecture/31-postgresql-persistence-foundation.md`. Persian documentation standard: `docs/architecture/32-persian-code-documentation-standard.md`. Outbox/events/background foundation: `docs/architecture/33-outbox-domain-events-background-foundation.md`. MassTransit PostgreSQL SQL Transport: `docs/architecture/34-masstransit-postgresql-sql-transport.md`. Cache abstraction foundation: `docs/architecture/35-cache-abstraction-foundation.md`. Module composition and boundary enforcement: `docs/architecture/36-module-composition-boundary-enforcement.md`. P01 gate evidence: `docs/evidence/TB-P01-GATE.md`. Identity authentication foundation: `docs/architecture/37-identity-authentication-foundation.md`. SpiceDB authorization foundation: `docs/architecture/38-spicedb-authorization-foundation.md`. Party organization membership foundation: `docs/architecture/39-party-organization-membership-foundation.md`. Session/token/credential lifecycle: `docs/architecture/40-session-token-credential-lifecycle.md`. Authentication HTTP boundary: `docs/architecture/41-authentication-http-boundary.md`. Catalog product/variant foundation: `docs/architecture/42-catalog-product-variant-foundation.md`. Seller offer/listing foundation: `docs/architecture/43-seller-offer-listing-foundation.md`. Pricing foundation: `docs/architecture/44-pricing-foundation.md`. Deep Shopeiva Study and Professional Data Grid remain mandatory before serious UI.
