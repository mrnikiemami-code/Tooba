# 22 — Seller data map (TB-P05-T023)

| UI field | Source | Module / API |
|---|---|---|
| Seller display name | Host seller dashboard | `GET /v1/seller/dashboard` → `sellerDisplayName` |
| Active offers count | Host | `activeOffers` |
| Open orders count | Host | `openOrders` |
| Paid orders count | Host | `paidOrders` |
| Offer list rows | Host seller offers | `GET /v1/seller/offers` |
| Offer price / units / status | Offer aggregate | Seller Offer projection (not Product) |
| Order list / detail | Host seller orders | `GET /v1/seller/orders` (+ detail) |
| Actor / Seller context | Dev contexts + local storage | `/v1/seller/dev-contexts` + SpiceDB checks |
| Wallet / analytics / coupons / reviews / tickets / gift / settings / customers | — | Honest unavailable (no capability) |
