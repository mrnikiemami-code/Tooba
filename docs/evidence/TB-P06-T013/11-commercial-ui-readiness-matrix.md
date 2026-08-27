# 11 — Commercial UI readiness matrix (TB-P06-T013)

Task: `TB-P06-T013`

Honest matrix from nav shells + prior P06 acceptances + this Content work.

| Surface | Status | Notes |
|---|---|---|
| Storefront Home / PDP | LIVE (visual review open) | Critical Shopeiva-locked; Content rail LIVE |
| Blog list `/blogs` | **LIVE** (T013) | Host articles |
| Blog detail `/blogs/[slug]` | **LIVE** (T013) | Host article body + SEO metadata |
| Customer panel (orders, wishlist, addresses, profile, returns/fulfillment entry) | Largely LIVE | Stubs remain: wallet, tickets, gift-cards, notifications, settings |
| Seller / vendor panel (products, orders, fulfillments, returns, wallet) | Largely LIVE | Stubs remain: customers, analytics, coupons, reviews, tickets, gift-cards, settings |
| Admin ops (products, orders, fulfillments, returns, settlement, payouts, sellers, customers, reviews) | Largely LIVE | **Content LIVE (T013)**; settings stub |
| Admin settings | STUB | `live: false` in `admin-shell.tsx` |

## Remaining large gaps (explicit)

- Seller: customers / analytics / coupons / reviews / tickets / gift-cards / settings stubs
- Admin: settings stub
- Customer: wallet / tickets / gift-cards / notifications / settings stubs
- Shopeiva engagement (likes/views) not ported

## Audit flag

`COMMERCIAL_UI_READINESS = AUDITED` (this matrix).
