# 11 — Seller panel regression audit (TB-P05-T023)

## Before state (pre-restore shell)

Tooba `/vendor-panel` used a custom horizontal/gold-banner shell (`vendor-shell.tsx`), not the Shopeiva sticky header + `w-64` sidebar + mobile `w-[280px]` drawer.

| Area | Before | Gap vs Shopeiva |
|---|---|---|
| Shell/sidebar | Horizontal pills under gold bar | Missing vertical sidebar + sticky 65px header |
| Header | Custom gold gradient | Missing store badge + logout pattern |
| Dashboard cards | 3 live cards; honest | Missing Shopeiva density/quick actions layout |
| Product list | Offer DataGrid (correct architecture) | Keep Offer model; improve shell only |
| Product editor | Offer detail route | Keep; no Product.Price/Stock |
| Orders | Live seller orders | Keep isolation |
| Status presentation | Offer/order status badges | OK |
| Store/profile | Missing nav item | Add settings route as honest unavailable |
| Spacing/density | Flatter storefront-like | Align panel gray-50 + white cards |
| Mobile | Simple drawer with pills | Shopeiva right drawer 280px |
| Unavailable routes | Absent from nav | Add full nav with honest shells |

## After restore target

Shopeiva vendor layout structure with Tooba blue accent; live Dashboard/Products/Orders; honest unavailable for customers/analytics/coupons/reviews/wallet/tickets/gift-cards/settings.
