# 17 — Admin live data map (TB-P05-T024)

| UI | API | Notes |
|---|---|---|
| Dashboard KPIs | `GET /v1/admin/dashboard` | products/offers/orders/sellers/customers counts |
| Product list | `GET /v1/admin/products` | Catalog composition |
| Product workspace | Product Workspace endpoints | Offers/Pricing/Inventory separate |
| Orders list/detail | `GET /v1/admin/orders[/{id}]` | payment + shipping snapshot |
| Sellers | `GET /v1/admin/sellers` | Party + offer/order aggregates |
| Customers | `GET /v1/admin/customers` | checkout-derived |
| Reviews | Admin review list + moderate | real actions only |
| Dev actor | `GET /v1/admin/dev-context` | header to Host; no browser authz |
| Settings | — | unavailable |
