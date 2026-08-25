# TB-P05-T004 — Admin surface architecture map

```text
Admin UI (/admin)
  → Next rewrite /v1/*
  → Host Admin endpoints (/v1/admin/*)
  → AdminPanelAccess
  → authenticated user + current TenantId
  → IAuthorizationGuard (tenant#view)
  → AdminPanelComposer / ProductWorkspaceComposer
       → CatalogDbContext
       → OfferDbContext
       → PricingDbContext
       → InventoryDbContext
       → OrderDbContext
       → PartyDbContext / IPartyLookupGateway
```

## Invariants proven

| Rule | Evidence |
| --- | --- |
| Server-side Admin authorization | Every Admin endpoint calls `AdminPanelAccess`; frontend route visibility is not authority |
| Seller cannot become Admin | Seller actors have Party membership only; Admin requires the current Tenant `view` permission |
| Missing actor fails closed | Admin access returns an authentication error before composition |
| No direct DB from UI | UI calls only Host `/v1/admin/*` contracts |
| No cross-module SQL JOIN | Composers query each module DbContext independently and compose in memory |
| Catalog Product ≠ Seller Offer | Product rows compose offer count, price range, and availability without `Product.Price` or `Product.Stock` |
| Customer view is honest | Customer rows are checkout-derived aggregates, not an invented CRM |

## Routes

| Admin UI | Host API |
| --- | --- |
| `/admin` | `GET /v1/admin/dashboard` |
| `/admin/products` | `GET /v1/admin/products` |
| `/admin/products/[productId]` | `GET /v1/admin/products/{productId}` |
| `/admin/orders` | `GET /v1/admin/orders` |
| `/admin/orders/[checkoutId]` | `GET /v1/admin/orders/{checkoutId}` |
| `/admin/sellers` | `GET /v1/admin/sellers` |
| `/admin/customers` | `GET /v1/admin/customers` |

