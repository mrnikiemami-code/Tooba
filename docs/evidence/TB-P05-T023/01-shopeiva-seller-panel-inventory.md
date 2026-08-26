# 01 — Shopeiva Seller Panel Inventory (TB-P05-T023)

Source root: `D:\Users\User\source\repos\SarvNewVerRequirment\reference\shopeiva`

Primary shell: `src/app/(vendor)/vendor-panel/layout.jsx`

## Routes / components

| Surface | Route | Source path | Visual structure | Current backend capability (Tooba) | Current Tooba binding | Gap | Action |
|---|---|---|---|---|---|---|---|
| Dashboard | `/vendor-panel` | `src/components/vendor/panel/dashboard/dashboard.jsx` | Welcome banner, 4 stat cards, charts, top products, quick actions, recent orders | Live seller dashboard summary only (`activeOffers`, `openOrders`, `paidOrders`, display name) | `/vendor-panel` + `loadSellerDashboard` | Charts/revenue/fake demos in Shopeiva | Keep shell density; bind live cards only; no fake revenue/charts |
| Products list | `/vendor-panel/products` | `src/components/vendor/panel/products/productsList.jsx` | Table/cards of vendor products | Live seller Offers list | `/vendor-panel/products` Offer DataGrid | Product≠Offer; Shopeiva product-centric | Preserve Offer architecture + Shopeiva-adjacent table |
| Product create/edit | `/vendor-panel/products/new`, `/products/[id]` | `productForm.jsx` | Multi-field product form | Offer detail/edit (not Catalog Product ownership) | `/vendor-panel/products/[offerId]` | No Catalog Product edit by seller | Keep Offer editor; do not collapse into Product.Price/Stock |
| Orders | `/vendor-panel/orders` | `orders/ordersList.jsx` | Order list + status | Live seller orders | `/vendor-panel/orders` | Visual density | Preserve isolation + live rows |
| Order detail | `/vendor-panel/orders/[id]` | `orders/orderDetail.jsx` | Detail pane | Live seller order detail | `/vendor-panel/orders/[sellerOrderId]` | — | Keep SpiceDB boundary |
| Customers | `/vendor-panel/customers` | `customers/customersList.jsx` | Customer table | None | Honest unavailable shell | No seller-customers API | Unavailable route |
| Analytics | `/vendor-panel/analytics` | `analytics/analytics.jsx` + charts | Charts | None | Honest unavailable | No analytics backend | Unavailable |
| Coupons | `/vendor-panel/coupons` | `coupons/*` | Coupon CRUD UI | None (storefront coupons ≠ seller-owned) | Honest unavailable | No seller coupon capability | Unavailable |
| Reviews | `/vendor-panel/reviews` | `reviews/reviewsList.jsx` | Review list | None for seller panel | Honest unavailable | — | Unavailable |
| Wallet | `/vendor-panel/wallet` | `wallet/wallet.jsx` | Balance/settlement | None | Honest unavailable | Never fake settlement | Unavailable |
| Tickets | `/vendor-panel/tickets` | `tickets/*` | Ticket list/form | None | Honest unavailable | — | Unavailable |
| Gift cards | `/vendor-panel/gift-cards` | (nav in layout) | Gift card mgmt | None | Honest unavailable | — | Unavailable |
| Settings / store profile | `/vendor-panel/settings` | `settings/settings.jsx` | Store profile forms | No dedicated seller settings API | Honest unavailable shell (structure preserved) | Profile write API missing | Unavailable until capability exists |

## Shell nav order (locked)

Dashboard → Products → Orders → Customers → Analytics → Coupons → Reviews → Wallet → Tickets → Gift-cards → Settings

Accent in Shopeiva: `#E53935`. Tooba keeps `#2563EB` (MINOR TECHNICAL DEVIATION, consistent with prior P05 tasks).
