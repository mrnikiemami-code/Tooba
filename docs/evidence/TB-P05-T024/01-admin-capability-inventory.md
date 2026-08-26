# 01 — Admin capability inventory (TB-P05-T024)

| Surface | Route | Backend capability | Current UI | Operational purpose | UX weakness (before) | Required action |
|---|---|---|---|---|---|---|
| Dashboard | `/admin` | `GET /v1/admin/dashboard` | KPI cards | Ops overview | Gold + horizontal pills; low density | Shopeiva shell + live KPIs only |
| Catalog / products | `/admin/products` | `GET /v1/admin/products` | Tooba Data Grid | Catalog list | Same shell | Keep Grid; new shell |
| Product workspace | `/admin/products/[id]` | Product Workspace APIs | Tabs compose product/variants/offers/pricing/inventory | Cross-module ops UI | Shell only | Preserve Product≠Offer |
| Orders / payments | `/admin/orders`, detail | `GET /v1/admin/orders[/{id}]` | Grid + detail cards | Order ops | Shell | Live payment/order state only |
| Sellers | `/admin/sellers` | `GET /v1/admin/sellers` | Data Grid | Marketplace sellers | Shell | Keep authz |
| Customers | `/admin/customers` | `GET /v1/admin/customers` | Data Grid | Checkout-derived buyers | Not CRM | No identity mutation |
| Reviews moderation | `/admin/reviews` | Admin review list + moderate | Data Grid + actions | Moderation queue | Shell | Real publish/reject only |
| Settings | `/admin/settings` | None | Honest unavailable | Placeholder nav | Missing | Honest shell |
| Categories / Brands / Pricing / Inventory standalone menus | — | Via product workspace / module APIs | Workspace composition | Ops grouping | Separate menus unnecessary | Keep workspace composition |
| Promotions / Analytics / Audit | — | Not exposed as Admin UI | — | — | — | Unavailable / deferred |
| Q&A / Bulk inquiry admin | — | No dedicated Admin UI here | — | — | — | Deferred unless Host endpoint exists |
