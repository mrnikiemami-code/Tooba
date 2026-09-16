# Admin Performance Risk — TB-P11-T001

| Risk | Module | Impact | Evidence |
| --- | --- | --- | --- |
| Unbounded status materialization | Dashboard | CPU/memory grows with order volume | `AdminPanelComposer.GetDashboardAsync` pulls statuses then counts in CLR |
| Legacy unbounded order list | Orders API | `GET /v1/admin/orders` still exposed; FE grid uses `/query` | `AdminPanelEndpoints.cs`, `admin-api.ts` |
| Client-side full load | Settlement balances | All balances → client grid | `loadAdminSettlementBalances` + ClientGridPage |
| Category full tree | Catalog categories | Large catalogs load full tree | `category-admin-screen.tsx` |
| Tickets pageSize 100 | Support | Soft unbounded list (not ADG server query) | `tickets/page.tsx` |
| Attribute/schema client grids | Catalog attributes | Client adapters over loaded sets | `catalog-attribute-ui.tsx` |
| Gift cards client grid | Wallet | Same pattern | wallet UI |
| Fulfillment bulk | Fulfillments | Sequential per-item processing — watch latency | `AdminFulfillmentWorkQueueComposer.ExecuteBulkAsync` |
| Order detail Includes | Orders | Heavy Include graph per detail | `GetOrderAsync` |
| Settlement directory loops | Settlement BE | Multiple foreach over ids/accounts | `SettlementDirectory.cs` |

Focus: high-risk production paths only. No broad refactor in T001.
