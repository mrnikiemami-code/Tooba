# 22 — Original vs Tooba Customer Map

| Shopeiva | Tooba | Binding |
| --- | --- | --- |
| `/user-panel` layout shell | `/customer-panel` shell | Same structure; blue accent |
| Dashboard | `/customer-panel` | Live counts + recent orders |
| Orders (+ modal detail) | `/orders` + `/orders/[checkoutId]` | Live Host orders |
| Wishlist | `/wishlist` | Live Wishlist |
| Addresses | `/addresses` | Live AddressBook |
| Profile | `/profile` | Live CustomerProfile |
| Wallet/Tickets/Gift/Notifications/Settings | same paths | Honest unavailable shells |

Route prefix differs (`user-panel` vs `customer-panel`) — MINOR TECHNICAL DEVIATION accepted for Tooba naming.
