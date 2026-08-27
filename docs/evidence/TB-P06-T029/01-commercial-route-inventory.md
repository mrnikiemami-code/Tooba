# 01 — Commercial route inventory (TB-P06-T029)

Source: actual FE nav shells + HTTP probes (`00-route-http-probe.txt`, `03-navigation-integrity.md`). Classification from code `live` flags + runtime open.

## Storefront

| Route | Path | Class | Notes |
| --- | --- | --- | --- |
| Home | `/fa` | LIVE | 200 |
| Listing | `/fa/products` | LIVE | 200; filters/sort live |
| Search | header search | LIVE | storefront search binding |
| PDP | `/fa/products/[slug]` | LIVE | offer/pricing Host |
| Cart | `/fa/cart` | LIVE | 200 |
| Checkout | `/fa/checkout` | LIVE | 200 |
| Payment result | `/fa/payment/result` | LIVE | wallet/PSP result |
| Blog listing | `/fa/blogs` | LIVE | 200 (not `/blog`) |
| Blog article | `/fa/blogs/[slug]` | LIVE | Content owner |

## Customer (`customer-panel-shell.tsx`)

| Route | Path | Class |
| --- | --- | --- |
| Dashboard | `/customer-panel` | LIVE |
| Orders | `/customer-panel/orders` | LIVE |
| Order detail | `/customer-panel/orders/[checkoutId]` | LIVE |
| Wishlist | `/customer-panel/wishlist` | LIVE (capability-gated empty) |
| Addresses | `/customer-panel/addresses` | LIVE |
| Notifications | `/customer-panel/notifications` | LIVE |
| Tickets | `/customer-panel/tickets` | LIVE |
| Wallet | `/customer-panel/wallet` | LIVE |
| Gift cards | `/customer-panel/gift-cards` | LIVE |
| Profile | `/customer-panel/profile` | LIVE |
| Settings | `/customer-panel/settings` | LIVE |

## Seller (`vendor-shell.tsx`)

| Route | Path | Class |
| --- | --- | --- |
| Dashboard | `/vendor-panel` | LIVE |
| Products | `/vendor-panel/products` | LIVE |
| Orders | `/vendor-panel/orders` | LIVE |
| Notifications | `/vendor-panel/notifications` | LIVE |
| Stories | `/vendor-panel/stories` | LIVE |
| Coupons | `/vendor-panel/coupons` | LIVE |
| Reviews | `/vendor-panel/reviews` | LIVE |
| Fulfillments | `/vendor-panel/fulfillments` | LIVE |
| Returns | `/vendor-panel/returns` | LIVE |
| Tickets | `/vendor-panel/tickets` | LIVE |
| Analytics | `/vendor-panel/analytics` | LIVE (charts deferred honest) |
| Wallet/settlement | `/vendor-panel/wallet` | LIVE |
| Access control | `/vendor-panel/access-control` | LIVE |
| Settings | `/vendor-panel/settings` | LIVE |

## Admin (`admin-shell.tsx`)

All listed nav items `live: true` including dashboard, products, orders, fulfillments, returns, settlement, payouts, content, stories, page-composition, sellers, customers, reviews, tickets, gift-cards, wallets, promotions, settings, access-control — HTTP 200 on sampled routes.

## Unauthorized visual deviation

None newly claimed in this inventory pass; Shopeiva structure remains locked contract. Detail sweeps in evidence 12–15.
