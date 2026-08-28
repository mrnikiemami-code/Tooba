# TB-P07-T001-R3 — Admin screenshot pack index

Capture date: 2026-08-28  
Actor: `01a036c2-970e-7000-8eb7-94bf5cc2d8db` (`/v1/admin/dev-context` → `localStorage` key `tooba.adminActorUserId`)  
FE: `http://localhost:3000` · Host: `http://127.0.0.1:5088`  
Routes derived from `listLiveAdminNavHrefs()` in `src/frontend/app/admin/admin-shell.tsx` plus dashboard `/admin` and extras below.  
Viewport emulation: CDP `Emulation.setDeviceMetricsOverride` (desktop 1440×900; mobile 390×844).

| # | File | Route | Viewport | Notes |
| --- | --- | --- | --- | --- |
| 01 | `screenshots/01-admin-dashboard.png` | `/admin` | 1440×900 | Localized chrome («مرکز عملیات توبا»، «داشبورد»); metric cards live |
| 02 | `screenshots/02-products.png` | `/admin/products` | 1440×900 | FA grid labels; draft/archived R3 rows visible; thumbnails OK (`brokenImages=0`) |
| 03 | `screenshots/03-catalog-attributes.png` | `/admin/catalog/attributes` | 1440×900 | Nav label «تعاریف ویژگی» |
| 04 | `screenshots/04-category-schema.png` | `/admin/catalog/category-schema` | 1440×900 | Nav label «طرح ویژگی رده» |
| 05 | `screenshots/05-orders.png` | `/admin/orders` | 1440×900 | Localized orders grid |
| 06 | `screenshots/06-fulfillments.png` | `/admin/fulfillments` | 1440×900 | Localized fulfillments |
| 07 | `screenshots/07-returns.png` | `/admin/returns` | 1440×900 | Localized returns |
| 08 | `screenshots/08-settlement.png` | `/admin/settlement` | 1440×900 | Localized settlement |
| 09 | `screenshots/09-payouts.png` | `/admin/payouts` | 1440×900 | Localized payouts |
| 10 | `screenshots/10-content.png` | `/admin/content` | 1440×900 | Localized content |
| 11 | `screenshots/11-stories.png` | `/admin/stories` | 1440×900 | Localized stories |
| 12 | `screenshots/12-page-composition.png` | `/admin/page-composition` | 1440×900 | Localized page composition |
| 13 | `screenshots/13-sellers.png` | `/admin/sellers` | 1440×900 | Localized sellers |
| 14 | `screenshots/14-customers.png` | `/admin/customers` | 1440×900 | Localized customers |
| 15 | `screenshots/15-reviews.png` | `/admin/reviews` | 1440×900 | Localized reviews |
| 16 | `screenshots/16-tickets.png` | `/admin/tickets` | 1440×900 | Localized tickets |
| 17 | `screenshots/17-gift-cards.png` | `/admin/gift-cards` | 1440×900 | Localized gift cards |
| 18 | `screenshots/18-wallets.png` | `/admin/wallets` | 1440×900 | Localized wallets |
| 19 | `screenshots/19-promotions.png` | `/admin/promotions` | 1440×900 | Localized promotions |
| 20 | `screenshots/20-settings.png` | `/admin/settings` | 1440×900 | Localized settings |
| 21 | `screenshots/21-access-control.png` | `/admin/access-control` | 1440×900 | Localized access control center |
| 22 | `screenshots/22-product-workspace-shirt.png` | `/admin/products/01a030d1-4056-7000-baf1-99951569bc6b` | 1440×900 | Live linen shirt (`workspace-live-shirt`); **رسانه** tab; gallery alts «نمای پشت»…«روی مانکن»; 6 imgs, `brokenImages=0` (SVG storefront media previews) |
| 23 | `screenshots/23-mobile-products.png` | `/admin/products` | 390×844 | Hamburger chrome; stacked filter/export controls; sidebar drawer collapsed |
| 24 | `screenshots/24-mobile-access-control.png` | `/admin/access-control` | 390×844 | Mobile AC center layout |

**PNG count:** 24  
**Global notes:** All live nav labels Persian (no raw English nav). Product list + shirt media: no broken `<img>` at capture time. FE was restarted once mid-pack after stale Next chunk 404s blocked hydration.
