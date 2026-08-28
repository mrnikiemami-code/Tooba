# TB-P07-T001-R5 — Screenshot pack index

Capture date: 2026-08-28  
FE: `http://localhost:3000` · Host: `http://127.0.0.1:5088` · Shopeiva: `http://localhost:3001`  
Viewport emulation: CDP `Emulation.setDeviceMetricsOverride` (desktop **1440×900**; mobile **390×844**).

## Dev context

| Surface | Storage keys | Value |
| --- | --- | --- |
| Admin | `tooba.adminActorUserId` (`ADMIN_ACTOR_STORAGE_KEY`) | `01a036c2-970e-7000-8eb7-94bf5cc2d8db` from `GET /v1/admin/dev-context` |
| Seller | `tooba.sellerActorUserId`, `tooba.sellerPartyId` | Actor `01a03628-3f68-7000-844d-99f1cadb54b0`, party `01a030d1-40cb-7000-8abe-6d31739956c5` from `GET /v1/seller/dev-contexts` |

Routes derived from `listLiveAdminNavHrefs()` (`admin-shell.tsx`) and live `menuItems` in `vendor-shell.tsx`. Shopeiva pack covers matching vendor routes that exist on `:3001` (no Tooba-only routes).

---

## A) Admin — `screenshots/admin/` (23 PNG)

| # | File | URL | Viewport | Notes |
| --- | --- | --- | --- | --- |
| 01 | `01-admin-dashboard.png` | `/admin` | 1440×900 | Localized ops center |
| 02 | `02-products.png` | `/admin/products` | 1440×900 | FA DataGrid; live rows |
| 03 | `03-catalog-attributes.png` | `/admin/catalog/attributes` | 1440×900 | |
| 04 | `04-category-schema.png` | `/admin/catalog/category-schema` | 1440×900 | |
| 05 | `05-orders.png` | `/admin/orders` | 1440×900 | |
| 06 | `06-fulfillments.png` | `/admin/fulfillments` | 1440×900 | |
| 07 | `07-returns.png` | `/admin/returns` | 1440×900 | |
| 08 | `08-settlement.png` | `/admin/settlement` | 1440×900 | |
| 09 | `09-payouts.png` | `/admin/payouts` | 1440×900 | |
| 10 | `10-content.png` | `/admin/content` | 1440×900 | |
| 11 | `11-stories.png` | `/admin/stories` | 1440×900 | |
| 12 | `12-page-composition.png` | `/admin/page-composition` | 1440×900 | |
| 13 | `13-sellers.png` | `/admin/sellers` | 1440×900 | |
| 14 | `14-customers.png` | `/admin/customers` | 1440×900 | |
| 15 | `15-reviews.png` | `/admin/reviews` | 1440×900 | |
| 16 | `16-tickets.png` | `/admin/tickets` | 1440×900 | |
| 17 | `17-gift-cards.png` | `/admin/gift-cards` | 1440×900 | |
| 18 | `18-wallets.png` | `/admin/wallets` | 1440×900 | |
| 19 | `19-promotions.png` | `/admin/promotions` | 1440×900 | |
| 20 | `20-settings.png` | `/admin/settings` | 1440×900 | |
| 21 | `21-access-control.png` | `/admin/access-control` | 1440×900 | Multi-state AC center |
| 22 | `22-products-filters-open.png` | `/admin/products` | 1440×900 | **Multi-state:** DataGrid «فیلترها» drawer open |
| 23 | `23-product-workspace-media.png` | `/admin/products/01a030d1-4056-7000-baf1-99951569bc6b` | 1440×900 | **Multi-state:** workspace «رسانه» tab (`workspace-live-shirt`) |

---

## B) Seller — `screenshots/seller/` (14 PNG)

| # | File | URL | Viewport | Notes |
| --- | --- | --- | --- | --- |
| 01 | `01-dashboard.png` | `/vendor-panel` | 1440×900 | Shopeiva-fidelity welcome band |
| 02 | `02-products.png` | `/vendor-panel/products` | 1440×900 | |
| 03 | `03-orders.png` | `/vendor-panel/orders` | 1440×900 | |
| 04 | `04-analytics.png` | `/vendor-panel/analytics` | 1440×900 | |
| 05 | `05-coupons.png` | `/vendor-panel/coupons` | 1440×900 | |
| 06 | `06-reviews.png` | `/vendor-panel/reviews` | 1440×900 | |
| 07 | `07-wallet.png` | `/vendor-panel/wallet` | 1440×900 | |
| 08 | `08-tickets.png` | `/vendor-panel/tickets` | 1440×900 | |
| 09 | `09-notifications.png` | `/vendor-panel/notifications` | 1440×900 | Tooba-only live route |
| 10 | `10-stories.png` | `/vendor-panel/stories` | 1440×900 | |
| 11 | `11-fulfillments.png` | `/vendor-panel/fulfillments` | 1440×900 | |
| 12 | `12-returns.png` | `/vendor-panel/returns` | 1440×900 | |
| 13 | `13-access-control.png` | `/vendor-panel/access-control` | 1440×900 | |
| 14 | `14-settings.png` | `/vendor-panel/settings` | 1440×900 | |

---

## C) Original Shopeiva — `screenshots/shopeiva/` (9 PNG)

| # | File | URL | Viewport | Notes |
| --- | --- | --- | --- | --- |
| 01 | `01-dashboard.png` | `http://localhost:3001/vendor-panel` | 1440×900 | Reference vendor dashboard |
| 02 | `02-products.png` | `/vendor-panel/products` | 1440×900 | |
| 03 | `03-orders.png` | `/vendor-panel/orders` | 1440×900 | |
| 04 | `04-analytics.png` | `/vendor-panel/analytics` | 1440×900 | |
| 05 | `05-coupons.png` | `/vendor-panel/coupons` | 1440×900 | |
| 06 | `06-reviews.png` | `/vendor-panel/reviews` | 1440×900 | |
| 07 | `07-wallet.png` | `/vendor-panel/wallet` | 1440×900 | |
| 08 | `08-tickets.png` | `/vendor-panel/tickets` | 1440×900 | |
| 09 | `09-settings.png` | `/vendor-panel/settings` | 1440×900 | |

Shopeiva has no live routes for notifications, stories, fulfillments, returns, or access-control (404 on `:3001`).

---

## D) Mobile — `screenshots/mobile/` (4 PNG)

| # | File | URL | Viewport | Notes |
| --- | --- | --- | --- | --- |
| 01 | `admin-products.png` | `/admin/products` | 390×844 | Hamburger chrome; stacked grid toolbar |
| 02 | `admin-access-control.png` | `/admin/access-control` | 390×844 | AC center stacks |
| 03 | `vendor-dashboard.png` | `/vendor-panel` | 390×844 | Vendor drawer collapsed |
| 04 | `vendor-products.png` | `/vendor-panel/products` | 390×844 | List header + cards |

---

## Totals

| Pack | PNG count |
| --- | ---: |
| Admin | 23 |
| Seller | 14 |
| Shopeiva | 9 |
| Mobile | 4 |
| **Grand total** | **50** |

Machine manifest: `screenshot-manifest.json`. Capture log: `capture-run-r2.log`.
