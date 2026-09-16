# Admin Gap Priority — TB-P11-T001

Severity labels only: BLOCKER / HIGH / MEDIUM / LOW.

| Sev | Module | Defect | Impact | Dependency | Task group | FE/BE/Both | Schema? | Migration? |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| BLOCKER | Access Control | Fail-open `accesscontrol.view/manage` | Unauthorized ACC mutation | SpiceDB tuples seed | ACC harden | BE | N | N |
| HIGH | Access Control | Nav fail-open when caps fetch fails; settings/languages/customers ungated | Over-exposure | ACC API | ACC + shell | Both | N | N |
| HIGH | Access Control | No audit UI for access_audit_events | No operator accountability | audit read API | ACC | Both | maybe read model | low |
| HIGH | Edition | Marketplace modules always visible; auth CallContext hardcoded SingleStore | Wrong edition UX/auth | ICurrentEdition | Edition boundaries | Both | N | N |
| HIGH | Menus | Custom table; no dirty guard; not ADG | Ops inconsistency | AppDataGrid | CMS lists | FE | N | N |
| HIGH | Tickets | Custom cards; not server ADG | Unscalable support ops | support query API | Support grid | Both | N | N |
| HIGH | Page composition | Custom list; overlaps Store Pages | Dual home-composition UX | Store Pages decision | CMS consolidate | FE | N | N |
| HIGH | Brands | No Admin Brands CRUD | Catalog gap | Catalog brand APIs | Catalog forms | Both | N if reuse | N |
| HIGH | Sellers/Customers | List-only; no detail/actions | Incomplete market ops | party/order APIs | Market modules | Both | N | N |
| HIGH | Reviews/Promotions/Shipping | Text ops; no confirm | Accidental destructive actions | none | Grid UX harden | FE | N | N |
| HIGH | Wallets/Gift cards | GUID inputs / CardId exposure | Unusable/unsafe for ordinary admins | identity search | Wallet UX | Both | N | N |
| HIGH | Settlement | Client unbounded + marketplace always-on | Perf + edition leak | server grid | Finance | Both | N | N |
| HIGH | Dashboard | Coarse auth; O(n) status pull; edition-blind | Misleading/expensive home | SQL aggregates | Dashboard | Both | N | N |
| MEDIUM | Units/Stories/Attributes/Languages/Receipts | Below-canonical pin/filter/icon maturity | Inconsistent Admin feel | AppDataGrid | Grid convergence | FE | N | N |
| MEDIUM | Store Pages | Export off; Builder visual ACCEPT=NO | Incomplete listing polish | locked Builder | Store Pages polish | FE | N | N |
| MEDIUM | Settings | Mixed EN; no global dirty | Operator confusion | i18n pack | Settings | FE | N | N |
| MEDIUM | Orders | Legacy unbounded GET /orders remains | Accidental misuse | deprecate endpoint | Orders harden | BE | N | N |
| MEDIUM | Categories | Full-tree load | Scale risk | lazy/tree API | Catalog perf | Both | maybe | low |
| LOW | Fulfillments | Bulk latency under large sets | Slow ops | already guarded | Ops polish | BE | N | N |
| LOW | Deferred category-schema | Deep-link only | Discoverability | product decision | Nav triage | FE | N | N |
