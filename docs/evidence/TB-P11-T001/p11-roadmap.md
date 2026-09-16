# P11 Implementation Roadmap — TB-P11-T001

Rules: start at TB-P11-T002; do **not** invent TB-P10-T023; do **not** start T002 in this audit; Admin completion over storefront polish; reuse AppDataGrid; avoid speculative modules.

| Task | Title | Depends on | Scope |
| --- | --- | --- | --- |
| **TB-P11-T002** | Access Control fail-closed + nav capability fail-closed | — | Close BE fail-open; shell deny-on-caps-error; gate settings/languages/customers; tests |
| **TB-P11-T003** | Access Control Center auditability + effective-permissions polish | T002 | Audit event list UI/API; reduce raw key leakage; seller vs admin ceiling verification |
| **TB-P11-T004** | Edition-aware Admin shell & finance surfaces | T002 | Hide/disable sellers/settlement/payouts/dashboard seller tiles in SingleStore; fix auth CallContext to current edition |
| **TB-P11-T005** | DataGrid convergence wave A (Menus, Tickets, Shipping, Units) | — | Migrate menus/tickets to AppDataGrid; pin ops; confirms; no new tables |
| **TB-P11-T006** | DataGrid convergence wave B (Reviews, Promotions, Stories, Attributes, Languages, Gift cards) | T005 | Icon kebab; typed filters; GUID-free columns |
| **TB-P11-T007** | Sellers & Customers operational depth | T004 | Row actions, detail drawers, ACC deep-link; still no fake CRM |
| **TB-P11-T008** | Brands Admin CRUD (list+form) | — | Reuse Catalog brand APIs; ADG list; product/landing selectors already exist |
| **TB-P11-T009** | Settlement/Payouts server-grid + edition gating | T004 | Replace client unbounded settlement; localize statuses; remove GUID prefixes |
| **TB-P11-T010** | Dashboard production hardening | T004 | SQL aggregates; loading skeletons; permission-aware tiles; optional date range if truthful |
| **TB-P11-T011** | CMS composition consolidation decision | Store Pages R13 accepted | Decide fate of `/admin/page-composition` vs Store Pages; migrate or deprecate without dual SoT |
| **TB-P11-T012** | Settings/i18n + dirty-state | — | Persianize residual EN; global unsaved guard on settings tabs |
| **TB-P11-T013** | Wallet/GiftCard ordinary-user UX | T006 | Replace GUID actor/card inputs with searchable selectors |
| **TB-P11-T014** | Orders API deprecation + category tree scale | — | Remove/lock unbounded `GET /v1/admin/orders`; lazy category tree if needed |
| **TB-P11-T015** | Admin ACC audit log + final hardening + locks SF-363…366 validation | T003–T014 | Cross-cutting regression pack; theme isolation; critical-storefront still PASS |

**Out of scope for early P11:** reinventing Builder, storefront visual polish, inventing Notifications Admin without product requirement, RabbitMQ redesign.

**Do not execute TB-P11-T002 in this task.**
