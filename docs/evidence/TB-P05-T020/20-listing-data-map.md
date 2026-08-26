# 20 — Listing Data Map

| UI field | Source | Module |
| --- | --- | --- |
| products[] | `/v1/storefront/products` | Host composition over Catalog+Offer+Pricing+Inventory |
| totalCount / page / pageSize | same | Host |
| sort | `default\|newest\|price-asc\|price-desc` | Host |
| category filter | `categoryId` (+ descendants) | Catalog |
| inStock | boolean | Inventory via composed card |
| seller filter | `sellerPartyId` | Offer/Party display |
| brands sidebar links | `/v1/storefront/brands` → `/brand/[slug]` | Catalog brands |
| query `q` | title/category/seller text contains | Host listing filter |
| price display | offer / promotional amount | Pricing |
| rating | only if `reviewCount > 0` | Reviews |

No Product.Price / Product.Stock. No fake filters/sorts/counts.
