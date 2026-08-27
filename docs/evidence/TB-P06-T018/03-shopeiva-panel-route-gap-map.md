# 03 — Shopeiva panel route gap map (TB-P06-T018)

Classification key:

1. Live in Tooba  
2. Partial  
3. Missing  
4. Not commercially required for sellable MVP (Wave 1)  
5. Tooba exceeds Shopeiva (source-derived UI required)

## Customer / Account

| Shopeiva-visible route / capability | Tooba route | Class | Wave 1 action |
|---|---|---|---|
| Dashboard | `/customer-panel` | 1 | Keep live; quick actions live-only |
| Orders | `/customer-panel/orders` | 1 | Keep |
| Addresses | `/customer-panel/addresses` | 1 | Keep |
| Wishlist | `/customer-panel/wishlist` | 1 / 2 | Keep |
| Profile | `/customer-panel/profile` | 1 | Keep; settings bridges here |
| Settings | `/customer-panel/settings` | 2 → Wave 1 live prefs | Profile bridge + locale cookie; security/notification honestly unavailable |
| Wallet | `/customer-panel/wallet` | 3 / 4 | Hide from primary nav; deep-link capability shell remains |
| Tickets/Support | `/customer-panel/tickets` | 3 / 4 | Hide from primary nav; deep-link shell remains |
| Notifications | `/customer-panel/notifications` | 3 / 4 | Hide from primary nav; deep-link shell remains |
| Gift Cards | `/customer-panel/gift-cards` | 3 / 4 | Hide from primary nav; deep-link shell remains |
| Reviews (account) | — | 4 | Not selected |

## Seller / Vendor

| Shopeiva-visible route / capability | Tooba route | Class | Wave 1 action |
|---|---|---|---|
| Dashboard | `/vendor-panel` | 1 | Settings quick action live; remove stub N/A tile |
| Products | `/vendor-panel/products` | 1 | Keep |
| Orders | `/vendor-panel/orders` | 1 | Keep |
| Analytics | `/vendor-panel/analytics` | 1 / 2 | Keep |
| Wallet | `/vendor-panel/wallet` | 1 | Keep |
| Returns | `/vendor-panel/returns` | 1 | Keep |
| Fulfillment | `/vendor-panel/fulfillments` | 1 / 5 | Keep (Tooba exceeds; already source-derived) |
| Coupons/Discounts | `/vendor-panel/coupons` | 3 / 4 | Hide from primary nav |
| Reviews | `/vendor-panel/reviews` | 3 / 4 | Hide from primary nav |
| Customers | `/vendor-panel/customers` | 3 / 4 | Hide from primary nav |
| Tickets/Support | `/vendor-panel/tickets` | 3 / 4 | Hide from primary nav |
| Gift Cards | `/vendor-panel/gift-cards` | 3 / 4 | Hide from primary nav |
| Settings / Profile | `/vendor-panel/settings` | 2 → Wave 1 live operational | Live seller dashboard API read; no fake business profile save |

## Admin

| Capability | Tooba route | Class | Wave 1 action |
|---|---|---|---|
| Operational dashboards / catalogs / orders / etc. | `/admin/*` live set | 1 | Keep |
| Settings | `/admin/settings` | 3 / 4 | Hide from primary nav; route remains honest unavailable |
| Stories / content / composition | `/admin/stories`, `/admin/content`, `/admin/page-composition` | 1 | Keep (prior tasks) |

## Mapping notes

- Deep-link capability shells for deferred Customer/Seller routes remain so accidental bookmarks do not become fake dashboards.
- Primary navigation must never advertise Class 3/4 capabilities as live.
- No second design language: shells stay Shopeiva-derived panel chrome (Tooba blue accent retained as known minor technical deviation).
