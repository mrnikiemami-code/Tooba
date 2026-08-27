# 11 — Panel navigation integrity (TB-P06-T018)

## Rule

Every visible primary nav item must:

1. Lead to a real live route, **or**
2. Be hidden if unavailable  

Never placeholder / dead link / “به‌زودی” advertisement in primary nav for deferred capabilities.

## Customer primary nav (post Wave 1)

| Item | Href | Visible | Live |
|---|---|---|---|
| Dashboard | `/customer-panel` | yes | yes |
| Orders | `/customer-panel/orders` | yes | yes |
| Wishlist | `/customer-panel/wishlist` | yes | yes |
| Addresses | `/customer-panel/addresses` | yes | yes |
| Profile | `/customer-panel/profile` | yes | yes |
| Settings | `/customer-panel/settings` | yes | yes (honest subset) |
| Wallet | `/customer-panel/wallet` | **no** | deep-link shell only |
| Tickets | `/customer-panel/tickets` | **no** | deep-link shell only |
| Gift cards | `/customer-panel/gift-cards` | **no** | deep-link shell only |
| Notifications | `/customer-panel/notifications` | **no** | deep-link shell only |

## Seller primary nav (post Wave 1)

| Item | Href | Visible | Live |
|---|---|---|---|
| Dashboard | `/vendor-panel` | yes | yes |
| Products | `/vendor-panel/products` | yes | yes |
| Orders | `/vendor-panel/orders` | yes | yes |
| Fulfillments | `/vendor-panel/fulfillments` | yes | yes |
| Returns | `/vendor-panel/returns` | yes | yes |
| Analytics | `/vendor-panel/analytics` | yes | yes |
| Wallet | `/vendor-panel/wallet` | yes | yes |
| Settings | `/vendor-panel/settings` | yes | yes (operational read) |
| Customers | `/vendor-panel/customers` | **no** | deep-link shell only |
| Coupons | `/vendor-panel/coupons` | **no** | deep-link shell only |
| Reviews | `/vendor-panel/reviews` | **no** | deep-link shell only |
| Tickets | `/vendor-panel/tickets` | **no** | deep-link shell only |
| Gift cards | `/vendor-panel/gift-cards` | **no** | deep-link shell only |

## Admin primary nav (post Wave 1)

| Item | Href | Visible | Live |
|---|---|---|---|
| Operational set (dashboard, products, orders, fulfillments, returns, settlement, payouts, content, stories, page-composition, sellers, customers, reviews) | `/admin/*` | yes | yes |
| Settings | `/admin/settings` | **no** | honest unavailable if deep-linked |

## Integrity proof approach

- Frontend unit/nav integrity tests assert deferred hrefs are absent from primary nav DOM.
- Manual browser check on desktop + mobile drawers for all three panels (captures pending in `15`).
