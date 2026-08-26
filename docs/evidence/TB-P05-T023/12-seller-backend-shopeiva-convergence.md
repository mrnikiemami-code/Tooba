# 12 — Seller backend ↔ Shopeiva convergence (TB-P05-T023)

| Shopeiva surface | Tooba Host capability | Convergence |
|---|---|---|
| Dashboard metrics | `GET /v1/seller/dashboard` via `loadSellerDashboard` | LIVE counts only |
| Products | `GET /v1/seller/offers` | LIVE Offer list (Product≠Offer) |
| Product edit | Seller offer detail endpoints | LIVE Offer fields; no Product.Price/Stock |
| Orders | `GET /v1/seller/orders` (+ detail) | LIVE seller-scoped |
| Customers / Analytics / Coupons / Reviews / Wallet / Tickets / Gift / Settings | No seller panel APIs | Honest unavailable shells |
| AuthZ | SpiceDB seller isolation (T001) | Preserved via Actor+Seller context select |

No fake revenue, settlement, charts, or cross-seller leakage.
