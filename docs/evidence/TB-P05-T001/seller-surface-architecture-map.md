# TB-P05-T001 — Seller surface architecture map

```text
Seller UI (/vendor-panel)
  → Next rewrite /v1/*
  → Host SellerPanelEndpoints (/v1/seller/...)
  → X-Tooba-Seller-Party-Id (required)
  → SellerPanelComposer
       → OfferDbContext (Offers filtered by SellerPartyId)
       → CatalogDbContext (variant/product titles — read-only context)
       → PricingDbContext (authored amounts by OfferId)
       → InventoryDbContext (positions by OfferId)
       → OrderDbContext (SellerOrders + CheckoutGroup snapshots)
       → IPartyLookupGateway (seller display name)
```

## Invariants proven

| Rule | Evidence |
| --- | --- |
| No cross-module SQL JOIN | Composer queries each DbContext separately |
| No frontend-only authz | Missing/wrong seller header → 400/404 at Host |
| Catalog Product ≠ Seller Offer | UI labels Offer; Catalog section `CatalogReadOnly` |
| No Product.Price / Product.Stock | List DTO has `amount`/`availableUnits` on Offer seam |
| Seller order slice | Detail lines only for that `SellerPartyId` |

## Routes

| UI | Host |
| --- | --- |
| `/vendor-panel` | `GET /v1/seller/dashboard` |
| `/vendor-panel/products` | `GET /v1/seller/offers` |
| `/vendor-panel/products/[offerId]` | `GET/PATCH /v1/seller/offers/{id}` |
| `/vendor-panel/orders` | `GET /v1/seller/orders` |
| `/vendor-panel/orders/[sellerOrderId]` | `GET /v1/seller/orders/{id}` |

## Deferred (carried)

- Payment missing `IdempotencyKey` → 500/NRE (P04)
- Cart replace-hold release→reserve window (P04)
- Seller header is Party-scoped seam; full SpiceDB session binding for seller users remains future hardening when Authorization Mode is enabled
