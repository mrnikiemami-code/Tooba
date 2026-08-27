# 14 — Navigation integrity (TB-P06-T020)

Date: 2026-08-27  
Rule: `PANEL_NAVIGATION = HONEST_LIVE_ONLY` — only surfaces with real Host capability appear in primary nav.

## Seller (`vendor-shell.tsx`)

| Item | Href | Nav status | Notes |
|---|---|---|---|
| تخفیف‌ها (coupons) | `/vendor-panel/coupons` | **LIVE** (`live: true`) | Removed from `VENDOR_DEFERRED_NAV_HREFS` |
| نظرات (reviews) | `/vendor-panel/reviews` | **LIVE** (`live: true`) | Removed from deferred set |
| Notifications | — | **Not exposed** | No Bell item; no `/vendor-panel/notifications` route |
| Tickets | `/vendor-panel/tickets` | Deferred / deep-link only | Still in `VENDOR_DEFERRED_NAV_HREFS` |
| Customers / gift-cards | deferred hrefs | Deferred / deep-link only | Unchanged Wave-1 honesty |

Integrity test: `panel-nav-integrity.test.ts` asserts `/vendor-panel/coupons` and `/vendor-panel/reviews` are LIVE and not deferred.

## Customer (`customer-panel-shell.tsx`)

| Item | Href | Nav status |
|---|---|---|
| Notifications | `/customer-panel/notifications` | **Hidden** — remains in `CUSTOMER_DEFERRED_NAV_HREFS` |
| Tickets / wallet / gift-cards | deferred hrefs | Hidden (Wave 1 + Wave 2 unchanged) |

No notification inbox promotion into live menu (decision gate Option C).

## Admin (`admin-shell.tsx`)

| Item | Href | Nav status |
|---|---|---|
| نظرات | `/admin/reviews` | **LIVE** (pre-existing) |
| پروموشن‌ها | `/admin/promotions` | **LIVE** (Wave 2) |
| Settings | `/admin/settings` | Deferred — `ADMIN_DEFERRED_NAV_HREFS` |

## Dead-link check

| Surface | Result |
|---|---|
| Live seller coupons / reviews | Bound to Host APIs + live pages |
| Live admin promotions / reviews | Bound to Host APIs + live screens |
| Deferred notifications / tickets | Deep-link shells or absent; not in primary nav |

## Verdict

Seller coupons + reviews live; notifications deferred/hidden; admin promotions + reviews live; tickets still deferred.
