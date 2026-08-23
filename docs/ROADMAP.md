# Tooba — Roadmap

## Current Phase

```text
P01 — Platform Foundation
```

Status:

```text
IN_PROGRESS
```

P00 Architecture / Discovery is COMPLETE (Architect accepted TB-P00-GATE).
Do not execute packages without an Architect-authorized envelope.

## P01 Work Packages

| ID | Work package | Status |
| --- | --- | --- |
| P01-01 | Platform foundation bootstrap | COMPLETE (Architect accepted TB-P01-T001) |
| P01-02 | Observability & error handling foundation | COMPLETE (Architect accepted TB-P01-T002) |
| P01-03 | Tenant, edition & database resolution foundation | COMPLETE (Architect accepted TB-P01-T003) |
| P01-04 | PostgreSQL persistence & module data ownership | COMPLETE (Architect accepted TB-P01-T004) |
| P01-05 | Persian code documentation & quality guardrails | COMPLETE (Architect accepted TB-P01-T005) |
| P01-06 | Outbox, domain/integration events & background dispatcher | IN_PROGRESS |

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
- P01 Platform Foundation is IN_PROGRESS under TB-P01-T006 (awaiting Architect accept).
- `TB-P01-T007` is not authorized.

## Mandatory future UX / template work (not authorized now)

These remain durable requirements. Do not execute without an Architect envelope. Shopeiva is UI/reference input, not architecture truth. Paths must not hard-code a local Windows folder.

| Future package | Status |
| --- | --- |
| Deep Shopeiva Study (file/route/component/layout/dependency/responsive/RTL/docs; reuse map REUSE/ADAPT/REBUILD/DROP/DEFER) | PLANNED — before serious UI / template migration |
| Template reuse map | PLANNED |
| Design System extraction | PLANNED |
| Professional Data Grid foundation | PLANNED |
| Workspace interaction patterns | PLANNED |
| Visual acceptance gates | PLANNED |
