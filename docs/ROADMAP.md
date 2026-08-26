# Tooba — Roadmap

## Current Phase

```text
P06 — Core API Integration / Operational Hardening
```

Status:

```text
IN_PROGRESS (TB-P05-GATE ACCEPTED; P05 COMPLETE; TB-P06-T001/T002 ACCEPTED; TB-P06-T003 SUPERSEDED_WRONG_TRANSPORT; TB-P06-T003-R1 AWAITING_ARCHITECT_ACCEPT; MESSAGING_TRANSPORT = MASSTRANSIT_POSTGRESQL_SQL_TRANSPORT; HOME_VISUAL_REVIEW = OPEN_FOR_USER_FEEDBACK; PDP_VISUAL_REVIEW = OPEN_FOR_USER_FEEDBACK)
```

P00 Architecture / Discovery is COMPLETE (Architect accepted TB-P00-GATE).
P01 Platform Foundation is COMPLETE (Architect accepted TB-P01-GATE).
P02 Identity / Authorization is COMPLETE (Architect accepted TB-P02-GATE).
P03 Commerce Core is COMPLETE after Architect ACCEPT of TB-P03-GATE.
P04 Experience Foundation is COMPLETE after Architect ACCEPT of TB-P04-GATE.
P05 Operational Surface Integration is COMPLETE (Architect accepted TB-P05-GATE).
P06 Core API Integration / Operational Hardening is IN_PROGRESS.

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
- P04 Experience Foundation is COMPLETE after Architect ACCEPT of TB-P04-GATE. P05 is IN_PROGRESS. TB-P05-T001 through T013, TB-P05-T009-REPAIR-01, and Bridge-V2 governance migration are accepted. TB-P05-T014 is AWAITING_ARCHITECT_ACCEPT after connecting Shopeiva customer Address Book and checkout saved-address selection to dedicated private AddressBook ownership, actor isolation, CRUD, one-default invariant, and immutable Order shipping snapshots. Purchased Shopeiva is Next.js 16.2.6 / React 19.2.4 / Tailwind 4. Sell-first rule: preserve Shopeiva with minimum change, connect Tooba live backend. Persian RTL first. Tooba Data Grid remains. Core API integration by end of P06.

## P04 Work Packages

| ID | Work package | Status |
| --- | --- | --- |
| P04-01 | Deep Shopeiva study & reuse map | COMPLETE (Architect accepted TB-P04-T001) |
| P04-02 | Design System extraction | COMPLETE (Architect accepted TB-P04-T002) |
| P04-03 | Professional Data Grid foundation | COMPLETE (Architect accepted TB-P04-T003) |
| P04-04 | Workspace interaction patterns | COMPLETE (Architect accepted TB-P04-T004) |
| P04-05 | Admin Product Workspace | COMPLETE (TB-P04-T005 ACCEPTED as functional/interaction foundation) |
| P04-06 | Shopeiva storefront live Home/Listing/PDP | COMPLETE (Architect accepted TB-P04-T007) |
| P04-07 | Live Cart integration | COMPLETE (Architect accepted TB-P04-T008) |
| P04-08 | Live Checkout → Order | COMPLETE (Architect accepted TB-P04-T009) |
| P04-09 | Live Payment boundary | COMPLETE (Architect accepted TB-P04-T010) |
| P04-GATE | Experience foundation acceptance gate | COMPLETE (Architect accepted TB-P04-GATE) |

## P05 Work Packages

| ID | Work package | Status |
| --- | --- | --- |
| P05-01 | Seller Panel Products & Orders live slice | ACCEPTED (TB-P05-T001) |
| P05-02 | Storefront Home Fast Connect | ACCEPTED (TB-P05-T002) |
| P05-03 | Shopeiva Customer Panel live customer data | ACCEPTED (TB-P05-T003) |
| P05-04 | Shopeiva-derived Admin operational surfaces | ACCEPTED (TB-P05-T004) |
| P05-05 | Shopeiva commercial content and campaign surfaces | ACCEPTED (TB-P05-T005) |
| P05-06 | Shopeiva storefront search and discovery | ACCEPTED (TB-P05-T006) |
| P05-07 | Shopeiva PDP live purchase experience | ACCEPTED (TB-P05-T007) |
| P05-08 | Customer purchase continuity and order history | ACCEPTED (TB-P05-T008) |
| P05-09 | Public merchandising and discovery routes | ACCEPTED (TB-P05-T009) |
| P05-09R | Demo catalog depth, brand seed and mega menu evidence | ACCEPTED (TB-P05-T009-REPAIR-01) |
| P05-GOV | Bridge-V2 governance migration | ACCEPTED (TB-P05-GOV-MIGRATION-BRIDGE-V2) |
| P05-GOV-WAKE | Bridge-Wake-V1 governance migration | ACCEPTED (TB-P05-GOV-MIGRATION-BRIDGE-WAKE-V1) |
| P05-16 | Mega Menu level-3 hierarchy | ACCEPTED (TB-P05-T016 / TB-P05-T016-R1) |
| P05-17 | Complete Shopeiva PDP tabs + live Q&A/wholesale | ACCEPTED (TB-P05-T017 / R1 / UNBLOCK-01) |
| P05-18 | Home page Shopeiva fidelity + live data | ACCEPTED (TB-P05-T018 / UNBLOCK-01) |
| P05-19 | Critical Home/PDP visual regression guards | ACCEPTED (TB-P05-T019) |
| P05-20 | Category/Search/Listing Shopeiva fidelity | ACCEPTED (TB-P05-T020) |
| P05-21 | Cart + Checkout Shopeiva fidelity + live commerce | ACCEPTED (TB-P05-T021) |
| P05-22 | Customer Panel Shopeiva fidelity + live data | ACCEPTED (TB-P05-T022) |
| P05-23 | Seller Panel Shopeiva fidelity + live data | ACCEPTED (TB-P05-T023) |
| P05-24 | Admin Panel Shopeiva-compatible ops UX + live data | ACCEPTED (TB-P05-T024) |
| P05-25 | Live runtime visual acceptance + user preview | ACCEPTED (TB-P05-T025) |
| P05-26 | P05 live sellability completion gate | ACCEPTED (TB-P05-T026) |
| P05-26-R1 | Side-by-side Shopeiva review + NU1900 gate repair | ACCEPTED (TB-P05-T026-R1) |
| P05-26-R2 | Home visual fidelity (CSS/motion/carousel/reviews/articles) | ACCEPTED (TB-P05-T026-R2) |
| P05-GATE | P05 architect gate finalization | ACCEPTED (TB-P05-GATE) |

## P06 Work Packages

| ID | Work package | Status |
| --- | --- | --- |
| P06-01 | Production runtime baseline (health/readiness/config/observability) | AWAITING_ARCHITECT_ACCEPT (TB-P06-T001) |
| P05-15 | Customer Profile editing behind Shopeiva UI | ACCEPTED (TB-P05-T015 / TB-P05-T015-R1) |
| P05-10 | PDP backend capability completeness | ACCEPTED (TB-P05-T010) |
| P05-11 | Shopeiva Mega Menu visual fidelity repair | ACCEPTED (TB-P05-T011) |
| P05-12 | Reviews & Ratings behind Shopeiva review UI | ACCEPTED (TB-P05-T012) |
| P05-13 | Wishlist behind Shopeiva customer/PDP/card UI | ACCEPTED (TB-P05-T013) |
| P05-14 | Customer Address Book + checkout saved-address selection | ACCEPTED (TB-P05-T014) |

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
| Live Cart | COMPLETE (TB-P04-T008) |
| Live Checkout | COMPLETE (TB-P04-T009) |
| Live Payment | COMPLETE (TB-P04-T010) |
| P04 Gate | COMPLETE (Architect accepted TB-P04-GATE) |
| P05 Seller Products/Orders | ACCEPTED (TB-P05-T001) |
| P05 Storefront Home Fast Connect | ACCEPTED (TB-P05-T002) |
| P05 Shopeiva Customer Panel | ACCEPTED (TB-P05-T003) |
| P05 Admin Fast Connect | ACCEPTED (TB-P05-T004) |
| P05 Commercial Content Fast Connect | ACCEPTED (TB-P05-T005) |
| P05 Search and Discovery Fast Connect | ACCEPTED (TB-P05-T006) |
| P05 Product Detail Purchase Fast Connect | ACCEPTED (TB-P05-T007) |
| P05 Customer Purchase Continuity | ACCEPTED (TB-P05-T008) |
| P05 Public Merchandising Fast Connect | ACCEPTED (TB-P05-T009) |
| P05 Demo Catalog Depth Repair | ACCEPTED (TB-P05-T009-REPAIR-01) |
| P05 Bridge-V2 Governance Migration | ACCEPTED |
| P05 PDP Backend Capability Completeness | ACCEPTED (TB-P05-T010) |
| P05 Shopeiva Mega Menu Visual Fidelity Repair | ACCEPTED (TB-P05-T011) |
| P05 Reviews & Ratings live convergence | ACCEPTED (TB-P05-T012) |
| P05 Wishlist live convergence | ACCEPTED (TB-P05-T013) |
| P05 Address Book live convergence | AWAITING_ARCHITECT_ACCEPT (TB-P05-T014) |
