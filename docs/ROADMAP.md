# Tooba — Roadmap

## Current Phase

```text
P04 — Experience Foundation
```

Status:

```text
IN_PROGRESS
```

P00 Architecture / Discovery is COMPLETE (Architect accepted TB-P00-GATE).
P01 Platform Foundation is COMPLETE (Architect accepted TB-P01-GATE).
P02 Identity / Authorization is COMPLETE (Architect accepted TB-P02-GATE).
P03 Commerce Core is COMPLETE after Architect ACCEPT of TB-P03-GATE.

## P01 Work Packages

| ID | Work package | Status |
| --- | --- | --- |
| P01-01 | Platform foundation bootstrap | COMPLETE (Architect accepted TB-P01-T001) |
| P01-02 | Observability & error handling foundation | COMPLETE (Architect accepted TB-P01-T002) |
| P01-03 | Tenant, edition & database resolution foundation | COMPLETE (Architect accepted TB-P01-T003) |
| P01-04 | PostgreSQL persistence & module data ownership | COMPLETE (Architect accepted TB-P01-T004) |
| P01-05 | Persian code documentation & quality guardrails | COMPLETE (Architect accepted TB-P01-T005) |
| P01-06 | Outbox, domain/integration events & background dispatcher | COMPLETE (Architect accepted TB-P01-T006) |
| P01-07 | MassTransit PostgreSQL SQL Transport alignment | COMPLETE (Architect accepted TB-P01-T007) |
| P01-08 | Cache abstraction & tenant-aware caching foundation | COMPLETE (Architect accepted TB-P01-T008) |
| P01-09 | Module composition & boundary enforcement | COMPLETE (Architect accepted TB-P01-T009) |
| P01-GATE | Platform foundation acceptance gate | COMPLETE (Architect accepted TB-P01-GATE) |

## P02 Work Packages

| ID | Work package | Status |
| --- | --- | --- |
| P02-01 | Identity / authentication foundation | COMPLETE (Architect accepted TB-P02-T001) |
| P02-02 | SpiceDB authorization foundation | COMPLETE (Architect accepted TB-P02-T002) |
| P02-03 | Party / organization / membership foundation | COMPLETE (Architect accepted TB-P02-T003) |
| P02-04 | Session / token / credential lifecycle | COMPLETE (Architect accepted TB-P02-T004) |
| P02-05 | Authentication HTTP / API boundary | COMPLETE (Architect accepted TB-P02-T005) |
| P02-GATE | Identity / authorization acceptance gate | COMPLETE (Architect accepted TB-P02-GATE) |

## P03 Work Packages

| ID | Work package | Status |
| --- | --- | --- |
| P03-01 | Catalog product & variant foundation | COMPLETE (Architect accepted TB-P03-T001) |
| P03-02 | Seller offer & listing foundation | COMPLETE (Architect accepted TB-P03-T002) |
| P03-03 | Pricing foundation | COMPLETE (Architect accepted TB-P03-T003) |
| P03-04 | Inventory foundation | COMPLETE (Architect accepted TB-P03-T004) |
| P03-05 | Cart foundation | COMPLETE (Architect accepted TB-P03-T005) |
| P03-06 | Checkout & order foundation | COMPLETE (Architect accepted TB-P03-T006) |
| P03-07 | Tax calculation foundation | COMPLETE (Architect accepted TB-P03-T007) |
| P03-08 | Payment foundation | COMPLETE (Architect accepted TB-P03-T008) |
| P03-09 | Promotion & discount foundation | COMPLETE (Architect accepted TB-P03-T009) |
| P03-GATE | Commerce core acceptance gate | COMPLETE (Architect accepted TB-P03-GATE) |

## P00 Work Packages

Statuses: `PLANNED` | `IN_PROGRESS` | `BLOCKED` | `COMPLETE`

| ID | Work package | Status |
| --- | --- | --- |
| P00-01 | Repository/template technical inventory | COMPLETE |
| P00-02 | Product capability/domain map | COMPLETE |
| P00-03 | Edition/deployment/tenant model | COMPLETE |
| P00-04 | Bounded-context/data-ownership rules | COMPLETE |
| P00-05 | Identity/authentication | COMPLETE |
| P00-06 | SpiceDB authorization | COMPLETE |
| P00-07 | Party/Organization/B2B foundation | COMPLETE |
| P00-08 | Catalog/Product/Variant/Offer/Seller | COMPLETE |
| P00-09 | Pricing/Market/Currency | COMPLETE |
| P00-10 | Inventory | COMPLETE |
| P00-11 | Cart/Checkout/Order | COMPLETE |
| P00-12 | Payment | COMPLETE |
| P00-13 | Content + Page Composition | COMPLETE |
| P00-14 | SEO architecture | COMPLETE |
| P00-15 | Search/indexing | COMPLETE |
| P00-16 | Media pipeline | COMPLETE |
| P00-17 | First-party analytics | COMPLETE |
| P00-18 | AI/RAG | COMPLETE |
| P00-19 | Observability/Audit | COMPLETE |
| P00-20 | Caching/infrastructure abstractions | COMPLETE |
| P00-21 | Frontend/template adaptation strategy | COMPLETE |
| P00-22 | Fulfillment | COMPLETE |
| P00-23 | Promotion / Discount | COMPLETE |
| P00-24 | Capability gap closure review | COMPLETE |
| P00-25 | Reviews / Ratings | COMPLETE |
| P00-26 | Returns / RMA | COMPLETE |
| P00-27 | Tax | COMPLETE |
| P00-28 | P00 architecture gate | COMPLETE |

## Notes

- TB-P00-T000 through TB-P00-T027 and TB-P00-GATE are Architect-accepted.
- P00 is COMPLETE after Architect ACCEPT of Gate.
- P01 Platform Foundation is COMPLETE after Architect ACCEPT of TB-P01-GATE.
- P02 Identity / Authorization is COMPLETE after Architect ACCEPT of TB-P02-GATE.
- P03 Commerce Core is COMPLETE after Architect ACCEPT of TB-P03-GATE.
- P04 Experience Foundation is IN_PROGRESS under TB-P04-T009 (live Checkout → Order). TB-P04-T008 is Architect-accepted. TB-P04-T007 is Architect-accepted. TB-P04-T006 is Architect-accepted. TB-P04-T005 is Architect-accepted as live functional/interaction foundation. Purchased Shopeiva is Next.js 16.2.6 / React 19.2.4 / Tailwind 4. Sell-first rule: preserve Shopeiva with minimum change, connect Tooba live backend. Persian RTL first. Tooba Data Grid remains. Core API integration by end of P06.

## P04 Work Packages

| ID | Work package | Status |
| --- | --- | --- |
| P04-01 | Deep Shopeiva study & reuse map | COMPLETE (Architect accepted TB-P04-T001) |
| P04-02 | Design System extraction | COMPLETE (Architect accepted TB-P04-T002) |
| P04-03 | Professional Data Grid foundation | COMPLETE (Architect accepted TB-P04-T003) |
| P04-04 | Workspace interaction patterns | COMPLETE (Architect accepted TB-P04-T004) |
| P04-05 | Admin Product Workspace | COMPLETE (TB-P04-T005 ACCEPTED as functional/interaction foundation) |
| P04-06 | Shopeiva storefront live Home/Listing/PDP | COMPLETE (Architect accepted TB-P04-T007) |
| P04-07 | Live Cart integration | ACCEPTED (TB-P04-T008) |
| P04-08 | Live Checkout → Order | IN_PROGRESS (TB-P04-T009 awaiting Architect ACCEPT) |

## Mandatory UX sequence (locked)

Do not skip. Later steps wait for new envelopes.

| Future package | Status |
| --- | --- |
| Deep Shopeiva Study | COMPLETE |
| Template reuse map | COMPLETE (TB-P04-T001) |
| Design System extraction | COMPLETE (TB-P04-T002) |
| Professional Data Grid foundation | COMPLETE (TB-P04-T003) |
| Workspace interaction patterns | COMPLETE (TB-P04-T004) |
| Admin Product Workspace | COMPLETE (TB-P04-T005 functional/interaction foundation) |
| Shopeiva storefront live slice | COMPLETE (TB-P04-T007) |
| Live Cart | ACCEPTED (TB-P04-T008) |
| Live Checkout | IN_PROGRESS (TB-P04-T009) |
| Visual evidence / Architect visual ACCEPT | PLANNED |
