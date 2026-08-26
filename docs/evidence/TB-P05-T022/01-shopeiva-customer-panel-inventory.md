# 01 — Shopeiva Customer Panel Inventory

Accent: Shopeiva `#E53935`; Tooba keeps `#2563EB` (MINOR TECHNICAL DEVIATION).

| Route (Shopeiva) | Source | Visual structure | Backend capability | Tooba binding | Gap (before) | Action |
| --- | --- | --- | --- | --- | --- | --- |
| `/user-panel` | `layout.jsx` + dashboard clients | sticky header, full-height sidebar `w-64`, welcome, stats, quick actions, recent orders | live orders/wishlist/address counts | `/customer-panel` | storefront-like chrome; wrong nav order | Restore Shopeiva shell + live dashboard |
| `/user-panel/orders` | `orders.jsx` + modal | stats + chips + expandable cards | live customer orders | `/customer-panel/orders` + `[checkoutId]` | OK live; shell drift | Keep live list/detail; shell fix |
| order detail | modal `orderDetailModal.jsx` | modal summary | live checkout read | dedicated detail route | route differs (MINOR) | Keep live detail page (tech deviation) |
| `/user-panel/wishlist` | `wishlistItems.jsx` | 2/4 product grid | Wishlist module | `/customer-panel/wishlist` | shell only | Keep live |
| `/user-panel/addresses` | `addressesList.jsx` | card grid + form modal | AddressBook | `/customer-panel/addresses` | shell only | Keep live |
| `/user-panel/profile` | `profileForm.jsx` | `max-w-2xl` form | CustomerProfile | `/customer-panel/profile` | shell only | Keep live |
| `/user-panel/wallet` | `wallet.jsx` | balance hero | none | `/customer-panel/wallet` | fake risk | Honest unavailable |
| `/user-panel/tickets` (+new/id) | tickets list/form/detail | list + chat | none | `/customer-panel/tickets` | missing route | Honest unavailable shell |
| `/user-panel/gift-cards` | `userGiftCards` | balance + redeem | none | `/customer-panel/gift-cards` | — | Honest unavailable |
| `/user-panel/notifications` | `notifications.jsx` | typed cards | none | `/customer-panel/notifications` | — | Honest unavailable |
| `/user-panel/settings` | `settings.jsx` | tabs | none | `/customer-panel/settings` | missing | Honest unavailable |

Nav order (locked): Dashboard → Orders → Wishlist → Wallet → Tickets → Gift-cards → Addresses → Notifications → Profile → Settings.
